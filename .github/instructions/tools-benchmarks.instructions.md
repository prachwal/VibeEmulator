---
applyTo: "tools/**/*.cs"
description: "Reguły dla narzędzi pomiarowych i diagnostycznych (tools/): Release, warmup, minimum z rund, brak optymalizacji psujących pomiar."
---

# Narzędzia w `tools/` — pomiary i diagnostyka

Te projekty są **narzędziami**, nie kodem produkcyjnym: mogą pisać do `Console`, mogą mieć
argumenty wiersza poleceń i nie wymagają testów jednostkowych. Ale podlegają rygorowi pomiarowemu.

## 1. Zawsze Release

- Każde polecenie pomiarowe w dokumentacji i w kodzie musi zawierać `-c Release`.
- W `Debug` JIT nie optymalizuje — wynik jest fikcją, nie pomiarem.
- Hook `guard-dangerous-commands.sh` wymaga potwierdzenia dla `dotnet run tools/Bench` bez
  `-c Release`. Nie obchodź tego warunku.

## 2. Metodyka pomiaru

- **Warmup** przed pomiarem: pierwsze wywołania idą przez tier-0 JIT.
- **Minimum z N rund**, nigdy średnia — obciążenie systemu tylko zawyża wynik.
- **Przeplataj warianty** w rundach, jeśli mierzysz kilka implementacji.
- **Nie dopuść do eliminacji kodu**: wynik trafia do zmiennej `sink`, argumenty pochodzą z `args`,
  nigdy ze stałych w pętli.
- Podaj `nproc` i `load average` w raporcie, jeśli wynik ma być porównywany między uruchomieniami.

## 3. Czego nie robić

- Nie wyłączaj `TieredCompilation` ani `TieredPGO` — zmierzono spadek odpowiednio ~43% i ~33%.
- Nie dodawaj `[MethodImpl(AggressiveOptimization)]`; wyłącza rejit z PGO.
- Nie porównuj czasów z różnych maszyn i różnych stanów obciążenia.
- Nie zmieniaj kilku rzeczy naraz w jednym pomiarze.

## 4. Struktura narzędzia

- `Harness`/wspólny kod pomiarowy trzymaj osobno od przypadków testowych.
- Tryb wybieraj argumentem (np. `dispatch|memory|cache|alu|blocks|cpu|all`), nie wieloma plikami
  `Main`.
- Pomiar i prezentacja rozdzielone: funkcja zwraca liczby, `Program` je formatuje.
- Dla narzędzi diagnostycznych (np. `C64Trace`) podawaj `--help` z przykładami i kody wyjścia:
  `0` = OK, `2` = błąd użycia (także błąd zapisu `--dump`, nieznany scenariusz, zły argument), `3` = CPU zawieszony
  (JAM) bez `--until jam`. Błędy wejścia dają kod 2 i jedną linię `error: …` na stderr, **bez** stack trace'a.
- Narzędzie czytane przez agenta: wynik na **stdout**, ostrzeżenia i nagłówki w trybach `vice`/`json` na **stderr**; format
  `json` to NDJSON (jedna linia = jeden obiekt, ostatnia linia to podsumowanie). Nieznane wejście nie może po cichu
  uruchomić domyślnego scenariusza. Zmiana tego kontraktu wymaga aktualizacji skilla `c64-trace-debug`, README i
  `CLAUDE.md`.
- Pomiar wydajności maszyny: minimum i mediana z ≥ 9 przebiegów **naprzemiennie** (baza, nowa, baza, nowa); próg
  istotności 3% dla klatki C64, a przekroczenie wymaga wyjaśnionej przyczyny w opisie zmiany.

## 5. Punkt odniesienia w tym repo

Zmierzone na i5-10400F, .NET 10.0.12, Release:

| miara | wartość |
|---|---|
| `Step` modelu CPU (atomowy) | ~11,8 ns/instrukcję |
| klatka C64 PAL (cycle-exact) | ~0,35 ms z budżetu 20 ms (**~1,7%**) |
| tick maszyny stanów | ~17,6 ns |
| dispatch: switch vs delegat | ~7,9 vs ~11,2 ns |

Gdy nowy pomiar odbiega o > 20%, najpierw sprawdź `load average` i czy build był w Release.
