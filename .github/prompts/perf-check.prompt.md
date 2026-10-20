---
name: "Sprawdź wydajność"
description: "Zbuduj solucję w Release i uruchom benchmark Fibonacciego, a następnie zinterpretuj wyniki. Use when: weryfikacja wydajności po zmianie, porównanie implementacji, sprawdzenie czy optymalizacja nie jest regresją."
argument-hint: "n i liczba rund, np. 38 7 (domyślnie 38 7)"
agent: "agent"
tools: [execute/getTerminalOutput, execute/runInTerminal, read/readFile, read/problems]
---

# Weryfikacja wydajności

Wykonaj procedurę i zwróć raport. Nie modyfikuj kodu produkcyjnego bez wyraźnej prośby.

## 1. Zbuduj w Release

```bash
dotnet build -c Release --nologo -v q
```

Jeśli build się nie powiedzie — zatrzymaj się, pokaż błędy i nie uruchamiaj benchmarku.
Pomiar nieistniejącego binarium albo z Debug jest bezwartościowy.

## 2. Ocen szum pomiarowy

```bash
nproc; uptime
```

Podaj `load average` w raporcie. Jeśli load jest porównywalny z liczbą rdzeni, wynik obarczony
błędem — zaznacz to i rozważ zwiększenie liczby rund. Nie ukrywaj tego faktu.

## 3. Uruchom benchmark

Argumenty z `argument-hint` (jeśli podane), inaczej `38 7`:

```bash
dotnet run -c Release --project tools/Bench --no-build -- <n> <rounds>
```

Zasady, których nie wolno łamać:

- **Zawsze `-c Release`** — w Debug JIT nie optymalizuje i wynik jest fikcją.
- **Nie wyłączaj tieringu ani PGO** (`TieredCompilation`, `TieredPGO`).
- **Nie dodawaj `[MethodImpl(AggressiveOptimization)]`** — na metodach rekurencyjnych zmierzono
  pogorszenie ~20%, bo atrybut blokuje rejit z PGO.
- **Nie porównuj średnich** — raportuj minimum z rund (skrypt już to robi).
- Dla `n > 40` rekurencja naiwna jest pomijana automatycznie (rośnie jak φⁿ).

## 4. Zwróć raport

Format:

```text
n = <n>, rounds = <rounds>, load average = <x> / <rdzenie>

implementation             total [ms]      ns/call   vs fastest
----------------------------------------------------------------
<wyniki z benchmarku>
```

Następnie 2-4 zdania interpretacji:

- która implementacja jest najszybsza i o ile,
- czy różnica jest większa od szumu (przy wysokim load traktuj różnice < 10% jako nieistotne),
- czy wynik odbiega od poprzednich pomiarów w tym repo.

## 5. Jeśli wynik jest regresją

1. Sprawdź `git diff` pod kątem zmian w `src/Core/Fibonacci.cs` i `Directory.Build.props`.
2. Sprawdź, czy ktoś nie ustawił `TieredPGO=false` albo `Optimize=false`.
3. Dopiero potem proponuj zmianę — z liczbami przed i po.

## Odniesienie

Wyniki bazowe z tego repozytorium (i5-10400F, .NET 10, n = 38, best-of-rounds):

| implementacja | ns/wywołanie |
|---|---|
| FastDoubling O(log n) | ~10.7 |
| Iterative O(n) | ~21.7 |
| Memoized O(n) | ~613 |
| Recursive O(φⁿ) | ~137 000 000 |

Benchmark nie testuje ogólnej wydajności języka — mierzy narzut wywołań funkcji. Przy wnioskach
o „szybkości C#" zaznacz ten kontekst.
