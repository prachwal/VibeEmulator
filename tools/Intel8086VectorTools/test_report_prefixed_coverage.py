import json
import tempfile
import unittest
from pathlib import Path

from report_prefixed_coverage import build_report


class ReportPrefixedCoverageTests(unittest.TestCase):
    def write_json(self, path: Path, value: object) -> Path:
        path.write_text(json.dumps(value), encoding="utf-8")
        return path

    def test_reconciles_every_source_pair_and_runner_result(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self.write_json(root / "manifest.json", {
                "sourceSha": "abc123",
                "active": [
                    {"prefixSequence": "26", "opcode": "90", "file": "26-90.json", "records": 3},
                    {"prefixSequence": "F3 26", "opcode": "A4", "file": "F3_26-A4.json", "records": 2},
                ],
            })
            run = self.write_json(root / "run.json", {
                "sourceSha": "abc123", "total": 5, "passed": 4, "skipped": 0, "mismatched": 1,
                "failures": ["F3_26-A4.json: test_num=1: IP mismatch"],
            })
            audit = self.write_json(root / "audit.json", {
                "source_sha": "abc123", "source_files": 2, "source_records": 10,
                "prefixed_opcodes": {"26 90": 3, "F3 26 A4": 2},
            })

            report = build_report(manifest, run, audit)

        self.assertEqual((2, 5, 4, 1), (
            report["prefix_opcode_pairs"], report["records"], report["passed"], report["mismatched"]
        ))
        self.assertEqual((1, 1), (report["full_pass_pairs"], report["known_gap_pairs"]))
        self.assertEqual("known-gap", report["groups"][1]["status"])

    def test_rejects_manifest_that_does_not_cover_source_pairs(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self.write_json(root / "manifest.json", {"sourceSha": "abc123", "active": []})
            run = self.write_json(root / "run.json", {
                "sourceSha": "abc123", "total": 0, "passed": 0, "skipped": 0, "mismatched": 0, "failures": []
            })
            audit = self.write_json(root / "audit.json", {
                "source_sha": "abc123", "source_files": 1, "source_records": 1,
                "prefixed_opcodes": {"26 90": 1},
            })

            with self.assertRaisesRegex(ValueError, "differ from the pinned source audit"):
                build_report(manifest, run, audit)

    def test_rejects_nonzero_runner_skips(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self.write_json(root / "manifest.json", {
                "sourceSha": "abc123", "active": [{"prefixSequence": "26", "opcode": "90", "file": "26-90.json", "records": 1}]
            })
            run = self.write_json(root / "run.json", {
                "sourceSha": "abc123", "total": 1, "passed": 1, "skipped": 1, "mismatched": 0, "failures": []
            })
            audit = self.write_json(root / "audit.json", {
                "source_sha": "abc123", "source_files": 1, "source_records": 1,
                "prefixed_opcodes": {"26 90": 1},
            })

            with self.assertRaisesRegex(ValueError, "skipped records"):
                build_report(manifest, run, audit)


if __name__ == "__main__":
    unittest.main()
