# Opkody: NMOS 6502/6510, 65C02, 2A03

Tabele maszynowe: [opcodes/nmos6502.csv](opcodes/nmos6502.csv) i [opcodes/wdc65c02.csv](opcodes/wdc65c02.csv)
(256 wierszy każda). Powstały 2026-10-21 z kilku niezależnych źródeł i zostały sprawdzone programowo (metoda niżej).
Zawartość i weryfikacja są też w notatce 14 w vaultcie Obsidiana.

## Kolumny CSV

| kolumna | znaczenie |
|---|---|
| `opcode` | bajt opkodu, hex |
| `klasa` | NMOS: `official`, `unofficial`, `KIL`. 65C02: `official-NMOS (dziedziczony)`, `nowy-65C02`, `undefined-NOP`, `KIL` |
| `mnemonik`, `aliasy` | nazwa z Oxyron; aliasy z masswerk i porównania źródeł |
| `tryb` | `imm zp zpx zpy abs abx aby izx izy ind rel zpr izp iax`; pusty = niejawny lub akumulator |
| `bajty`, `cykle_bazowe` | długość i liczba cykli bez korekt; pusta dla KIL |
| `dodatkowe` | `+1 page cross` lub `branch +1/+2` |
| `wektory_cykle`, `wektory_n` | liczby cykli widziane w wektorach SingleStepTests i liczba testów w próbce |
| `masswerk` | NMOS: `tak` lub alias z masswerk |
| `datasheet_wdc` | 65C02: czy opkod jest udokumentowany w datasheecie WDC |
| `uwagi` | rozbieżności i wyjątki |

## Liczby

| zestaw | oficjalne | nowe | nieoficjalne / niezdefiniowane |
|---|---|---|---|
| NMOS 6502/6510 | 151 | **93 nieoficjalne (etap J)** | **12 KIL (etap K)** — po nich **256/256** |
| 65C02 (WDC) | 151 (dziedziczone) | **61** | 44 niezdefiniowane = NOP |

65C02 ma 212 opkodów udokumentowanych w datasheecie WDC (151 + 61). Nowe: instrukcje bitowe `RMB0–7`, `SMB0–7`,
`BBR0–7`, `BBS0–7` (32), `BRA`, `PHX`, `PHY`, `PLX`, `PLY`, `STZ` ×4, `TSB` ×2, `TRB` ×2, `BIT` ×3 (`#`, `zp,X`, `abs,X`),
`INC A`, `DEC A`, osiem form `(zp)` (`ORA`, `AND`, `EOR`, `ADC`, `STA`, `LDA`, `CMP`, `SBC`), `JMP (abs,X)`, `WAI`, `STP`.

## Weryfikacja

| porównanie | wynik |
|---|---|
| Oxyron NMOS vs wektory SingleStepTests (cykle, długości, 256 opkodów, 61 057 testów) | **0 niezgodności** |
| Oxyron NMOS vs masswerk (nieoficjalne) | 93/93 pokrytych, **0 różnic w cyklach**, 6 różnic w nazwach (aliasy) |
| datasheet WDC (212 opkodów) vs Oxyron 65C02 | **zbiory identyczne**, 151 NMOS jest podzbiorem |
| 44 niezdefiniowane NOP-y: bajty i cykle z tabeli datasheetu vs Oxyron vs wektory | zgodne dla 43; **`$5C` się różni** |
| NESdev: „All illegal 6502 opcodes execute identically on the 2A03/2A07” | potwierdza, że opkody nieoficjalne NMOS dotyczą też NES |

Metoda: parsowanie macierzy HTML Oxyron; żądania `Range` o pierwsze 96 KB każdego z 512 plików wektorów; tabele
masswerk; rozpakowanie strumieni Flate z PDF datasheetu WDC i parsowanie tabeli operacji. Wektory z commita
`2f6980a2d95757486c7bee24355c360e40e2a224`.

## Nieoficjalne opkody NMOS (etap J)

