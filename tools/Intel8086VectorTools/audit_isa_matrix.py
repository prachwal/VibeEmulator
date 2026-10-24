#!/usr/bin/env python3
"""Build a conservative per-form inventory of the pinned 8086 vectors."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import Counter
from typing import Any


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vectors", required=True, type=pathlib.Path, help="Local Vectors directory")
    parser.add_argument("--output", type=pathlib.Path, help="Write the matrix as JSON")
    parser.add_argument("--sample-report", type=pathlib.Path, help="Optional vector-runner report for source samples")
    parser.add_argument("--alias-jcc-report", type=pathlib.Path, help="Full pinned-vector report for unprefixed 60h–6Fh aliases")
    parser.add_argument("--alias-return-report", type=pathlib.Path, help="Full pinned-vector report for unprefixed C0/C1/C8/C9 aliases")
    parser.add_argument("--alias-decisions", type=pathlib.Path, help="Reviewed explicit decisions for remaining metadata alias forms")
    parser.add_argument("--esc-report", type=pathlib.Path, help="Full pinned-vector report for CPU ESC D8h–DFh behavior")
    return parser.parse_args()


def read_json(path: pathlib.Path) -> Any:
    with path.open(encoding="utf-8") as stream:
        return json.load(stream)


def form_key(opcode: str, extension: Any) -> tuple[str, int | None]:
    return opcode.upper(), None if extension is None else int(extension)


def disposition(source_status: str | None, active_entries: int) -> str:
    review = {
        "alias": "alias-review",
        "fpu": "fpu-review",
        "prefix": "prefix-review",
        "undocumented": "undocumented-review",
        None: "metadata-gap",
    }.get(source_status)
    if review is not None:
        return review
    return "sampled" if active_entries else "no-active-sample"


def build_matrix(
    vectors: pathlib.Path,
    sample_report: pathlib.Path | None = None,
    alias_jcc_report: pathlib.Path | None = None,
    alias_return_report: pathlib.Path | None = None,
    alias_decisions_path: pathlib.Path | None = None,
    esc_report: pathlib.Path | None = None,
) -> dict[str, Any]:
    inventory = read_json(vectors / "source-inventory.json")
    metadata = read_json(vectors / "metadata.json")
    source_sha = inventory["source_sha"]
    coverage: Counter[tuple[str, int | None]] = Counter()
    active_entries = 0
    active_records = 0
    unmapped_entries: list[str] = []
    sample_failures: Counter[str] = Counter()
    sample_summary: dict[str, Any] | None = None

    if sample_report is not None:
        report = read_json(sample_report)
        if report.get("sourceSha") != source_sha:
            raise ValueError(f"Sample report SHA {report.get('sourceSha')} != inventory SHA {source_sha}")
        if report.get("skipped") != 0:
            raise ValueError(f"Source sample report contains {report.get('skipped')} skipped records")
        for failure in report.get("failures", []):
            filename = str(failure).split(":", 1)[0]
            sample_failures[filename] += 1
        sample_summary = {
            "records": report.get("total"),
            "passed": report.get("passed"),
            "mismatched": report.get("mismatched"),
            "skipped": report.get("skipped"),
        }

    alias_jcc_evidence: dict[str, Any] | None = None
    if alias_jcc_report is not None:
        evidence = read_json(alias_jcc_report)
        if evidence.get("sourceSha") != source_sha:
            raise ValueError("Alias Jcc report SHA does not match source inventory")
        forms = evidence.get("forms")
        if not isinstance(forms, list) or {item.get("opcode") for item in forms} != {f"{opcode:02X}" for opcode in range(0x60, 0x70)}:
            raise ValueError("Alias Jcc report must cover every opcode 60h–6Fh exactly once")
        if len(forms) != 16 or any(not isinstance(item.get("records"), int) or item["records"] < 1 for item in forms):
            raise ValueError("Alias Jcc report contains invalid per-opcode record counts")
        total = sum(item["records"] for item in forms)
        if evidence.get("total") != total or evidence.get("passed") != total or evidence.get("skipped") != 0 or evidence.get("mismatched") != 0:
            raise ValueError("Alias Jcc report is incomplete or contains failures")
        alias_jcc_evidence = {"source_sha": source_sha, "records": total, "passed": total, "scope": evidence.get("scope")}

    alias_return_evidence: dict[str, Any] | None = None
    if alias_return_report is not None:
        evidence = read_json(alias_return_report)
        expected_opcodes = {"C0", "C1", "C8", "C9"}
        forms = evidence.get("forms")
        if evidence.get("sourceSha") != source_sha:
            raise ValueError("Alias RET report SHA does not match source inventory")
        if not isinstance(forms, list) or {item.get("opcode") for item in forms} != expected_opcodes or len(forms) != len(expected_opcodes):
            raise ValueError("Alias RET report must cover opcodes C0h, C1h, C8h and C9h exactly once")
        if any(not isinstance(item.get("records"), int) or item["records"] < 1 for item in forms):
            raise ValueError("Alias RET report contains invalid per-opcode record counts")
        total = sum(item["records"] for item in forms)
        if evidence.get("total") != total or evidence.get("passed") != total or evidence.get("skipped") != 0 or evidence.get("mismatched") != 0:
            raise ValueError("Alias RET report is incomplete or contains failures")
        alias_return_evidence = {"source_sha": source_sha, "records": total, "passed": total, "scope": evidence.get("scope")}

    alias_decisions: dict[tuple[str, int | None], dict[str, Any]] = {}
    if alias_decisions_path is not None:
        decisions_data = read_json(alias_decisions_path)
        if decisions_data.get("sourceSha") != source_sha:
            raise ValueError("Alias decisions SHA does not match source inventory")
        items = decisions_data.get("decisions")
        if not isinstance(items, list):
            raise ValueError("Alias decisions must contain a decisions array")
        for item in items:
            key = form_key(item["opcode"], item.get("modrm_extension"))
            if key in alias_decisions:
                raise ValueError(f"Duplicate alias decision for {key[0]}/{key[1]}")
            if item.get("disposition") not in {"known-gap", "out-of-scope"} or not item.get("reason"):
                raise ValueError(f"Alias decision for {key[0]}/{key[1]} must state known-gap/out-of-scope and a reason")
            alias_decisions[key] = item

    esc_evidence: dict[str, Any] | None = None
    if esc_report is not None:
        evidence = read_json(esc_report)
        expected_opcodes = {f"{opcode:02X}" for opcode in range(0xD8, 0xE0)}
        forms = evidence.get("forms")
        if evidence.get("sourceSha") != source_sha:
            raise ValueError("ESC report SHA does not match source inventory")
        if not isinstance(forms, list) or {item.get("opcode") for item in forms} != expected_opcodes or len(forms) != len(expected_opcodes):
            raise ValueError("ESC report must cover opcodes D8h–DFh exactly once")
        if any(not isinstance(item.get("records"), int) or item["records"] < 1 for item in forms):
            raise ValueError("ESC report contains invalid per-opcode record counts")
        total = sum(item["records"] for item in forms)
        if evidence.get("total") != total or evidence.get("passed") != total or evidence.get("skipped") != 0 or evidence.get("mismatched") != 0:
            raise ValueError("ESC report is incomplete or contains failures")
        esc_evidence = {"source_sha": source_sha, "records": total, "passed": total, "scope": evidence.get("scope")}

    for manifest_path in sorted(vectors.rglob("manifest.json")):
        manifest = read_json(manifest_path)
        manifest_sha = manifest.get("sourceSha")
        if manifest_sha is not None and manifest_sha != source_sha:
            raise ValueError(f"{manifest_path}: manifest SHA {manifest_sha} != inventory SHA {source_sha}")
        for entry in manifest.get("active", []):
            active_entries += 1
            active_records += int(entry["records"])
            opcode = entry.get("opcode")
            if not isinstance(opcode, str):
                filename = pathlib.Path(str(entry.get("file", ""))).name
                match = re.match(r"^([0-9A-Fa-f]{2})(?:\.|$)", filename)
                opcode = match.group(1) if match else None
            if not isinstance(opcode, str):
                unmapped_entries.append(f"{manifest_path.relative_to(vectors)}:{entry.get('file', '<no file>')}")
                continue
            coverage[form_key(opcode, entry.get("modrmExtension"))] += 1

    rows: list[dict[str, Any]] = []
    seen: set[tuple[str, int | None]] = set()
    for entry in inventory["entries"]:
        key = form_key(entry["opcode"], entry.get("modrm_extension"))
        if key in seen:
            raise ValueError(f"Duplicate source form: {entry['source_file']}")
        seen.add(key)
        opcode_status = metadata["opcodes"].get(key[0], {})
        extension_status = opcode_status.get("reg", {}).get(
            None if key[1] is None else str(key[1]), {}
        )
        source_status = extension_status.get("status", opcode_status.get("status"))
        count = coverage[key]
        source_filename = entry["source_file"].removesuffix(".json.gz") + ".json"
        source_sample = None
        if sample_report is not None and source_status == "normal":
            sample_records = int(entry["sample_records"])
            mismatched = sample_failures[source_filename]
            if mismatched > sample_records:
                raise ValueError(f"{source_filename}: {mismatched} mismatches exceed {sample_records} sampled records")
            passed = sample_records - mismatched
            sample_status = "all-passed" if passed == sample_records else "none-passed" if passed == 0 else "partial"
            source_sample = {
                "records": sample_records,
                "passed": passed,
                "mismatched": mismatched,
                "status": sample_status,
            }
        row = {
                "source_file": entry["source_file"],
                "opcode": key[0],
                "modrm_extension": key[1],
                "source_status": source_status,
                "implementation_stage_note": entry.get("implementation_stage"),
                "active_manifest_entries": count,
                "disposition": disposition(source_status, count),
                "source_first_sample": source_sample,
            }
        if alias_jcc_evidence is not None and source_status == "alias" and key[1] is None and 0x60 <= int(key[0], 16) <= 0x6F:
            if count == 0:
                raise ValueError(f"{entry['source_file']}: full alias evidence requires an active local sample")
            row["disposition"] = "implemented-alias"
            row["decision"] = "Alias warunkowego skoku Jcc; warunek odpowiada opcode 70h–7Fh o tym samym dolnym nibblu."
            row["full_vector_evidence"] = alias_jcc_evidence
        if alias_return_evidence is not None and source_status == "alias" and key[1] is None and key[0] in {"C0", "C1", "C8", "C9"}:
            if count == 0:
                raise ValueError(f"{entry['source_file']}: full alias evidence requires an active local sample")
            row["disposition"] = "implemented-alias"
            row["decision"] = "Alias RETN/RETF: C0→C2, C1→C3, C8→CA, C9→CB, potwierdzony pełnymi wektorami."
            row["full_vector_evidence"] = alias_return_evidence
        if key in alias_decisions:
            if source_status != "alias":
                raise ValueError(f"{entry['source_file']}: explicit alias decision targets non-alias metadata")
            row["disposition"] = alias_decisions[key]["disposition"]
            row["decision"] = alias_decisions[key]["reason"]
        if source_status == "fpu" and esc_evidence is not None:
            if key[0] not in {f"{opcode:02X}" for opcode in range(0xD8, 0xE0)} or key[1] is not None or count == 0:
                raise ValueError(f"{entry['source_file']}: ESC evidence does not map to an active D8h–DFh source form")
            row["disposition"] = "implemented-escape"
            row["decision"] = "Implementowane jest zachowanie CPU dla ESC; semantyka zewnętrznego koprocesora 8087 pozostaje poza rdzeniem."
            row["full_vector_evidence"] = esc_evidence
        if source_status == "undocumented":
            row["disposition"] = "known-gap"
            row["decision"] = "Przypięte wektory zawierają nieudokumentowane zachowanie P80C86A-2; wymaga osobnego zakresu i pełnej weryfikacji przed implementacją."
        if source_status is None:
            row["disposition"] = "known-gap"
            row["decision"] = "Brak statusu opcode-level w metadata.json; forma nie jest utożsamiana z undefined. Wymaga rozbicia grupy i uzgodnienia z manualem."
        if source_status == "normal" and sample_report is not None:
            sample = source_sample or {"records": 0, "passed": 0, "mismatched": 0}
            row["disposition"] = "known-gap"
            if sample["mismatched"]:
                row["decision"] = f"Znana luka wykonania/weryfikacji: deterministyczna próbka {sample['records']} rekordów ma {sample['mismatched']} rozbieżności."
            elif count == 0:
                row["decision"] = f"Znana luka pokrycia: brak aktywnego fixture; próbka {sample['records']} rekordów przeszła, ale nie ma lokalnej regresji."
            else:
                row["decision"] = f"Znana luka dowodowa: fixture jest aktywny, ale macierz nie ma pełnego raportu źródłowego dla formy; próbka to tylko {sample['records']} rekordów."
        rows.append(row)

    unknown_decisions = set(alias_decisions) - seen
    if unknown_decisions:
        rendered = ", ".join(f"{opcode}/{extension}" for opcode, extension in sorted(unknown_decisions, key=lambda key: (key[0], -1 if key[1] is None else key[1])))
        raise ValueError(f"Alias decisions refer to forms outside the source inventory: {rendered}")

    if alias_decisions_path is not None:
        unresolved_aliases = {
            form_key(row["opcode"], row["modrm_extension"])
            for row in rows
            if row["source_status"] == "alias" and row["disposition"] == "alias-review"
        }
        if unresolved_aliases:
            rendered = ", ".join(f"{opcode}/{extension}" for opcode, extension in sorted(unresolved_aliases, key=lambda key: (key[0], -1 if key[1] is None else key[1])))
            raise ValueError(f"Unresolved metadata aliases: {rendered}")

    if sample_report is not None:
        unknown_failures = sorted(set(sample_failures) - {row["source_file"].removesuffix(".json.gz") + ".json" for row in rows})
        if unknown_failures:
            raise ValueError(f"Sample report contains files outside the inventory: {', '.join(unknown_failures[:5])}")
        expected_records = sum(int(entry["sample_records"]) for entry in inventory["entries"] if entry.get("status") == "normal")
        if sample_summary is None or sample_summary["records"] != expected_records:
            raise ValueError(f"Sample report covers {None if sample_summary is None else sample_summary['records']} records; expected {expected_records}")

    source_sample_counts = Counter(
        row["source_first_sample"]["status"]
        for row in rows
        if row["source_first_sample"] is not None
    )

    return {
        "source_sha": source_sha,
        "source_files": len(rows),
        "active_manifest_entries": active_entries,
        "active_manifest_records": active_records,
        "mapped_source_forms": sum(1 for row in rows if row["active_manifest_entries"]),
        "source_first_sample": sample_summary,
        "source_first_sample_status_counts": dict(sorted(source_sample_counts.items())),
        "unmapped_active_manifest_entries": unmapped_entries,
        "disposition_counts": dict(sorted(Counter(row["disposition"] for row in rows).items())),
        "alias_jcc_evidence": alias_jcc_evidence,
        "alias_return_evidence": alias_return_evidence,
        "esc_evidence": esc_evidence,
        "interpretation": (
            "known-gap means implementation or coverage evidence is incomplete; it does not mean the hardware encoding is unsupported. "
            "A local sample alone never proves complete source-vector or ModR/M coverage."
        ),
        "forms": rows,
    }


def main() -> int:
    args = parse_arguments()
    try:
        matrix = build_matrix(
            args.vectors.resolve(),
            args.sample_report.resolve() if args.sample_report else None,
            args.alias_jcc_report.resolve() if args.alias_jcc_report else None,
            args.alias_return_report.resolve() if args.alias_return_report else None,
            args.alias_decisions.resolve() if args.alias_decisions else None,
            args.esc_report.resolve() if args.esc_report else None,
        )
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as exception:
        print(f"ERROR: {exception}", file=sys.stderr)
        return 1

    rendered = json.dumps(matrix, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(rendered, encoding="utf-8")
    print(json.dumps({key: value for key, value in matrix.items() if key != "forms"}, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
