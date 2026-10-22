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

## Stan implementacji, skrót (stan na 2026-10-22)

Pełny stan i liczby utrzymuje plan ([Stan realizacji](../plan-c64-cycle-exact.md) i „Warianty procesora”); tu tylko to, co
ważne dla testów.

- **Zakres opkodów:** liczby wpisów wariantów i to, które opkody potwierdzono wektorami, utrzymuje plan
  ([Stan realizacji](../plan-c64-cycle-exact.md) i „Warianty procesora”). Nie powtarzaj ich tutaj — po zmianie
  tabeli wariantu popraw plan, a zgodność z `docs/cpu/opcodes/*.csv` sprawdź przez `tools/OpcodeInfo --verify-code`.
- **Zakres weryfikacji:** pełne pliki (10 000 testów na opkod) puszczono niezależnym runnerem **poza repo**;
  liczby i wynik powtórzone 2026-10-22 trzyma tabela „Status wariantów” niżej. W repo (i w testach) leży tylko
  **próbka: pierwsze 100 testów na opkod**.
- **`BRK` i przerwania (etap F, 2026-10-21):** BRK to pełna sekwencja siedmiu cykli (sygnatura, pushy PCH/PCL/P|B=1,
  wektor `$FFFE`/`$FFFF`) — zwalidowana wektorami (próbka + pełne 2 × 10 000). IRQ (poziom, maskowane `I`, opóźnienie po
  `CLI`/`SEI`/`PLP`) i NMI (zbocze, wektor `$FFFA`) wchodzą przez piny `IInterruptLines`; 65C02 czyści `D` po odłożeniu `P`
  ([interrupts-and-reset.md](interrupts-and-reset.md)). Test przerwań Dormanna (H4) jest uruchomiony
  i zielony (etap O), a polaryzacja rejestru zwrotnego `$BFFC` potwierdzona przebiegiem.
  **Brakuje** wyłącznie sekwencji RESET i hijackingu wektorów.

## Status wariantów (2026-10-22)

Wszystkie liczby poniżej pochodzą z przebiegów wykonanych **w tej dacie**, nie z notatek etapowych:
pełne wektory puszczono jednym skryptem po całym zestawie (`256 × 10 000` itd.), a testy binarne uruchomiono
`tools/CpuHarness` na obrazach pobranych poza repo.

### Zgodność z wektorami referencyjnymi (SingleStepTests/65x02)

| wariant | tabela opkodów | pełne wektory | wynik |
|---|---|---|---|
| NMOS 6502 / 6510 | 256/256 bajtów (151 oficjalnych, 93 nieoficjalne, 12 KIL) | 256 × 10 000 | **2 560 000 / 2 560 000, 0 błędnych opkodów** |
| CMOS 65C02 (WDC) | 256/256 bajtów (baza NMOS nadpisana dodatkami) | 254 × 10 000 | **2 540 000 / 2 540 000, 0 błędnych opkodów**; `cb`/`db` bez wektorów (pliki źródłowe 0 B) |
| Ricoh 2A03 (NES) | 256/256 (ta sama tabela co NMOS, bez trybu dziesiętnego) | 256 × 10 000 | **2 560 000 / 2 560 000, 0 błędnych opkodów** |
| **razem** | — | **766 plików** | **7 660 000 testów, 0 niezgodności** |

Zakres porównania: stan końcowy rejestrów, pamięć końcowa **oraz sekwencja dostępów do magistrali cykl po
cyklu** (adres, wartość, kierunek). Próbki w repo to pierwsze 100 testów na opkod — testy w CI sprawdzają
766 × 100 = 76 600 przypadków; pełne zestawy są poza repo (≈3 GB).

### Testy binarne (programy, nie pojedyncze opkody, wszystkie 2026-10-22)

| test | wariant | wynik | dane |
|---|---|---|---|
| Dormann `6502_functional_test` | NMOS | **pass** (`PC = $3469`) | 96 241 367 cykli, 30 646 177 instrukcji |
| Dormann `65C02_extended_opcodes_test` | 65C02 | **pass** (`PC = $24F1`) | 66 907 072 cykle, 21 986 986 instrukcji |
| Dormann `6502_interrupt_test` | NMOS | **pass** (`PC = $06F5`) | 3 016 cykli, 24 zapisy `$BFFC` |
| Dormann `6502_interrupt_test` (`D_clear = 1`) | 65C02 | **pass** (`PC = $0719`) | 3 058 cykli |
| Dormann/Clark `6502_decimal_test` (`cputype = 0`) | NMOS | **pass** (`ERROR = 0`) | 53 953 828 cykli |
| Dormann/Clark `6502_decimal_test` (`cputype = 1`) | 65C02 | **pass** (`ERROR = 0`) | 56 640 804 cykle |
| `nestest` + `nestest.log` | 2A03 | **pass**, 8991/8991 linii | 26 553 cykli, porównanie `PC A X Y P SP CYC` |
| Blargg `instr_test-v5/rom_singles` | 2A03 | **15 / 16 pass** | porażka: `03-immediate`, powód `ATX` (`$AB`) |

