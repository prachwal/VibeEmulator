import unittest

from audit_prefix_sequences import scan_prefixed_opcodes, scan_records


class PrefixSequenceAuditTests(unittest.TestCase):
    def test_counts_distinct_leading_prefix_sequences(self):
        records = [
            {"bytes": [0x26, 0xF3, 0xA4]},
            {"bytes": [0x26, 0x26, 0xA4]},
            {"bytes": [0xF3, 0x26, 0xA4]},
            {"bytes": [0x90, 0x26]},
            {"bytes": [0xF0]},
        ]

        sequences = scan_records(records)

        self.assertEqual({(0x26, 0xF3): 1, (0x26, 0x26): 1, (0xF3, 0x26): 1, (0xF0,): 1}, sequences)

    def test_excludes_instruction_with_no_leading_prefix(self):
        self.assertEqual({}, scan_records([{"bytes": [0x90, 0xF3, 0xA4]}]))

    def test_counts_following_opcodes_by_prefix_sequence(self):
        records = [
            {"bytes": [0x26, 0xFF, 0x00]},
            {"bytes": [0x26, 0xFF, 0xC0]},
            {"bytes": [0x26, 0xF3, 0xA4]},
            {"bytes": [0x90]},
        ]

        self.assertEqual(
            {((0x26,), 0xFF): 2, ((0x26, 0xF3), 0xA4): 1},
            scan_prefixed_opcodes(records),
        )


if __name__ == "__main__":
    unittest.main()
