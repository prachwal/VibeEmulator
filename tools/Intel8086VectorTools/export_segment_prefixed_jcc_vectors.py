#!/usr/bin/env python3
"""Export samples for every observed segment-prefixed 8086 Jcc alias group."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess

SEGMENT_PREFIXES = (0x26, 0x2E, 0x36, 0x3E)
OBSERVED_ALIAS_OPCODES = (0x69, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F)


def select_records(records: list[dict[str, object]], prefix: int, opcode: int) -> list[dict[str, object]]:
    return [
        record
        for record in records
        if len(record.get("bytes", [])) >= 3
        and record["bytes"][0] == prefix
        and record["bytes"][1] == opcode
    ]


def export(source: pathlib.Path, expected_sha: str, output: pathlib.Path, sample_count: int = 20) -> dict[str, object]:
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        raise ValueError("--source must point to the pinned checkout's v1 directory")
    actual_sha = subprocess.check_output(["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True).strip()
    if actual_sha.lower() != expected_sha.lower():
        raise ValueError(f"Source SHA mismatch: expected {expected_sha}, found {actual_sha}")
    if sample_count <= 0:
        raise ValueError("sample_count must be positive")

    output.mkdir(parents=True, exist_ok=True)
    active: list[dict[str, object]] = []
    for opcode in OBSERVED_ALIAS_OPCODES:
        with gzip.open(source / f"{opcode:02X}.json.gz", "rt", encoding="utf-8") as stream:
            records = json.load(stream)
        for prefix in SEGMENT_PREFIXES:
            selected = select_records(records, prefix, opcode)[:sample_count]
            if len(selected) != sample_count:
                raise ValueError(
                    f"{opcode:02X} with segment prefix {prefix:02X}: expected {sample_count}, found {len(selected)}"
                )
            filename = f"{prefix:02X}-{opcode:02X}.json"
            (output / filename).write_text(json.dumps(selected, separators=(",", ":")) + "\n", encoding="utf-8")
            active.append({
                "file": filename,
                "opcode": f"{opcode:02X}",
                "segmentPrefix": prefix,
                "records": len(selected),
                "testNumbers": [record["test_num"] for record in selected],
            })

    if len(active) != 24:
        raise ValueError(f"Expected 24 observed segment/opcode pairs, found {len(active)}")
    manifest: dict[str, object] = {"sourceSha": actual_sha, "sampleRecordsPerFile": sample_count, "active": active}
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=pathlib.Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    parser.add_argument("--count", type=int, default=20)
    args = parser.parse_args()
    manifest = export(args.source, args.source_sha, args.output, args.count)
    total = sum(entry["records"] for entry in manifest["active"])
    print(f"SHA {manifest['sourceSha']}; {len(manifest['active'])} segment/Jcc pairs; {total} records")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
