import unittest

from export_segment_prefixed_mov_vectors import select_records


class ExportSegmentPrefixedMovVectorsTests(unittest.TestCase):
    def test_selects_only_register_direct_mov_with_requested_segment_prefix(self):
        records = [
            {"bytes": [0x26, 0x89, 0xC3], "test_num": 1},
            {"bytes": [0x2E, 0x89, 0xC3], "test_num": 2},
            {"bytes": [0x26, 0x89, 0x06, 0x00, 0x10], "test_num": 3},
            {"bytes": [0x26, 0x88, 0xC3], "test_num": 4},
        ]

        selected = select_records(records, 0x26, 0x89)

        self.assertEqual([1], [record["test_num"] for record in selected])

    def test_preserves_source_order(self):
        records = [
            {"bytes": [0x3E, 0x8B, 0xC3], "test_num": 8},
            {"bytes": [0x3E, 0x8B, 0xD4], "test_num": 11},
            {"bytes": [0x3E, 0x8B, 0x00], "test_num": 12},
        ]

        selected = select_records(records, 0x3E, 0x8B)

        self.assertEqual([8, 11], [record["test_num"] for record in selected])


if __name__ == "__main__":
    unittest.main()
