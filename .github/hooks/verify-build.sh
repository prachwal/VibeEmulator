#!/usr/bin/env bash
# Stop hook: nie pozwala zakończyć tury, gdy zmieniony kod .NET nie buduje się
# albo nie przechodzi `dotnet format --verify-no-changes`.
#
# Wejście:  JSON hooka na stdin (pola cwd, stop_hook_active).
# Wyjście:  brak, albo dokładnie jeden obiekt JSON {"decision":"block","reason":"..."} na stdout.
# Kody:     zawsze 0. Diagnostyka wyłącznie na stderr.
# Zmienne:  VERIFY_BUILD_SKIP=1 — pomija weryfikację (testy, CI, awaria).
#
# Wymagania: jq lub python3 (parsowanie i budowanie JSON), git, dotnet w PATH.

set -uo pipefail

# Wyjście awaryjne / do testów.
if [ "${VERIFY_BUILD_SKIP:-}" = "1" ]; then
    exit 0
fi

have() { command -v "$1" >/dev/null 2>&1; }

# Bez parsera JSON nie ma jak bezpiecznie działać.
if ! have jq && ! have python3; then
    echo "verify-build: brak jq i python3 — pomijam." >&2
    exit 0
fi

input="$(cat)"

# --- Parsowanie payloadu ---------------------------------------------------
valid_payload() {
    if have jq; then
        printf '%s' "$input" | jq -e 'type == "object"' >/dev/null 2>&1
    else
        printf '%s' "$input" | python3 -c 'import json,sys; sys.exit(0 if isinstance(json.load(sys.stdin), dict) else 1)' >/dev/null 2>&1
    fi
}

# field KLUCZ — wartość pola top-level jako tekst (pusty, gdy brak).
field() {
    if have jq; then
        printf '%s' "$input" | jq -r --arg k "$1" \
            'if has($k) and .[$k] != null then .[$k] | tostring else empty end' 2>/dev/null
    else
        printf '%s' "$input" | python3 -c '
import json, sys
try:
    data = json.load(sys.stdin)
except Exception:
    sys.exit(0)
value = data.get(sys.argv[1]) if isinstance(data, dict) else None
if value is not None:
    print(str(value).lower() if isinstance(value, bool) else str(value))
' "$1" 2>/dev/null
    fi
}

# trunc N TEKST — pierwsze N znaków (nie bajtów) tekstu.
trunc() {
    if have jq; then
        printf '%s' "$2" | jq -Rrs --argjson n "$1" '.[0:$n]' 2>/dev/null
    else
        printf '%s' "$2" | python3 -c '
import sys
sys.stdout.write(sys.stdin.read()[:int(sys.argv[1])])
' "$1" 2>/dev/null
    fi
}

# emit_block POWÓD — jedyne wyjście na stdout: blokada z powodem.
emit_block() {
    if have jq; then
        jq -cn --arg reason "$1" '{decision:"block", reason:$reason}'
    else
        python3 -c 'import json,sys; print(json.dumps({"decision":"block","reason":sys.argv[1]}, ensure_ascii=False))' "$1"
    fi
    exit 0
}

if ! valid_payload; then
    echo "verify-build: niepoprawne wejście JSON — pomijam." >&2
    exit 0
fi

# Pętla blokowania: CLI sam ogranicza kolejne tury, ale nie zapętlamy się sami.
if [ "$(field stop_hook_active)" = "true" ]; then
    exit 0
fi

# --- Katalog roboczy -------------------------------------------------------
cwd="$(field cwd)"
if [ -n "$cwd" ] && [ -d "$cwd" ]; then
    cd "$cwd" 2>/dev/null || true
fi

top="$(git rev-parse --show-toplevel 2>/dev/null)" || top=""
if [ -z "$top" ]; then
    echo "verify-build: nie jestem w repozytorium git — pomijam." >&2
    exit 0
fi
cd "$top" || exit 0

# --- Czy zmieniono kod .NET? -----------------------------------------------
# Format porcelain v1: "XY ścieżka"; dla zmian nazwy "old -> new"; ścieżki
# z nietypowymi znakami bywają w cudzysłowach — filtr po rozszerzeniu to obsługuje.
relevant_change() {
    local line path
    while IFS= read -r line; do
        [ "${#line}" -lt 4 ] && continue
        path="${line:3}"
        path="${path#\"}"
        path="${path%\"}"
        [[ "$path" == *" -> "* ]] && path="${path##* -> }"
        case "$path" in
            *.cs|*.csproj|*.props|*.slnx|*.targets|.editorconfig|*/.editorconfig)
                return 0 ;;
        esac
    done
    return 1
}

# --untracked-files=all: nowy katalog z plikami .cs ma być widoczny jako pliki, nie jako "dir/".
changes="$(git status --porcelain --untracked-files=all 2>/dev/null)" || changes=""
if [ -z "$changes" ] || ! printf '%s\n' "$changes" | relevant_change; then
    exit 0
fi

# --- Uruchamianie poleceń z limitem czasu ----------------------------------
run_limited() {
    if have timeout; then
        timeout 75 "$@"
    else
        "$@"
    fi
}

# Bez kolorów ANSI, ze ścieżkami względnymi, bez sufiksu [projekt.csproj] i bez zduplikowanych linii
# (MSBuild raportuje ten sam błąd per projekt) — limit znaków w powodzie jest krótki.
strip_ansi() {
    sed -E -e 's/\x1b\[[0-9;?]*[A-Za-z]//g' -e "s#${top}/##g" -e 's/ \[[^]]*\.(csproj|slnx)\]//g' | awk '!seen[$0]++'
}

# --- 1. Build --------------------------------------------------------------
build_raw="$(run_limited dotnet build VibeEmulator.slnx -c Debug --nologo -v q 2>&1)"
build_rc=$?
if [ "$build_rc" -ne 0 ]; then
    build_out="$(printf '%s\n' "$build_raw" | strip_ansi)"
    errors="$(printf '%s\n' "$build_out" | grep 'error' || true)"
    [ -z "$errors" ] && errors="$build_out"
    excerpt="$(trunc 1500 "$errors")"
    emit_block "Build się nie powiódł. Napraw błędy i zakończ dopiero po zielonym \`dotnet build\`.
$excerpt"
fi

# --- 2. Format / analizatory ----------------------------------------------
format_raw="$(run_limited dotnet format VibeEmulator.slnx --verify-no-changes --severity warn 2>&1)"
format_rc=$?
if [ "$format_rc" -ne 0 ]; then
    format_out="$(printf '%s\n' "$format_raw" | strip_ansi)"
    excerpt="$(trunc 1000 "$format_out")"
    emit_block "Formatowanie/analizatory zgłaszają zmiany. Uruchom \`dotnet format VibeEmulator.slnx\` i popraw ostrzeżenia, potem zakończ.
$excerpt"
fi

exit 0
