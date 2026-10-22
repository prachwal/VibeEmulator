# Testy: Dormann, nestest, Blargg, SingleStepTests

Wszystkie liczby, adresy i protokoły poniżej pochodzą z plików testów pobranych i przeczytanych 2026-10-21 (źródła i
licencje: [sources.md](sources.md)). Wymagania od CPU: [README.md](README.md).

## Przegląd

| zestaw | typ | wynik | wymaga poza CPU |
|---|---|---|---|
| Klaus Dormann: `6502_functional_test` | NMOS, opkody udokumentowane | pętla `PC` pod adresem sukcesu | 64 KB RAM |
| Klaus Dormann: `65C02_extended_opcodes_test` | 65C02, dodatkowe opkody | pętla `PC` | 64 KB RAM |
| Klaus Dormann: `6502_interrupt_test` | IRQ, NMI | pętla `PC` | RAM + rejestr zwrotny |
| Klaus Dormann: `6502_decimal_test` (Bruce Clark) | tryb dziesiętny | bajt `ERROR` | RAM |
| `nestest` (Kevin Horton) | CPU NES, oficjalne i część nieoficjalnych | log, bajty `$02`/`$03` | NROM |
| Blargg: `instr_test-v5`, `instr_misc` | zachowanie instrukcji | `$6000` | NROM (bez PPU) |
| Blargg: `instr_timing`, `cpu_timing_test6`, `branch_timing_tests` | czas instrukcji | `$6000`, ekran, dźwięk | APU (licznik długości), NMI z PPU |
| Blargg: `cpu_interrupts_v2` | przerwania | `$6000` | APU (ramka IRQ), NMI z PPU |
| Blargg: `cpu_reset` | stan po włączeniu i resecie | `$6000` | reset |
| SingleStepTests/65x02 | każdy opkod, dostępy cykl po cyklu | JSON | — |

## Klaus Dormann: 6502_65C02_functional_tests

Repozytorium: <https://github.com/Klaus2m5/6502_65C02_functional_tests>. **Licencja GPL-3.0** (`license.txt`): to ważne,
jeśli binaria miałyby trafić do repozytorium; źródło testu dziesiętnego Bruce'a Clarka jest w domenie publicznej.

Pliki w repozytorium: `6502_functional_test.a65`, `65C02_extended_opcodes_test.a65c`, `6502_interrupt_test.a65`,
`6502_decimal_test.a65`, `as65_142.zip` (asembler AS65), `report.i65`, `readme.txt`, oraz `bin_files/` z **gotowymi
obrazami tylko dla testu funkcjonalnego i 65C02**: `6502_functional_test.bin`, `65C02_extended_opcodes_test.bin`
(po 65 536 bajtów) wraz z listingami `.lst`. **Dla testu przerwań i dziesiętnego nie ma gotowych binariów**; trzeba je
zasemblować (AS65 jest w repozytorium; readme wskazuje też wersję zgodną z CA65 autora amb5l).

### Test funkcjonalny (NMOS)

Źródło: `6502_functional_test.a65` (wersja 05-jan-2020), listing `.lst`.

| element | wartość |
|---|---|
| obraz | 65 536 bajtów, ładowany od `$0000` (listing: „edited to provide binaries loading from 0”) |
| `zero_page` | `$0A` (potrzeba `$52` bajtów) |
| `data_segment` | `$0200` (potrzeba `$7B` bajtów; młodszy bajt adresu musi być `$00`) |
| `code_segment` | **`$0400`** (13,1 kB) |
| start | ustaw **`PC = $0400`** ręcznie; wektor resetu w obrazie wskazuje pułapkę, więc nie używaj sekwencji resetu |
| sukces | pętla `jmp *` pod adresem **`$3469`** (listing, linia 13377: „test passed, no errors”) |
| porażka | pętla `PC` w którymkolwiek innym miejscu (`jmp *` lub gałąź `*`); adres wskazuje test w listingu |
| I/O | **brak** („No IO - should be run from a monitor with access to registers”) |
| wektory | w obrazie: `NMI` i `RESET` na pułapki (`dw nmi_trap`, `dw res_trap` pod `$FFFA`); RESET, NMI i IRQ **nie powinny wystąpić** |

