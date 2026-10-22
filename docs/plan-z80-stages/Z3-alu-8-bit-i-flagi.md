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

- [ ] 92 pliki wektorów w repo i w manifeście, testy zielone (stan, `ram`, `cycles`).
- [ ] Osobne testy jednostkowe czystych funkcji ALU dla przypadków granicznych (przeniesienie z bitu 3/7, przepełnienie).
- [ ] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia.

## Pułapki

1. **`X`/`Y` w `CP`** pochodzą z operandu, a nie z wyniku (niezweryfikowane — wektory rozstrzygają; sprawdź na `B8`–`BF`).
2. **`INC`/`DEC` nie zmieniają `C`.** **`SCF`/`CCF`** biorą `X`/`Y` z kombinacji `A` i poprzednich flag/`Q`
   (niezweryfikowane) — po to jest pole `q`.
3. **`DAA`** zależy od `N`, `H`, `C` i wartości `A`; tablicę oczekiwań weź z wektorów, nie z algorytmu z pamięci.
4. **`F` jako jeden bajt** — nie rozbijaj na `bool`e, wektory porównują całą wartość (w tym `X`/`Y`).
5. **`SBC`/`SUB` a `H`** — borrow z bitu 4, nie 3; typowy błąd wychodzi dopiero na `ADC`/`SBC` z `C=1`.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
