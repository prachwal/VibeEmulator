import gzip
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from export_prefixed_vectors import export


class ExportPrefixedVectorsTests(unittest.TestCase):
    def test_exports_all_leading_prefix_opcode_groups_with_tstate_limits(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "source" / "v1"
            source.mkdir(parents=True)
            (source / "metadata.json").write_text("{}", encoding="utf-8")
            records = [
                {"test_num": 1, "bytes": [0x26, 0x90], "cycles": [[], []]},
                {"test_num": 2, "bytes": [0xF3, 0x26, 0xA4], "cycles": [[], [], [], []]},
                {"test_num": 3, "bytes": [0x90], "cycles": [[]]},
                {"test_num": 4, "bytes": [0x26], "cycles": [[]]},
            ]
            with gzip.open(source / "90.json.gz", "wt", encoding="utf-8") as stream:
                json.dump(records, stream)
            output = root / "out"

            with patch("export_prefixed_vectors.subprocess.check_output", return_value="abc123\n"):
                manifest = export(source, output, "abc123")

            self.assertEqual(2, manifest["sourceRecords"])
            self.assertEqual(2, len(manifest["active"]))
            by_sequence = {entry["prefixSequence"]: entry for entry in manifest["active"]}
            self.assertEqual({"26": (1, 3), "F3 26": (1, 5)}, {
                sequence: (entry["records"], entry["maxTStates"])
                for sequence, entry in by_sequence.items()
            })
            for entry in manifest["active"]:
                exported = json.loads((output / entry["file"]).read_text(encoding="utf-8"))
                self.assertEqual(1, len(exported))
                self.assertEqual(entry["records"], len(exported))

    def test_rejects_a_different_source_pin(self):
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / "v1"
            source.mkdir()
            (source / "metadata.json").write_text("{}", encoding="utf-8")
            with patch("export_prefixed_vectors.subprocess.check_output", return_value="wrong\n"):
                with self.assertRaisesRegex(ValueError, "SHA mismatch"):
                    export(source, source.parent / "out", "expected")


if __name__ == "__main__":
    unittest.main()
