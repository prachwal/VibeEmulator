import json
import tempfile
import unittest
from pathlib import Path

from audit_isa_matrix import build_matrix


class IsaMatrixTests(unittest.TestCase):
    def write_json(self, path: Path, value: object) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(value), encoding="utf-8")

    def test_maps_source_forms_to_active_manifest_samples_conservatively(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary)
            self.write_json(
                vectors / "source-inventory.json",
                {
                    "source_sha": "abc123",
                    "entries": [
                        {"source_file": "80.0.json.gz", "opcode": "80", "modrm_extension": "0", "implementation_stage": "A6", "sample_records": 3},
                        {"source_file": "D8.json.gz", "opcode": "D8", "modrm_extension": None, "implementation_stage": None},
                        {"source_file": "60.json.gz", "opcode": "60", "modrm_extension": None, "implementation_stage": None},
                    ],
                },
            )
            self.write_json(
                vectors / "metadata.json",
                {
                    "opcodes": {
                        "80": {"reg": {"0": {"status": "normal"}}},
                        "D8": {"status": "fpu"},
                        "60": {"status": "alias"},
                    }
                },
            )
            self.write_json(
                vectors / "manifest.json",
                {
                    "sourceSha": "abc123",
                    "active": [
                        {"file": "80.0.json", "opcode": "80", "modrmExtension": 0, "records": 3},
                        {"file": "D8.json", "records": 1},
                        {"file": "unmapped.json", "records": 2},
                    ],
                },
            )

            result = build_matrix(vectors)

        self.assertEqual(3, result["source_files"])
        self.assertEqual(6, result["active_manifest_records"])
        self.assertEqual(2, result["mapped_source_forms"])
        self.assertEqual(1, len(result["unmapped_active_manifest_entries"]))
        rows = {row["source_file"]: row for row in result["forms"]}
        self.assertEqual("sampled", rows["80.0.json.gz"]["disposition"])
        self.assertEqual("fpu-review", rows["D8.json.gz"]["disposition"])
        self.assertEqual("alias-review", rows["60.json.gz"]["disposition"])
        self.assertIn("never proves complete source-vector", result["interpretation"])

    def test_rejects_manifest_from_a_different_pin(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary)
            self.write_json(vectors / "source-inventory.json", {"source_sha": "expected", "entries": []})
            self.write_json(vectors / "metadata.json", {"opcodes": {}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "other", "active": []})

            with self.assertRaisesRegex(ValueError, "manifest SHA"):
                build_matrix(vectors)

    def test_rejects_duplicate_source_forms(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary)
            self.write_json(
                vectors / "source-inventory.json",
                {
                    "source_sha": "abc123",
                    "entries": [
                        {"source_file": "90.json.gz", "opcode": "90", "modrm_extension": None},
                        {"source_file": "90-copy.json.gz", "opcode": "90", "modrm_extension": None},
                    ],
                },
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"90": {"status": "normal"}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})

            with self.assertRaisesRegex(ValueError, "Duplicate source form"):
                build_matrix(vectors)

    def test_attaches_per_form_results_from_matching_source_sample_report(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(
                vectors / "source-inventory.json",
                {
                    "source_sha": "abc123",
                    "entries": [
                        {"source_file": "90.json.gz", "opcode": "90", "modrm_extension": None, "status": "normal", "sample_records": 2}
                    ],
                },
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"90": {"status": "normal"}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            report = Path(temporary) / "sample-report.json"
            self.write_json(
                report,
                {
                    "sourceSha": "abc123",
                    "total": 2,
                    "passed": 1,
                    "skipped": 0,
                    "mismatched": 1,
                    "failures": ["90.json: test_num=1: register AX mismatch"],
                },
            )

            result = build_matrix(vectors, report)

        row = result["forms"][0]
        self.assertEqual({"records": 2, "passed": 1, "mismatched": 1, "status": "partial"}, row["source_first_sample"])
        self.assertEqual({"partial": 1}, result["source_first_sample_status_counts"])
        self.assertEqual("known-gap", row["disposition"])

    def test_marks_jcc_aliases_implemented_only_with_complete_pinned_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            inventory_entries = []
            metadata = {"opcodes": {}}
            active = []
            forms = []
            for opcode in range(0x60, 0x70):
                key = f"{opcode:02X}"
                inventory_entries.append({"source_file": f"{key}.json.gz", "opcode": key, "modrm_extension": None, "status": "alias"})
                metadata["opcodes"][key] = {"status": "alias"}
                active.append({"file": f"A10Alias/{key}.json", "opcode": key, "records": 20})
                forms.append({"opcode": key, "records": 2000})
            self.write_json(vectors / "source-inventory.json", {"source_sha": "abc123", "entries": inventory_entries})
            self.write_json(vectors / "metadata.json", metadata)
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": active})
            evidence = Path(temporary) / "alias-report.json"
            self.write_json(
                evidence,
                {
                    "sourceSha": "abc123",
                    "scope": "bytes[0] == opcode",
                    "forms": forms,
                    "total": 32000,
                    "passed": 32000,
                    "skipped": 0,
                    "mismatched": 0,
                },
            )

            result = build_matrix(vectors, alias_jcc_report=evidence)

        self.assertEqual({"implemented-alias": 16}, result["disposition_counts"])
        self.assertEqual(32000, result["alias_jcc_evidence"]["passed"])

    def test_rejects_jcc_alias_evidence_with_a_missing_opcode(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(vectors / "source-inventory.json", {"source_sha": "abc123", "entries": []})
            self.write_json(vectors / "metadata.json", {"opcodes": {}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            evidence = Path(temporary) / "alias-report.json"
            self.write_json(evidence, {"sourceSha": "abc123", "forms": [], "total": 0, "passed": 0, "skipped": 0, "mismatched": 0})

            with self.assertRaisesRegex(ValueError, "every opcode 60h–6Fh"):
                build_matrix(vectors, alias_jcc_report=evidence)

    def test_marks_return_aliases_implemented_with_full_vector_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            aliases = ["C0", "C1", "C8", "C9"]
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": f"{opcode}.json.gz", "opcode": opcode, "modrm_extension": None} for opcode in aliases]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {opcode: {"status": "alias"} for opcode in aliases}})
            self.write_json(
                vectors / "manifest.json",
                {"sourceSha": "abc123", "active": [{"file": f"A10Alias/{opcode}.json", "opcode": opcode, "records": 20} for opcode in aliases]},
            )
            evidence = Path(temporary) / "return-alias-report.json"
            forms = [{"opcode": opcode, "records": 2000} for opcode in aliases]
            self.write_json(
                evidence,
                {"sourceSha": "abc123", "scope": "bytes[0] == opcode", "forms": forms, "total": 8000, "passed": 8000, "skipped": 0, "mismatched": 0},
            )

            result = build_matrix(vectors, alias_return_report=evidence)

        self.assertEqual({"implemented-alias": 4}, result["disposition_counts"])
        self.assertEqual(8000, result["alias_return_evidence"]["passed"])

    def test_requires_an_explicit_decision_for_every_remaining_alias(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": "82.0.json.gz", "opcode": "82", "modrm_extension": 0}]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"82": {"reg": {"0": {"status": "alias"}}}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            decisions = Path(temporary) / "alias-decisions.json"
            self.write_json(
                decisions,
                {"sourceSha": "abc123", "decisions": [{"opcode": "82", "modrm_extension": 0, "disposition": "known-gap", "reason": "80 /0 baseline missing"}]},
            )

            result = build_matrix(vectors, alias_decisions_path=decisions)

        self.assertEqual({"known-gap": 1}, result["disposition_counts"])

    def test_rejects_unresolved_metadata_aliases_when_decisions_are_supplied(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": "82.0.json.gz", "opcode": "82", "modrm_extension": 0}]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"82": {"reg": {"0": {"status": "alias"}}}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            decisions = Path(temporary) / "alias-decisions.json"
            self.write_json(decisions, {"sourceSha": "abc123", "decisions": []})

            with self.assertRaisesRegex(ValueError, "Unresolved metadata aliases"):
                build_matrix(vectors, alias_decisions_path=decisions)

    def test_classifies_ff_push_alias_only_with_complete_partitioned_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": "FF.7.json.gz", "opcode": "FF", "modrm_extension": "7", "source_records": 2000}]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"FF": {"reg": {"7": {"status": "alias"}}}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            report = Path(temporary) / "ff7-report.json"
            self.write_json(
                report,
                {
                    "sourceSha": "abc123",
                    "sourceFile": "FF.7.json.gz",
                    "total": 2000,
                    "passed": 2000,
                    "skipped": 0,
                    "mismatched": 0,
                    "timeouts": 0,
                    "parseErrors": 0,
                    "scopes": [
                        {"scope": "unprefixed", "records": 996, "passed": 996, "skipped": 0, "mismatched": 0, "timeouts": 0, "parseErrors": 0},
                        {"scope": "segment-prefixed", "records": 1004, "passed": 1004, "skipped": 0, "mismatched": 0, "timeouts": 0, "parseErrors": 0},
                    ],
                },
            )

            result = build_matrix(vectors, alias_ff_push_report_path=report)

        self.assertEqual({"implemented-alias": 1}, result["disposition_counts"])
        self.assertEqual(2000, result["alias_ff_push_evidence"]["passed"])

    def test_rejects_incomplete_ff_push_alias_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": "FF.7.json.gz", "opcode": "FF", "modrm_extension": "7", "source_records": 2000}]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {"FF": {"reg": {"7": {"status": "alias"}}}}})
            self.write_json(vectors / "manifest.json", {"sourceSha": "abc123", "active": []})
            report = Path(temporary) / "ff7-report.json"
            self.write_json(
                report,
                {
                    "sourceSha": "abc123",
                    "sourceFile": "FF.7.json.gz",
                    "total": 2000,
                    "passed": 2000,
                    "skipped": 0,
                    "mismatched": 0,
                    "timeouts": 0,
                    "parseErrors": 0,
                    "scopes": [
                        {"scope": "unprefixed", "records": 996, "passed": 996, "skipped": 0, "mismatched": 0, "timeouts": 0, "parseErrors": 0},
                        {"scope": "segment-prefixed", "records": 1003, "passed": 1003, "skipped": 0, "mismatched": 0, "timeouts": 0, "parseErrors": 0},
                    ],
                },
            )

            with self.assertRaisesRegex(ValueError, "incomplete or duplicate scope counts"):
                build_matrix(vectors, alias_ff_push_report_path=report)

    def test_classifies_esc_forms_only_with_full_vector_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            vectors = Path(temporary) / "Vectors"
            opcodes = [f"{opcode:02X}" for opcode in range(0xD8, 0xE0)]
            self.write_json(
                vectors / "source-inventory.json",
                {"source_sha": "abc123", "entries": [{"source_file": f"{opcode}.json.gz", "opcode": opcode, "modrm_extension": None} for opcode in opcodes]},
            )
            self.write_json(vectors / "metadata.json", {"opcodes": {opcode: {"status": "fpu"} for opcode in opcodes}})
            self.write_json(
                vectors / "manifest.json",
                {"sourceSha": "abc123", "active": [{"file": f"A8/{opcode}.json", "opcode": opcode, "records": 20} for opcode in opcodes]},
            )
            report = Path(temporary) / "esc-report.json"
            forms = [{"opcode": opcode, "records": 10} for opcode in opcodes]
            self.write_json(
                report,
                {"sourceSha": "abc123", "scope": "opcode at byte zero", "forms": forms, "total": 80, "passed": 80, "skipped": 0, "mismatched": 0},
            )

            result = build_matrix(vectors, esc_report=report)

        self.assertEqual({"implemented-escape": 8}, result["disposition_counts"])
        self.assertEqual(80, result["esc_evidence"]["passed"])


if __name__ == "__main__":
    unittest.main()
