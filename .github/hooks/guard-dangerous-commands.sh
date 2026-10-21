#!/usr/bin/env bash
# PreToolUse hook: blokuje lub wymaga potwierdzenia dla niebezpiecznych poleceń.
#
# Decyzje:
#   deny — twarda blokada (operacja nieodwracalna lub wymagająca hasła, którego
#          agent nie może podać; nie ma sensu próbować).
#   ask  — operacja legalna, ale o dużym zasięgu; użytkownik ma potwierdzić.
#
# Wejście:  JSON hooka na stdin. Analizowane są pola command / terminalCommand / commandLine.
# Wyjście: przy decyzji deny/ask jeden obiekt JSON zawierający OBA kształty naraz:
#          płaskie permissionDecision/permissionDecisionReason na top-levelu oraz
#          hookSpecificOutput:{hookEventName,permissionDecision,permissionDecisionReason}.
#          Brak decyzji = brak wyjścia (domyślne zachowanie).
# Kody:    0 zawsze (decyzja przekazywana w JSON, nie przez exit code).
#
# Wymagania: jq lub python3 (ekstrakcja), nic więcej.

set -uo pipefail

root="$(pwd)"
input="$(cat)"

decision=""
reason=""

# --- Decyzje ---------------------------------------------------------------
set_decision() {
    local level="$1" why="$2"

    case "$level" in
        deny)
            decision="deny"
            reason="$why"
            ;;
        ask)
            if [ "$decision" != "deny" ]; then
                decision="ask"
                reason="$why"
            fi
            ;;
    esac
}

# --- Ekstrakcja poleceń ----------------------------------------------------
extract_commands() {
    if command -v jq >/dev/null 2>&1; then
        printf '%s' "$input" | jq -r '
            [.. | objects | (.command?, .terminalCommand?, .commandLine?) | strings]
            | .[]' 2>/dev/null
    elif command -v python3 >/dev/null 2>&1; then
        printf '%s' "$input" | python3 -c '
import json, sys
try:
    data = json.load(sys.stdin)
except Exception:
    sys.exit(0)
out = []
def walk(node):
    if isinstance(node, dict):
        for key in ("command", "terminalCommand", "commandLine"):
            value = node.get(key)
            if isinstance(value, str):
                out.append(value)
        for value in node.values():
            walk(value)
    elif isinstance(node, list):
        for value in node:
            walk(value)
walk(data)
print("\n".join(out))
' 2>/dev/null
    fi
}

