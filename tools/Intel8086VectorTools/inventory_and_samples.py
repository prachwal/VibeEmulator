#!/usr/bin/env python3
"""Offline inventory and deterministic prefix extraction for pinned SST 8086 data."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess
import sys
from collections import Counter


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=pathlib.Path, help="Pinned checkout's v1 directory")
    parser.add_argument("--source-sha", required=True, help="Expected Git commit SHA")
    parser.add_argument("--inventory", type=pathlib.Path, help="Write machine-readable inventory JSON")
    parser.add_argument("--samples-output", type=pathlib.Path, help="Optional output directory for first-N JSON samples")
    parser.add_argument("--count", type=int, default=20, help="Number of source records per sample (default: 20)")
    return parser.parse_args()


def read_prefix(path: pathlib.Path, count: int) -> list[dict[str, object]]:
    decoder = json.JSONDecoder()
    records: list[dict[str, object]] = []
    with gzip.open(path, "rt", encoding="utf-8") as stream:
        buffer = ""
        position = 0
        eof = False

        def refill() -> bool:
            nonlocal buffer, eof
            chunk = stream.read(65536)
            if not chunk:
                eof = True
                return False
            buffer += chunk
            return True

        while not buffer.lstrip() and refill():
            pass
        position = len(buffer) - len(buffer.lstrip())
        if position == len(buffer) and eof:
            raise ValueError(f"Empty source file: {path}")
        if buffer[position] != "[":
            raise ValueError(f"Top-level JSON value is not an array: {path}")
        position += 1

        while len(records) < count:
            while True:
                while position < len(buffer) and buffer[position].isspace():
                    position += 1
                if position < len(buffer) and buffer[position] == ",":
                    position += 1
                    continue
                if position < len(buffer) and buffer[position] == "]":
                    return records
                if position < len(buffer):
                    break
                if eof:
                    raise ValueError(f"Unexpected end of JSON array: {path}")
                refill()

            try:
                record, next_position = decoder.raw_decode(buffer, position)
            except json.JSONDecodeError:
                if eof:
                    raise
                refill()
                continue

            if not isinstance(record, dict) or "test_num" not in record:
                raise ValueError(f"Unexpected test record schema: {path}")
            records.append(record)
            position = next_position

            if position > 1_048_576:
                buffer = buffer[position:]
                position = 0

    return records


def count_source_records(path: pathlib.Path) -> tuple[int, int]:
    record_count = 0
    uncompressed_bytes = 0
    with gzip.open(path, "rb") as stream:
        while chunk := stream.read(1 << 20):
            uncompressed_bytes += len(chunk)
            record_count += chunk.count(b'"test_num"')
    return record_count, uncompressed_bytes


def main() -> int:
    args = parse_arguments()
    if args.count < 1:
        raise ValueError("--count must be positive")

    source = args.source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        raise ValueError("--source must point to a pinned checkout's v1 directory")
    actual_sha = subprocess.check_output(
        ["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True
    ).strip()
    if actual_sha.lower() != args.source_sha.lower():
        raise ValueError(f"Source SHA mismatch: expected {args.source_sha}, found {actual_sha}")

    metadata = json.loads((source / "metadata.json").read_text(encoding="utf-8"))
    paths = sorted(source.glob("*.json.gz"))
    if not paths:
        raise ValueError("No *.json.gz files found")

    entries: list[dict[str, object]] = []
    for path in paths:
        stem = path.name.removesuffix(".json.gz")
        parts = stem.split(".")
        opcode = parts[0].upper()
        extension = parts[1] if len(parts) == 2 else None
        opcode_metadata = metadata["opcodes"][opcode]
        extension_metadata = opcode_metadata.get("reg", {}).get(extension, {}) if extension else {}
        record_count, uncompressed_bytes = count_source_records(path)
        entries.append(
            {
                "source_file": path.name,
                "opcode": opcode,
                "modrm_extension": extension,
                "status": extension_metadata.get("status", opcode_metadata.get("status")),
                "flags_mask": extension_metadata.get("flags-mask", opcode_metadata.get("flags-mask")),
                "source_records": record_count,
                "sample_records": min(args.count, record_count),
                "active_sample_records": 0,
                "implementation_stage": None,
                "compressed_bytes": path.stat().st_size,
                "uncompressed_bytes": uncompressed_bytes,
            }
        )

    moo_dir = source.parent / "v1_binary"
    moo_files = sorted(moo_dir.glob("*.MOO.gz")) if moo_dir.exists() else []
    status_counts = Counter(item.get("status", "group-or-unspecified") for item in metadata["opcodes"].values())
    inventory = {
        "source": "https://github.com/SingleStepTests/8086",
        "source_sha": actual_sha,
        "data_directory": "v1",
        "metadata_version": metadata.get("version"),
        "metadata_cpu": metadata.get("cpu"),
        "metadata_cpu_detail": metadata.get("cpu_detail"),
        "metadata_generator": metadata.get("generator"),
        "metadata_opcode_count": len(metadata["opcodes"]),
        "metadata_status_counts": dict(sorted(status_counts.items())),
        "json_test_file_count": len(entries),
        "json_record_count": sum(int(item["source_records"]) for item in entries),
        "json_compressed_bytes": sum(int(item["compressed_bytes"]) for item in entries),
        "json_uncompressed_bytes": sum(int(item["uncompressed_bytes"]) for item in entries),
        "moo_file_count": len(moo_files),
        "moo_compressed_bytes": sum(path.stat().st_size for path in moo_files),
        "sample_records_per_file": args.count,
        "active_sample_records": 0,
        "entries": entries,
    }

    if args.inventory:
        args.inventory.parent.mkdir(parents=True, exist_ok=True)
        args.inventory.write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")

    if args.samples_output:
        output = args.samples_output.resolve()
        output.mkdir(parents=True, exist_ok=True)
        for path in paths:
            records = read_prefix(path, args.count)
            if not records:
                raise ValueError(f"No source records found: {path}")
            target = output / (path.name.removesuffix(".json.gz") + ".json")
            target.write_text(json.dumps(records, separators=(",", ":")) + "\n", encoding="utf-8")

    print(
        f"SHA {actual_sha}; {len(entries)} JSON files; {inventory['json_record_count']} records; "
        f"{len(moo_files)} MOO files; sample size {args.count}; active samples 0",
        file=sys.stdout,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
