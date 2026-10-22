#!/usr/bin/env python3
"""Dostosowanie kontraktu hooków Codex do wspólnych skryptów Copilota."""

import json
from pathlib import Path
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = {
    "guard": "guard-dangerous-commands.sh",
    "format": "format-csharp.sh",
    "verify": "verify-build.sh",
}


def patch_paths(command):
    """apply_patch przekazuje ścieżki wewnątrz tekstu łatki."""
    prefixes = ("*** Add File: ", "*** Update File: ", "*** Move to: ")
    for line in command.splitlines():
        for prefix in prefixes:
            if line.startswith(prefix):
                yield line[len(prefix):]


def format_payload(payload):
    """Przekaż formatterowi wyłącznie istniejące pliki wewnątrz repo."""
    paths = []

    def collect(value):
        if isinstance(value, dict):
            for item in value.values():
                collect(item)
        elif isinstance(value, list):
            for item in value:
                collect(item)
        elif isinstance(value, str):
            if value.endswith(".cs") and "\n" not in value:
                paths.append(value)
            paths.extend(patch_paths(value))

    collect(payload.get("tool_input", {}))
    cwd = Path(payload.get("cwd") or ROOT)
    if not cwd.is_absolute():
        cwd = ROOT / cwd
    files = set()
    for name in paths:
        path = Path(name.removeprefix("file://").replace("%20", " "))
        path = (cwd / path).resolve()
        if path.suffix == ".cs" and path.is_relative_to(ROOT) and path.is_file():
            files.add(str(path))
    return {"files": sorted(files)}


def run(mode, payload):
    if mode == "format":
        payload = format_payload(payload)
        if not payload["files"]:
            return
    else:
        payload = dict(payload, cwd=str(ROOT))
    result = subprocess.run(
        ["bash", str(ROOT / ".github/hooks" / SCRIPTS[mode])],
        input=json.dumps(payload), text=True, capture_output=True, cwd=ROOT,
        timeout={"guard": 10, "format": 60, "verify": 160}[mode],
        check=False,
    )
    if result.stderr:
        print(result.stderr, file=sys.stderr, end="")
    if result.returncode:
        raise RuntimeError(f"{SCRIPTS[mode]}: kod wyjścia {result.returncode}")
    if not result.stdout.strip():
        return
    output = json.loads(result.stdout)
    if mode == "guard":
        specific = output.get("hookSpecificOutput", output)
        decision = specific.get("permissionDecision")
        if decision in ("deny", "ask"):
            reason = specific.get("permissionDecisionReason", "Blokada hooka projektu")
            if decision == "ask":
                reason += (
                    ". Codex nie obsługuje decyzji ask z hooka; operacja została "
                    "zablokowana. Wymaga osobnej decyzji użytkownika."
                )
            output = {"hookSpecificOutput": {
                "hookEventName": "PreToolUse", "permissionDecision": "deny",
                "permissionDecisionReason": reason,
            }}
        else:
            return
    print(json.dumps(output, ensure_ascii=False))


def main():
    mode = sys.argv[1]
    try:
        payload = json.load(sys.stdin)
        if not isinstance(payload, dict):
            return 0
    except (ValueError, TypeError):
        return 0
    try:
        run(mode, payload)
    except (OSError, ValueError, RuntimeError, subprocess.TimeoutExpired) as error:
        print(f"Hook Codex ({mode}): {error}", file=sys.stderr)
        return 2 if mode == "guard" else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
