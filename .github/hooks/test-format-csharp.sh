#!/usr/bin/env bash
# Testy regresyjne hooka .github/hooks/format-csharp.sh
# Uruchomienie: ./.github/hooks/test-format-csharp.sh
#
# Skrypt tworzy pliki-fixtury w katalogu tymczasowym wewnątrz repo,
# sprawdza efekt formatowania i przypadki brzegowe, a potem sprząta.

set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
hook="$root/.github/hooks/format-csharp.sh"
fixture="$root/src/Core/__hook_fixture.cs"
passed=0
failed=0

cleanup() {
    rm -f "$fixture"
}
trap cleanup EXIT

ok() {
    printf '  PASS  %s\n' "$1"
    passed=$((passed + 1))
}

fail() {
    printf '  FAIL  %s\n' "$1"
    failed=$((failed + 1))
}

run_hook() {
    printf '%s' "$1" | "$hook" >/dev/null 2>&1
}

echo "Testy hooka: $(basename "$hook")"
echo

# --- 1. Formatowanie niepoprawnego pliku ------------------------------------
cat > "$fixture" <<'CS'
namespace VibeEmulator.Core;
public static class Fixture   {
public static int   Add(int a,int b){return a+b;}
}
CS

run_hook '{"hook_event_name":"PostToolUse","tool_input":{"file_path":"src/Core/__hook_fixture.cs"}}'
if grep -q '^namespace VibeEmulator.Core;$' "$fixture" \
    && grep -q '    public static int Add(int a, int b)' "$fixture"; then
    ok "formatuje plik .cs (spacje, wcięcia, przecinek)"
else
    fail "nie sformatował pliku .cs"
    sed 's/^/        /' "$fixture"
fi

# --- 2. Kody wyjścia i przypadki brzegowe -----------------------------------
check_exit() {
    local name="$1" payload="$2" expected="$3"
    run_hook "$payload"
    local code=$?
    if [ "$code" -eq "$expected" ]; then
        ok "$name (exit $code)"
    else
        fail "$name (oczekiwano $expected, jest $code)"
    fi
}

check_exit "pusty stdin" "" 0
check_exit "niepoprawny JSON" '{"tool_input":' 0
check_exit "payload bez ścieżki .cs" '{"tool_input":{"file_path":"README.md"}}' 0
check_exit "nieistniejący plik .cs" '{"tool_input":{"file_path":"src/Nope.cs"}}' 0
check_exit "ścieżka poza repozytorium" '{"tool_input":{"file_path":"/etc/passwd.cs"}}' 0
check_exit "wiele ścieżek w tablicy" '{"tool_input":{"paths":["src/Core/Fibonacci.cs","tests/x.cs"]}}' 0
check_exit "prefiks file://" '{"tool_input":{"uri":"file://src/Core/Fibonacci.cs"}}' 0

# --- 3. Plik niezmieniony przez przypadek -----------------------------------
before="$(sha256sum "$root/src/Core/Fibonacci.cs" | cut -d' ' -f1)"
run_hook '{"tool_input":{"file_path":"src/Core/Fibonacci.cs"}}'
after="$(sha256sum "$root/src/Core/Fibonacci.cs" | cut -d' ' -f1)"
if [ "$before" = "$after" ]; then
    ok "nie zmienia już poprawnie sformatowanego pliku"
else
    fail "zmodyfikował plik, który był poprawny (idempotencja)"
fi

echo
printf 'Wynik: %d passed, %d failed\n' "$passed" "$failed"
[ "$failed" -eq 0 ]
