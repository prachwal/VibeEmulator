#!/usr/bin/env bash
# Testy regresyjne hooka .github/hooks/verify-build.sh (Stop).
# Uruchomienie: ./.github/hooks/test-verify-build.sh
#
# Test jest hermetyczny: w katalogu tymczasowym tworzy własne repo git oraz
# atrapę `dotnet` (w osobnym katalogu bin/ na początku PATH). Prawdziwy dotnet
# ani prawdziwe repo nie są używane ani modyfikowane.

set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
hook="$root/.github/hooks/verify-build.sh"
passed=0
failed=0

tmp="$(mktemp -d)"
cleanup() {
    rm -rf "$tmp"
}
trap cleanup EXIT

repo="$tmp/repo"
nogit="$tmp/nogit"
bin="$tmp/bin"
calls="$tmp/dotnet-calls"
mkdir -p "$repo" "$nogit" "$bin"
: > "$calls"

# --- Atrapa dotnet ---------------------------------------------------------
cat > "$bin/dotnet" <<'FAKE'
#!/usr/bin/env bash
# Atrapa: zlicza wywołania i sterowana zmiennymi FAKE_* zwraca wynik.
echo "$*" >> "$FAKE_CALLS_FILE"
case "$1" in
    build)
        if [ -n "${FAKE_BUILD_OUT:-}" ]; then
            printf '%s\n' "$FAKE_BUILD_OUT"
        elif [ "${FAKE_BUILD_RC:-0}" != "0" ]; then
            echo 'Foo.cs(1,1): error CS1002: ; expected'
        fi
        exit "${FAKE_BUILD_RC:-0}"
        ;;
    format)
        if [ "${FAKE_FORMAT_RC:-0}" != "0" ]; then
            echo 'Formatted 1 of 1 files'
        fi
        exit "${FAKE_FORMAT_RC:-0}"
        ;;
esac
exit 0
FAKE
chmod +x "$bin/dotnet"

export PATH="$bin:$PATH"
export FAKE_CALLS_FILE="$calls"

# --- Fixture repo ----------------------------------------------------------
git init -q "$repo"
: > "$repo/VibeEmulator.slnx"
printf 'original\n' > "$repo/Foo.cs"
printf 'readme\n' > "$repo/README.md"
git -C "$repo" add VibeEmulator.slnx Foo.cs README.md
git -C "$repo" -c user.name=test -c user.email=test@example.invalid -c commit.gpgsign=false \
    commit -q -m init

# set_state clean|cs|readme — ustawia stan roboczy fixture repo.
set_state() {
    printf 'original\n' > "$repo/Foo.cs"
    printf 'readme\n' > "$repo/README.md"
    case "$1" in
        cs) printf 'changed\n' > "$repo/Foo.cs" ;;
        readme) printf 'changed\n' > "$repo/README.md" ;;
    esac
}

reset_env() {
    unset FAKE_BUILD_RC FAKE_FORMAT_RC FAKE_BUILD_OUT VERIFY_BUILD_SKIP
    : > "$calls"
}

# run_hook PAYLOAD [KATALOG] — wynik w $out, kod w $rc.
run_hook() {
    local payload="$1" dir="${2:-$repo}"
    out="$(printf '%s' "$payload" | (cd "$dir" && "$hook") 2>/dev/null)"
    rc=$?
}

count_calls() {
    wc -l < "$calls" | tr -d ' '
}

# block_ok JSON FRAGMENT — true, gdy JSON ma decision=block i powód zawiera FRAGMENT.
block_ok() {
    printf '%s' "$1" | python3 -c '
import json, sys
try:
    data = json.loads(sys.stdin.read())
except Exception:
    sys.exit(1)
ok = data.get("decision") == "block" and sys.argv[1] in data.get("reason", "")
sys.exit(0 if ok else 1)
' "$2" 2>/dev/null
}

ok() {
    printf '  PASS  %s\n' "$1"
    passed=$((passed + 1))
}

fail() {
    printf '  FAIL  %s\n' "$1"
    failed=$((failed + 1))
}

echo "Testy hooka: $(basename "$hook")"
echo

stop_payload() {
    printf '{"hook_event_name":"Stop","stop_hook_active":%s,"cwd":"%s"}' "$1" "$repo"
}

# --- 1. stop_hook_active=true ----------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
run_hook "$(stop_payload true)"
if [ -z "$out" ] && [ "$rc" -eq 0 ]; then ok "1 stop_hook_active=true — brak wyjścia"
else fail "1 stop_hook_active=true (wyjście: '$out', kod $rc)"; fi

