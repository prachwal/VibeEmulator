import itertools
import unittest

from generate_prefixed_word_fetch_profile import (
    PROFILE_COUNT,
    PROFILE_WIDTH,
    effective_address_parity,
    expected_fetch_starts,
    profile_index,
    profile_key,
    register_profile_index,
    register_profile_key,
    render_profile,
)


def vector(code, queue, regs=None, cycles=None):
    registers = {
        "ax": 0,
        "bx": 1,
        "cx": 0,
        "dx": 0,
        "cs": 0x1000,
        "ss": 0x2000,
        "ds": 0x3000,
        "es": 0x4000,
        "sp": 0,
        "bp": 0,
        "si": 2,
        "di": 4,
        "ip": 0,
        "flags": 0,
    }
    if regs:
        registers.update(regs)
    return {
        "bytes": code,
        "initial": {"queue": queue, "regs": registers},
        "cycles": cycles or [],
    }


class PrefixedWordFetchProfileTests(unittest.TestCase):
    def test_calculates_absolute_displacement_address_parity(self):
        item = vector([0x26, 0x81, 0x06, 0x33, 0x12], [0x26, 0x81, 0x06, 0x33, 0x12])

        self.assertEqual(1, effective_address_parity(item))

    def test_calculates_signed_byte_displacement_address_parity(self):
        item = vector([0x3E, 0x83, 0x40, 0xFF], [0x3E] * 5)

        self.assertEqual(0, effective_address_parity(item))

    def test_profile_key_separates_opcode_extension_queue_and_alignment(self):
        item = vector([0x36, 0x81, 0x06, 0x33, 0x12], [0x36] * 6)

        self.assertEqual((0x81, 0, 0, 6, 6, 1), profile_key(item))

    def test_profile_indices_are_unique_and_in_range(self):
        indices = {
            profile_index((opcode, extension, mod, rm, queue_length, parity))
            for opcode, extension, mod, rm, queue_length, parity in itertools.product(
                (0x81, 0x83), range(8), range(3), range(8), (5, 6), range(2)
            )
        }

        self.assertEqual(PROFILE_COUNT, len(indices))
        self.assertEqual(set(range(PROFILE_COUNT)), indices)

    def test_extracts_every_code_fetch_t1(self):
        item = vector(
            [0x26, 0x83, 0x00],
            [0x26] * 5,
            cycles=[
                [0] * 7 + ["CODE", "T1"],
                [0] * 7 + ["MEMR", "T1"],
                [0] * 7 + ["CODE", "T1"],
            ],
        )

        self.assertEqual((1, 3), expected_fetch_starts(item))

    def test_register_profile_key_excludes_memory_alignment(self):
        item = vector([0x2E, 0x83, 0xC7], [0x2E] * 5)

        self.assertEqual((0x83, 0, 7, 5), register_profile_key(item))

    def test_register_profile_indices_are_unique_and_in_range(self):
        indices = {
            register_profile_index((opcode, extension, rm, queue_length))
            for opcode, extension, rm, queue_length in itertools.product((0x81, 0x83), range(8), range(8), (5, 6))
        }

        self.assertEqual(256, len(indices))
        self.assertEqual(set(range(256)), indices)

    def test_renders_fixed_profile_size(self):
        rendered = render_profile([0] * (PROFILE_COUNT * PROFILE_WIDTH), [0] * 512)

        self.assertIn("  0", rendered)
        self.assertIn("PrefixedWordImmediateRegisterProfiles", rendered)
        self.assertGreater(len(rendered.splitlines()), 1)


if __name__ == "__main__":
    unittest.main()
