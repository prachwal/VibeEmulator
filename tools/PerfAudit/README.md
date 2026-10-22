# PerfAudit

Pomiary aktualnego modelu C64. Narzędzie generuje własne dane ROM (BASIC 8 KiB,
KERNAL 8 KiB, CHAR 4 KiB); nie wymaga oryginalnych ROM-ów ani assemblera.
Źródła programów: [NOP/JMP](../../docs/perf-test-program.asm) i
[program mieszany](../../docs/perf-test-program-full.asm); bajty używane przez narzędzie
są w [TestPrograms.cs](TestPrograms.cs). Opkody i sumy cykli pochodzą z
[nmos6502.csv](../../docs/cpu/opcodes/nmos6502.csv).

```bash
dotnet run -c Release --project tools/PerfAudit
dotnet run -c Release --project tools/PerfAudit -- --verify-only
dotnet run -c Release --project tools/PerfAudit -- --rounds 11 --frames 200 --iterations 1000000 --seed 255
```

`--help` opisuje opcje i kody wyjścia. Pomiar w Debug jest odrzucany.

## Scenariusze

| przypadek | jednostka | zakres |
|---|---|---|
| `Frame/nop` | ms/klatkę | maszyna z pętlą NOP/JMP i aktywnym badline VIC-II |
| `Frame/full + banking` | ms/klatkę | program mieszany; przed każdą klatką następna z 8 konfiguracji banków |
| `CpuTick/nop`, `CpuTick/full` | ns/tick CPU | sam rdzeń, bez ticków urządzeń, bez AEC i bez zmiany banków |
| `Bus/mixed + banking` | ns/iterację | zapis RAM, odczyt jednego z 16 adresów, tick portu; nowy bank co 256 iteracji |

Port `$00/$01` nie jest jeszcze podłączony w emulatorze. Dlatego przełączanie
banków wykonuje host przez `ProcessorPort.WriteData`. W pomiarze klatki opóźnienie
odlicza `C64Machine.Tick`; w pomiarze magistrali — `ProcessorPort.Tick`.
`Effective` zmienia się po trzecim ticku portu. W maszynie port tickuje po CPU,
więc dostęp CPU w tym trzecim cyklu korzysta jeszcze z poprzedniego banku.
Program mieszany pobiera kod ze stałego RAM pod `$0200` we wszystkich bankach.
Sprawdzenie banków odzwierciedla bieżący dekoder `C64Bus`, w tym CHAR pod `$1000`;
nie potwierdza zgodności mapy pamięci z fizycznym C64.

Przed pomiarem narzędzie sprawdza:

- pełną pętlę NOP/JMP: 5 cykli;
- pełną pętlę mieszaną: 59 cykli, w każdej z 8 konfiguracji;
- opóźnienie portu po zmianach danych i DDR oraz zgodność cache flag z `Effective`;
- wartości odczytów RAM/BASIC/KERNAL/CHAR/VIC/SID/CIA i wynikowe zapisy RAM;
- dwa zapisy `INC $0300` (stara i nowa wartość, także przy przepełnieniu bajtu);
- odczyt open bus i jego wartość po pętli.

Obserwator magistrali działa wyłącznie podczas weryfikacji. Pomiary używają osobnych
maszyn bez obserwatora. `sink` obejmuje stan CPU/pamięci albo sumę odczytów,
żeby wynik mierzonej pracy był obserwowalny. Seed zmienia dane i kolejność banków/adresów.
Stan jest kontynuowany między rundami; nie obejmują one resetu ani alokowania maszyn/ROM-ów.

## Metoda pomiaru

Każdy przypadek ma co najmniej 300 ms rozgrzewki i co najmniej 9 rund pomiaru.
Przypadki są przeplatane w rundach. Wynik zawiera minimum, medianę i maksymalną
liczbę bajtów zaalokowanych na wątku pomiarowym w jednej rundzie.
Nagłówek podaje runtime, architekturę, liczbę dostępnych CPU, load average i parametry.
Nie zmieniamy ustawień tieringu ani PGO.

Wyniki różnych scenariuszy opisują różne obciążenia. `Bus/mixed` obejmuje również
zapis i zegar portu, więc nie jest miarą samego `C64Bus.Read`.
Małe `--frames`/`--iterations` służą do szybkiego sprawdzenia działania; zwiększaj je,
aż koszt zegara i wywołania delegata przestanie wpływać na wynik.

Do porównania rewizji zbuduj **ten sam kod narzędzia** przeciw obu wersjom bibliotek
w Release. Na tej samej maszynie wykonaj co najmniej 9 par uruchomień naprzemiennie
`baza, nowa, baza, nowa`, z identycznymi parametrami. Zachowaj wyniki i obciążenie
systemu dla obu wersji; porównuj minimum i medianę dla tego samego przypadku.
Różnica klatki powyżej 3% wymaga wyjaśnienia. Pojedyncze uruchomienie obecnej wersji
nie potwierdza przyspieszeń z historycznego audytu.