Konfiguracja źródła (domyślna): `I_flag = 3` (zmiana flagi `I` dozwolona, potrzebna do testu `SEI`/`CLI`),
`disable_selfmod = 0` (część kodu modyfikuje się, więc musi leżeć w RAM), `report = 0` (pętle zamiast raportu),
`ram_top = −1` (bez kontroli zapisów), `disable_decimal = 0`. Test sprawdza **tylko udokumentowane** zachowanie NMOS:
żadnych opkodów nieoficjalnych, w trybie dziesiętnym tylko poprawne operandy BCD, flagi `N`/`V`/`Z` ignorowane.
Uwaga autora: we wczesnych testach niektóre instrukcje muszą być użyte, zanim zostaną przetestowane.

### Test 65C02

Źródło: `65C02_extended_opcodes_test.a65c` (wersja 04-dec-2017), listing `.lst`. Sukces: pętla `jmp *` pod **`$24F1`**.
Obraz 65 536 bajtów; start `PC = $0400`. Testuje wszystkie dodatkowe opkody 65C02, tryby i funkcjonalność niedostępną w
NMOS; **wymaga wcześniejszego zaliczenia testu funkcjonalnego**. NMI, IRQ, `STP` i `WAI` pokrywa test przerwań.
Opcje wariantu: `wdc_op`, `rkwl_wdc_op`, `skip_nop` (np. „NOPs not available on Rockwell & WDC 65C02”,
„NOPs not available on WDC 65C02 (WAI, STP)”).

### Test przerwań

Opis i wymagania harnessu: [interrupts-and-reset.md](interrupts-and-reset.md) (rejestr zwrotny `$BFFC`, bity IRQ = 0 i
NMI = 1, `I_drive`, `I_filter`, `D_clear`). *Dokładna polaryzacja rejestru zwrotnego i znaczenie filtra: niezweryfikowane;
czytaj komentarze i kod w `6502_interrupt_test.a65`.* Wymaga zapisywalnych wektorów i zmienialnej flagi `I`.

### Test dziesiętny

Opis, konfiguracja (`cputype`, `vld_bcd`, `chk_*`) i algorytmy: [decimal-mode.md](decimal-mode.md). Wynik: `ERROR` (0 = sukces).
Wymaga 17 bajtów RAM; ok. minuty przy 1 MHz.

### Uruchomienie w repo

Harness: `tools/CpuHarness` (etap L). Obrazy **nie są w repozytorium** (GPL-3.0); pobiera się je z
`bin_files/` repozytorium źródłowego do katalogu tymczasowego.

```bash
dotnet build tools/CpuHarness -c Release
dotnet tools/CpuHarness/bin/Release/net10.0/VibeEmulator.CpuHarness.dll \
    --image /tmp/dormann/6502_functional_test.bin \
    --variant nmos --pc 0x0400 --success-pc 0x3469 --limit-cycles 100000000

# 65C02 (wymaga wcześniejszego zaliczenia testu funkcjonalnego NMOS)
dotnet tools/CpuHarness/bin/Release/net10.0/VibeEmulator.CpuHarness.dll \
    --image /tmp/dormann/65C02_extended_opcodes_test.bin \
    --variant c02 --pc 0x0400 --success-pc 0x24F1 --limit-cycles 100000000
```

Wynik (2026-10-22, `master` repozytorium źródłowego, pełny przebieg):

| test | wariant | wynik | PC | cykle | instrukcje |
|---|---|---|---|---|---|
| `6502_functional_test.bin` | NMOS 6502 | **pass** | `$3469` | 96 241 367 | 30 646 177 |
| `65C02_extended_opcodes_test.bin` | CMOS 65C02 | **pass** | `$24F1` | 66 907 072 | 21 986 986 |
| `6502_interrupt_test.bin` | NMOS 6502 | **pass** | `$06F5` | 3 016 | 1 050 |
| `6502_interrupt_test.bin` (`D_clear = 1`) | CMOS 65C02 | **pass** | `$0719` | 3 058 | 1 068 |
| `6502_decimal_test.bin` (`cputype = 0`) | NMOS 6502 | **pass** (`ERROR = 0`) | `$044B` | 53 953 828 | 17 609 916 |
| `6502_decimal_test.bin` (`cputype = 1`) | CMOS 65C02 | **pass** (`ERROR = 0`) | `$044B` | 56 640 804 | 18 396 348 |

