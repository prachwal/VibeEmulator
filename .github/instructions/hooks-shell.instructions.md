---
applyTo: ".github/hooks/**/*.sh"
description: "Reguły dla skryptów hooków: kontrakt stdin/stdout, bezpieczeństwo, tolerancja wejścia, testy regresyjne."
---

# Skrypty hooków (`.github/hooks/*.sh`)

Hooki są **deterministyczne** — w odróżnieniu od instructions i skills zawsze się wykonają.
Dlatego jakość tych skryptów ma bezpośredni wpływ na bezpieczeństwo repozytorium.

## 1. Kontrakt

- Wejście: JSON hooka na **stdin**. Wyjście: JSON na **stdout** (albo brak = brak decyzji).
- `PreToolUse`: decyzja w `hookSpecificOutput.permissionDecision` = `allow` | `ask` | `deny`.
- Kody wyjścia: `0` = sukces, `2` = błąd blokujący, inne = ostrzeżenie nieblokujące.
- Hook formatujący (np. `format-csharp.sh`) **nigdy nie blokuje** — ostrzega na `stderr` i zwraca `0`.

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
