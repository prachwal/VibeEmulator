#!/usr/bin/env python3
"""Export all pinned segment+REP-prefixed 8086 string vectors."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess


SEGMENT_PREFIXES = (0x26, 0x2E, 0x36, 0x3E)
REPEAT_PREFIXES = (0xF2, 0xF3)
STRING_OPCODES = (*range(0xA4, 0xA8), *range(0xAA, 0xB0))


def select_records(
    records: list[dict[str, object]], segment_prefix: int, repeat_prefix: int, opcode: int
) -> list[dict[str, object]]:
    return [
        record
        for record in records
        if len(record.get("bytes", [])) >= 3
        and record["bytes"][0] == segment_prefix
        and record["bytes"][1] == repeat_prefix
        and record["bytes"][2] == opcode
    ]


def export(source: pathlib.Path, expected_sha: str, output: pathlib.Path) -> dict[str, object]:
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        raise ValueError("--source must point to the pinned checkout's v1 directory")
    actual_sha = subprocess.check_output(["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True).strip()
    if actual_sha.lower() != expected_sha.lower():
        raise ValueError(f"Source SHA mismatch: expected {expected_sha}, found {actual_sha}")

    output.mkdir(parents=True, exist_ok=True)
    active: list[dict[str, object]] = []
    for opcode in STRING_OPCODES:
        with gzip.open(source / f"{opcode:02X}.json.gz", "rt", encoding="utf-8") as stream:
            records = json.load(stream)
        for segment_prefix in SEGMENT_PREFIXES:
            for repeat_prefix in REPEAT_PREFIXES:
                selected = select_records(records, segment_prefix, repeat_prefix, opcode)
                if not selected:
                    continue
                filename = f"{segment_prefix:02X}-{repeat_prefix:02X}-{opcode:02X}.json"
                (output / filename).write_text(json.dumps(selected, separators=(",", ":")) + "\n", encoding="utf-8")
                active.append(
                    {
                        "file": filename,
                        "opcode": f"{opcode:02X}",
                        "segmentPrefix": segment_prefix,
                        "repeatPrefix": repeat_prefix,
                        "records": len(selected),
                        "testNumbers": [record["test_num"] for record in selected],
                        "maxTStates": max(len(record["cycles"]) for record in selected) + 1,
                    }
                )

    if len(active) != len(SEGMENT_PREFIXES) * len(REPEAT_PREFIXES) * len(STRING_OPCODES):
        raise ValueError(f"Expected 80 non-empty prefix/opcode forms, found {len(active)}")
    manifest: dict[str, object] = {"sourceSha": actual_sha, "active": active}
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=pathlib.Path, help="Pinned checkout's v1 directory")
    parser.add_argument("--source-sha", required=True, help="Expected Git commit SHA")
    parser.add_argument("--output", required=True, type=pathlib.Path, help="Output directory for filtered JSON and manifest")
    args = parser.parse_args()
    manifest = export(args.source, args.source_sha, args.output)
    total = sum(entry["records"] for entry in manifest["active"])
    print(f"SHA {manifest['sourceSha']}; {len(manifest['active'])} prefix/opcode forms; {total} records")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
