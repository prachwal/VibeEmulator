# Etap Z4 — operacje 16-bitowe, stos, wymiany

Poziom: **A** · Pliki wektorów: **31** · Nowe M-cykle: wewnętrzne T-state, zapis/odczyt 16-bitowy

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

- Zapis i odczyt 16-bitowy jako dwa M-cykle: LD i POP — młodszy bajt pierwszy; PUSH i zapisy
  EX (SP),HL — starszy bajt pierwszy. Potwierdzono na pełnych wektorach, źródło poniżej.
- Bank cieni (`af_ bc_ de_ hl_`) i wymiana w `EX AF,AF'`/`EXX`.
- INC/DEC rr i LD SP,HL: 6 T; ADD HL,rr: 11 T. Wewnętrzne T-state po fetch zachowują adres
  odświeżania sprzed inkrementacji R. Potwierdzono na pełnych wektorach, źródło poniżej.

## Kryteria akceptacji

- [x] 31 plików wektorów w repo i w manifeście, testy zielone.
- [x] Test jednostkowy: `EXX`/`EX AF,AF'` zamienia rejestry i jest odwracalne po drugim wywołaniu.
- [x] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [x] Definicja ukończenia.

## Pułapki

1. **`ADD HL,rr` nie zmienia `S`, `Z`, `PV`;** X/Y to bity 3/5 starszego bajtu wyniku,
   H to przeniesienie z bitu 11. Potwierdzone na 4000 pełnych wektorów `09 19 29 39`.
2. **`POP AF` i `PUSH AF`** przenoszą pełne `F`, łącznie z `X`/`Y`.
3. **`WZ`** po ADD HL,rr to stare HL + 1; po EX (SP),HL — słowo odczytane ze stosu;
   po LD (nn),HL / LD HL,(nn) — nn + 1 (modulo 65536). Potwierdzone na pełnych wektorach.
4. **`SP` to zwykła 16-bitowa liczba:** zawijanie przy PUSH/POP i EX (SP),HL sprawdza dodatkowy
   deterministyczny test na granicy `FFFF/0000`.
5. POP AF i EX AF,AF′ przenoszą F bez zmian, ale zerują Q; tylko ADD HL,rr z tego etapu zapisuje Q=F.
   Potwierdzone na pełnych 31 000 wektorów.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | LD rr,nn; LD (nn),HL; LD HL,(nn); LD SP,HL | 7/31 | 140/140 próbki; 7000/7000 pełne | 10 / 16 / 6 T-state |
| 2026-10-22 | INC/DEC rr; ADD HL,rr | 19/31 | 240/240 próbki; 12 000/12 000 pełne | 6 / 11 T-state; flagi i WZ zgodne |
| 2026-10-22 | PUSH/POP; EX (SP),HL | 28/31 | 180/180 próbki; 9000/9000 pełne | 11 / 10 / 19 T-state; kolejność dostępów zgodna |
| 2026-10-22 | EX DE,HL; EX AF,AF′; EXX | 31/31 | 60/60 próbki; 3000/3000 pełne | 4 T-state; test odwracalności |


Źródło: [SingleStepTests/z80](https://github.com/SingleStepTests/z80/tree/ebe1875d48f374bcfd4b505d8eb8ee751568b5f7/v1),
commit `ebe1875d48f374bcfd4b505d8eb8ee751568b5f7`. Próbki w repo: **31 plików × 20 = 620 przypadków**.
Pełne pliki poza repo: **31 000 przeszło / 0 pominięto / 0 niezgodnych**. Runner w `/tmp` używa tego samego
harnessu co próbki: cały stan, RAM, każdy T-state (adres/dana/piny) i rzeczywiste accessy pamięci.
Wektory pochodzą z modelu Ares, nie z pomiaru krzemu.

`Z80Core.InternalCycles` obsługuje wewnętrzne T-state z zachowaniem adresu i możliwością WAIT.
`WordCycles`/`StackCycles` realizują sekwencje odczytów i zapisów; `WordOperations` — pary i wymiany;
czysta funkcja `Z80Alu.Add16` liczy wynik i flagi. Testy dodatkowe obejmują przeniesienia, odwracalne wymiany,
pełne F przy PUSH/POP AF, zawijanie SP/PC/adresu słowa oraz WZ.

Regresja po zmianie wspólnych wewnętrznych T-state: pełne Z3 ponownie **92 000 przeszło / 0 pominięto /
0 niezgodnych**; razem pełne Z3–Z4: **123 000/123 000**. Pełnych Z1–Z2 nie uruchomiono.

Kontrola końcowa: build Debug i Release — po **0 ostrzeżeń i 0 błędów**; testy Release — **1326 zaliczonych,
0 nieudanych, 0 pominiętych** (Z80 245, Mos6502 1014, Core 50, Memory 11, Cli 6).
`dotnet format --verify-no-changes --severity warn` — exit 0; `OpcodeInfo --verify-code` — 512 wierszy CSV,
0 niezgodności (kontrola 6502; Z80 nie ma CSV). Manifest: 201 unikalnych opkodów; próbki Z4 zgodne z pierwszymi
20 rekordami źródła dla wszystkich 31 plików.
