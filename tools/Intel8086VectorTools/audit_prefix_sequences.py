#!/usr/bin/env python3
"""Summarize leading 8086 prefix sequences in a pinned SingleStepTests checkout."""

from __future__ import annotations

import argparse
import collections
import gzip
import json
import pathlib
import subprocess
import sys
from typing import Any

PREFIXES = {0x26, 0x2E, 0x36, 0x3E, 0xF0, 0xF2, 0xF3}


def scan_records(records: list[dict[str, Any]]) -> collections.Counter[tuple[int, ...]]:
    sequences: collections.Counter[tuple[int, ...]] = collections.Counter()
    for record in records:
        data = record.get("bytes", [])
        prefix_count = 0
        while prefix_count < len(data) and data[prefix_count] in PREFIXES:
            prefix_count += 1
        if prefix_count:
            sequences[tuple(data[:prefix_count])] += 1
    return sequences


def scan_prefixed_opcodes(
    records: list[dict[str, Any]],
) -> collections.Counter[tuple[tuple[int, ...], int]]:
    prefixed_opcodes: collections.Counter[tuple[tuple[int, ...], int]] = collections.Counter()
    for record in records:
        data = record.get("bytes", [])
        prefix_count = 0
        while prefix_count < len(data) and data[prefix_count] in PREFIXES:
            prefix_count += 1
        if prefix_count and prefix_count < len(data):
            prefixed_opcodes[(tuple(data[:prefix_count]), data[prefix_count])] += 1
    return prefixed_opcodes


def audit_source(source: pathlib.Path, expected_sha: str) -> tuple[list[str], dict[str, Any]]:
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        return ["Katalog źródła musi wskazywać przypięty katalog v1."], {}

    actual_sha = subprocess.check_output(
        ["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True
    ).strip()
    if actual_sha.lower() != expected_sha.lower():
        return [f"SHA checkoutu: oczekiwano {expected_sha}, znaleziono {actual_sha}."], {}

    files = sorted(source.glob("*.json.gz"))
    sequences: collections.Counter[tuple[int, ...]] = collections.Counter()
    prefixed_opcodes: collections.Counter[tuple[tuple[int, ...], int]] = collections.Counter()
    record_count = 0
    for path in files:
        with gzip.open(path, "rt", encoding="utf-8") as stream:
            records = json.load(stream)
        record_count += len(records)
        sequences.update(scan_records(records))
        prefixed_opcodes.update(scan_prefixed_opcodes(records))

    summary: dict[str, Any] = {
        "source_sha": actual_sha,
        "source_files": len(files),
        "source_records": record_count,
        "prefix_sequences": {
            " ".join(f"{byte:02X}" for byte in sequence): count
            for sequence, count in sorted(sequences.items())
        },
        "prefixed_opcodes": {
            f"{' '.join(f'{byte:02X}' for byte in sequence)} {opcode:02X}": count
            for (sequence, opcode), count in sorted(prefixed_opcodes.items())
        },
    }
    return [], summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=pathlib.Path, help="Pinned checkout's v1 directory")
    parser.add_argument("--sha", required=True, help="Expected Git commit SHA")
    args = parser.parse_args()

    errors, summary = audit_source(args.source, args.sha)
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(json.dumps({"status": "ok", **summary}, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
