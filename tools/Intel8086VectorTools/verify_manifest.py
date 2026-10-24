#!/usr/bin/env python3
"""Verify pinned 8086 inventory, local manifests, and optionally the source checkout."""

from __future__ import annotations

import argparse
import gzip
import json
import pathlib
import subprocess
import sys
from collections import Counter
from typing import Any


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vectors", required=True, type=pathlib.Path, help="Local Vectors directory")
    parser.add_argument("--source", type=pathlib.Path, help="Optional pinned checkout's v1 directory")
    return parser.parse_args()


def read_json(path: pathlib.Path) -> Any:
    with path.open(encoding="utf-8") as stream:
        return json.load(stream)


def count_records(path: pathlib.Path) -> int:
    with path.open(encoding="utf-8") as stream:
        return sum(1 for _ in json.load(stream))


def count_source_records(path: pathlib.Path) -> int:
    count = 0
    needle = b'"test_num"'
    overlap = len(needle) - 1
    tail = b""
    with gzip.open(path, "rb") as stream:
        while chunk := stream.read(1 << 20):
            data = tail + chunk
            count += data.count(needle)
            tail = data[-overlap:]
    return count


def verify_source_checkout(
    source: pathlib.Path,
    expected_sha: str,
    inventory_names: set[str],
    inventory_entries: dict[str, dict[str, Any]],
) -> list[str]:
    errors: list[str] = []
    source = source.resolve()
    if source.name != "v1" or not (source / "metadata.json").is_file():
        return ["--source musi wskazywać katalog v1 przypiętego checkoutu."]

    actual_sha = subprocess.check_output(
        ["git", "-C", str(source.parent), "rev-parse", "HEAD"], text=True
    ).strip()
    if actual_sha.lower() != expected_sha.lower():
        errors.append(f"SHA checkoutu: oczekiwano {expected_sha}, znaleziono {actual_sha}.")

    source_paths = {path.name: path for path in source.glob("*.json.gz")}
    missing = sorted(inventory_names - source_paths.keys())
    extra = sorted(source_paths.keys() - inventory_names)
    if missing:
        errors.append(f"Brak {len(missing)} plików inwentarza w checkoutcie: {', '.join(missing[:8])}.")
    if extra:
        errors.append(f"Checkout zawiera {len(extra)} plików spoza inwentarza: {', '.join(extra[:8])}.")

    for name in sorted(inventory_names & source_paths.keys()):
        expected = inventory_entries[name]["source_records"]
        actual = count_source_records(source_paths[name])
        if actual != expected:
            errors.append(f"{name}: inwentarz deklaruje {expected} rekordów, źródło ma {actual}.")

    return errors