Limit cykli 100 000 000 jest z zapasem ok. 4% ponad zmierzony przebieg — nie jest to wartość zgadywana, lecz
pomiar + margines; przy zmianach rdzenia podnieś go, jeśli przebieg zbliży się do limitu.

Kody wyjścia harnessu: `0` = pętla pod adresem sukcesu, `2` = inna pętla (porażka), `3` = limit cykli.

### Test przerwań i test dziesiętny: asemblacja (as65 → ca65)

Dla tych dwóch testów repozytorium źródłowe **nie ma binariów**, a dołączony `as65` to 32-bitowy ELF
(`/lib/ld-linux.so.2`) — nie uruchamia się na amd64 bez bibliotek i386. Dlatego źródła as65 konwertujemy
skryptem `tools/dormann-asm/as65_to_ca65.py` (tylko reguły składni; **żadnego kodu testów w repo** — GPL-3.0)
i asemblujemy `ca65`/`ld65`.

```bash
# 1. źródła i skrypt (poza repo trzymamy .a65 — GPL-3.0)
curl -O https://raw.githubusercontent.com/Klaus2m5/6502_65C02_functional_tests/master/6502_interrupt_test.a65
python3 tools/dormann-asm/as65_to_ca65.py 6502_interrupt_test.a65 int.s
ca65 -o int.o int.s && ld65 -C int.cfg -o int.bin int.o      # segmenty: $000A/$0200/$0400/$FFFA, fill $00

# 2. przebieg (rejestr zwrotny pod $BFFC steruje IRQ/NMI)
dotnet tools/CpuHarness/bin/Release/net10.0/VibeEmulator.CpuHarness.dll \
    --image /tmp/dormann/int.bin --variant nmos \
    --pc 0x0400 --success-pc 0x06F5 --watch-register 0xBFFC --limit-cycles 50000000
```

Test dziesiętny ma gotowy port ca65 (`amb5l/6502_65C02_functional_tests`, plik `ca65/6502_decimal_test.ca65`)
— konwersja nie jest potrzebna; wystarczy `ca65` + `ld65` z segmentami `ZEROPAGE` od `$0000` i `CODE` od
`$0400`, odczyt `ERROR` pod `$000B` (`--report-byte 0x000B`), `cputype` 0 = NMOS, 1 = 65C02.

Adresy pętli sukcesu **nie są zgadywane**: są odczytywane z listingu `ca65 -l` (makro `success` rozwija się do
`jmp *`; listing pokazuje offsety segmentu `CODE`, więc adres bezwzględny to offset + `$0400`).

### Szkic harnessu (realizacja)

1. Płaska pamięć 64 KB (`FlatMemory`); obraz wczytany od `$0000`; `PC = $0400` ustawiany ręcznie.
2. Rdzeń cycle-exact; po każdej ukończonej instrukcji (`Pending == 0` w fazie `Fetch`) porównanie `PC` z `PC`
   sprzed instrukcji. **Pętla `PC`** (instrukcja skacze do siebie) oznacza koniec.
3. `PC == $3469` (lub `$24F1` dla 65C02) → sukces; każdy inny adres → porażka (znajdź go w `.lst`).
4. Limit cykli z pomiaru, nie z intuicji.

## nestest (Kevin Horton)

