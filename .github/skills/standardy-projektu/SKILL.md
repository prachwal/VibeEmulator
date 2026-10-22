---
name: standardy-projektu
description: "Precyzyjne standardy projektu Vibe Emulator w jednym miejscu: kodowanie C# (styl, warstwy, pliki partial, testy, definicja ukończenia), optymalizacja (kolejność działań, zakazy JIT, metodyka pomiaru, raport) i używanie wektorów testowych SingleStepTests (próbka vs pełny zestaw, dodawanie plików, raportowanie; planowo Z80). Use when: piszesz lub recenzujesz kod w src/ lub tests/, optymalizujesz gorącą ścieżkę albo mierzysz wydajność, dodajesz opkod, dodajesz lub zmieniasz pliki w Vectors/, raportujesz wynik wektorów."
---
# Standardy projektu

Skrót obowiązujących reguł, zebrany pod jednym adresem dla każdego agenta (Copilot, opencode, Claude Code).
**Źródłem prawdy są pliki w `.github/instructions/`, `.editorconfig`, `Directory.Build.props` i `docs/`.**
Przy sprzeczności wygrywa źródło, a ten skill trzeba poprawić. Każda reguła niżej wskazuje swoje źródło.

## Co przeczytać

| zadanie | plik |
|---|---|
| piszesz lub recenzujesz kod w `src/`, `tests/`, `tools/` | [kodowanie.md](kodowanie.md) |
| optymalizujesz, mierzysz wydajność, oceniasz „czy to szybsze" | [optymalizacja.md](optymalizacja.md) |
| dotykasz `Vectors/`, dodajesz opkod, raportujesz wynik wektorów | [wektory.md](wektory.md) |

Zadanie zwykle dotyka kilku z nich: nowy opkod to kodowanie **i** wektory; optymalizacja rdzenia to optymalizacja
**i** wierność cyklowa z kodowania.

## Pięć reguł, które wolno łamać tylko na wyraźne polecenie użytkownika

1. **Zachowanie CPU nie z pamięci.** Opkody, cykle, dummy read i flagi bierz z wektorów SingleStepTests i `docs/cpu/`.
   Czego tam nie ma, oznacz jako **(niezweryfikowane)**.
2. **Warstwy.** `Memory` i `Cpu.Abstractions` nie znają 6502, `Mos6502` nie zna C64. Różnica wariantów to dane
   (`CpuVariant`), nie `if`.
3. **Zielony build, zero ostrzeżeń** (`TreatWarningsAsErrors`). Nie obiecuj zakończenia z czerwonym buildem,
   pominiętym testem ani ostrzeżeniem.
4. **Pomiar zamiast intuicji.** Twierdzenie „szybciej" wymaga liczb z `Release`, po rozgrzewce, z wielu przebiegów.
5. **Próbka to nie pełny zestaw.** Zielone 100 testów na opkod nie znaczy „zwalidowane". Wynik pełnych wektorów
   podawaj osobno albo napisz, że ich nie uruchomiono.

Dodatkowo: **nie commituj ani nie wypychaj bez wyraźnej prośby**, a recenzja pliku to sam raport (bez pytania
o poprawki i bez ich nanoszenia) — `AGENTS.md`, `.github/copilot-instructions.md`.

## Format odpowiedzi o wyniku

Liczby, nie przymiotniki: „1081 testów zielonych, 0 ostrzeżeń", „minimum z 9 rund: 0,423 ms (baza 0,442)",
„próbka 100 testów w repo, pełne wektory nie uruchomione". Nigdy „powinno działać".

## Rozbieżności w źródłach (stan na 2026-10-22, sprawdzone na plikach)

Nie kopiuj ich dalej; zgłoś, gdy dotykasz tych plików.

| gdzie | rozbieżność | co stosować |
|---|---|---|
| liczba rund pomiaru | `tools-benchmarks.instructions.md` i `tools/PerfAudit`: ≥ 9; skill `csharp-perf-benchmark`: 5–7; skill `perf-check`: 5 | pomiar maszyny i rdzenia: **≥ 9**; mikropomiar Fibonacciego z `tools/Bench` zostaje przy jego domyślnych |
| numer sekcji o `partial` | `copilot-instructions.md` odsyła do „pkt 8", w `csharp.instructions.md` to sekcja **7** | sekcja 7 |
| katalogi wektorów | `single-step-vectors.instructions.md` wymienia `nmos6502/` i `wdc65c02/`, a w repo jest też `nes6502/` | trzy katalogi (patrz [wektory.md](wektory.md)) |
| wyjątek dziesiętny w wektorach | komentarz w `SingleStepVectorTests.cs` mówi o wyjątku ADC/SBC z `D=1`, `csharp-tests.instructions.md` mówi, że od etapu G go nie ma | brak wyjątków |
| bezwzględny czas klatki | `tools-benchmarks.instructions.md`: ~0,35 ms; `docs/perf-audit.md`: ~0,44 ms | nie porównuj liczb między dokumentami, tylko bazę z nową wersją na jednej maszynie |
