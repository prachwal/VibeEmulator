#!/usr/bin/env python3
"""Generate the prefixed 81/83 queue-fetch profile from pinned 8086 vectors."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess
import sys
from collections import defaultdict
from typing import Any

SOURCE_SHA = "e71c68d215a6bb8c356bd4cb3842de3bef345ca9"
SEGMENT_PREFIXES = {0x26, 0x2E, 0x36, 0x3E}
OPCODES = (0x81, 0x83)
QUEUE_LENGTHS = (5, 6)
PROFILE_COUNT = 2 * 8 * 3 * 8 * 2 * 2
PROFILE_WIDTH = 6
REGISTER_PROFILE_COUNT = 2 * 8 * 8 * 2
REGISTER_PROFILE_WIDTH = 2


def effective_address_parity(vector: dict[str, Any]) -> int:
    """Return physical EA parity for the vector's 8086 ModR/M address."""
    code = vector["bytes"]
    mod_rm = code[2]
    mod = mod_rm >> 6
    rm = mod_rm & 7
    regs = vector["initial"]["regs"]
    bases = (
        regs["bx"] + regs["si"],
        regs["bx"] + regs["di"],
        regs["bp"] + regs["si"],
        regs["bp"] + regs["di"],
        regs["si"],
        regs["di"],
        0 if mod == 0 else regs["bp"],
        regs["bx"],
    )

    displacement_index = 3
    if mod == 0 and rm == 6:
        displacement = code[displacement_index] | (code[displacement_index + 1] << 8)
    elif mod == 1:
        value = code[displacement_index]
        displacement = value if value < 0x80 else value - 0x100
    elif mod == 2:
        value = code[displacement_index] | (code[displacement_index + 1] << 8)
        displacement = value if value < 0x8000 else value - 0x10000
    else:
        displacement = 0

    offset = (bases[rm] + displacement) & 0xFFFF
    segment = {
        0x26: regs["es"],
        0x2E: regs["cs"],
        0x36: regs["ss"],
        0x3E: regs["ds"],
    }[code[0]]
    return ((segment << 4) + offset) & 1


def profile_key(vector: dict[str, Any]) -> tuple[int, int, int, int, int, int]:
    code = vector["bytes"]
    opcode = code[1]
    mod_rm = code[2]
    mod = mod_rm >> 6
    rm = mod_rm & 7
    queue_length = len(vector["initial"]["queue"])
    if queue_length not in QUEUE_LENGTHS:
        raise ValueError(f"unsupported initial queue length {queue_length}")
    return (
        opcode,
        (mod_rm >> 3) & 7,
        mod,
        rm,
        queue_length,
        effective_address_parity(vector),
    )


def expected_fetch_starts(vector: dict[str, Any]) -> tuple[int, ...]:
    return tuple(
        cycle_number
        for cycle_number, cycle in enumerate(vector["cycles"], 1)
        if cycle[7] == "CODE" and cycle[8] == "T1"
    )


def profile_index(key: tuple[int, int, int, int, int, int]) -> int:
    opcode, extension, mod, rm, queue_length, parity = key
    opcode_index = 0 if opcode == 0x81 else 1
    return (((((opcode_index * 8 + extension) * 3 + mod) * 8 + rm) * 2 + (queue_length - 5)) * 2) + parity


def register_profile_key(vector: dict[str, Any]) -> tuple[int, int, int, int]:
    code = vector["bytes"]
    mod_rm = code[2]
    queue_length = len(vector["initial"]["queue"])
    if mod_rm >> 6 != 3 or queue_length not in QUEUE_LENGTHS:
        raise ValueError("register profile requires ModR/M mod=3 and a queue length of 5 or 6")
    return code[1], (mod_rm >> 3) & 7, mod_rm & 7, queue_length


def register_profile_index(key: tuple[int, int, int, int]) -> int:
    opcode, extension, rm, queue_length = key
    opcode_index = 0 if opcode == 0x81 else 1
    return (((opcode_index * 8 + extension) * 8 + rm) * 2) + (queue_length - 5)