# --- 2. Brak zmian w repo ---------------------------------------------------
reset_env; set_state clean
run_hook "$(stop_payload false)"
if [ -z "$out" ] && [ "$(count_calls)" -eq 0 ]; then ok "2 brak zmian — brak wyjścia, dotnet nie wywołany"
else fail "2 brak zmian (wyjście: '$out', wywołania: $(count_calls))"; fi

# --- 3. Zmieniony tylko README.md ------------------------------------------
reset_env; set_state readme
export FAKE_BUILD_RC=1
run_hook "$(stop_payload false)"
if [ -z "$out" ] && [ "$(count_calls)" -eq 0 ]; then ok "3 zmieniony tylko README.md — brak wyjścia, dotnet nie wywołany"
else fail "3 README (wyjście: '$out', wywołania: $(count_calls))"; fi

# --- 4. Build się nie powodzi -----------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
run_hook "$(stop_payload false)"
if block_ok "$out" "Build się nie powiódł" && block_ok "$out" "error CS1002"; then
    ok "4 build rc=1 — block z powodem i tekstem błędu CS1002"
else
    fail "4 build rc=1 (wyjście: '$out')"
fi

# --- 5. Build OK, format nie przechodzi -------------------------------------
reset_env; set_state cs
export FAKE_FORMAT_RC=1
run_hook "$(stop_payload false)"
if block_ok "$out" "Formatowanie" && [ "$(count_calls)" -eq 2 ]; then
    ok "5 format rc=1 — block z powodem „Formatowanie”"
else
    fail "5 format rc=1 (wyjście: '$out', wywołania: $(count_calls))"
fi

# --- 6. Wszystko OK ---------------------------------------------------------
reset_env; set_state cs
run_hook "$(stop_payload false)"
if [ -z "$out" ] && [ "$rc" -eq 0 ] && [ "$(count_calls)" -eq 2 ]; then
    ok "6 build i format OK — brak wyjścia"
else
    fail "6 wszystko OK (wyjście: '$out', kod $rc, wywołania: $(count_calls))"
fi

# --- 7. Niepoprawny JSON na stdin ------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
run_hook '{"stop_hook_active":'
if [ -z "$out" ] && [ "$rc" -eq 0 ] && [ "$(count_calls)" -eq 0 ]; then
    ok "7 niepoprawny JSON — brak wyjścia, exit 0"
else
    fail "7 niepoprawny JSON (wyjście: '$out', kod $rc)"
fi

# --- 8. Pusty stdin ---------------------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
run_hook ''
if [ -z "$out" ] && [ "$rc" -eq 0 ] && [ "$(count_calls)" -eq 0 ]; then
    ok "8 pusty stdin — brak wyjścia, exit 0"
else
    fail "8 pusty stdin (wyjście: '$out', kod $rc)"
fi

# --- 9. VERIFY_BUILD_SKIP=1 -------------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1 VERIFY_BUILD_SKIP=1
run_hook "$(stop_payload false)"
if [ -z "$out" ] && [ "$rc" -eq 0 ] && [ "$(count_calls)" -eq 0 ]; then
    ok "9 VERIFY_BUILD_SKIP=1 — brak wyjścia, dotnet nie wywołany"
else
    fail "9 VERIFY_BUILD_SKIP (wyjście: '$out', wywołania: $(count_calls))"
fi

# --- 10. Katalog bez gita ---------------------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
run_hook "$(printf '{"hook_event_name":"Stop","stop_hook_active":false,"cwd":"%s"}' "$nogit")"
if [ -z "$out" ] && [ "$rc" -eq 0 ] && [ "$(count_calls)" -eq 0 ]; then
    ok "10 katalog bez gita — brak wyjścia"
else
    fail "10 katalog bez gita (wyjście: '$out', wywołania: $(count_calls))"
fi

# --- 11. Escapowanie JSON w powodzie ----------------------------------------
reset_env; set_state cs
export FAKE_BUILD_RC=1
export FAKE_BUILD_OUT=$'Foo.cs(2,5): error CS0103: The name "x" does not exist\nFoo.cs(3,1): error CS1002: bad'
run_hook "$(stop_payload false)"
if block_ok "$out" '"x" does not exist' \
    && python3 -c '
import json, sys
reason = json.loads(sys.argv[1])["reason"]
sys.exit(0 if "\n" in reason and "error CS1002" in reason else 1)
' "$out" 2>/dev/null; then
    ok "11 cudzysłowy i znaki nowej linii poprawnie escapowane"
else
    fail "11 escapowanie (wyjście: '$out')"
fi

echo
printf 'Wynik: %d passed, %d failed\n' "$passed" "$failed"
[ "$failed" -eq 0 ]