# --- Analiza rm ------------------------------------------------------------
# Bezpieczne są tylko katalogi artefaktów generowanych przez build.
check_rm() {
    local cmd="$1"

    # Interesuje nas wyłącznie polecenie rm na początku segmentu (nie `git rm`).
    [[ "$cmd" =~ (^|[;\&\|])[[:space:]]*(sudo[[:space:]]+(-[^[:space:]]+[[:space:]]+)*)?rm[[:space:]] ]] || return 0

    local words=()
    read -ra words <<< "$cmd"

    local recursive=0 force=0
    local targets=()
    local word

    for word in "${words[@]}"; do
        case "$word" in
            rm|sudo|env|-n) ;;
            --recursive) recursive=1 ;;
            --force) force=1 ;;
            -*)
                [[ "$word" == *[rR]* ]] && recursive=1
                [[ "$word" == *f* ]] && force=1
                ;;
            *) targets+=("$word") ;;
        esac
    done

    # rm bez -r nie jest krytyczny (pojedynczy plik da się odzyskać z gita).
    if [ "$recursive" -eq 0 ] || [ "$force" -eq 0 ]; then
        return 0
    fi

    if [ ${#targets[@]} -eq 0 ]; then
        set_decision ask "rm -rf bez podanego celu — nie da się ocenić zakresu"
        return 0
    fi

    local target base
    for target in "${targets[@]}"; do
        case "$target" in
            "/"|"/*"|"~"|"~"/*|"\$HOME"|"\$HOME"/*|"."|".."|"*")
                set_decision deny "rm -rf na ścieżce krytycznej ($target) — operacja nieodwracalna"
                return 0
                ;;
        esac

        # Katalog repo jest zawsze chroniony.
        if [ "${target%/}" = "$root" ]; then
            set_decision deny "rm -rf na katalogu repozytorium ($target)"
            return 0
        fi

        # Artefakty budowania wolno usuwać nawet wewnątrz src/ (np. src/Core/bin).
        base="$(basename -- "${target%/}")"
        case "$base" in
            bin|obj|artifacts|out|dist|node_modules|coverage|htmlcov|__pycache__|BenchmarkDotNet.Artifacts|TestResults|"*.log"|"*.tmp"|"*.trx"|"*.binlog")
                continue
                ;;
        esac

        # Katalogi źródłowe: dużo pracy do odtworzenia.
        case "$target" in
            "src"|"src/"*|"tests"|"tests/"*|".github"|".github/"*|"tools"|"tools/"*)
                set_decision deny "rm -rf na katalogu źródłowym ($target) — użyj git restore"
                return 0
                ;;
        esac

        set_decision ask "rm -rf usuwa katalog spoza artefaktów ($target) — potwierdź"
        return 0
    done

    return 0
}

# --- Analiza git push ------------------------------------------------------
check_git_push() {
    local cmd="$1"

    [[ "$cmd" =~ (^|[[:space:]]|[;\&\|])git[[:space:]]+push ]] || return 0

    if [[ "$cmd" == *--force-with-lease* ]]; then
        set_decision ask "git push --force-with-lease nadpisze historię zdalną — potwierdź"
        return 0
    fi

    if [[ "$cmd" =~ --force([[:space:]]|$) ]] \
        || [[ "$cmd" =~ [[:space:]]-f([[:space:]]|$) ]] \
        || [[ "$cmd" =~ [[:space:]]\+[a-zA-Z] ]] \
        || [[ "$cmd" =~ --mirror([[:space:]]|$) ]] \
        || [[ "$cmd" =~ --delete([[:space:]]|$) ]] \
        || [[ "$cmd" =~ [[:space:]]:[a-zA-Z] ]]; then
        set_decision deny "git push z wymuszeniem usunie lub nadpisze zdalną historię — użyj --force-with-lease po potwierdzeniu"
        return 0
    fi

    return 0
}

# --- Wzorce prostoliniowe --------------------------------------------------
check_patterns() {
    local cmd="$1"
    local lc
    lc="$(printf '%s' "$cmd" | tr '[:upper:]' '[:lower:]')"

    # sudo bez -n blokuje terminal na haśle, którego agent nie ma jak podać.
    if [[ "$lc" =~ (^|[[:space:]]|[;\&\|])sudo[[:space:]] ]] \
        && ! [[ "$lc" =~ [[:space:]]-n([[:space:]]|$) ]]; then
        set_decision deny "sudo bez -n zawiesi wykonanie na promptcie hasła — uruchom ręcznie w terminalu"
        return 0
    fi

    # Kasowanie historii i nieodwracalne operacje na repozytorium.
    if [[ "$lc" =~ git[[:space:]]+reset[[:space:]]+--hard ]]; then
        set_decision deny "git reset --hard porzuci niezacommitowane zmiany — użyj git stash"
        return 0
    fi

    if [[ "$lc" =~ git[[:space:]]+clean[[:space:]]+-[a-z]*f[a-z]*x ]] \
        || [[ "$lc" =~ git[[:space:]]+clean[[:space:]]+-[a-z]*x[a-z]*f ]]; then
        set_decision deny "git clean -fdx usuwa również pliki ignorowane (sekrety, konfiguracja lokalna)"
        return 0
    fi

    if [[ "$lc" =~ git[[:space:]]+clean[[:space:]]+-[a-z]*f ]]; then
        set_decision ask "git clean -f usunie nieśledzone pliki — potwierdź"
        return 0
    fi

    if [[ "$lc" =~ git[[:space:]]+filter-(branch|repo) ]]; then
        set_decision deny "git filter-* przepisuje całą historię — operacja nieodwracalna"
        return 0
    fi

    if [[ "$lc" =~ git[[:space:]]+branch[[:space:]]+-d ]]; then
        set_decision ask "git branch -D usuwa gałąź mimo niezacommitowanych commitów — potwierdź"
        return 0
    fi

    if [[ "$lc" =~ git[[:space:]]+config[[:space:]]+--global ]]; then
        set_decision ask "zmiana globalnej konfiguracji gita wpłynie na wszystkie repozytoria"
        return 0
    fi

    # Operacje na surowych urządzeniach i systemie plików.
    if [[ "$lc" =~ (^|[[:space:]]|[;\&\|])mkfs ]] \
        || [[ "$lc" =~ (^|[[:space:]])dd[[:space:]]+if= ]] \
        || [[ "$lc" =~ (^|[[:space:]])shred[[:space:]] ]] \
        || [[ "$lc" =~ truncate[[:space:]]+-s[[:space:]]+0 ]]; then
        set_decision deny "operacja na surowym nośniku lub nieodwracalne nadpisanie danych"
        return 0
    fi

    if [[ "$lc" =~ \>[[:space:]]*/dev/(sd|nvme|hd|disk|mapper) ]]; then
        set_decision deny "zapis bezpośrednio do urządzenia blokowego zniszczy system plików"
        return 0
    fi

    if [[ "$lc" =~ (\>|\>\>)[[:space:]]*/etc/ ]] \
        || [[ "$lc" =~ /etc/sudoers ]]; then
        set_decision deny "modyfikacja plików systemowych w /etc wymaga świadomej decyzji administratora"
        return 0
    fi

    if [[ "$lc" =~ chmod[[:space:]]+-r[[:space:]]+777 ]] \
        || [[ "$lc" =~ chmod[[:space:]]+-r[[:space:]]+a\+rwx ]]; then
        set_decision deny "chmod -R 777 otwiera całe drzewo katalogów na zapis dla wszystkich"
        return 0
    fi

    if [[ "$lc" =~ chmod[[:space:]]+777 ]]; then
        set_decision ask "chmod 777 nadaje prawa zapisu wszystkim użytkownikom"
        return 0
    fi

    # Wykonywanie kodu pobranego z sieci bez weryfikacji.
    if [[ "$lc" =~ (curl|wget).*\|[[:space:]]*(sudo[[:space:]]+)?(bash|sh|zsh|fish) ]]; then
        set_decision deny "wykonywanie skryptu pobranego z sieci bez weryfikacji — pobierz i przejrzyj najpierw"
        return 0
    fi

    if [[ "$lc" =~ :\([[:space:]]*\)[[:space:]]*\{ ]]; then
        set_decision deny "fork bomb"
        return 0
    fi

    # Agent nie może sam sobie poluzować zasad.
    if [[ "$lc" =~ (\>|\>\>|tee|cp|mv|rm)[[:space:]].*\.github/hooks/ ]]; then
        set_decision deny "modyfikacja własnych hooków przez agenta jest zabroniona — zmień je ręcznie"
        return 0
    fi

    # Publikacja jest nieodwracalna.
    if [[ "$lc" =~ dotnet[[:space:]]+nuget[[:space:]]+push ]] \
        || [[ "$lc" =~ npm[[:space:]]+publish ]] \
        || [[ "$lc" =~ dotnet[[:space:]]+nuget[[:space:]]+delete ]]; then
        set_decision ask "publikacja paczki jest nieodwracalna — potwierdź wersję i rejestr"
        return 0
    fi

    # Reguła z .github/skills/csharp-perf-benchmark: pomiar w Debug jest bezwartościowy.
    if [[ "$lc" =~ dotnet[[:space:]]+run ]] \
        && [[ "$lc" =~ tools/bench ]] \
        && ! [[ "$lc" =~ (-c|--configuration)[[:space:]]*release ]]; then
        set_decision ask "benchmark uruchomiony bez -c Release — wyniki będą zniekształcone przez nieoptymalizowany JIT"
        return 0
    fi

    return 0
}

# --- Główna pętla ----------------------------------------------------------
commands="$(extract_commands)"

if [ -n "$commands" ]; then
    while IFS= read -r command; do
        [ -z "$command" ] && continue

        check_rm "$command"
        [ "$decision" = "deny" ] && break

        check_git_push "$command"
        [ "$decision" = "deny" ] && break

        check_patterns "$command"
        [ "$decision" = "deny" ] && break
    done <<< "$commands"
fi

# --- Wyjście ---------------------------------------------------------------
case "$decision" in
    deny|ask)
        if command -v jq >/dev/null 2>&1; then
            jq -cn --arg decision "$decision" --arg reason "$reason" \
                '{permissionDecision:$decision,permissionDecisionReason:$reason,hookSpecificOutput:{hookEventName:"PreToolUse",permissionDecision:$decision,permissionDecisionReason:$reason}}'
        else
            python3 -c '
import json, sys
decision, reason = sys.argv[1], sys.argv[2]
print(json.dumps({
    "permissionDecision": decision,
    "permissionDecisionReason": reason,
    "hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": decision,
        "permissionDecisionReason": reason,
    },
}))
' "$decision" "$reason"
        fi
        ;;
esac

exit 0
