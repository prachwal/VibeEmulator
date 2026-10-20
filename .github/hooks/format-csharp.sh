#!/usr/bin/env bash
# PostToolUse hook: formatuje pliki .cs po edycji przez agenta.
#
# Wejście: JSON na stdin (kontrakt hooka). Skrypt jest tolerancyjny — wyciąga
# ścieżki .cs z typowych pól (file_path/path/uri) oraz z dowolnego stringa,
# który wygląda jak ścieżka do pliku .cs.
# Wyjście: brak (sukces nie zakłóca przepływu). Kody: 0 = OK, 2 = błąd blokujący.
#
# Wymagania: jq lub python3 (jeden z nich), dotnet w PATH.

set -uo pipefail

root="$(pwd)"
input="$(cat)"

# --- Wyciągnięcie kandydatów na ścieżki -------------------------------------
paths=""
if command -v jq >/dev/null 2>&1; then
    paths="$(printf '%s' "$input" | jq -r '
        [.. | strings | select(test("\\.cs$"))]
        | .[]
    ' 2>/dev/null)"
elif command -v python3 >/dev/null 2>&1; then
    paths="$(printf '%s' "$input" | python3 -c '
import json, re, sys
raw = sys.stdin.read()
try:
    data = json.loads(raw)
except Exception:
    data = raw
out = []
def walk(node):
    if isinstance(node, dict):
        for value in node.values():
            walk(value)
    elif isinstance(node, list):
        for value in node:
            walk(value)
    elif isinstance(node, str) and node.endswith(".cs"):
        out.append(node)
walk(data)
print("\n".join(out))
' 2>/dev/null)"
else
    # Bez parsera JSON nie ryzykujemy błędnego formatowania.
    exit 0
fi

if [ -z "$paths" ]; then
    exit 0
fi

# --- Filtrowanie: istniejące pliki .cs w repozytorium -----------------------
files=()
while IFS= read -r candidate; do
    [ -z "$candidate" ] && continue

    # Usuń prefiks file:// i zakodowane znaki, jeśli są.
    candidate="${candidate#file://}"
    candidate="${candidate//%20/ }"

    case "$candidate" in
        /*) absolute="$candidate" ;;
        *)  absolute="$root/$candidate" ;;
    esac

    # Tylko pliki wewnątrz repozytorium, które faktycznie istnieją.
    case "$absolute" in
        "$root"/*) ;;
        *) continue ;;
    esac

    if [ -f "$absolute" ]; then
        # dotnet format --include dopasowuje wyłącznie ścieżki względne wobec
        # katalogu solucji; ścieżka absolutna jest po cichu pomijana.
        files+=("${absolute#"$root"/}")
    fi
done <<< "$paths"

if [ ${#files[@]} -eq 0 ]; then
    exit 0
fi

# --- Formatowanie ----------------------------------------------------------
# Formatujemy tylko zmienione pliki; bez --verify-no-changes, żeby faktycznie poprawić kod.
# Ścieżki są względne wobec root repo (patrz wyżej) — inaczej --include nic nie dopasuje.
if ! dotnet format "$root/VibeEmulator.slnx" --include "${files[@]}" --severity info >/dev/null 2>&1; then
    # Formatowanie nie może blokować pracy agenta — zgłaszamy ostrzeżenie, ale nie przerywamy.
    echo "format-csharp: dotnet format nie powiódł się (kod nadal zapisany)." >&2
fi

exit 0
