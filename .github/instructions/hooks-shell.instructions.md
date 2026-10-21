---
applyTo: ".github/hooks/**"
description: "Reguły dla hooków Copilota (pliki .json i skrypty .sh): format pliku, kontrakt stdin/stdout, kody wyjścia, hook Stop, bezpieczeństwo, tolerancja wejścia, testy regresyjne."
---

# Hooki (`.github/hooks/`): konfiguracja i skrypty

Hooki są **deterministyczne** — w odróżnieniu od instructions i skills zawsze się wykonają.
Dlatego jakość tych skryptów ma bezpośredni wpływ na bezpieczeństwo repozytorium.

## 1. Kontrakt

- **Plik konfiguracji** (`*.json`): `{"version": 1, "hooks": {"<Zdarzenie>": [{"type": "command", "command": "./.github/hooks/x.sh", "timeoutSec": N}]}}`.
  Zdarzenia PascalCase (`PreToolUse`, `PostToolUse`, `Stop`) dostają payload w snake_case (`tool_name`, `tool_input`, `cwd`,
  `stop_hook_active`). Czas to `timeoutSec` (nie `timeout`); nie dodawaj pól per system (`linux`, `osx`) — `command` jest wspólne.
- Wejście: JSON na **stdin**. Wyjście: JSON na **stdout** albo brak (= brak decyzji). Diagnostyka tylko na **stderr**.
- `PreToolUse`: decyzja `allow` | `ask` | `deny` z powodem. Skrypt zwraca **oba kształty naraz**: `permissionDecision` i
  `permissionDecisionReason` na najwyższym poziomie oraz to samo w `hookSpecificOutput` (z `hookEventName`). Dzięki temu działa
  zarówno parser formatu GitHub, jak i Claude Code. Test sprawdza, że oba kształty są zgodne.
- Kody wyjścia: `0` = sukces, `2` = odmowa dla `PreToolUse`; inny kod niezerowy to błąd hooka, który **nie blokuje** (fail-open),
  poza `PreToolUse`, które przy błędzie blokuje. Przekroczenie `timeoutSec` też nie blokuje — hook ma więc mieścić się w limicie
  z zapasem (suma limitów poleceń wewnątrz < `timeoutSec`).
- Hook formatujący (`format-csharp.sh`, `PostToolUse`) **nigdy nie blokuje** — ostrzega na `stderr` i zwraca `0`.
- Hook `Stop` (`verify-build.sh`) może zablokować zakończenie tury jednym obiektem `{"decision":"block","reason":"…"}`:
  gdy zmieniono `.cs`, `.csproj`, `.props`, `.slnx`, `.targets` lub `.editorconfig`, a `dotnet build` albo
  `dotnet format --verify-no-changes` nie przechodzi. Obowiązkowo: wyjście `0` przy `stop_hook_active == true` (bez
  zapętlenia), brak wyjścia, gdy nie ma zmian w kodzie, skrócony i oczyszczony powód (ścieżki względne, bez duplikatów,
  ≤ 1500 znaków), `VERIFY_BUILD_SKIP=1` pomija weryfikację, brak parsera lub gita to cichy `0`.

## 2. Tolerancja wejścia

Kontrakt JSON hooków nie jest publicznym API — pole może się zmienić lub nazywać inaczej.
Dlatego przy ekstrakcji danych:

- zbieraj wartości rekurencyjnie po kluczach (`command`, `terminalCommand`, `commandLine`,
  `file_path`, `path`, `uri`), nie pojedynczo po jednej ścieżce,
- przy błędzie parsowania zwracaj `0` (fail-open), nie blokuj losowo,
- obsługuj brak `jq`: fallback na `python3`, a bez obu parserów — wyjdź bez akcji.

## 3. Bezpieczeństwo

- **Bez zmiennych środowiskowych do ścieżek.** `cwd` = root repo; używaj ścieżek względnych.
- Nie hardkoduj sekretów ani ścieżek absolutnych.
- Sprawdzaj, że ścieżka jest **wewnątrz repozytorium**, zanim cokolwiek zmodyfikujesz:
  odrzucaj `../`, ścieżki absolutne spoza root i pliki nieistniejące.
- `rm`/`mv`/`cp` w hookach: najpierw walidacja zakresu, potem akcja.

## 4. Styl skryptu

- `set -uo pipefail` (bez `-e` — hook nie może się wywalić w połowie).
- Funkcje pomocnicze (`set_decision`, `check_*`) zamiast długiego bloku `if`-ów.
- Regexy w `[[ ]]`, iteracja po tablicach, cytowanie `"$var"`.
- Każda decyzja ma **czytelny powód** w `permissionDecisionReason` — użytkownik musi wiedzieć,
  co zablokował.

## 5. Testy regresyjne — obowiązkowe

Każdy hook ma towarzyszący `test-*.sh`:

- przypadki dozwolone (`allow`) — zwykłe polecenia deweloperskie,
- przypadki wymagające potwierdzenia (`ask`) — operacje o dużym zasięgu,
- przypadki blokowane (`deny`) — nieodwracalne i niebezpieczne,
- przypadki brzegowe: pusty stdin, niepoprawny JSON, brak pliku, ścieżka poza repo,
- idempotencja: hook nie może zmieniać pliku, który jest już poprawny.

Test musi kończyć się kodem `0` przy sukcesie i wypisywać `N passed, M failed`.

## 6. Zakres odpowiedzialności

- Guard blokuje to, co **nieodwracalne** (`rm -rf /`, `git push --force`, `git reset --hard`,
  `sudo` bez `-n`, `mkfs`, `curl | bash`).
- Guard **pyta** o to, co legalne, ale o dużym zasięgu (`git clean -fd`, `git branch -D`,
  `dotnet nuget push`, `rm -rf` poza artefaktami).
- Guard **przepuszcza** artefakty budowania (`bin`, `obj`, `artifacts`).
- Formatowanie należy do `PostToolUse`, nie do `PreToolUse`.
- Agent nie może modyfikować własnych hooków — guard blokuje zapis do `.github/hooks/`.
- Testy hooków muszą być hermetyczne: tymczasowe repo (`mktemp -d` + `trap`), atrapa `dotnet` w `PATH`, brak zapisu do
  prawdziwego repo. Dla hooka dotykającego `dotnet` jednorazowo sprawdź też zachowanie na prawdziwym `dotnet`
  (zepsuty `.cs` w kopii repo daje `block`, czysty stan daje brak wyjścia).
