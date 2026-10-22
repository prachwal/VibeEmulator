# Plan implementacji: C64 cycle-exact (6510 + VIC-II)

Status: **w realizacji** (szkielet maszyny działa; zgodność z zestawami referencyjnymi jeszcze niepotwierdzona — patrz sekcja „Stan realizacji", stan na 2026-10-21) · Target: **PAL 6569R5** · Zakres: **poziom 3** (gry + większość dem)

---

## Stan realizacji

Stan na 2026-10-21, ocena na podstawie kodu w `src/Core/C64` i testów w
`tests/VibeEmulator.Core.Tests/C64/` (testów nie uruchamiano przy sporządzaniu tej tabeli).
Faza jest **zrobiona** tylko wtedy, gdy spełnione jest jej kryterium akceptacji z sekcji 3;
w przeciwnym razie jest **w toku** z wyszczególnionymi brakami.

| faza | status | co jest (pliki) | czego brakuje do akceptacji |
|---|---|---|---|
| 0. Fundament czasowy | w toku (prawie) | `C64Machine.Tick()`/`RunFrame()` w `C64Machine.cs`; `VicII.Tick()`; test `Frame_HasExactly19656Cycles` w `C64MachineTests.cs` | test 100 klatek bez dryfu; licznik cykli jest w `ICpuCore.Cycles` (`long`), nie w `CpuState.Cycles`; `VicII.Tick()` jest już pełniejszy niż „pusty" |
| 1. CPU jako maszyna stanów | w toku (etapy 2a–2e, **A–Q** zrobione; plan dojścia do 100% — sekcja 9) | `src/Mos6502/CycleExact/StateMachineCpuCore.cs` (`Tick()` = 1 cykl, `Stall()`, RMW z dwoma zapisami, JAM zamiast wyjątku, dummy read w 2. cyklu instrukcji jednobajtowych, adres dummy przy indeksowaniu zależny od wariantu, sekwencje BRK/IRQ/NMI po 7 cykli przez piny `IInterruptLines`, tryb dziesiętny ADC/SBC, WAI/STP, tryby 65C02 `(zp)`/`zp,rel`/`(abs,X)` i NOP-y o długości 1–3 bajtów), `src/Mos6502/OpcodeCatalog/CpuVariant.cs` (NMOS 6502: **256** wpisów, pełna tablica; 65C02: **256** = baza NMOS nadpisana dodatkami 65C02; Ricoh 2A03 = tabela NMOS bez trybu dziesiętnego, właściwości `JmpWrap`, `InterruptClearsDecimal`, `DecimalExtraCycle`, `DecimalNzFromResult`), `src/Mos6502/OpcodeCatalog/Disassembler.cs`; testy wektorowe `tests/VibeEmulator.Mos6502.Tests/SingleStepVectorTests.cs` (766 próbek po 100 testów, **bez `Skip`** — jedyny wyjątek ADC/SBC z D=1 zdjęty w etapie G) | **zweryfikowane pełnymi wektorami** (10 000 testów na opkod, poza repo): **256/256 opkodów NMOS** (w tym 12 KIL) i 254/254 65C02 (WAI/STP bez wektorów) bez niezgodności, bez pominięć (szczegóły w sekcji 4). Poziom A: 151/151 opkodów, tryb dziesiętny zamknięty; **poziom B: NMOS 256/256 bajtów**; **poziom C: 65C02 256/256 bajtów**; sekwencja RESET i hijacking wektorów nie są zaimplementowane; testy Dormanna H1/H3/H4/H5 oraz `nestest`
uruchomione i zielone (sekcja 4); jedyna znana rozbieżność to niestabilny `ATX` w Blargg `03-immediate` |
| 2. Open bus | w toku (częściowo) | `C64Bus.OpenBus` (aktualizowany odczytami), `$DE00-$DFFF` zwraca open bus; test `OpenBus_ReturnsLastValueOnBus`; `ProcessorPort.ReadData()` | `$D400-$D7FF` (SID) zwraca zawartość rejestrów, nie open bus; zapisy nie aktualizują `OpenBus`; `$00`/`$01` nie są podpięte do `C64Bus` (patrz faza 4); brak testu „dwa różne poprzednie odczyty dają różne wyniki" dla `LDA $D419` |
| 3. Nieoficjalne opkody | **zrobione** (etapy J i K) | `OpcodeCatalog/OpcodeCatalog.Nmos.Unofficial.cs` (93 opkody) i `OpcodeCatalog/OpcodeCatalog.Nmos.Kil.cs` (12 KIL); NMOS **256/256** bajtów, pełne wektory 2 639 907 / 2 639 907 | — |
| 4. Processor port 6510 | w toku | `ProcessorPort.cs` (DDR/dane, `ReadData()`, opóźnienie 3 cykli, `Tick()` wywoływany z `C64Machine.Tick()`); testy `BankSwitch_TakesThreeCyclesToSettle`, `ReadData_MixesOutputAndInputPins`, `DefaultBanking_EnablesBasicAndKernalRom` w `C64BusTests.cs` | `C64Bus` nie routuje adresów `$0000`/`$0001` do portu (zapis CPU pod `$01` idzie do RAM, więc opóźnienie nie działa end-to-end); `IoVisible` nie uwzględnia reguły „I/O tylko gdy (ddr & 0x04) != 0"; brak testów dla 8 konfiguracji |
| 5. C64Bus | w toku | `C64Bus.cs` (dekoder RAM/BASIC/KERNAL/CHAR/I/O, mirror VIC co 64 B, SID co 32 B, CIA przez `address & 0x0F`, zapis do ROM ignorowany, `Observer`); testy w `C64BusTests.cs` | brak cartridge; `$D800-$DBFF` to zwykły RAM (bez 4-bitowego kolor-RAM); zapisy do `$DE00-$DFFF` ignorowane; brak testów mirrorów CIA i pełnej macierzy 8 konfiguracji; brak porównania z mapą z „Mapping the C64" |
| 6. VIC-II: szkielet i raster | w toku | `VicII.cs` (63×312, `Tick()` → `VicTickResult` z AEC/BA, `$D011`/`$D012`/`$D016`/`$D018` jako rejestry, odczyt `$D012`, flaga/maska `$D019`/`$D01A`); testy `Bus_ReadsRasterFromD012`, `Raster_AdvancesOneLinePerCycleLine` | porównanie rastra z `$D012` (zapis) i ustawianie flagi przerwania rastra nie są zaimplementowane (`IrqRaised` tylko czyta flagi); bit 8 rastra w `$D011` nie jest obsłużony; IRQ VIC podłączony do CPU od etapu F (`IInterruptLines`) |
| 7. VIC-II: badline i cycle stealing | w toku | `VicII.IsBadLine()`, BA niskie w cyklach 12–54, AEC niskie w cyklach 15–54 (40 cykli); testy `BadLine_StealsExactlyFortyCycles`, `NoBadLine_WhenDisplayDisabled`, `IsBadLine_DetectsOnlyMatchingScrollLine`, `Aec_BlocksCpuOnlyInStealingWindow`; tracer `tools/C64Trace` (`badline`, `--format vice`) | idle state (`$3FFF`), grey dot, border opener, rendering; brak logu referencyjnego VICE i diffu `(cykl, AEC)` dla 312 linii (tracer potrafi wygenerować format do diffu, ale referencji w repo nie ma) |
| 8. VIC-II: sprite DMA | nie rozpoczęta | tylko rejestry `$D01E`/`$D01F` (zapis jedynki kasuje flagę; brak źródła kolizji) | wszystkie punkty |
| 9. CIA 6526 × 2 | w toku (mockup) | `Cia6526.cs`: rejestry, dwa timery zliczające w dół z ustawianiem flag w `$0D`, maska IRQ; podpięta w `C64Bus` (`$DC00`/`$DD00`) i `C64Machine.Tick()`; IRQ (CIA1) i NMI (CIA2) dostarczane do CPU od etapu F | tryby timerów (one-shot, CNT, zliczanie Timer A), force load, TOD i alarm, porty A/B; brak testów `Cia6526` |
| 10. SID (minimalny) | w toku (mockup) | `Sid6581.cs`: 32 rejestry z mirrorem co 32, `$D419`/`$D41A` zwracają 0; test `Sid_Registers_AreMirroredEveryThirtyTwoBytes` | `$D41B`/`$D41C` (oscylator/obwiednia), timing zapisu |

**Elementy spoza planu, które już istnieją:**

- `AtomicCpuCore.cs` + `CycleCostTable.cs` — adapter nad `Cpu<TBus>` (nie cycle-exact; służy do porównania
  modeli czasu i testów logiki), testy w `AtomicCpuCoreTests.cs`. Nie zmienia decyzji z sekcji 8
  (`Cpu<TBus>.Step()` nie jest bazą C64).
- `src/Memory/IBusObserver.cs` + `C64Bus.Observer` — hook śledzenia dostępów do magistrali.
- `tools/C64Trace` — tracer cykl po cyklu (scenariusze `demo`, `rmw`, `badline`, `count`; własne programy `--hex`/`--load`; warunki stopu `--until`/`--instr`; zrzut `text`/`vice`/`json`).
- `tools/CycleModel` — demonstracja różnicy modeli czasu (atomowy vs cykl po cyklu).

**Zestawy walidacyjne (sekcja 4):** częściowo podłączony — próbka SingleStepTests/65x02 w repo (100 testów na opkod,
patrz „Warianty procesora" niżej). Pełne zestawy (10 000 testów na opkod) nie są w repo; ich przebieg wykonano poza repo
niezależnym runnerem (wyniki w „Warianty procesora"). Nie są jeszcze podłączone: pełny Tom Harte w testach, test Dormanna, `perfect6502`
ani programy z VICE `testprogs`. Obecne testy C64 to ręcznie pisane testy jednostkowe w
`tests/VibeEmulator.Core.Tests/C64/`.

**Kamienie milowe (sekcja 7):** M1–M5 nieosiągnięte.

**Uwaga o numeracji cykli (bez zmiany założeń):** w fazie 7 sformułowanie „AEC wraca w cyklu 54" oznacza,
że ostatnim cyklem z AEC niskim jest cykl 54 (okno 15–54 włącznie = 40 cykli); w kodzie AEC jest wysokie
ponownie od cyklu 55 (numeracja cykli w linii od 0).

---

## 0. Definicja celu

„100% zgodności cyklowej" wymaga związania z konkretnym wariantem sprzętu. Przyjmujemy:

| parametr | wartość | uzasadnienie |
|---|---|---|
| Region | **PAL** (6569R5) | standard europejski, 50 Hz |
| Cykle / linia | **63** | |
| Linie / klatka | **312** | 63 × 312 = **19 656 cykli/klatkę** |
| Częstotliwość CPU | 985 248 Hz | 17 734 475 / 18 |
| Odświeżanie | 50,125 Hz | |
| CPU | **6510** (port `$00`/`$01`) | nie 6502 |
| Dokładność | cykl + wybrane efekty półcyklowe | poziom 3–4 |

**Kryterium końcowe:** emulator przechodzi zestawy VICE `testprogs` dla CPU i VIC-II oraz odtwarza
badline/sprite DMA z dokładnością do cyklu, potwierdzoną porównaniem logów z referencją.

**Poza zakresem (świadomie):** NTSC 6567, rewizje 6569R1/R3 (grey dot, inne idle), pełny model SID
(na razie rejestry + timing odczytów), emulacja stacji dysków.

---

## 1. Zasada nadrzędna: VIC-II jest masterem

Na C64 CPU **nie jest** sterownikiem. VIC-II pobiera dane z pamięci i wstrzymuje CPU przez linie `BA`
i `AEC`. To determinuje architekturę: CPU musi być **maszyną stanów**, którą można zatrzymać
w środku instrukcji.

```mermaid
flowchart TD
    MC["C64Machine.Tick()<br/>1 tick = 1 cykl φ2"] --> VIC
    VIC["VicII.Tick() → AEC?"]
    VIC -->|"AEC = true"| CPU["ICpuCore.Tick()<br/>(StateMachineCpuCore)"]
    VIC -->|"AEC = false"| STALL["ICpuCore.Stall()"]
    VIC --> CIA["Cia6526 × 2"]
    VIC --> SID["Sid6581"]
    CPU --> BUS["C64Bus"]
    BUS --> VIC
    BUS --> CIA
    BUS --> SID
    BUS --> PORT["ProcessorPort 6510"]
```

Konsekwencja: `Cpu<TBus>.Step()` z `src/Mos6502` **nie może** być bazą. Zostaje jako referencja
funkcjonalna dla testów ALU (29 opkodów już zweryfikowanych) i jako punkt wyjścia do tabel.

---

## 2. Architektura docelowa

Układ plików w `src/Core/C64/` — stan faktyczny względem pierwotnego planu:

```
src/Core/C64/
  AtomicCpuCore.cs          # adapter nad Cpu<TBus>, nie cycle-exact (nie było w planie)
  CycleCostTable.cs         # koszty cykli dla adaptera atomowego (nie było w planie)
  VicII.cs                  # 63×312, BA/AEC, badline, rejestry $D000-$D3FF (jeden plik)
  C64Bus.cs                 # dekoder $0000-$FFFF + banking + open bus (OpenBus jako właściwość)
  ProcessorPort.cs          # $00/$01, DDR, opóźnienie 3 cykli
  Cia6526.cs                # timery A/B, flagi IRQ (mockup)
  Sid6581.cs                # rejestry (mockup, bez syntezy)
  C64Machine.cs             # kompozycja: VIC → CPU/stall → CIA ×2 → SID → port; RunFrame()
```

Pliki przeniesione z `src/Core/C64/` do bibliotek warstwowych (ścieżki bieżące; opisy jak wyżej):

```
  src/Cpu.Abstractions/ICpuCore.cs      # kontrakt rdzenia: Tick()/Stall() (nie było w planie)
  src/Mos6502/CycleExact/StateMachineCpuCore.cs    # CPU jako maszyna stanów + tabela opkodów (plan: Mos6510.cs + Mos6510.Table.cs)
  src/Memory/IBusObserver.cs            # śledzenie dostępów do magistrali (nie było w planie)
```

Pliki z pierwotnego planu, które **jeszcze nie istnieją** (lub zostały wchłonięte):

```
  MasterClock.cs            # wchłonięty przez C64Machine (Tick/RunFrame)
  Mos6510.cs                # = src/Mos6502/CycleExact/StateMachineCpuCore.cs
  Mos6510.Table.cs          # tabela jest w StateMachineCpuCore.cs (BuildDefaultTable)
  UnstableOpcodes.cs        # faza 3
  VicII.Registers.cs        # rejestry są w VicII.cs
  VicII.Sprites.cs          # faza 8
  VicII.Palette.cs          # po wprowadzeniu renderingu
  OpenBus.cs                # open bus to dziś C64Bus.OpenBus
```

W dalszej części dokumentu `Mos6510` oznacza docelową nazwę roli, którą dziś pełni `StateMachineCpuCore`.

---

## 3. Fazy implementacji

Każda faza ma **kryterium akceptacji** — bez niego nie przechodzimy dalej.

### Faza 0 — Fundament czasowy (bez logiki CPU)

**Zakres**
- `MasterClock`: pętla `while (Cycles < 19656) { ... }`.
- Pusta implementacja `VicII.Tick()` zwracająca `true` (CPU zawsze dostaje dostęp).
- `CpuState.Cycles` jako `long`.

**Pliki:** `MasterClock.cs`, `C64Machine.cs` (stan faktyczny: pętla klatki w `C64Machine.cs`)

**Akceptacja**
- [x] Klatka = dokładnie 19 656 ticków.
- [ ] 100 klatek = 1 965 600 ticków bez dryfu.
- [ ] Test: `Frame_HasExactly19656Cycles`.

---

### Faza 1 — Mos6510 jako maszyna stanów

**Zakres**
- `Tick()` wykonuje **jeden cykl**: fetch, operand, operand, execute — zależnie od `_step`.
- `Stall()` — cykl ukradziony: `Cycles++`, brak zmian rejestrów.
- Pełna implementacja 151 oficjalnych opkodów.
- Page crossing (+1 dla `abs,X`/`abs,Y`/`(zp),Y`) i branch penalty (+1/+2).
- Ghost byte: `abs,X`/`abs,Y` czyta spod adresu **bez** korekty strony.
- RMW: `read → write(old) → write(new)`.
- `JAM`/`KIL` (`$02`, `$12`, `$22`, …): zatrzymanie CPU, nie wyjątek.

**Pliki:** `Mos6510.cs`, `Mos6510.Table.cs` (stan faktyczny: `src/Mos6502/CycleExact/StateMachineCpuCore.cs`)

**Akceptacja**
- [ ] Tom Harte `65x02` — **100%** dla 151 oficjalnych opkodów (stan końcowy + cykle).
- [x] RMW generuje 2 zapisy, nie 1.
- [x] `LDA $10FF,X` = 5 cykli z dummy read, `LDA $1000,X` = 4 cykle (wektory, opkod `BD`, oba warianty).
- [ ] Testy w `tests/VibeEmulator.Core.Tests/C64/`.

**Ryzyko:** to największy etap (~2 500 linii + dane testowe). Mitygacja: generowanie tabel z danych
Tom Harte, nie ręczne przepisywanie.

---

### Faza 2 — Open bus

**Zakres**
- `OpenBus`: rejestr „ostatnia wartość na magistrali", aktualizowany każdym odczytem.
- Odczyt z `$D400-$DFFF`, `$DE00-$DFFF`, `$DF00-$DFFF` zwraca open bus.
- Odczyt z `$01` w trybie output zwraca stan portu (mieszanka DDR/danych).

**Akceptacja**
- [ ] `LDA $D419` zwraca wartość zależną od poprzedniego dostępu.
- [ ] Dwa różne poprzedzające odczyty dają różne wyniki (dowód, że to nie stała).

**Dlaczego tak wcześnie:** bez tego nieoficjalne opkody z fazy 3 będą błędne.

---

### Faza 3 — Nieoficjalne opkody

**Zakres**
- Stabilne: `LAX`, `SAX`, `DCP`, `ISC`, `SLO`, `RLA`, `SRE`, `RRA`, `ANC`, `ALR`, `ARR`, `SBX`.
- Niestabilne, zależne od open bus: `ANE`, `LXA`, `SHA`, `SHX`, `SHY`, `TAS`.
- `LAS`, `SHS` — warianty adresowania.

**Akceptacja**
- [ ] Tom Harte `65x02` — 100% dla wszystkich 256 opkodów.
- [ ] Testy niestabilnych z jawnie ustawionym open bus (bez tego test jest niedeterministyczny).

---

### Faza 4 — Processor port 6510

**Zakres**
- `$00` = DDR, `$01` = dane. `$01` read zwraca `(data & ddr) | (~ddr & 0x17)`.
- Bankowanie: LORAM/HIRAM/CHAREN → 8 konfiguracji.
- **Opóźnienie 3 cykli** przy przełączeniu banków: zapis do `$01` wpływa na dostęp dopiero
  po 3 cyklach.

**Akceptacja**
- [ ] Zapis `$01` → odczyt `$A000` w cyklu 1 zwraca stary bank, w cyklu 3+ nowy.
- [ ] 8 konfiguracji bankowania ma pokrycie testami.
- [ ] I/O visible tylko gdy `(ddr & 0x04) != 0`.

---

### Faza 5 — C64Bus

**Zakres**
- Dekoder `$0000-$FFFF`: RAM, BASIC ROM, KERNAL ROM, CHAR ROM, I/O (`$D000-$DFFF`), cart.
- Mirrorowanie VIC: `$D000-$D3FF` co 64 bajty.
- Mirrorowanie CIA: `$DC00-$DCFF` co 16, `$DD00-$DDFF` co 16.
- Zapis do ROM → ignorowany (ale przechodzi przez open bus).

**Akceptacja**
- [ ] Mapa adresów zgodna z tabelą z „Mapping the C64".
- [ ] Wszystkie 8 konfiguracji bankowania × 4 regiony pokryte testami.

---

### Faza 6 — VIC-II: szkielet i raster

**Zakres**
- 63 cykle × 312 linii. `Tick()` zwraca `AEC`.
- Rejestry `$D011` (YSROLL, DEN, RSEL, BMM), `$D012` (raster), `$D016` (XSROLL, MCM), `$D018`.
- BA/AEC: brak na razie stealing.
- Przerwanie raster: porównanie `$D012` z licznikiem, flaga w `$D019`.

**Akceptacja**
- [ ] Raster rośnie 0→311 i zawija.
- [ ] `$D012` odczyt zwraca aktualną linię.
- [ ] Przerwanie raster zgłaszane w cyklu zgodnym z referencją.

---

### Faza 7 — VIC-II: badline i cycle stealing

**Zakres**
- Warunek badline: `DEN=1` ∧ `raster ∈ [0x30, 0xF7]` ∧ `(raster & 7) == YSROLL`.
- **BA opada w cyklu 12, AEC w cyklu 15, AEC wraca w cyklu 54.**
- **CPU wstrzymany przez 40 cykli** (15–54).
- Idle state: VIC czyta `$3FFF`, gdy DEN=0.
- GREY DOT: pierwszy piksel w trybie tekstowym, gdy `$D020` == `$D021`.

**Akceptacja**
- [x] Linia z badline: CPU traci dokładnie 40 cykli.
- [x] Linia bez badline: CPU traci 0.
- [ ] Border opener działa (test: otwarcie borderu przez zmianę `$D011` w cyklu 55+).
- [ ] Porównanie logu `(cykl, AEC)` z VICE dla 312 linii — identyczne.

**Ryzyko:** to serce zgodności. Bez tego nie ma „100%".

---

### Faza 8 — VIC-II: sprite DMA

**Zakres**
- Odczyt wskaźników sprite'ów w cyklach 55–62 (jeden na sprite'a).
- Warunek startu DMA: `raster == sprite.Y` ∧ `sprite.enabled`.
- MC/MCBASE/DBUF; 2 cykle na sprite'a przy pobieraniu danych.
- Kolizje sprite-sprite i sprite-background, rejestry `$D01E`/`$D01F`.
- Priorytety, multicolor, rozciąganie X/Y.

**Akceptacja**
- [ ] 8 sprite'ów na linii: liczba skradzionych cykli zgodna z referencją.
- [ ] Kolizja wykrywana w prawidłowym cyklu.
- [ ] Odczyt `$D01E` kasuje flagę.

**Uwaga:** dokładne przypisanie cykli do poszczególnych sprite'ów wymaga tabeli z dokumentacji
Bauera — **nie przepisywać z pamięci**, weryfikować z VICE.

---

### Faza 9 — CIA 6526 × 2

**Zakres**
- Timer A (3 tryby), Timer B (4 tryby, w tym zliczanie Timer A / CNT).
- Latch, force load, one-shot.
- TOD (zegar czasu rzeczywistego) + alarm.
- Przerwania: `$DC0D` (IRQ), `$DD0D` (NMI), bity maskujące.
- Porty A/B, kierunki.

**Akceptacja**
- [ ] Timer A w trybie one-shot zgłasza IRQ w prawidłowym cyklu.
- [ ] NMI z CIA2 generuje wektor `$0318`.
- [ ] TOD przechodzi przez północ.

---

### Faza 10 — SID (minimalny)

**Zakres**
- Rejestry `$D400-$D418`, mirror co 32.
- Odczyt `$D41B`/`$D41C` zwraca stan oscylatora (potrzebne grom).
- Timing zapisu (2 cykle).
- **Bez syntezy audio** — tylko model rejestrowy.

**Akceptacja**
- [ ] Zapisy/odczyty zgodne z mapą.
- [ ] Odczyt potencjometrów nie zawiesza gier.

---

## 4. Walidacja — co decyduje o „100%"

| zestaw | pokrywa | faza | kryterium |
|---|---|---|---|
| **Tom Harte 65x02** | 256 opkodów, stan + cykle | 1, 3 | 100% przechodzi |
| **Klaus Dormann** functional test (NMOS) | całościowe działanie CPU, opkody udokumentowane | 1 | pętla `jmp *` pod `$3469` (start `PC = $0400`; [cpu/tests.md](cpu/tests.md)) |
| **Klaus Dormann** 65C02 extended opcodes | 61 nowych opkodów, NOP-y | 3 | pętla `jmp *` pod `$24F1` |
| **Klaus Dormann** interrupt test | IRQ, NMI, BRK, RTI | 2d | pętla `PC`; rejestr zwrotny `$BFFC` |
| **Klaus Dormann** decimal test (Bruce Clark) | tryb dziesiętny, 262 144 przypadki | 2e | `ERROR = 0` |
| **nestest** (+ `nestest.log`) | CPU NES, oficjalne i 10 rodzin nieoficjalnych | 2b–2d, 3 | zgodność rejestrów i `CYC` z logiem; `$02 = $03 = $00` |
| **perfect6502** | referencja tranzystorowa | 1, 3 | zgodność stanu po każdej instrukcji |
| **VICE `testprogs/cpu`** | cykle, pułapki | 1, 4 | 100% |
| **VICE `testprogs/vicii`** | badline, sprite, timing | 7, 8 | 100% |
| **VSP bug test** | `$D011` na końcu linii | 7 | efekt odtworzony |
| **CIA timer tests** | tryby timerów | 9 | 100% |

Testy w repo: dane jako JSON w `tests/`, `[Theory]` + `[MemberData]`, **zero sieci**, deterministyczne.
Zgodne z `.github/instructions/csharp-tests.instructions.md`.

### Warianty procesora

Rdzeń cyklowy jest parametryzowany wariantem `CpuVariant` (`src/Mos6502/OpcodeCatalog/CpuVariant.cs`): nazwa, **pełna** tabela
opkodów wariantu, styl RMW (`RmwStyle`) i adres dummy read przy indeksowaniu `abs,X` / `abs,Y` / `(zp),Y`
(`IndexedDummyRead`). Obie wartości są czytane raz w konstruktorze rdzenia do pól `readonly`; `Tick` tylko odczytuje
pole i rozgałęzia się na `bool`. Wpisy tabeli pochodzą z plików `src/Mos6502/OpcodeCatalog/OpcodeCatalog*.cs`, gdzie każdy opkod ma
adnotację `[Opcode]` (zestaw, bajt, mnemonik, tryb, rodzaj, akcja); adnotacja jest metadanymi i nie jest czytana
przez `Tick`. Zgodność adnotacji z `docs/cpu/opcodes/*.csv` sprawdza `tools/OpcodeInfo --verify-code`.

| wariant | tabela | RMW | dummy read przy indeksowaniu |
|---|---|---|---|
| `CpuVariants.Nmos6502` (domyślny, C64) | 143 wpisy (142 + BRK) | `DoubleWrite` | `GhostByte`: bajt spod `(base & 0xFF00) \| ((base + indeks) & 0xFF)` |
| `CpuVariants.Cmos65C02` | 149 (NMOS + STZ ×4, INC A, DEC A) | `DoubleRead` | `LastOperandByte`: bajt operandu wysokiego (abs) lub zerowej strony ((zp),Y), czyli `PC − 1` |

Reguły dummy read zweryfikowane wektorami i wspólne dla obu wariantów: `abs,X`/`abs,Y`/`(zp),Y` czytają dodatkowo
tylko przy przekroczeniu strony, a zapisy zawsze; `zp,X`/`zp,Y`/`(zp,X)` czytają zawsze spod bazy zerowej strony
przed dodaniem indeksu; instrukcje jednobajtowe (implied) czytają bajt spod PC w swoim drugim cyklu.
Jedyną rzeczą zależną od wariantu jest więc adres dummy read (`IndexedDummyRead`), nie sama obecność odczytu.

Model atomowy `Cpu<TBus>` (`Addressing.Resolve`, `OpcodeGroups.Standard`) nie rozróżnia wariantów i **nie** obsługuje
nowych trybów; zostaje przy 29 opkodach. Nowe tryby (`ZeroPageY`, `AbsoluteY`, `IndexedIndirect`, `IndirectIndexed`)
ma tylko rdzeń cyklowy i dekoder.

Etapy:

1. **Mechanizm wariantów + różnice 65C02 w zbiorze 31 opkodów** — zrobione: STZ ($64, $74, $9C, $9E),
   INC A ($1A), DEC A ($3A), RMW 65C02.
2. **Dokończenie NMOS** — podetapy:
   - **2a. Dummy reads i tryby indeksowe** — **zrobione**: dummy read w 2. cyklu instrukcji implied (wszystkie
     opkody `OpKind.Implied`, w tym BRK w pierwszym z jego cykli wewnętrznych); dummy read spod zerowej strony
     przy `zp,X`/`zp,Y`/`(zp,X)`; adres dummy przy `abs,X`/`abs,Y`/`(zp),Y` zależny od wariantu (`IndexedDummyRead`;
     wyjątek `Stz` usunięty); tryby `zp,Y`, `abs,Y`, `(zp,X)`, `(zp),Y`; 26 nowych opkodów NMOS:
     LDA `B9 A1 B1`, LDX `B6 AE BE`, LDY `B4 AC BC`, STA `95 99 81 91`, STX `96 8E`, STY `94 8C`,
     flagi `18 38 58 78 B8 D8 F8` (CLC, SEC, CLI, SEI, CLV, CLD, SED), `BA 9A` (TSX, TXS).
     Wszystkie opkody obu wariantów przechodzą pełne wektory.
   - **2b. Logika i arytmetyka** — ADC/SBC w pozostałych trybach (dziś immediate i zero page), AND/ORA/EOR,
     CMP/CPX/CPY, BIT; flagi zgodnie z wektorami.
   - **2c. Przesunięcia i RMW** — ASL/LSR/ROL/ROR (akumulator i pamięć), INC/DEC w `zp,X` i `abs,X`.
   - **2d. Stos i sterowanie** — PHA/PLA/PHP/PLP, JSR/RTS/RTI, BRK (kolejność na stosie, wektor przerwania),
     gałęzie (8 opkodów; kara cyklu przy skoku na inną stronę), JMP (abs) z błędem strony, JMP (ind.),
     przerwania IRQ/NMI.
   - **2e. Tryb dziesiętny** — ADC/SBC z flagą D (BCD). Do tego etapu wektory `69`/`E9` z D=1 są jedynym
     wyjątkiem (pomijane w obu wariantach).
3. **Reszta 65C02** — **61 nowych opkodów** wg datasheetu WDC ([cpu/opcodes.md](cpu/opcodes.md)): 32 instrukcje bitowe
   RMB/SMB/BBR/BBS, BRA, PHX/PHY/PLX/PLY, TSB ×2, TRB ×2, BIT ×3 (`#`, `zp,X`, `abs,X`), 8 form `(zp)`,
   `JMP (abs,X)`, WAI, STP; 44 niezdefiniowane opkody jako NOP o długościach i cyklach z datasheetu (dziś JAM);
   poprawki BRK i `JMP (ind.)`; flaga D czyszczona przy przerwaniach; +1 cykl `ADC`/`SBC` w trybie dziesiętnym.
   Decyzja do podjęcia: `$5C` (datasheet: 8 cykli, wektory: 4).
4. **6510** — wariant z portem na chipie (`$00`/`$01`) podpięty pod `C64Bus`; mechanizm (osobny `CpuVariant`)
   już to dopuszcza.

**Walidacja wektorami SingleStepTests/65x02** (MIT, commit `2f6980a2d95757486c7bee24355c360e40e2a224`).
W repo leżą **pierwsze 100 testów** z każdego pliku (`tests/VibeEmulator.Mos6502.Tests/Vectors/`, 290 plików:
142 dla NMOS i 148 dla 65C02; patrz `Vectors/README.md`). Test porównuje cykl po cyklu dostępy do magistrali, stan
końcowy i pamięć końcową. Brak `Skip` w testach wektorowych.

Pełne zestawy (10 000 testów na opkod) nie są w repo. Przebieg pełny wykonano niezależnym runnerem spoza repo
(czyta pliki z commita źródłowego, porównuje to samo co test w repo):

| wariant | opkody w zakresie | wynik pełnych wektorów (pomiar jednolity 2026-10-22) |
|---|---|---|
| NMOS 6502 | 256 | **256/256 opkodów bez niezgodności, bez pominięć**: 2 560 000 testów (= 256 × 10 000) |
| CMOS 65C02 | 254 | **254/254 opkodów bez niezgodności, bez pominięć**: 2 540 000 testów (= 254 × 10 000); WAI/STP bez wektorów w źródle (pliki 0 B) |

Liczby z tabeli pochodzą z **jednego przebiegu całego zestawu** (2026-10-22), nie z sumowania etapów.
Wcześniejsze notatki zbierały sumy przebiegów etapowych (np. „2 639 907” dla NMOS) — te wartości obejmowały
ten sam opkod liczony w kilku etapach i **nie są** porównywalne z jednolitym pomiarem; odtąd obowiązuje
liczba `opkody × 10 000`.

Historia przyrostowa (dla porządku, przebiegi etapowe z 2026-10-21/22): etapy 2a i A–E dodawały opkody
oficjalne, etap F dodał BRK (10 000), etap G **zdjął ostatni wyjątek** od porównania — 16 form `ADC`/`SBC`
z `D=1` porównywane w całości (wcześniej pomijano 80 093 / 80 216 przypadków), etap H dodał 53 opkody 65C02
(530 000), etap I — 44 NOP-y (440 000), etap J — 93 nieoficjalne (930 000), etap K — 12 KIL (120 000),
etap P — cały zestaw `nes6502` (2 560 000).
`$5C`: implementacja 4 cykle (wektory), datasheet 8 — rozbieżność udokumentowana w CSV i
`docs/cpu/opcodes.md`.

### Harnessy testów CPU (nie zaplanowane wcześniej)

Dokumentacja testów, adresów i protokołów: [cpu/README.md](cpu/README.md). Zadania harnessów (każde to osobne,
małe zlecenie; zależności od etapów w nawiasach). **Etapy L–Q** w [plan-c64-stages/](plan-c64-stages/README.md)
realizują tę listę w całości (L = H1/harness, M = H1, N = H3, O = H4/H5, P = H6, Q = H2/H7):

- **H1. Dormann, test funkcjonalny:** ✅ **zamknięty** (etapy L i M) — `tools/CpuHarness`, sukces
  `PC = $3469`, 96 241 367 cykli, 30 646 177 instrukcji (wynik w `docs/cpu/tests.md`).
- **H2. Komparator `nestest.log`:** ✅ **zamknięty** (etap Q) — 8991/8991 linii, 26 553 cykli.
  Stan początkowy `PC=$C000 SP=$FD P=$24 CYC=7`; porównanie `PC A X Y P SP CYC`; wymaga
  tylko `DCP ISB LAX NOP RLA RRA SAX SBC($EB) SLO SRE` z opkodów nieoficjalnych (po 2b–2d i części fazy 3).
- **H3. Dormann, test 65C02:** ✅ **zamknięty** (etap N) — sukces `PC = $24F1`, 66 907 072 cykli.
- **H4. Test przerwań Dormanna:** ✅ **zamknięty** (etap O) — rejestr zwrotny `$BFFC`, sukces
  `$06F5` (NMOS) i `$0719` (65C02); polaryzacja bitów potwierdzona przebiegiem.
- **H5. Test dziesiętny Clarka:** ✅ **zamknięty** (etap O) — `ERROR = 0` dla NMOS i 65C02;
  binarium zasemblowane portem ca65.
- **H6. Wariant 2A03:** ✅ **zamknięty** (etap P) — `CpuVariants.Nes2A03`, pełne wektory `nes6502`
  2 560 000 / 2 560 000, 0 pominięć.
- **H7. Blargg `instr_test-v5`:** ✅ **zamknięty częściowo** (etap Q) — `rom_singles` 15/16 (NROM + RAM
  `$6000`); porażka `03-immediate` to niestabilny `ATX` (`$AB`), udokumentowany. Testy czasu i przerwań
  Blargga wymagają APU/PPU, czyli emulacji systemu NES, nie samego CPU — poza zakresem.

---

## 5. Budżet wydajnościowy

Z pomiarów w `tools/CycleModel` i `tools/EmuBench`:

| element | wartość |
|---|---|
| Cykle / klatkę | 19 656 |
| Koszt ticku (maszyna stanów, naiwna) | ~25 ns |
| Koszt klatki | **~490 µs** |
| Budżet 50 Hz | 20 000 µs |
| **Udział CPU** | **2,5%** |

Wniosek: 100% zgodności cyklowej **nie jest ograniczeniem wydajnościowym**. Pozostałe 97% budżetu
zostaje na VIC-II rendering i SID.

---

## 6. Ryzyka

| ryzyko | wpływ | mitygacja |
|---|---|---|
| Niepełna dokumentacja niestabilnych opkodów | błędy w demach | testy na Tom Harte + `perfect6502` |
| Różnice rewizji VIC-II | efekt widoczny w <1% softwaru | świadomie poza zakresem (6569R5) |
| Timing sprite DMA z pamięci | błędne cykle | tabela z Bauera + porównanie z VICE |
| Złożoność fazy 7 | opóźnienie całego projektu | osobne testy per-linia, nie per-klatka |
| Brak ROM-ów w repo | brak testów end-to-end | ROM-y nie trafiają do repo; testy na wektorach |

---

## 7. Kolejność wykonania

```mermaid
flowchart LR
    F0["0. Fundament<br/>czasu"] --> F1["1. Mos6510<br/>maszyna stanów"]
    F1 --> F2["2. Open bus"]
    F2 --> F3["3. Nieoficjalne<br/>opkody"]
    F3 --> F4["4. Processor<br/>port"]
    F4 --> F5["5. C64Bus"]
    F5 --> F6["6. VIC szkielet"]
    F6 --> F7["7. VIC badline<br/>cycle stealing"]
    F7 --> F8["8. VIC sprite<br/>DMA"]
    F8 --> F9["9. CIA"]
    F9 --> F10["10. SID"]
```

**Kamienie milowe:**

| kamień | warunek |
|---|---|
| M1 | Pełny 6510 przechodzi Tom Harte 100% (oficjalne + nieoficjalne) |
| M2 | Badline kradnie dokładnie 40 cykli, log zgodny z VICE |
| M3 | Sprite DMA zgodny z VICE dla 8 sprite'ów |
| M4 | Przechodzi `vice testprogs` dla CPU i VIC-II |
| M5 | Uruchamia się prosta gra (bez SID audio) |

---

## 8. Co zostaje z obecnego kodu

| element | decyzja |
|---|---|
| `src/Mos6502/Atomic/*` (29 opkodów, kompozycja) | **zostaje** jako referencja ALU i baza tabel |
| `MemoryMap`, `OpcodeTable`, `CpuBuilder` | wzorce do ponownego użycia |
| `ref CpuState` (zweryfikowane 11,8 ns/instr.) | wzorzec wydajnościowy |
| `tools/CycleModel` | **zostaje** jako dokumentacja różnicy modeli czasu |
| `Cpu<TBus>.Step()` jako baza C64 | **odrzucone** — brak możliwości stallu (istnieje tylko adapter `AtomicCpuCore` do porównań i testów logiki) |
| `src/Core/Fibonacci.cs`, `tools/Bench` | poza zakresem, zostają |

---

## 9. Plan dojścia do 100% pokrycia opkodów

Stan wyjściowy (2026-10-21, `tools/OpcodeInfo --verify-code`): **57 z 151** oficjalnych opkodów NMOS (37,7%),
**6 z 61** nowych opkodów 65C02, **0 z 93** nieoficjalnych NMOS, **0 z 44** niezdefiniowanych NOP-ów 65C02.

### 9.1 Co znaczy „100%" — trzy poziomy

| poziom | zakres | znaczenie praktyczne | dziś |
|---|---|---|---|
| **A. 6502 funkcjonalny** | 151 oficjalnych NMOS | uruchamia programy 6502; gry bez trików demosceny | 57 (37,7%) |
| **B. 6502 kompletny** | 151 + 93 nieoficjalne + 12 KIL = 256/256 | zgodność z `nestest`, pełnymi wektorami, demoscena | **256/256 (100%)** — etapy J i K |
| **C. 65C02 kompletny** | 212 udokumentowanych + 44 NOP = 256/256 | drugi wariant w pełni | 63 (29,7% udokumentowanych) |

Poziomy są **niezależne**: B nie wymaga C, C nie wymaga nieoficjalnych NMOS. Kolejność poniżej prowadzi najpierw
do A (największy zysk funkcjonalny za najmniejszy koszt), potem do B i C.

### 9.2 Zasada: plan mówi „co", wektory mówią „jak"

Ten plan **nie opisuje** kolejności faz, dummy ready ani wartości na magistrali dla nowych opkodów. To bierze się
z wektorów SingleStepTests (`tests/VibeEmulator.Mos6502.Tests/Vectors/README.md`) i `docs/cpu/` — zgodnie z regułą
„zachowania CPU nie wpisuj z pamięci”. Plan ustala kolejność pracy, kryteria akceptacji i pułapki do sprawdzenia.

### 9.3 Etapy

Szczegóły każdego etapu (zakres, opkody, akceptacja, pułapki, **dziennik postępu**) są w osobnym pliku
w [plan-c64-stages/](plan-c64-stages/README.md). Tutaj tylko mapa i kolejność — nie duplikuję zawartości etapów.

| etap | plik | zakres | opkody | nowe tryby | nowe fazy | nowe akcje | po etapie (oficjalne NMOS) |
|---|---|---|---|---|---|---|---|
| **A** | [A-logika-i-porownania.md](plan-c64-stages/A-logika-i-porownania.md) | logika, porównania, ADC/SBC w pozostałych trybach (2b) | 54 | 0 | 0 | 7 | **111/151 (73,5%)** |
| **B** | [B-przesuniecia-i-rotacje.md](plan-c64-stages/B-przesuniecia-i-rotacje.md) | ASL/LSR/ROL/ROR, INC/DEC `zp,X` `abs,X` (2c) | 24 | 0 | 0 | 4 | **135/151 (89,4%)** |
| **C** | [C-rozgalezienia.md](plan-c64-stages/C-rozgalezienia.md) | rozgałęzienia (2d) | 8 | 1 | 1 | 8 | **143/151 (94,7%)** |
| **D** | [D-stos.md](plan-c64-stages/D-stos.md) | stos PHA/PHP/PLA/PLP (2d) | 4 | 0 | 2 | 4 | **147/151 (97,4%)** |
| **E** | [E-sterowanie.md](plan-c64-stages/E-sterowanie.md) | JSR/RTS/RTI/JMP (ind.) (2d) | 4 | 0 | 2 | 3 | **151/151 (100% = poziom A)** |
| **F** | [F-przerwania.md](plan-c64-stages/F-przerwania.md) | przerwania: pełny BRK, IRQ, NMI, RTI (2d) | 0 | 0 | 1 | 0 | zamyka poprawność A |
| **G** | [G-tryb-dziesietny.md](plan-c64-stages/G-tryb-dziesietny.md) | tryb dziesiętny ADC/SBC (2e) | 0 | 0 | 0 | 0 | zamyka 2e |
| **H** | [H-nowe-opkody-65c02.md](plan-c64-stages/H-nowe-opkody-65c02.md) | nowe opkody 65C02 (etap 3) | 55 | 3 | 3 | 13 | c02: **212/212 udokumentowanych** |
| **I** | [I-nop-65c02.md](plan-c64-stages/I-nop-65c02.md) | niezdefiniowane NOP-y 65C02 (etap 3) | 44 | 0 | 0 | 0 | c02: **256/256 bajtów** |
| **J** | [J-nieoficjalne-nmos.md](plan-c64-stages/J-nieoficjalne-nmos.md) | nieoficjalne NMOS (faza 3) | 93 | 0 | 0 | 20 | NMOS: **244/256** (po J) |
| **K** | [K-kil.md](plan-c64-stages/K-kil.md) | KIL × 12 | 12 | 0 | 2 | 1 | NMOS: **256/256 (poziom B)** |

Rozmiar pracy podany w jednostkach obiektywnych (opkody / tryby / fazy / akcje), nie w godzinach.
Listy opkodów pochodzą z `docs/cpu/opcodes/*.csv` (odczyt `tools/OpcodeInfo --include-missing`, 2026-10-21);
przy rozbieżności źródłem jest CSV i wektory, nie ten plan.

**Obowiązek postępu:** po każdej grupie (rodzina mnemoniczna albo ~10 opkodów) wpisz wiersz w dzienniku etapu
(data, grupa, `x/y`, wynik `--verify-code` i testów, uwagi). Zasady i wspólny rytuał zakończenia etapu:
[plan-c64-stages/README.md](plan-c64-stages/README.md).

### 9.4 Kolejność, zależności i weryfikacja

```mermaid
flowchart LR
    A["A. Logika<br/>54 opk."] --> B["B. Przesunięcia<br/>24 opk."]
    B --> C["C. Rozgałęzienia<br/>8 opk."]
    C --> D["D. Stos<br/>4 opk."]
    D --> E["E. Sterowanie<br/>4 opk.<br/>poziom A"]
    E --> F["F. Przerwania"]
    F --> G["G. Tryb dziesiętny"]
    E --> H["H. 65C02 nowe<br/>55 opk."]
    H --> I["I. NOP 65C02<br/>44 opk."]
    E --> J["J. Nieoficjalne<br/>93 opk."]
    J --> K["K. KIL<br/>12 opk.<br/>poziom B"]
```

- **A jest niezależne** od reszty i daje najwięcej — warto zrobić je pierwsze nawet bez dalszych etapów.
- **C i D** wprowadzają fazy, z których korzysta E, H (PHX/PLY) i J — nie da się ich przeskoczyć.
- **F i G** nie zmieniają liczby opkodów, więc nie poprawiają procentu, ale bez nich poziom A **nie jest**
  poprawny funkcjonalnie (BRK bez wektorów przerwań, ADC/SBC binarnie zamiast dziesiętnie).
- **I zależy od H** (te same nowe tryby), **K zależy od J** (nie kolidować z NOP-ami).

Po każdym etapie, jedną komendą:

```bash
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code --include-missing
```

Oczekiwane `counts.missingInCode` dla poziomu A: 94 → **40 → 16 → 8 → 4 → 0**.
Dla 65C02: 94 (NMOS dziedziczone) → 0 po A–E, potem 55 → 0 po H, 44 → 0 po I.

### 9.5 Co może zatrzymać etap (ryzyka)

| ryzyko | etap | mitygacja |
|---|---|---|
| Rozbieżność źródeł dla opkodów niestabilnych i `$5C` | I, J | wektory jako rozstrzygnięcie; różnica w `uwagi` i w teście, nie po cichu |
| Fazy dla `zp,rel` i `(abs,X)` są nietrywialne | H | najpierw wektory jednego opkodu, potem tabela dla całej rodziny |
| Interrupt polling: kiedy rdzeń próbkuje `IRQ`/`NMI` | F | wektory nie pokrywają przerwań — oprzeć się na teście Dormanna (H4), nie na intuicji |
| Tryb dziesiętny: `N`/`Z`/`V` różnią się między wariantami | G | `docs/cpu/decimal-mode.md` + test Clarka jako bramka |
| Duplikacja pole + adnotacja rośnie do ~300 wpisów | H–K | próg z `OpcodeCatalog`: po ~80 wpisach usunąć duplikację, zanim etapy H–K ją potroją |
| Czas: 6 etapów wymaga po 1 próbce wektorów na opkod | wszystkie | pobierać próbki partiami po rodzinie, wpis w `Vectors/README.md` raz na rodzinę |

### 9.6 Czego ten plan nie obejmuje

- **Modelu atomowego** `Cpu<TBus>` (29 opkodów, `OpcodeGroups`) — zostaje jako referencja; nie jest ścieżką C64.
- **Wariantu 6510** (port na chipie) i **2A03** — osobne warianty, po poziomie A.
- **Wydajności** — każdy etap musi utrzymać budżet z sekcji 5; pomiar po A i po J (`tools/EmuBench`).
