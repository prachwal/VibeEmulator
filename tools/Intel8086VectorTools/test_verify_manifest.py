import json
import tempfile
import unittest
from pathlib import Path

from verify_manifest import verify_vectors


class VerifyManifestTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.vectors = Path(self.temporary_directory.name) / "Vectors"
        self.vectors.mkdir()
        self.metadata = {
            "opcodes": {
                "90": {"status": "normal"},
            },
        }
        self.inventory = {
            "source_sha": "a" * 40,
            "metadata_opcode_count": 1,
            "metadata_status_counts": {"normal": 1},
            "json_test_file_count": 1,
            "json_record_count": 1,
            "entries": [
                {
                    "source_file": "90.json.gz",
                    "opcode": "90",
                    "modrm_extension": None,
                    "status": "normal",
                    "source_records": 1,
                },
            ],
        }
        self.manifest = {
            "sourceSha": "a" * 40,
            "active": [
                {
                    "file": "90.json",
                    "records": 1,
                    "maxTStates": 2,
                },
            ],
        }
        self._write_json(self.vectors / "metadata.json", self.metadata)
        self._write_json(self.vectors / "source-inventory.json", self.inventory)
        self._write_json(self.vectors / "manifest.json", self.manifest)
        self._write_json(
            self.vectors / "90.json",
            [{"test_num": 0, "cycles": [[0, 0, "read"]]}],
        )

    def tearDown(self):
        self.temporary_directory.cleanup()

    def test_accepts_complete_inventory_and_active_sample(self):
        errors, summary = verify_vectors(self.vectors)

        self.assertEqual([], errors)
        self.assertEqual(1, summary["source_files"])
        self.assertEqual({"normal": 1}, summary["metadata_status_counts"])
        self.assertEqual(1, summary["active_manifest_records"])

    def test_detects_missing_inventory_entry(self):
        self.inventory["entries"] = []
        self._write_json(self.vectors / "source-inventory.json", self.inventory)

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("Liczba plików" in error for error in errors))
        self.assertTrue(any("Liczba rekordów" in error for error in errors))

    def test_detects_missing_active_sample(self):
        (self.vectors / "90.json").unlink()

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("brak aktywnej próbki 90.json" in error for error in errors))

    def test_detects_active_record_count_and_tstate_limit_mismatch(self):
        self.manifest["active"][0]["records"] = 2
        self.manifest["active"][0]["maxTStates"] = 1
        self._write_json(self.vectors / "manifest.json", self.manifest)

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("manifest deklaruje 2" in error for error in errors))

    def test_detects_tstate_limit_that_excludes_active_record(self):
        self.manifest["active"][0]["maxTStates"] = 1
        self._write_json(self.vectors / "manifest.json", self.manifest)

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("maxTStates" in error for error in errors))

    def test_detects_duplicate_inventory_entries(self):
        self.inventory["entries"].append(self.inventory["entries"][0])
        self.inventory["json_test_file_count"] = 2
        self.inventory["json_record_count"] = 2
        self._write_json(self.vectors / "source-inventory.json", self.inventory)

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("Duplikat pliku inwentarza" in error for error in errors))

    def test_detects_metadata_status_mismatch(self):
        self.inventory["entries"][0]["status"] = "alias"
        self._write_json(self.vectors / "source-inventory.json", self.inventory)

        errors, _ = verify_vectors(self.vectors)

        self.assertTrue(any("status inwentarza" in error for error in errors))

    def _write_json(self, path: Path, value):
        path.write_text(json.dumps(value), encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
