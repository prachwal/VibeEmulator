# Etap Z4 — operacje 16-bitowe, stos, wymiany

Poziom: **A** · Pliki wektorów: **31** · Nowe M-cykle: wewnętrzne 2–T, zapis/odczyt 16-bitowy

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| `LD rr,nn` | `01 11 21 31` | 4 |
| `LD (nn),HL`, `LD HL,(nn)` | `22 2A` | 2 |
| `LD SP,HL` | `F9` | 1 |
| `INC rr`, `DEC rr` | `03 13 23 33`, `0B 1B 2B 3B` | 8 |
| `ADD HL,rr` | `09 19 29 39` | 4 |
| `PUSH rr`, `POP rr` | `C5 D5 E5 F5`, `C1 D1 E1 F1` | 8 |
| `EX (SP),HL` | `E3` | 1 |
| `EX DE,HL`, `EX AF,AF'`, `EXX` | `EB 08 D9` | 3 |

## Nowe elementy

- Zapis i odczyt 16-bitowy jako dwa M-cykle (młodszy bajt pierwszy); kolejność **potwierdź w `cycles`**,
  szczególnie dla `PUSH` (starszy bajt pierwszy) i `EX (SP),HL`.
- Bank cieni (`af_ bc_ de_ hl_`) i wymiana w `EX AF,AF'`/`EXX`.
- Wewnętrzne T-state (`INC rr`: 6 T, `ADD HL,rr`: 11 T wg datasheetu — **niezweryfikowane**, wektory decydują).

## Kryteria akceptacji

- [ ] 31 plików wektorów w repo i w manifeście, testy zielone.
- [ ] Test jednostkowy: `EXX`/`EX AF,AF'` zamienia rejestry i jest odwracalne po drugim wywołaniu.
- [ ] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia.

## Pułapki

1. **`ADD HL,rr` nie zmienia `S`, `Z`, `PV`;** `X`/`Y`/`H` pochodzą z bajtu starszego (niezweryfikowane — sprawdź na `29`, gdzie `rr = HL`).
2. **`POP AF` i `PUSH AF`** przenoszą pełne `F`, łącznie z `X`/`Y`.
3. **`WZ`** po `ADD HL,rr` i `EX (SP),HL` — wartość z pola `wz` wektora.
4. **`SP` to zwykła 16-bitowa liczba:** zawijanie `0000` ↔ `FFFF` przy `PUSH`/`POP` pokryją losowe testy; nie dodawaj osobnych klauzul.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
