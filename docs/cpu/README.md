# Dokumentacja CPU: 6502, 65C02, 6510, 2A03

Jedno miejsce z wiedzą potrzebną do zbudowania procesora, który przechodzi testy Klausa Dormanna i testy NES
(`nestest`, testy Blargga). Zebrane 2026-10-21 ze źródeł pierwotnych; każdy plik podaje, skąd pochodzi dany fakt.

**Zasada:** fakt jest tu tylko wtedy, gdy źródło zostało pobrane i przeczytane albo sprawdzone programowo. Czego nie
sprawdzono, jest oznaczone **(niezweryfikowane)**. Dokumentacja opisuje zachowanie sprzętu i testów; to, co robi nasz kod
i jak daleko doszliśmy, jest w planie ([../plan-c64-cycle-exact.md](../plan-c64-cycle-exact.md)), a nie tutaj.

## Mapa

| plik | zawartość |
|---|---|
| [registers-and-flags.md](registers-and-flags.md) | rejestry, flagi (w tym flaga B), stos, wektory, stan po włączeniu i po resecie |
| [interrupts-and-reset.md](interrupts-and-reset.md) | IRQ, NMI, BRK, RESET tick po ticku; próbkowanie linii; opóźnienie po `CLI`/`SEI`/`PLP`; gałęzie; hijacking |
| [decimal-mode.md](decimal-mode.md) | algorytmy `ADC`/`SBC` w trybie dziesiętnym per wariant, flagi, cykle, 2A03 bez trybu dziesiętnego |
| [opcodes.md](opcodes.md) | 256 opkodów NMOS (z nieoficjalnymi) i 65C02; źródła, weryfikacja, rozbieżności, aliasy |
| [opcodes/nmos6502.csv](opcodes/nmos6502.csv), [opcodes/wdc65c02.csv](opcodes/wdc65c02.csv) | tabele maszynowe (opkod, mnemonik, tryb, bajty, cykle, potwierdzenia) |
| [tests.md](tests.md) | katalog testów: Dormann, `nestest`, Blargg, SingleStepTests; adresy, protokoły wyników, wymagania, szkic harnessu |
| [sources.md](sources.md) | wszystkie źródła: adres, co przeczytano, czego nie, licencje; historia poprawek dokumentacji |

Poza tym katalogiem: próbka wektorów SingleStepTests, jej licencja i opis przycięcia —
[tests/VibeEmulator.Mos6502.Tests/Vectors/README.md](../../tests/VibeEmulator.Mos6502.Tests/Vectors/README.md).

## Czego wymaga każdy test od CPU

Tabela opisuje testy, nie nasz stan. „Wymaga poza CPU” to infrastruktura, którą musi dostarczyć harness.

| zestaw | CPU musi umieć | wymaga poza CPU |
|---|---|---|
| Dormann: test funkcjonalny (NMOS) | wszystkie **oficjalne** opkody i tryby, flagi, tryb dziesiętny (chyba że zasemblowany z `disable_decimal`) | płaski RAM 64 KB, bez I/O |
| Dormann: test 65C02 | jw. + 61 nowych opkodów 65C02 i 44 niezdefiniowane opkody jako NOP; wymaga zaliczenia testu funkcjonalnego | płaski RAM 64 KB |
| Dormann: test przerwań | IRQ, NMI, BRK, RTI, `CLI`/`SEI`/`PLP`; w 65C02 także `WAI`, `STP` | RAM + **rejestr zwrotny** (`$BFFC`) sterujący liniami IRQ/NMI |
| Dormann: test dziesiętny (Bruce Clark) | `ADC`/`SBC` w trybie dziesiętnym, wszystkie 262 144 przypadki | 17 bajtów RAM; wynik w `ERROR` |
| `nestest` | wszystkie oficjalne opkody **i opkody nieoficjalne użyte przez test**; flagi, stos, gałęzie; bez trybu dziesiętnego | NROM; porównanie z `nestest.log` (z licznikiem cykli) |
| Blargg: `instr_test-v5` | oficjalne (`official_only`) i nieoficjalne (`all_instrs`) opkody | NROM, bez PPU; wynik w `$6000` |
| Blargg: `instr_timing`, `cpu_timing_test6`, `branch_timing_tests` | dokładny czas każdej instrukcji | **licznik długości APU** i NMI z PPU |
| Blargg: `cpu_interrupts_v2` | opóźnienia po `CLI`/`SEI`/`PLP`/`RTI`, NMI przerywające BRK i IRQ | **ramka IRQ z APU** i NMI z PPU |
| Blargg: `cpu_reset` | wartości rejestrów po włączeniu i po resecie, brak zapisów na stos | reset CPU |
| SingleStepTests/65x02 | dostępy do magistrali cykl po cyklu, każdy opkod (zestawy `6502`, `nes6502`, `wdc65c02`) | — (pliki JSON) |