def build_profile(source: pathlib.Path) -> list[int]:
    groups: dict[tuple[int, int, int, int, int, int], set[tuple[tuple[int, ...], tuple[int, ...], int]]] = defaultdict(set)
    record_count = 0
    for opcode in OPCODES:
        for extension in range(8):
            path = source / f"{opcode:02X}.{extension}.json.gz"
            with gzip.open(path, "rt", encoding="utf-8") as stream:
                vectors = json.load(stream)
            for vector in vectors:
                code = vector["bytes"]
                if len(code) < 3 or code[0] not in SEGMENT_PREFIXES or code[1] != opcode or code[2] >> 6 == 3:
                    continue
                key = profile_key(vector)
                starts = expected_fetch_starts(vector)
                expected_first = 3 if key[4] == 5 else 5
                if not starts or starts[0] != expected_first:
                    raise ValueError(f"{path.name}: expected first CODE/T1 at {expected_first}, got {starts}")
                writes = tuple(
                    cycle_number
                    for cycle_number, cycle in enumerate(vector["cycles"], 1)
                    if cycle[7] == "MEMW" and cycle[8] == "T1"
                )
                if len(writes) > 2 or len(starts) > 4:
                    raise ValueError(f"{path.name}: unsupported bus schedule: fetches={starts}, writes={writes}")
                if len(writes) == 2 and writes[1] - writes[0] != 4:
                    raise ValueError(f"{path.name}: split word writes are not four T-states apart: {writes}")
                if ((key[1] == 7) != (len(writes) == 0)):
                    raise ValueError(f"{path.name}: unexpected write count for extension /{key[1]}: {writes}")
                groups[key].add((starts[1:], writes, len(vector["cycles"])))
                record_count += 1

    profile = [0] * (PROFILE_COUNT * PROFILE_WIDTH)
    for key, schedules in groups.items():
        if len(schedules) != 1:
            raise ValueError(f"non-deterministic fetch profile {key}: {sorted(schedules)}")
        starts, writes, cycle_count = next(iter(schedules))
        values = (*starts, *([0] * (3 - len(starts))), *writes, *([0] * (2 - len(writes))), cycle_count)
        if any(value > 255 for value in values):
            raise ValueError(f"unsupported fetch/write schedule {key}: {values}")
        start_index = profile_index(key) * PROFILE_WIDTH
        profile[start_index : start_index + PROFILE_WIDTH] = values

    if record_count != 11_870:
        raise ValueError(f"expected 11870 filtered records, found {record_count}")
    if len(groups) != 1_535:
        raise ValueError(f"expected 1535 distinct profiles, found {len(groups)}")
    return profile


def build_register_profile(source: pathlib.Path) -> list[int]:
    groups: dict[tuple[int, int, int, int], set[tuple[tuple[int, ...], int]]] = defaultdict(set)
    record_count = 0
    for opcode in OPCODES:
        for extension in range(8):
            path = source / f"{opcode:02X}.{extension}.json.gz"
            with gzip.open(path, "rt", encoding="utf-8") as stream:
                vectors = json.load(stream)
            for vector in vectors:
                code = vector["bytes"]
                if len(code) < 3 or code[0] not in SEGMENT_PREFIXES or code[1] != opcode or code[2] >> 6 != 3:
                    continue
                key = register_profile_key(vector)
                starts = expected_fetch_starts(vector)
                expected_first = 3 if key[3] == 5 else 5
                if not starts or starts[0] != expected_first:
                    raise ValueError(f"{path.name}: expected first register CODE/T1 at {expected_first}, got {starts}")
                if len(starts) > 2 or len(vector["cycles"]) not in (6, 7):
                    raise ValueError(f"{path.name}: unsupported register schedule: fetches={starts}, cycles={len(vector['cycles'])}")
                if len(starts) == 2 and (key[3] != 5 or starts[1] != 7 or len(vector["cycles"]) != 7):
                    raise ValueError(f"{path.name}: unexpected second register fetch: {starts}")
                groups[key].add((starts, len(vector["cycles"])))
                record_count += 1

    profile = [0] * (REGISTER_PROFILE_COUNT * REGISTER_PROFILE_WIDTH)
    for key, schedules in groups.items():
        if len(schedules) != 1:
            raise ValueError(f"non-deterministic register profile {key}: {sorted(schedules)}")
        starts, cycle_count = next(iter(schedules))
        second_fetch_start = starts[1] if len(starts) == 2 else 0
        start_index = register_profile_index(key) * REGISTER_PROFILE_WIDTH
        profile[start_index : start_index + REGISTER_PROFILE_WIDTH] = [second_fetch_start, cycle_count]

    if record_count != 4_075:
        raise ValueError(f"expected 4075 prefixed register records, found {record_count}")
    if len(groups) != REGISTER_PROFILE_COUNT:
        raise ValueError(f"expected {REGISTER_PROFILE_COUNT} distinct register profiles, found {len(groups)}")
    return profile


