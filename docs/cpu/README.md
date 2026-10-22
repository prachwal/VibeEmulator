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

- **Zakres opkodów:** liczby wpisów wariantów i to, które opkody potwierdzono wektorami, utrzymuje plan
  ([Stan realizacji](../plan-c64-cycle-exact.md) i „Warianty procesora”). Nie powtarzaj ich tutaj — po zmianie
  tabeli wariantu popraw plan, a zgodność z `docs/cpu/opcodes/*.csv` sprawdź przez `tools/OpcodeInfo --verify-code`.
- **Zakres weryfikacji:** pełne pliki (10 000 testów na opkod) puszczono niezależnym runnerem **poza repo**. Od etapu G
  (2026-10-21) **bez pominięć**: NMOS 1 589 907 i 65C02 1 649 784 testów, wszystkie porównane w całości (wcześniej
  pomijano `ADC`/`SBC` z D=1 — 80 093 / 80 216 przypadków — do etapu 2e). W repo (i w testach) leży tylko
  **próbka: pierwsze 100 testów na opkod**.
- **`BRK` i przerwania (etap F, 2026-10-21):** BRK to pełna sekwencja siedmiu cykli (sygnatura, pushy PCH/PCL/P|B=1,
  wektor `$FFFE`/`$FFFF`) — zwalidowana wektorami (próbka + pełne 2 × 10 000). IRQ (poziom, maskowane `I`, opóźnienie po
  `CLI`/`SEI`/`PLP`) i NMI (zbocze, wektor `$FFFA`) wchodzą przez piny `IInterruptLines`; 65C02 czyści `D` po odłożeniu `P`
  ([interrupts-and-reset.md](interrupts-and-reset.md)). **Brakuje** sekwencji RESET i hijackingu wektorów;
  test przerwań Dormanna (H4) nie był uruchamiany (binarium GPL do zasemblowania).

### Braki względem testów

| brak | potrzebny dla | etap w planie |
|---|---|---|
| reszta ALU, przesunięcia i RMW, stos, gałęzie, `JSR`/`RTS`/`RTI`, `JMP (ind)` | Dormann, `nestest` | 2b, 2c, 2d |
| sekwencja RESET (odczyty zamiast zapisów), hijacking wektorów | `cpu_reset`, `cpu_interrupts_v2` | 2d (etap F nie obejmował RESET) |
| opkody nieoficjalne NMOS i 12 KIL — **zaimplementowane** (etapy J i K) | `nestest`, `all_instrs`, `cpu_timing_test6` | brak kodu; pozostaje uruchomienie harnessów |
| 61 nowych opkodów 65C02 i 44 niezdefiniowane opkody jako NOP — **zaimplementowane** (etapy H i I) | test 65C02 Dormanna | brak kodu |
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
