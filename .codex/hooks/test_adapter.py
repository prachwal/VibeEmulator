"""Hermetyczne testy adaptera; bez formatowania i budowania roboczego repo."""

import contextlib
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import copilot_adapter as adapter


class AdapterTests(unittest.TestCase):
    def invoke(self, mode, payload, output="", code=0):
        result = subprocess.CompletedProcess([], code, output, "")
        stream = io.StringIO()
        with patch.object(adapter.subprocess, "run", return_value=result) as call:
            with contextlib.redirect_stdout(stream):
                adapter.run(mode, payload)
        return stream.getvalue(), call

    def test_guard_denies_destructive_command(self):
        # Prawdziwy guard analizuje tekst; nie wykonuje polecenia.
        stream = io.StringIO()
        with contextlib.redirect_stdout(stream):
            adapter.run("guard", {"tool_input": {"command": "git reset --hard"}})
        decision = json.loads(stream.getvalue())["hookSpecificOutput"]
        self.assertEqual(decision["permissionDecision"], "deny")

    def test_guard_allows_build(self):
        stream = io.StringIO()
        with contextlib.redirect_stdout(stream):
            adapter.run("guard", {"tool_input": {"command": "dotnet build"}})
        self.assertEqual(stream.getvalue(), "")

    def test_ask_becomes_deny_with_reason(self):
        output, _ = self.invoke("guard", {}, json.dumps({
            "permissionDecision": "ask", "permissionDecisionReason": "Potwierdź",
        }))
        result = json.loads(output)["hookSpecificOutput"]
        self.assertEqual(result["permissionDecision"], "deny")
        self.assertIn("nie obsługuje", result["permissionDecisionReason"])

    def test_stop_preserves_block_and_loop_flag(self):
        block = {"decision": "block", "reason": "Błąd buildu"}
        output, call = self.invoke("verify", {"stop_hook_active": True}, json.dumps(block))
        self.assertEqual(json.loads(output), block)
        self.assertTrue(json.loads(call.call_args.kwargs["input"])["stop_hook_active"])

    def test_patch_paths_and_safety(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "src").mkdir()
            (root / "src/A.cs").write_text("class A {}")
            (root / "src/B.cs").symlink_to("/etc/passwd")
            payload = {"cwd": str(root / "src"), "tool_input": {"command": (
                "*** Begin Patch\n*** Update File: A.cs\n"
                "*** Update File: ../src/A.cs\n*** Move to: B.cs\n"
                "*** Add File: /etc/Outside.cs\n*** End Patch"
            )}}
            with patch.object(adapter, "ROOT", root):
                self.assertEqual(adapter.format_payload(payload), {
                    "files": [str(root / "src/A.cs")],
                })

    def test_non_edit_does_not_run_formatter(self):
        _, call = self.invoke("format", {"tool_input": {"command": "dotnet build"}})
        call.assert_not_called()

    def test_failed_guard_is_not_silently_allowed(self):
        with self.assertRaises(RuntimeError):
            self.invoke("guard", {}, code=1)

    def test_config_schema_and_timeouts(self):
        config = json.loads((adapter.ROOT / ".codex/hooks.json").read_text())
        self.assertEqual(set(config["hooks"]), {"PreToolUse", "PostToolUse", "Stop"})
        for groups in config["hooks"].values():
            for group in groups:
                hook = group["hooks"][0]
                self.assertIn("timeout", hook)
                self.assertNotIn("timeoutSec", hook)
                self.assertIn("copilot_adapter.py", hook["command"])


if __name__ == "__main__":
    unittest.main()
