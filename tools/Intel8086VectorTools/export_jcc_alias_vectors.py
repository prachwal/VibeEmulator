#!/usr/bin/env python3
"""Export all unprefixed pinned vectors for the 8086 Jcc aliases 60h–6Fh."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess


def select_unprefixed_alias_records(records: list[dict[str, object]], opcode: int) -> list[dict[str, object]]:
    return [record for record in records if record.get("bytes") and record["bytes"][0] == opcode]


def export(source: pathlib.Path, expected_sha: str, output: pathlib.Path) -> dict[str, object]:
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        raise ValueError("--source must point to the pinned checkout's v1 directory")
    actual_sha = subprocess.check_output(["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True).strip()
    if actual_sha.lower() != expected_sha.lower():
        raise ValueError(f"Source SHA mismatch: expected {expected_sha}, found {actual_sha}")

    output.mkdir(parents=True, exist_ok=True)
    active: list[dict[str, object]] = []
    for opcode in range(0x60, 0x70):
        source_path = source / f"{opcode:02X}.json.gz"
        with gzip.open(source_path, "rt", encoding="utf-8") as stream:
            records = json.load(stream)
        selected = select_unprefixed_alias_records(records, opcode)
        if not selected:
            raise ValueError(f"No unprefixed source records for {opcode:02X}")
        filename = f"{opcode:02X}.json"
        (output / filename).write_text(json.dumps(selected, separators=(",", ":")) + "\n", encoding="utf-8")
        active.append({"file": filename, "opcode": f"{opcode:02X}", "records": len(selected)})

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
    print(f"SHA {manifest['sourceSha']}; 16 aliases; {total} unprefixed records")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
