# Dokumentacja CPU: 6502, 65C02, 6510, 2A03

Jedno miejsce z wiedzą potrzebną do zbudowania procesora, który przechodzi testy Klausa Dormanna i testy NES
(`nestest`, testy Blargga). Zebrane 2026-10-21 ze źródeł pierwotnych; każdy plik podaje, skąd pochodzi dany fakt.

**Zasada:** fakt jest tu tylko wtedy, gdy źródło zostało pobrane i przeczytane albo sprawdzone programowo. Czego nie
sprawdzono, jest oznaczone **(niezweryfikowane)**. Dokumentacja opisuje zachowanie sprzętu; to, co robi nasz kod, jest
w planie ([../plan-c64-cycle-exact.md](../plan-c64-cycle-exact.md)), a nie tutaj.

## Mapa

| plik | zawartość |
|---|---|
| [registers-and-flags.md](registers-and-flags.md) | rejestry, flagi (w tym flaga B), stos, wektory, stan po włączeniu i po resecie |
| [interrupts-and-reset.md](interrupts-and-reset.md) | IRQ, NMI, BRK, RESET tick po ticku; próbkowanie linii; opóźnienie po `CLI`/`SEI`/`PLP`; gałęzie; hijacking |
| [decimal-mode.md](decimal-mode.md) | algorytmy `ADC`/`SBC` w trybie dziesiętnym per wariant, flagi, cykle, 2A03 bez trybu dziesiętnego |
| [opcodes.md](opcodes.md) | 256 opkodów NMOS (z nieoficjalnymi) i 65C02; źródła, weryfikacja, rozbieżności, aliasy |
| [opcodes/nmos6502.csv](opcodes/nmos6502.csv), [opcodes/wdc65c02.csv](opcodes/wdc65c02.csv) | tabele maszynowe (opkod, mnemonik, tryb, bajty, cykle, potwierdzenia) |
| [tests.md](tests.md) | katalog testów: Dormann, `nestest`, Blargg, SingleStepTests; adresy, protokoły wyników, wymagania, szkic harnessu |
| [sources.md](sources.md) | wszystkie źródła: adres, co przeczytano, czego nie, licencje |

## Czego wymaga każdy test od CPU

| zestaw | CPU musi umieć | harness |
|---|---|---|
| Dormann, test funkcjonalny (NMOS) | wszystkie **oficjalne** opkody i tryby, flagi, tryb dziesiętny (chyba że zasemblowany z `disable_decimal`) | płaski RAM 64 KB, bez I/O |
| Dormann, 65C02 (rozszerzone opkody) | jw. + 61 nowych opkodów 65C02, nieudokumentowane jako NOP; wymaga zaliczenia testu funkcjonalnego | płaski RAM 64 KB |
| Dormann, test przerwań | IRQ, NMI, BRK, RTI, `CLI`/`SEI`/`PLP`; w 65C02 także `WAI`, `STP` | RAM + **rejestr zwrotny** (`$BFFC`) sterujący liniami IRQ/NMI |
| Dormann, test dziesiętny (Bruce Clark) | `ADC`/`SBC` w trybie dziesiętnym, wszystkie 262 144 przypadki | RAM 17 bajtów zmiennych; wynik w `ERROR` |
| `nestest` | wszystkie oficjalne opkody **i opkody nieoficjalne używane przez test**; flagi, stos, gałęzie; bez trybu dziesiętnego | CPU + NROM; porównanie z `nestest.log` (z licznikiem cykli) |
| Blargg `instr_test-v5` | oficjalne (`official_only`) i nieoficjalne (`all_instrs`) opkody | NROM, bez PPU, wynik w `$6000` |
| Blargg `instr_timing`, `cpu_timing_test6`, `branch_timing_tests` | dokładny czas każdej instrukcji | wymagają **licznika długości APU** i NMI z PPU |
| Blargg `cpu_interrupts_v2` | opóźnienia po `CLI`/`SEI`/`PLP`/`RTI`, NMI przerywające BRK i IRQ | wymaga **ramki IRQ z APU** i NMI z PPU |
| Blargg `cpu_reset` | wartości rejestrów po włączeniu i po resecie, brak zapisów na stos | reset CPU |
| SingleStepTests/65x02 | dostępy do magistrali cykl po cyklu, każdy opkod (zestawy `6502`, `nes6502`, `wdc65c02`) | już używane (etapy 1–2a) |

## Co mamy, a czego brakuje (stan na 2026-10-21)

Rdzeń cycle-exact: 57 opkodów NMOS z 151 oficjalnych (63 w wariancie 65C02), bez przerwań, bez trybu dziesiętnego, bez
opkodów nieoficjalnych. Przejście wektorów: NMOS 550 117 i 65C02 610 025 testów bez niezgodności (56 i 62 opkody).

| brak | potrzebny dla | etap w planie |
|---|---|---|
| reszta ALU, przesunięcia i RMW, stos, gałęzie, `JSR`/`RTS`/`RTI`, `JMP (ind)` | Dormann, `nestest` | 2b, 2c, 2d |
| sekwencje IRQ/NMI/BRK/RESET, próbkowanie linii, opóźnienia | test przerwań Dormanna, `cpu_interrupts_v2`, `cpu_reset` | 2d |
| tryb dziesiętny | test funkcjonalny i dziesiętny Dormanna | 2e |
| opkody nieoficjalne NMOS (105, w tym 12 KIL) | `nestest`, `all_instrs`, `cpu_timing_test6` | faza 3 planu (nie rozpoczęta) |
| 61 opkodów 65C02, nieudokumentowane jako NOP | test 65C02 Dormanna | etap 3 |
| wariant 2A03 (bez trybu dziesiętnego) | zestawy NES | **nie zaplanowany** |
| **harness Dormanna** (płaski RAM, wykrycie pętli sukcesu) | wszystkie testy Dormanna | **nie zaplanowany** |
| **komparator `nestest.log`** (rejestry i cykle) | `nestest` | **nie zaplanowany** |
| rejestr zwrotny IRQ/NMI w harnessie | test przerwań Dormanna | **nie zaplanowany** |

## Poprawka błędu w dokumentacji repo

Plan twierdził, że test Dormanna kończy się „kodem sukcesu w `$0000`”. To nieprawda: test funkcjonalny kończy się
pętlą `jmp *` pod adresem `$3469`, a test 65C02 pod `$24F1` (patrz [tests.md](tests.md)).
