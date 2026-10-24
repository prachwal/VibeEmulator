import unittest

from export_segment_rep_string_vectors import select_records


class ExportSegmentRepStringVectorsTests(unittest.TestCase):
    def test_selects_exact_segment_repeat_opcode_sequence(self):
        records = [
            {"bytes": [0x26, 0xF3, 0xA4], "test_num": 1},
            {"bytes": [0x2E, 0xF3, 0xA4], "test_num": 2},
            {"bytes": [0x26, 0xF2, 0xA4], "test_num": 3},
            {"bytes": [0x26, 0xF3, 0xA5], "test_num": 4},
            {"bytes": [0x26, 0xF3], "test_num": 5},
        ]

        selected = select_records(records, 0x26, 0xF3, 0xA4)

        self.assertEqual([1], [record["test_num"] for record in selected])

    def test_preserves_source_order(self):
        records = [
            {"bytes": [0x3E, 0xF2, 0xAF], "test_num": 8},
            {"bytes": [0x3E, 0xF2, 0xAF, 0x00], "test_num": 11},
        ]

        selected = select_records(records, 0x3E, 0xF2, 0xAF)

        self.assertEqual([8, 11], [record["test_num"] for record in selected])


if __name__ == "__main__":
    unittest.main()