Pliki w [nes-test-roms](https://github.com/christopherpow/nes-test-roms), katalog `other/`: `nestest.nes`,
`nestest.log`, `nestest.txt` (dokumentacja). NESdev o nim: „the best test to start with when getting a CPU emulator
working for the first time”; log wzorcowy powstał w Nintendulatorze.

| element | wartość |
|---|---|
| plik | `nestest.nes`, 24 592 bajty, iNES: **mapper 0 (NROM)**, PRG 1 × 16 KB, CHR 1 × 8 KB |
| wektory w PRG | NMI `$C5AF`, RESET `$C004`, IRQ `$C5F4` |
| tryb automatyczny | ustaw `PC = $C000` (dokumentacja: „run on automation”); test wykona wszystkie próby kolejno |
| stan początkowy (pierwsza linia logu) | `A = X = Y = 0`, `P = $24`, `SP = $FD`, licznik cykli `CYC = 7` |
| wynik | bajty `$02` i `$03`: `$02 = $00` („tests completed successfully”) i `$03 = $00` („no error, all tests pass”); testy opkodów nieoficjalnych zgłaszają błędy w `$03`, z wyjątkiem NOP-ów (`$02`); kody błędów w `nestest.txt` |
| długość | ok. 400 prób; log ma **8991 linii**, ostatnia: `RTS` pod `$C66E`, `CYC:26554` |

Format linii logu: `PC`, bajty opkodu i operandów, deasemblacja z wartością operandu (np. `STX $00 = 00`),
`A:.. X:.. Y:.. P:.. SP:..`, `PPU:<scanline>,<dot>`, `CYC:<licznik>`. Stan jest zapisany **przed** wykonaniem instrukcji.
Opkody nieoficjalne są oznaczone gwiazdką przed mnemonikiem. Kolumna `PPU` **wynika z `CYC`**: w całym logu jest
`PPU = divmod(3 × CYC, 341)` (sprawdzone na wszystkich 8991 liniach), więc harness CPU-only może porównywać samo `CYC`.

Wymagania zmierzone z logu:
- **opkody nieoficjalne użyte w teście:** tylko rodziny `DCP`, `ISB`, `LAX`, `NOP` (różne tryby), `RLA`, `RRA`, `SAX`,
  `SBC` (`$EB`), `SLO`, `SRE`. Test **nie używa** `ANC`, `ALR`, `ARR`, `XAA`, `AXS`, `AHX`, `TAS`, `SHY`, `SHX`, `LAS`. Do
  zaliczenia `nestest` nie trzeba więc całej fazy opkodów nieoficjalnych;
- **adresy:** zero page, `$0100`–`$01FF` (stos), `$0200`–`$07FF` (RAM), rejestry APU `$4000`–`$401F` (zapisy np. `STA $4015`,
  `STA $4007`) i ROM. W logu nie ma bezpośrednich operandów w lustrach RAM (`$0800`–`$1FFF`) ani w PPU. *(Zakres sprawdzono tylko dla
  operandów jawnych w deasemblacji; adresy wyliczone w trybach pośrednich mogą trafiać gdzie indziej.)*;
- **tryb dziesiętny nie jest potrzebny** (2A03 go nie ma).

Szkic: harness ładuje PRG 16 KB tak, by `$C000` wskazywało jego początek (standardowo NROM-128 odbija PRG w `$8000`
i `$C000`; *odbicie niezweryfikowane w źródle*), ustawia stan początkowy, a po każdej instrukcji porównuje `PC`, `A`, `X`, `Y`, `P`, `SP` i `CYC` z kolejną linią logu;
na końcu sprawdza bajty `$02` i `$03`. Zapisy do `$4000`–`$401F` mają być przyjmowane (ignorowane).

## Testy Blargga

Repozytorium: [nes-test-roms](https://github.com/christopherpow/nes-test-roms). Licencji tych testów nie sprawdzano
(niezweryfikowane).

**Protokół wyniku** (wspólny, z readme `instr_test-v5`, `instr_timing`, `cpu_reset`): tekst zapisywany od `$6004`, zakończony
bajtem 0 (terminator przesuwa się przy dopisywaniu); status w `$6000`: **`$80` = test trwa**, `$81` = test wymaga resetu
opóźnionego o co najmniej 100 ms, **`$00`–`$7F` = zakończony z tym kodem (0 = sukces)**; sygnatura w `$6001`–`$6003`
informuje, że dane w `$6000+` są ważne (readme zapisuje ją jako `$DE $B0 $G1`, najpewniej literówka). Wynik jest też
sygnalizowany dźwiękiem (kod binarnie: niski ton = 0, wysoki = 1).

| test | co sprawdza | wymagania |
|---|---|---|
| `instr_test-v5` (`all_instrs.nes`, `official_only.nes`, `rom_singles/`, `nsf_singles/`) | zachowanie instrukcji przez sumy kontrolne po wielu kombinacjach rejestrów, flag i pamięci; wypisuje, które opkody zawiodły. Nie testuje 12 KIL ani `$8B $93 $9B $9F $BB` | **NROM, bez PPU** (NESdev) |
| `instr_misc` | m.in. zawijanie adresu 16-bitowego i dummy ready | *niezweryfikowane (tylko opis NESdev)* |
| `instr_timing` | czas wszystkich instrukcji poza 12 zawieszającymi; `branch_timing`: osiem sytuacji (cztery niewykonane, cztery wykonane; poprawnie `2 2 2 2 3 3 4 4`) | licznik długości APU, NMI |
| `cpu_timing_test6` | czas oficjalnych i nieoficjalnych instrukcji poza gałęziami i HLT, z i bez przekroczenia strony; do 16 s; **tylko NTSC** | APU, NMI |
| `branch_timing_tests` (`1.Branch_Basics`, `2.Backward_Branch`, `3.Forward_Branch`) | niewykonana gałąź = 2 cykle, wykonana = 3, wykonana z przekroczeniem strony = 4; testy trzeba uruchamiać po kolei | NMI z PPU |
| `cpu_interrupts_v2` (`1-cli_latency` … `5-branch_delays_irq`) | opóźnienie po `CLI`/`SEI`/`PLP`/`RTI`, NMI przerywające BRK, NMI i IRQ, opóźnienia gałęzi | APU (ramka IRQ), NMI z PPU |
| `cpu_reset` | po włączeniu `A = X = Y = 0`, `P = $34`, `S = $FD`; po resecie `A`/`X`/`Y` bez zmian, `I = 1`, `S −= 3`, **nic nie zapisane na stos** | reset |
| `cpu_dummy_writes`, `cpu_dummy_reads` | dummy zapisy i odczyty | *tylko lista katalogu; readme `cpu_dummy_reads` nie istnieje (404)* |

Wniosek: `instr_test-v5` i `cpu_reset` da się uruchomić na minimalnym systemie (CPU + NROM + RAM `$6000`+);
testy czasu i przerwań wymagają już ruchu PPU/APU, czyli emulacji systemu, nie samego CPU.

## SingleStepTests/65x02 (Tom Harte)

Repozytorium: <https://github.com/SingleStepTests/65x02>, licencja MIT. Zestawy: `6502` (NMOS), **`nes6502`**,
`rockwell65c02`, `synertek65c02`, `wdc65c02`; ścieżka `<zestaw>/v1/<opkod hex>.json`; 256 plików na zestaw, po 10 000
testów (3–5 MB). Test: `{name, initial:{pc,s,a,x,y,p,ram}, final:{…}, cycles:[[adres, wartość, "read"|"write"], …]}`.
README: wektory wygenerowała implementacja zgodna z dostępną dokumentacją, przechodząca inne publikowane zestawy i
sprawdzona w emulowanej maszynie, czyli **model referencyjny, nie zapis z krzemu**. Pliki `wdc65c02/v1/cb.json` i `db.json`
(`WAI`, `STP`) są **puste**. Wykorzystanie w repo: próbka 100 testów na opkod w `tests/VibeEmulator.Mos6502.Tests/Vectors`,
pełne pliki poza repo (≈430 MB dla 118 opkodów; pełny przebieg etapu J: 93 × 10 000 = 930 000 testów); opis próbki, licencja i przycięcie:
[Vectors/README.md](../../tests/VibeEmulator.Mos6502.Tests/Vectors/README.md); metoda i wyniki: notatka 11 w vaultcie.

## Wzorzec na poziomie tranzystorów

[perfect6502](https://github.com/mist64/perfect6502) (symulacja siatki tranzystorów 6502) i
[Visual6502](http://visual6502.org/): adresy odpowiadają; treści nie badano.

## Proponowane harnessy (nie zaplanowane w roadmapie, patrz [README.md](README.md))

1. **Dormann, funkcjonalny i 65C02:** `FlatMemory` + wykrycie pętli `PC`; po etapach 2b–2d (oraz 2e dla trybu dziesiętnego).
2. **Komparator `nestest.log`:** CPU + RAM + ROM `$C000`; wymaga tylko dziesięciu rodzin opkodów nieoficjalnych.
3. **`instr_test-v5`:** NROM + RAM `$6000`; wymaga wszystkich nieoficjalnych poza wymienionymi.
4. **Test przerwań Dormanna:** rejestr zwrotny pod `$BFFC`; wymaga etapu 2d.
5. **Zestaw `nes6502` z SingleStepTests:** wariant 2A03 bez trybu dziesiętnego.