93 opkody z klasy `unofficial` są **zaimplementowane** i zweryfikowane pełnymi wektorami (930 000 testów,
0 niezgodności); opisy w CSV zostają bez zmian. Zapisano je w `src/Mos6502/OpcodeCatalog.Nmos.Unofficial.cs`.

**Opkody niestabilne** (wynik zależy od egzemplarza/chipu): `AHX` (`93`, `9F`), `SHX` (`9E`), `SHY` (`9C`),
`TAS` (`9B`), `LAS` (`BB`), `XAA` (`8B`), `LAX #imm` (`AB`), `SBC $EB`. Rozstrzygnięcie w repo: bierzemy **ten
wariant**, który jest w wektorach SingleStepTests — `value = rejestr & (high+1)`, a wartość ta trafia do bajtu
wysokiego adresu **tylko przy przekroczeniu strony**; `A = X = imm & (0xEE | (A & 0x11))` dla `LAX #imm`;
`A = (A | 0xEE) & X & imm` dla `XAA`. Źródło decyzji: wektory (plik `93.json` itd.), nie „No More Secrets”
(nieotwarte) — to oznacza, że zachowanie innych egzemplarzy 6502 nie jest modelowane.

`ARR` (`6B`) ma nietypowe flagi; reguły wyprowadzone z 6521 przypadków są w
[plan-c64-stages/J-nieoficjalne-nmos.md](../plan-c64-stages/J-nieoficjalne-nmos.md).

## Rozbieżności i wyjątki

1. **`$5C` (NOP 3 bajty) na 65C02:** datasheet WDC i Oxyron podają **8 cykli**, a wektory w trzech zestawach (WDC,
   Rockwell, Synertek) **4 cykle**. Nie wiadomo, które zgadza się z krzemem. **Rozstrzygnięcie w repo (2026-10-21,
   etap I):** implementacja ma **4 cykle** zgodnie z wektorami (reguła „wektory są źródłem prawdy”), a CSV zachowuje
   8 w kolumnie `cykle_bazowe` z adnotacją w `uwagi`; `--verify-code` ma **jawny wyjątek** dla `c02:5C`
   (oczekiwane 4 vs CSV 8), więc rozbieżność jest widoczna, nie wyciszona.
2. **`WAI` (`$CB`) i `STP` (`$DB`):** pliki wektorów istnieją, ale mają 0 bajtów.
3. **`ADC`/`SBC` na 65C02:** tabele podają cykle bazowe; w trybie dziesiętnym jest o 1 cykl więcej
   ([decimal-mode.md](decimal-mode.md)).
4. **KIL/JAM (12 opkodów NMOS: `$02 $12 $22 $32 $42 $52 $62 $72 $92 $B2 $D2 $F2`):** wektory pokazują 11 cykli
   i `PC + 1`. Kolejne cykle to odczyty `$FFFF`, `$FFFE`, `$FFFE`, `$FFFF` ×6 — ten sam wzorzec w 10 000 testów
   każdego z 12 opkodów (etap K). Rdzeń odtwarza tę sekwencję i zatrzymuje się (`Jammed`), zamiast być
   traktowanym jak nieznany bajt; zatrzymanie nie jest wyjątkiem.
   Na 65C02 `$02`, `$22`, `$42`, `$62`, `$82`, `$C2`, `$E2` to dwubajtowe NOP-y.

## Niezdefiniowane opkody 65C02: długości i cykle

Z tabeli opkodów nieprawidłowych w datasheecie WDC („Some terminate only by reset. Results are undefined. All are NOP's
(reserved for future use)”):

| opkody | bajty | cykle |
|---|---|---|
| `02 22 42 62 82`, `C2 E2` | 2 | 2 |
| `x3`, `0B 1B … BB` (kolumna `xB` poza `CB`, `DB`), `EB`, `FB` | 1 | 1 |
| `44` | 2 | 3 |
| `54 D4 F4` | 2 | 4 |
| `5C` | 3 | 8 *(wektory: 4)* |
| `DC FC` | 3 | 4 |

## Opkody nieoficjalne NMOS a testy

