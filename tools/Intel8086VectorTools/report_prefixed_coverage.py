#!/usr/bin/env python3
"""Reconcile a complete prefixed-vector run against its source audit and manifest."""

from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter
from typing import Any


def read_json(path: pathlib.Path) -> Any:
    with path.open(encoding="utf-8") as stream:
        return json.load(stream)


def build_report(manifest_path: pathlib.Path, run_path: pathlib.Path, audit_path: pathlib.Path) -> dict[str, Any]:
    manifest = read_json(manifest_path)
    run = read_json(run_path)
    audit = read_json(audit_path)
    source_sha = manifest.get("sourceSha")
    if source_sha != run.get("sourceSha") or source_sha != audit.get("source_sha"):
        raise ValueError("Manifest, runner report, and prefix audit must use the same source SHA.")
    source_pairs = audit.get("prefixed_opcodes")
    entries = manifest.get("active")
    if not isinstance(source_pairs, dict) or not isinstance(entries, list):
        raise ValueError("Prefix audit or manifest has an invalid schema.")
    manifest_pairs = {
        f"{entry.get('prefixSequence')} {entry.get('opcode')}": int(entry["records"])
        for entry in entries
    }
    if manifest_pairs != source_pairs:
        raise ValueError("Manifest prefix/opcode groups or counts differ from the pinned source audit.")

    failures = run.get("failures")
    if not isinstance(failures, list):
        raise ValueError("Runner report must contain a failures array.")
    failures_by_file: dict[str, list[str]] = {str(entry["file"]): [] for entry in entries}
    unexpected_failures: list[str] = []
    for failure in failures:
        if not isinstance(failure, str):
            raise ValueError("Runner report contains a non-string failure.")
        filename = failure.split(":", 1)[0]
        if filename not in failures_by_file:
            unexpected_failures.append(failure)
        elif "expected " not in failure or " records, found " not in failure:
            failures_by_file[filename].append(failure)

    groups = []
    total = passed = mismatched = 0
    for entry in entries:
        filename = str(entry["file"])
        records = int(entry["records"])
        group_failures = failures_by_file[filename]
        group_mismatched = len(group_failures)
        if group_mismatched > records:
            raise ValueError(f"{filename}: mismatch count exceeds source record count.")
        group_passed = records - group_mismatched
        total += records
        passed += group_passed
        mismatched += group_mismatched
        groups.append({
            "prefix_sequence": entry["prefixSequence"],
            "opcode": entry["opcode"],
            "file": filename,
            "records": records,
            "passed": group_passed,
            "mismatched": group_mismatched,
            "status": "full-pass" if group_passed == records else "known-gap",
            "failure_examples": group_failures[:3],
        })

    if total != run.get("total") or passed != run.get("passed") or mismatched != run.get("mismatched"):
        raise ValueError(
            f"Runner totals do not reconcile: manifest {total}, calculated {passed}/{mismatched}, "
            f"report {run.get('total')}/{run.get('passed')}/{run.get('mismatched')}."
        )
    if run.get("skipped") != 0:
        raise ValueError(f"Runner report contains {run.get('skipped')} skipped records.")
    if unexpected_failures:
        raise ValueError(f"Runner report has failures outside manifest groups: {unexpected_failures[:3]}")

    counts = Counter(group["status"] for group in groups)
    return {
        "source_sha": source_sha,
        "source_files": audit["source_files"],
        "source_records": audit["source_records"],
        "prefix_opcode_pairs": len(groups),
        "records": total,
        "passed": passed,
        "skipped": 0,
        "mismatched": mismatched,
        "full_pass_pairs": counts["full-pass"],
        "known_gap_pairs": counts["known-gap"],
        "groups": groups,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True, type=pathlib.Path)
    parser.add_argument("--run-report", required=True, type=pathlib.Path)
    parser.add_argument("--prefix-audit", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    try:
        report = build_report(args.manifest, args.run_report, args.prefix_audit)
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "groups"}, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
