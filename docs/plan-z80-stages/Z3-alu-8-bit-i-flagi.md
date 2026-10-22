# Etap Z3 — ALU 8-bitowe i flagi

Poziom: **A** · Pliki wektorów: **92** · Nowe akcje: ADD/ADC/SUB/SBC/AND/XOR/OR/CP, INC, DEC, DAA, CPL, SCF, CCF

Najbardziej zdradliwy etap: flagi nieudokumentowane `X` (bit 3) i `Y` (bit 5) oraz rejestr `Q`. Wektory mają pole
`q` i `p` właśnie po to, by dało się je zweryfikować.

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| ADD/ADC/SUB/SBC/AND/XOR/OR/CP z `r` i `(HL)` | `80`–`BF` | 64 |
| te same z `n` | `C6 CE D6 DE E6 EE F6 FE` | 8 |
| INC r, INC (HL) | `04 0C 14 1C 24 2C 34 3C` | 8 |
| DEC r, DEC (HL) | `05 0D 15 1D 25 2D 35 3D` | 8 |
| DAA, CPL, SCF, CCF | `27 2F 37 3F` | 4 |

## Nowe elementy

- Operacje ALU jako czyste funkcje (wejście: `A`, operand, `F`; wyjście: wynik, `F`) — testowalne bez magistrali.
- Rejestr `Q`: kopia `F` jeśli bieżąca instrukcja zmieniła flagi, inaczej `0`; potrzebny SCF/CCF. Semantykę **potwierdź na
  wektorach** (pole `q` przed i po), opis z README repo to tylko wskazówka.
- INC/DEC `(HL)` to odczyt–modyfikacja–zapis z dodatkowym T-state wewnętrznym (liczbę T-state bierz z `cycles`).

## Kryteria akceptacji

- [x] 92 pliki wektorów w repo i w manifeście, testy zielone (stan, `ram`, `cycles`).
- [x] Osobne testy jednostkowe czystych funkcji ALU dla przypadków granicznych (przeniesienie z bitu 3/7, przepełnienie).
- [x] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [x] Definicja ukończenia.

## Pułapki

1. **`X`/`Y` w `CP`** pochodzą z operandu, a nie z wyniku; potwierdzono na pełnych wektorach `B8`–`BF` i `FE`.
2. **`INC`/`DEC` nie zmieniają `C`.** **`SCF`/`CCF`** biorą `X`/`Y` z kombinacji `A` i poprzednich flag/`Q`
   — dla `Q=0` z `(A | F) & 0x28`, dla `Q!=0` z `A & 0x28`; potwierdzone na pełnych wektorach `37`/`3F`.
3. **`DAA`** zależy od `N`, `H`, `C` i wartości `A`; tablicę oczekiwań weź z wektorów, nie z algorytmu z pamięci.
4. **`F` jako jeden bajt** — nie rozbijaj na `bool`e, wektory porównują całą wartość (w tym `X`/`Y`).
5. **`SBC`/`SUB` a `H`** — borrow z bitu 4, nie 3; typowy błąd wychodzi dopiero na `ADC`/`SBC` z `C=1`.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | ADD/ADC/SUB/SBC/AND/XOR/OR/CP r/(HL) | 64/92 | 1280/1280 próbki; 64 000/64 000 pełne | 4 T-state dla r, 7 dla (HL) |
| 2026-10-22 | ALU n | 72/92 | 160/160 próbki; 8000/8000 pełne | 7 T-state |
| 2026-10-22 | INC/DEC r/(HL) | 88/92 | 320/320 próbki; 16 000/16 000 pełne | 4 T-state dla r, 11 dla (HL) |
| 2026-10-22 | DAA/CPL/SCF/CCF | 92/92 | 80/80 próbki; 4000/4000 pełne | 4 T-state; Q i X/Y zgodne |

Źródło weryfikacji: [SingleStepTests/z80](https://github.com/SingleStepTests/z80/tree/ebe1875d48f374bcfd4b505d8eb8ee751568b5f7/v1),
commit `ebe1875d48f374bcfd4b505d8eb8ee751568b5f7`. W repo pierwsze 20 rekordów/plik: **92 pliki, 1840 przypadków Z3**.
Pełny zestaw poza repo: **92 000 przeszło / 0 pominięto / 0 niezgodnych**. Runner w `/tmp` wywołał
ten sam harness porównujący stan, RAM, każdy T-state i rzeczywiste accessy pamięci; oczekiwania pochodzą z pełnych plików
źródłowych (1000/plik). Jest to zgodność z modelem Ares, nie weryfikacja na krzemie.

`Q=F` po każdej instrukcji Z3, `P=0`; INC/DEC zachowują C, a `(HL)` wykonuje odczyt (3 T-state),
wewnętrzny T-state z adresem HL i zapis (3 T-state), łącznie z fetch 11 T-state.
Czyste funkcje `Z80Alu` mają osobne testy graniczne; testy sekwencji sprawdzają Q po NOP i WAIT przed zapisem RMW.

Kontrola końcowa: build Debug i Release — po 0 ostrzeżeń i 0 błędów; testy Release — **1282 zaliczone**:
Z80 201, Mos6502 1014, Core 50, Memory 11, Cli 6; raportowane pominięcia 0.
Usunięto 12 powtórzonych wpisów danych CMOS w testach Mos6502; lista zachowuje 254 unikalne opkody,
a xUnit nie zgłasza już zduplikowanych identyfikatorów.
`dotnet format --verify-no-changes --severity warn` — exit 0; `OpcodeInfo --verify-code` — 512 wierszy CSV,
0 niezgodności (kontrola rodziny 6502; Z80 nie ma CSV).