- **`nestest`** testuje opkody nieoficjalne (sekcja „invalid opcode tests”, kody błędów w bajcie `$02`/`$03`).
- **Blargg `instr_test-v5`** (`all_instrs`) testuje prawie wszystkie nieoficjalne; **nie testuje**: 12 KIL (zawieszają
  CPU) oraz opkodów o niespójnym zachowaniu: `$8B XAA #n`, `$93 AXA (z),Y`, `$9B XAS abs,Y`, `$9F AXA abs,Y`,
  `$BB LAR abs,Y`. `official_only.nes` testuje tylko oficjalne.
- **Blargg `cpu_timing_test6`** domyślnie testuje tylko oficjalne; z przyciskiem A dodaje `$EB` i nieoficjalne NOP-y:
  1-bajtowe `1A 3A 5A 7A DA FA`, 2-bajtowe `04 14 34 44 54 64 74 80 82 89 C2 D4 E2 F4`, 3-bajtowe `0C 1C 3C 5C 7C DC FC`;
  z przyciskiem B wszystkie nieoficjalne. Nie testuje 12 HLT ani gałęzi (do nich służy `branch_timing_tests`).
- **Semantyka niestabilnych opkodów** (`ANE`/`XAA`, `LXA`, `SHA`, `SHX`, `SHY`, `TAS`, `LAS`) zależy od układu. Autorytetem jest
  [No More Secrets: NMOS 6510 Unintended Opcodes](https://www.cpcwiki.eu/imgs/7/7d/NoMoreSecrets-NMOS6510UnintendedOpcodes-20212412.pdf)
  (**znaleziony, nieotwarty**); wektory SingleStepTests przyjmują jeden wybór.
- **Dormann** testuje wyłącznie opkody **udokumentowane**; test 65C02 dodatkowo „all additional opcodes … including
  undefined opcodes”.

## Aliasy nazw nieoficjalnych opkodów NMOS

`ALR` = `ASR`; `SLO` = `ASO`; `SRE` = `LSE`; `DCP` = `DCM`; `ISC` = `ISB` = `INS`; `SAX` = `AAX` = `AXS`; `ANE` = `XAA`;
`SHA` = `AHX` = `AXA`; `SHY` = `SAY` = `SYA`; `SHX` = `SXA` = `XAS`; `TAS` = `SHS` = `XAS`; `LAS` = `LAR`;
`LXA` = `LAX #` = `OAL` = `ATX`; `SBX` = `AXS`; `SBC $EB` = `USBC`. Strona NESdev nazywa KIL jako `STP` (inna niż `STP`
z 65C02, `$DB`).

## Gdzie szukać reszty

- Odczyt programowy: `tools/OpcodeInfo` czyta te tabele i wypisuje wybrane opkody jako JSON
  (`dotnet run -c Release --project tools/OpcodeInfo -- --opcode 00-0F --set nmos --pretty`).
  Narzędzie tylko czyta CSV; nie zastępuje weryfikacji opisanej wyżej.
- Weryfikacja kodu z tabelami: każdy zarejestrowany opkod ma w `src/Mos6502/OpcodeCatalog*.cs` adnotację `[Opcode]`;
  `tools/OpcodeInfo --verify-code` porównuje ją z CSV i zwraca exit 1 przy niezgodności. Zakres porównania, zestawy
  i zachowanie `c02` (dziedziczy tabelę NMOS) opisuje `README.md` w sekcji o `tools/OpcodeInfo`. Krok działa w CI.
- Wpływ instrukcji na flagi: [NESdev: Instruction reference](https://www.nesdev.org/wiki/Instruction_reference) (pobrane, nie
  przeczytane w całości), [6502.org: opcodes](http://www.6502.org/tutorials/6502opcodes.html) (adres odpowiada; treść niepobrana).
- Dummy ready i dokładne dostępy do magistrali: wektory SingleStepTests; reguły wyczytane z nich są w notatce 11 w vaultcie
  i w `.github/instructions/c64-emulation.instructions.md`.