## Stan implementacji, skrót (stan na 2026-10-21)

Pełny stan i liczby utrzymuje plan ([Stan realizacji](../plan-c64-cycle-exact.md) i „Warianty procesora”); tu tylko to, co
ważne dla testów.

- **Zakres opkodów:** rdzeń cycle-exact ma **57 wpisów NMOS** (56 opkodów + `BRK`) z 151 oficjalnych i **63 wpisy** w
  wariancie 65C02. Wektorami SingleStepTests potwierdzono **56 (NMOS) i 62 (65C02)** opkody; `BRK` (`$00`) nie jest
  weryfikowany.
- **Zakres weryfikacji:** pełne pliki (10 000 testów na opkod) puszczono niezależnym runnerem **poza repo**: NMOS 550 117 i
  65C02 610 025 testów bez niezgodności. W repo (i w testach) leży tylko **próbka: pierwsze 100 testów na opkod**.
  Przypadki `ADC`/`SBC #` z ustawioną flagą Decimal (9 883 i 9 975) są pomijane do etapu 2e. Czyli: poza trybem
  dziesiętnym zwalidowane w całości, ale w repo tylko na próbce.
- **`BRK` jest dziś niepełny:** to instrukcja jednobajtowa, która tylko ustawia flagę `B` w `P`: bez odłożenia na stos i
  bez skoku pod wektor. Sprzętowo flaga `B` nie istnieje w rejestrze ([registers-and-flags.md](registers-and-flags.md)).
  Sekwencja `BRK`/IRQ/NMI/RESET jest w całości do zrobienia (etap 2d).

### Braki względem testów

| brak | potrzebny dla | etap w planie |
|---|---|---|
| reszta ALU, przesunięcia i RMW, stos, gałęzie, `JSR`/`RTS`/`RTI`, `JMP (ind)` | Dormann, `nestest` | 2b, 2c, 2d |
| sekwencje IRQ/NMI/BRK/RESET (stos, wektory), próbkowanie linii, opóźnienia | test przerwań Dormanna, `cpu_interrupts_v2`, `cpu_reset` | 2d |
| tryb dziesiętny | test funkcjonalny i dziesiętny Dormanna | 2e |
| opkody nieoficjalne NMOS (105, w tym 12 KIL; `nestest` używa 10 rodzin) | `nestest`, `all_instrs`, `cpu_timing_test6` | faza 3 planu (nie rozpoczęta) |
| 61 nowych opkodów 65C02 i 44 niezdefiniowane opkody jako NOP | test 65C02 Dormanna | etap 3 |
| harnessy: Dormann (płaski RAM, pętla sukcesu), komparator `nestest.log`, rejestr zwrotny IRQ/NMI | wszystkie testy Dormanna i `nestest` | H1–H7 w planie |
| wariant 2A03 | zestawy NES | H6 w planie |

## 6510 i 2A03

Katalog nie ma osobnych dokumentów dla tych dwóch układów; poniżej to, co o nich wiadomo i gdzie to jest.

- **6510** = NMOS 6502 + port wejścia/wyjścia na chipie (`$00`/`$01`). Rdzeń zachowuje się jak NMOS (patrz
  [opcodes.md](opcodes.md)). Port jest dziś modelowany po stronie magistrali (`ProcessorPort`, opóźnienie 3 cykli) i **nie
  jest jeszcze podpięty pod adresy `$00`/`$01`**. Opis i reguły: plan, faza 4, oraz
  [.github/instructions/c64-emulation.instructions.md](../../.github/instructions/c64-emulation.instructions.md), pkt 4.
  Źródeł o porcie 6510 w tym katalogu nie przeczytano.
- **2A03** (Ricoh, NES) = rdzeń 6502 **bez trybu dziesiętnego**; wszystkie opkody nieoficjalne wykonują się jak na 6502
  ([decimal-mode.md](decimal-mode.md), [opcodes.md](opcodes.md)). Stan po włączeniu i resecie:
  [registers-and-flags.md](registers-and-flags.md). Wektory: zestaw `nes6502` ([tests.md](tests.md)). APU, PPU i DMA są
  poza zakresem tej dokumentacji.
