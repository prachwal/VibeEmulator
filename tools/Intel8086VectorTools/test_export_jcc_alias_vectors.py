import unittest

from export_jcc_alias_vectors import select_unprefixed_alias_records


class ExportJccAliasVectorsTests(unittest.TestCase):
    def test_selects_records_with_alias_opcode_as_first_instruction_byte(self):
        records = [
            {"bytes": [0x60, 0x01], "test_num": 1},
            {"bytes": [0x26, 0x60, 0x01], "test_num": 2},
            {"bytes": [0x61, 0x02], "test_num": 3},
            {"bytes": [], "test_num": 4},
        ]

        selected = select_unprefixed_alias_records(records, 0x60)

        self.assertEqual([1], [record["test_num"] for record in selected])

    def test_selects_all_matching_opcode_records(self):
        records = [{"bytes": [0x6F, 0x00]}, {"bytes": [0x6F, 0xFF]}, {"bytes": [0x70, 0x00]}]

        self.assertEqual(2, len(select_unprefixed_alias_records(records, 0x6F)))


if __name__ == "__main__":
    unittest.main()
