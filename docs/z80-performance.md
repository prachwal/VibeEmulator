# Wydajność rdzenia Z80

## Plan i wynik

Profil CPU dla przebiegu ZEX oraz osobnego benchmarku rdzenia wskazał, że warto mierzyć pobieranie i dekodowanie instrukcji (`TickOpcodeFetch`), przygotowanie następnego cyklu (`PrepareFollowingCycle`) i obsługę odczytu sterowania (`CompleteControlRead`). Pierwszy test pokazał, że samo przesunięcie przypadku `NOP` nie daje stabilnego zysku.

Wprowadzona zmiana skraca `GetInstructionTStates` dla częstego zakresu `LD r,r` i ALU rejestrowego, omijając ogólny dekoder sterowania. Usunięto też powtarzany zapis `OpcodeFetch` w każdym T-state pobierania: cykl ustawiany jest już w `BeginInstruction`, a obsługa pobierania nie zmienia go.

## Benchmark

`tools/Z80PerfAudit` wykonuje dwa programy asemblerowe z [z80-perf-test-program.asm](z80-perf-test-program.asm) na płaskiej pamięci i bez harnessu CP/M:

| Przypadek | Kod | Cykle na iterację |
|---|---|---:|
| `z80-nop-jp` | `NOP; JP 0000h` | 14 T-state |
| `z80-register-alu` | `LD A,B; ADD A,C; INC D; XOR E; JP 0000h` | 26 T-state |

Uruchomienie:

```bash
dotnet build tools/Z80PerfAudit/VibeEmulator.Z80PerfAudit.csproj -c Release
dotnet tools/Z80PerfAudit/bin/Release/net10.0/VibeEmulator.Z80PerfAudit.dll
```

Benchmark rozgrzewa każdą pętlę przez 8 mln T-state, a potem wykonuje 9 pomiarów po 4 mln T-state. `sink` zależy od stanu CPU i licznika cykli. Porównanie wykonano w 9 naprzemiennych parach baza/zmiana, w Release, na tej samej maszynie. Każda liczba w tabeli to mediana odpowiednich 9 wyników z przebiegów benchmarku: osobno median pomiarowych i osobno minimów.

| Przypadek | Baza: min / mediana (ns/T-state) | Zmiana: min / mediana (ns/T-state) | Zmiana mediany |
|---|---:|---:|---:|
| `z80-nop-jp` | 17,126 / 18,988 | 16,775 / 18,620 | −1,9% |
| `z80-register-alu` | 24,775 / 26,204 | 22,610 / 24,299 | −7,3% |

Maszyna: Intel Core i5-10400F, 8 logicznych CPU, .NET 10.0.12, Release. Przy pomiarach load average wynosił około 4,9 / 9,8 / 8,2, więc obciążenie tła było wysokie. Wyniki wskazują na wyraźniejszy zysk w rejestrowej ścieżce ALU, a pętla `NOP/JMP` poprawiła się nieznacznie, poniżej progu 3%. To mikrobenchmark rdzenia, nie prognoza przyspieszenia całej maszyny ani pełnego ZEX.

## Dalsze kandydatury

Po tej zmianie następny pomiar powinien objąć pełne obciążenie ZEXDOC/ZEXALL oraz instrukcje pamięciowe, prefiksy i porty. Dopiero potem warto rozważać przebudowę dekodera lub zmianę wywołań magistrali; wymagałoby to osobnych benchmarków oraz pełnego zestawu testów i wektorów.