### Co „100%” znaczy, a co nie

- **100% zgodności z wektorami referencyjnymi — tak.** Wszystkie 766 opkodów przechodzi pełne pliki bez
  jednej niezgodności, w tym sekwencje dostępów.
- **100% zgodności ze „wszystkimi oryginałami” — nie jest twierdzone i nie da się tego twierdzić.** Trzy
  powody, wszystkie jawnie zapisane:
  1. **Opkody niestabilne** (`AHX`, `SHX`, `SHY`, `TAS`, `LAS`, `XAA`, `LAX #imm`, `SBC $EB`) zachowują się
     różnie na różnych egzemplarzach 6502. Model odwzorowuje **wariant z wektorów**, nie każdy chip.
  2. **Blargg `03-immediate` oczekuje innego wariantu `ATX`** niż wektory (`(A | 0xEE) & imm` vs
     `imm & (0xEE | (A & 0x11))`). Wektory są źródłem prawdy w tym repozytorium, więc test nie przechodzi
     i tak pozostaje.
  3. **Wektory są implementacją referencyjną, nie zapisem krzemu.** Zgodność z wektorami to najmocniejszy
     dostępny dowód, ale nie to samo co pomiar na układzie.
- **Czego nie zweryfikowano wcale:** sekwencja RESET i hijacking wektorów (brak kodu), testy Blargga
  wymagające APU/PPU (poza harnessem), `all_instrs.nes`/`official_only.nes` (mapper 1).

### Co jeszcze nie jest zweryfikowane

| brak | potrzebny dla | uwagi |
|---|---|---|
| sekwencja RESET (odczyty zamiast zapisów), hijacking wektorów | `cpu_reset`, `cpu_interrupts_v2` | jedyny brak kodu po stronie CPU; etap F nie obejmował RESET |
| testy Blargga wymagające czasu (APU) i przerwań (PPU) | `instr_timing`, `cpu_timing_test6`, `cpu_interrupts_v2`, `branch_timing_tests` | wymagają emulacji systemu NES, nie samego CPU — poza zakresem harnessu |
| `all_instrs.nes`, `official_only.nes` | pełny zestaw instrukcji Blargga | pliki to **mapper 1 (MMC1)**; harness obsługuje NROM, więc użyto `rom_singles` |
| `ATX` (`$AB`, `LAX #imm`) | Blargg `03-immediate` | opkod niestabilny: wektory i Blargg oczekują różnych wariantów; wybrano wektory |
| zachowanie innych egzemplarzy niestabilnych opkodów | — | model odwzorowuje wariant z wektorów, nie każdy egzemplarz 6502 |

Wszystko pozostałe z listy „Czego wymaga każdy test od CPU” jest zaimplementowane i uruchomione — wyniki
per wariant: [Status wariantów](#status-wariantów-2026-10-22).

## 6510 i 2A03

Katalog nie ma osobnych dokumentów dla tych dwóch układów; poniżej to, co o nich wiadomo i gdzie to jest.

- **6510** = NMOS 6502 + port wejścia/wyjścia na chipie (`$00`/`$01`). Rdzeń zachowuje się jak NMOS (patrz
  [opcodes.md](opcodes.md)). Port jest dziś modelowany po stronie magistrali (`ProcessorPort`, opóźnienie 3 cykli) i **nie
  jest jeszcze podpięty pod adresy `$00`/`$01`**. Opis i reguły: plan, faza 4, oraz
  [.github/instructions/c64-emulation.instructions.md](../../.github/instructions/c64-emulation.instructions.md), pkt 4.
  Źródeł o porcie 6510 w tym katalogu nie przeczytano.
- **2A03** (Ricoh, NES) = rdzeń 6502 **bez trybu dziesiętnego**; wszystkie opkody nieoficjalne wykonują się jak na 6502
  ([decimal-mode.md](decimal-mode.md), [opcodes.md](opcodes.md)). Realizacja: `CpuVariants.Nes2A03`
  (etap P, właściwość `HasDecimalMode`). Stan po włączeniu i resecie:
  [registers-and-flags.md](registers-and-flags.md). Wektory: zestaw `nes6502` ([tests.md](tests.md)). APU, PPU i DMA są
  poza zakresem tej dokumentacji.