def verify_vectors(vectors: pathlib.Path, source: pathlib.Path | None = None) -> tuple[list[str], dict[str, Any]]:
    vectors = vectors.resolve()
    inventory_path = vectors / "source-inventory.json"
    metadata_path = vectors / "metadata.json"
    if not inventory_path.is_file() or not metadata_path.is_file():
        return ["Brak source-inventory.json lub metadata.json."], {}

    inventory = read_json(inventory_path)
    metadata = read_json(metadata_path)
    errors: list[str] = []
    entries = inventory.get("entries")
    if not isinstance(entries, list):
        return ["source-inventory.json nie zawiera tablicy entries."], {}

    names: list[str] = []
    inventory_by_name: dict[str, dict[str, Any]] = {}
    record_count = 0
    for entry in entries:
        name = entry.get("source_file")
        if not isinstance(name, str) or not name.endswith(".json.gz"):
            errors.append(f"Niepoprawny source_file w inwentarzu: {name!r}.")
            continue
        names.append(name)
        if name in inventory_by_name:
            errors.append(f"Duplikat pliku inwentarza: {name}.")
            continue
        inventory_by_name[name] = entry
        record_count += int(entry.get("source_records", -1))

        stem_parts = name.removesuffix(".json.gz").split(".")
        opcode = stem_parts[0].upper()
        extension = stem_parts[1] if len(stem_parts) == 2 else None
        if entry.get("opcode") != opcode or entry.get("modrm_extension") != extension:
            errors.append(f"{name}: opcode/ModR/M w inwentarzu nie zgadza się z nazwą pliku.")
        opcode_metadata = metadata.get("opcodes", {}).get(opcode)
        if opcode_metadata is None:
            errors.append(f"{name}: opcode {opcode} nie istnieje w metadata.json.")
            continue
        extension_metadata = opcode_metadata.get("reg", {}).get(extension, {}) if extension else {}
        expected_status = extension_metadata.get("status", opcode_metadata.get("status"))
        if entry.get("status") != expected_status:
            errors.append(f"{name}: status inwentarza {entry.get('status')!r}, metadata {expected_status!r}.")

    expected_file_count = inventory.get("json_test_file_count")
    if len(inventory_by_name) != expected_file_count:
        errors.append(f"Liczba plików: inwentarz deklaruje {expected_file_count}, zawiera {len(inventory_by_name)}.")
    expected_record_count = inventory.get("json_record_count")
    if record_count != expected_record_count:
        errors.append(f"Liczba rekordów: nagłówek deklaruje {expected_record_count}, wpisy sumują się do {record_count}.")
    if len(metadata.get("opcodes", {})) != inventory.get("metadata_opcode_count"):
        errors.append("Liczba opcode metadata nie zgadza się z inwentarzem.")
    status_counts = Counter(
        opcode.get("status", "group-or-unspecified")
        for opcode in metadata.get("opcodes", {}).values()
    )
    if dict(sorted(status_counts.items())) != inventory.get("metadata_status_counts"):
        errors.append("Liczności statusów metadata nie zgadzają się z inwentarzem.")

    main_manifest_path = vectors / "manifest.json"
    if not main_manifest_path.is_file():
        errors.append("Brak manifest.json w katalogu Vectors.")
    else:
        main_manifest = read_json(main_manifest_path)
        source_sha = inventory.get("source_sha")
        if main_manifest.get("sourceSha") != source_sha:
            errors.append("SHA manifest.json nie zgadza się z source-inventory.json.")

    active_entries = 0
    active_records = 0
    for manifest_path in sorted(vectors.rglob("manifest.json")):
        manifest = read_json(manifest_path)
        manifest_sha = manifest.get("sourceSha")
        if manifest_sha is not None and manifest_sha != inventory.get("source_sha"):
            errors.append(f"{manifest_path.relative_to(vectors)}: SHA manifestu różni się od inwentarza.")
        active = manifest.get("active", [])
        if not isinstance(active, list):
            errors.append(f"{manifest_path.relative_to(vectors)}: active nie jest tablicą.")
            continue

        for item in active:
            relative_file = item.get("file")
            declared_records = item.get("records")
            if not isinstance(relative_file, str) or not isinstance(declared_records, int) or declared_records < 1:
                errors.append(f"{manifest_path.relative_to(vectors)}: aktywny wpis ma niepoprawne file/records.")
                continue
            local_sample_path = manifest_path.parent / relative_file
            sample_path = local_sample_path if local_sample_path.is_file() else vectors / relative_file
            if not sample_path.is_file():
                errors.append(f"{manifest_path.relative_to(vectors)}: brak aktywnej próbki {relative_file}.")
                continue
            try:
                sample = read_json(sample_path)
            except (OSError, json.JSONDecodeError) as exception:
                errors.append(f"{sample_path.relative_to(vectors)}: nie można odczytać JSON ({exception}).")
                continue
            if not isinstance(sample, list):
                errors.append(f"{sample_path.relative_to(vectors)}: próbka nie jest tablicą rekordów.")
                continue

            test_numbers = item.get("testNumbers")
            if test_numbers is not None:
                if len(test_numbers) != declared_records or len(set(test_numbers)) != len(test_numbers):
                    errors.append(f"{sample_path.relative_to(vectors)}: testNumbers nie zgadza się z records={declared_records}.")
                    continue
                record_by_number = {record.get("test_num"): record for record in sample}
                if any(number not in record_by_number for number in test_numbers):
                    errors.append(f"{sample_path.relative_to(vectors)}: testNumbers wskazuje brakujący rekord.")
                    continue
                selected = [record_by_number[number] for number in test_numbers]
            else:
                if len(sample) != declared_records:
                    errors.append(f"{sample_path.relative_to(vectors)}: manifest deklaruje {declared_records}, próbka ma {len(sample)} rekordów.")
                    continue
                selected = sample

            max_t_states = item.get("maxTStates")
            if max_t_states is not None and (
                not isinstance(max_t_states, int)
                or max_t_states <= 0
                or any(len(record.get("cycles", [])) >= max_t_states for record in selected)
            ):
                errors.append(f"{sample_path.relative_to(vectors)}: maxTStates nie obejmuje wszystkich aktywnych rekordów.")
                continue
            active_entries += 1
            active_records += declared_records

    if source is not None:
        errors.extend(verify_source_checkout(source, inventory["source_sha"], set(inventory_by_name), inventory_by_name))

    summary: dict[str, Any] = {
        "source_sha": inventory.get("source_sha", ""),
        "source_files": len(inventory_by_name),
        "source_records": record_count,
        "metadata_opcodes": len(metadata.get("opcodes", {})),
        "metadata_status_counts": dict(sorted(status_counts.items())),
        "active_manifest_entries": active_entries,
        "active_manifest_records": active_records,
    }
    return errors, summary


def main() -> int:
    args = parse_arguments()
    errors, summary = verify_vectors(args.vectors, args.source)
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(json.dumps({"status": "ok", **summary}, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
