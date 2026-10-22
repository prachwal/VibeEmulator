---
name: sprawdz-przed-commitem
description: "Pełna kontrola jakości przed commitem: build Debug i Release, testy, dotnet format, a przy zmianach w hookach lub serwerze MCP także ich testy. Zwraca raport z liczbami; nie commituje. Use when: przed commitem, po zakończeniu zadania, sprawdzenie czy repo jest zielone."
disable-model-invocation: true
argument-hint: "opcjonalnie zawężenie, np. tylko Mos6502 (domyślnie całe repo)"
---
# Kontrola przed commitem

Wykonaj kroki po kolei i zwróć raport. **Nie commituj, nie wypychaj i nie modyfikuj plików.** Zatrzymaj się po pierwszym
kroku, który się nie powiódł, i pokaż jego błędy (pierwsze ~20 linii zawierających `error` lub `FAIL`).

## 1. Co się zmieniło

```bash
git status --short
```

Zapamiętaj, czy zmieniono `.github/hooks/` lub `tools/obsidian-mcp/` (kroki 5 i 6).

## 2. Build

```bash
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
```

Oczekiwane: `0 Warning(s)` i `0 Error(s)` w obu. `TreatWarningsAsErrors=true`, więc ostrzeżenie jest błędem.

## 3. Testy

```bash
dotnet test VibeEmulator.slnx -c Release --nologo
```

Podaj liczby per projekt (zaliczone, nieudane, pominięte). **Pominięte testy wymień z opisem** (`Skip`): bez uzasadnienia
w kodzie to błąd. Zielona próbka wektorów w repo nie dowodzi zgodności pełnych zestawów — jeśli zmieniono zachowanie CPU,
napisz, czy pełne wektory uruchomiono.

## 4. Format i analizatory

```bash
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
```

## 5. Hooki (tylko gdy zmieniono `.github/hooks/`)

```bash
for t in .github/hooks/test-*.sh; do bash "$t" || echo "FAIL: $t"; done
```

## 6. Serwer MCP (tylko gdy zmieniono `tools/obsidian-mcp/`)

```bash
node tools/obsidian-mcp/test.js
```

## 7. Raport

```text
| krok | wynik |
|---|---|
| build Debug / Release | 0 ostrzeżeń, 0 błędów / … |
| testy | Cli x/x, Core x/x, Memory x/x, Mos6502 x/x, pominięte: … |
| format | czysto / zmiany w … |
| hooki / MCP | pominięte (bez zmian) / x passed, y failed |
```

Zakończ jednym zdaniem: czy repo jest gotowe do commita (wszystko zielone) czy nie i dlaczego. Bez pytań i ofert.
