import gzip
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from export_segment_prefixed_jcc_vectors import export, select_records


class ExportSegmentPrefixedJccVectorsTests(unittest.TestCase):
    def test_selects_records_by_exact_segment_and_alias_opcode(self):
        records = [
            {"test_num": 1, "bytes": [0x26, 0x69, 0x10]},
            {"test_num": 2, "bytes": [0x2E, 0x69, 0x10]},
            {"test_num": 3, "bytes": [0x26, 0x70, 0x10]},
        ]

        self.assertEqual([1], [record["test_num"] for record in select_records(records, 0x26, 0x69)])

    def test_exports_twenty_records_for_each_observed_pair(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "source" / "v1"
            source.mkdir(parents=True)
            (source / "metadata.json").write_text("{}", encoding="utf-8")
            for opcode in (0x69, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F):
                records = [
                    {"test_num": prefix * 100 + index, "bytes": [prefix, opcode, index], "cycles": [[], []]}
                    for prefix in (0x26, 0x2E, 0x36, 0x3E)
                    for index in range(20)
                ]
                with gzip.open(source / f"{opcode:02X}.json.gz", "wt", encoding="utf-8") as stream:
                    json.dump(records, stream)
            output = root / "out"

            with patch("export_segment_prefixed_jcc_vectors.subprocess.check_output", return_value="abc123\n"):
                manifest = export(source, "abc123", output)

            self.assertEqual(24, len(manifest["active"]))
            self.assertEqual(480, sum(entry["records"] for entry in manifest["active"]))
            for entry in manifest["active"]:
                self.assertEqual(20, len(entry["testNumbers"]))
                exported = json.loads((output / entry["file"]).read_text(encoding="utf-8"))
                self.assertEqual(entry["testNumbers"], [record["test_num"] for record in exported])

    def test_rejects_missing_records_in_a_required_pair(self):
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / "v1"
            source.mkdir()
            (source / "metadata.json").write_text("{}", encoding="utf-8")
            for opcode in (0x69, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F):
                with gzip.open(source / f"{opcode:02X}.json.gz", "wt", encoding="utf-8") as stream:
                    json.dump([], stream)
            with patch("export_segment_prefixed_jcc_vectors.subprocess.check_output", return_value="abc123\n"):
                with self.assertRaisesRegex(ValueError, "expected 20, found 0"):
                    export(source, "abc123", source.parent / "out")


if __name__ == "__main__":
    unittest.main()
