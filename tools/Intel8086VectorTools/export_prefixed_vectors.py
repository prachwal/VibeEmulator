#!/usr/bin/env python3
"""Export every leading-prefix record from the pinned SingleStepTests 8086 source."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess
import sys
from typing import Any, TextIO

PREFIXES = {0x26, 0x2E, 0x36, 0x3E, 0xF0, 0xF2, 0xF3}


def leading_prefix_count(data: list[int]) -> int:
    count = 0
    while count < len(data) and data[count] in PREFIXES:
        count += 1
    return count


def append_record(stream: TextIO, record: dict[str, Any], first: bool) -> None:
    if not first:
        stream.write(",\n")
    json.dump(record, stream, separators=(",", ":"))


def export(source: pathlib.Path, output: pathlib.Path, expected_sha: str) -> dict[str, Any]:
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        raise ValueError("Source must be the pinned checkout's v1 directory.")
    actual_sha = subprocess.check_output(["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True).strip()
    if actual_sha.lower() != expected_sha.lower():
        raise ValueError(f"Source SHA mismatch: expected {expected_sha}, found {actual_sha}.")

    output.mkdir(parents=True, exist_ok=True)
    streams: dict[tuple[tuple[int, ...], int], TextIO] = {}
    first_record: dict[tuple[tuple[int, ...], int], bool] = {}
    counts: dict[tuple[tuple[int, ...], int], int] = {}
    max_tstates: dict[tuple[tuple[int, ...], int], int] = {}
    try:
        for source_file in sorted(source.glob("*.json.gz")):
            with gzip.open(source_file, "rt", encoding="utf-8") as source_stream:
                records = json.load(source_stream)
            for record in records:
                data = record.get("bytes", [])
                prefix_count = leading_prefix_count(data)
                if prefix_count == 0 or prefix_count == len(data):
                    continue
                key = (tuple(data[:prefix_count]), data[prefix_count])
                if key not in streams:
                    prefix_text = "_".join(f"{byte:02X}" for byte in key[0])
                    filename = f"{prefix_text}-{key[1]:02X}.json"
                    streams[key] = (output / filename).open("w", encoding="utf-8")
                    streams[key].write("[\n")
                    first_record[key] = True
                    counts[key] = 0
                    max_tstates[key] = 0
                append_record(streams[key], record, first_record[key])
                first_record[key] = False
                counts[key] += 1
                max_tstates[key] = max(max_tstates[key], len(record.get("cycles", [])))
    finally:
        for stream in streams.values():
            stream.write("\n]\n")
            stream.close()

    active = []
    for sequence, opcode in sorted(counts):
        prefix_text = " ".join(f"{byte:02X}" for byte in sequence)
        filename = f"{'_'.join(f'{byte:02X}' for byte in sequence)}-{opcode:02X}.json"
        active.append({
            "file": filename,
            "opcode": f"{opcode:02X}",
            "prefixSequence": prefix_text,
            "records": counts[(sequence, opcode)],
            "maxTStates": max_tstates[(sequence, opcode)] + 1,
        })
    manifest = {
        "sourceSha": actual_sha,
        "sourceFiles": len(list(source.glob("*.json.gz"))),
        "sourceRecords": sum(counts.values()),
        "active": active,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=pathlib.Path, help="Pinned checkout's v1 directory")
    parser.add_argument("--source-sha", required=True, help="Expected pinned checkout commit SHA")
    parser.add_argument("--output", required=True, type=pathlib.Path, help="Output directory")
    args = parser.parse_args()
    try:
        manifest = export(args.source, args.output, args.source_sha)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(f"SHA {manifest['sourceSha']}; {len(manifest['active'])} prefix/opcode groups; {manifest['sourceRecords']} records")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