def render_profile(profile: list[int], register_profile: list[int] | None = None) -> str:
    if len(profile) != PROFILE_COUNT * PROFILE_WIDTH:
        raise ValueError(f"expected {PROFILE_COUNT * PROFILE_WIDTH} profile values, found {len(profile)}")
    values = [f"{value:3d}" for value in profile]
    rows = [", ".join(values[index : index + 48]) for index in range(0, len(values), 48)]
    initializer = ",\n".join(f"        {row}" for row in rows)
    register_table = ""
    register_helpers = ""
    if register_profile is not None:
        if len(register_profile) != REGISTER_PROFILE_COUNT * REGISTER_PROFILE_WIDTH:
            raise ValueError(f"expected {REGISTER_PROFILE_COUNT * REGISTER_PROFILE_WIDTH} register profile values, found {len(register_profile)}")
        register_values = [f"{value:3d}" for value in register_profile]
        register_rows = [", ".join(register_values[index : index + 32]) for index in range(0, len(register_values), 32)]
        register_initializer = ",\n".join(f"        {row}" for row in register_rows)
        register_table = f'''    // Register-direct segment-prefixed 81/83 profile: second CODE/T1 start, then total T-states.
    private static readonly byte[] PrefixedWordImmediateRegisterProfiles =
    [
{register_initializer}
    ];

'''
        register_helpers = f'''
    private int GetPrefixedWordImmediateRegisterSecondFetchStart()
    {{
        int opcodeIndex = Opcode == 0x81 ? 0 : 1;
        int extension = (_memoryModRm >> 3) & 7;
        int rm = _memoryModRm & 7;
        int queueIndex = _queueCountAtStart >= 6 ? 1 : 0;
        int profileIndex = (((opcodeIndex * 8 + extension) * 8 + rm) * 2) + queueIndex;
        return PrefixedWordImmediateRegisterProfiles[(profileIndex * {REGISTER_PROFILE_WIDTH})];
    }}

    private int GetPrefixedWordImmediateRegisterCycleCount()
    {{
        int opcodeIndex = Opcode == 0x81 ? 0 : 1;
        int extension = (_memoryModRm >> 3) & 7;
        int rm = _memoryModRm & 7;
        int queueIndex = _queueCountAtStart >= 6 ? 1 : 0;
        int profileIndex = (((opcodeIndex * 8 + extension) * 8 + rm) * 2) + queueIndex;
        return PrefixedWordImmediateRegisterProfiles[(profileIndex * {REGISTER_PROFILE_WIDTH}) + 1];
    }}
'''

    return f'''namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{{
    // CODE/T1 fetch schedule for segment-prefixed 81/83 memory forms from SST 8086 {SOURCE_SHA}.
    // Profile key: opcode, ModR/M extension, mod, r/m, queue length 5/6, effective-address parity.
    // Each profile stores three later CODE/T1 starts, two MEMW/T1 starts, then total instruction T-states.
    private static readonly byte[] PrefixedWordImmediateFetchStarts =
    [
{initializer}
    ];

{register_table}
    private int GetPrefixedWordImmediateFetchStart(int slot)
    {{
        int opcodeIndex = Opcode == 0x81 ? 0 : 1;
        int extension = (_memoryModRm >> 3) & 7;
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int queueIndex = _queueCountAtStart >= 6 ? 1 : 0;
        int addressParity = (int)(_memoryAddress & 1);
        int profileIndex = (((((opcodeIndex * 8 + extension) * 3 + mod) * 8 + rm) * 2 + queueIndex) * 2) + addressParity;
        return PrefixedWordImmediateFetchStarts[(profileIndex * {PROFILE_WIDTH}) + slot];
    }}

    private int GetPrefixedWordImmediateWriteStart(int slot)
    {{
        int profileIndex = GetPrefixedWordImmediateProfileIndex();
        return PrefixedWordImmediateFetchStarts[(profileIndex * {PROFILE_WIDTH}) + 3 + slot];
    }}

    private int GetPrefixedWordImmediateCycleCount()
    {{
        int profileIndex = GetPrefixedWordImmediateProfileIndex();
        return PrefixedWordImmediateFetchStarts[(profileIndex * {PROFILE_WIDTH}) + 5];
    }}

    private int GetPrefixedWordImmediateProfileIndex()
    {{
        int opcodeIndex = Opcode == 0x81 ? 0 : 1;
        int extension = (_memoryModRm >> 3) & 7;
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int queueIndex = _queueCountAtStart >= 6 ? 1 : 0;
        int addressParity = (int)(_memoryAddress & 1);
        return (((((opcodeIndex * 8 + extension) * 3 + mod) * 8 + rm) * 2 + queueIndex) * 2) + addressParity;
    }}
{register_helpers}
}}
'''


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=pathlib.Path, required=True, help="Pinned checkout's v1 directory")
    parser.add_argument("--output", type=pathlib.Path, required=True, help="Generated C# array initializer file")
    args = parser.parse_args()
    source = args.source.resolve()
    actual_sha = subprocess.check_output(["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True).strip()
    if actual_sha.lower() != SOURCE_SHA:
        raise ValueError(f"expected source SHA {SOURCE_SHA}, found {actual_sha}")
    profile = build_profile(source)
    register_profile = build_register_profile(source)
    args.output.write_text(render_profile(profile, register_profile) + "\n", encoding="utf-8")
    print(f"generated {len(profile) + len(register_profile)} values from {SOURCE_SHA}: 11870 memory vectors/1535 profiles, 4075 register vectors/256 profiles")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError, subprocess.CalledProcessError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(1)
