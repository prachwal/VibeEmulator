# Etap Z6 — prefiks `CB`: rotacje, przesunięcia, bity

Poziom: **B** · Pliki wektorów: **260** (256 `cb NN` + 4 bez prefiksu) · Nowy prefiks: `CB`

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| rotacje i przesunięcia (`RLC RRC RL RR SLA SRA SLL SRL`) | `cb 00`–`cb 3F` | 64 |
| `BIT n,r` | `cb 40`–`cb 7F` | 64 |
| `RES n,r` | `cb 80`–`cb BF` | 64 |
| `SET n,r` | `cb C0`–`cb FF` | 64 |
| akumulatorowe `RLCA RRCA RLA RRA` | `07 0F 17 1F` | 4 |

`SLL` (`cb 30`–`cb 37`) jest nieudokumentowany, ale ma pliki wektorów — implementujemy go.

## Nowe elementy

- Obsługa prefiksu: pobranie `CB` jest M1 (zwiększa `r`), drugie pobranie to też M1; łącznie 8 T-state dla `cb NN` na rejestrze
  (liczby T-state z wektorów).
- Rotacje na `(HL)` i `BIT n,(HL)` mają różne czasy — to RMW vs tylko odczyt.
- Flagi rotacji: parzystość w `PV`, `X`/`Y` z wyniku.

## Kryteria akceptacji

- [x] 260 plików wektorów w repo i w manifeście (pierwsze 20 rekordów), testy zielone.
- [x] Pełne wektory: wynik wpisany poniżej.
- [x] Definicja ukończenia: wszystkie operacje `CB` i cztery rotacje akumulatora przechodzą wektory; rozróżnione cykle M1, odczyt i zapis `(HL)`, flagi oraz stan końcowy.

## Pułapki

1. **`BIT n,(HL)`: `X`/`Y` z górnego bajtu `WZ`** (potwierdzone pełnymi wektorami); dla `BIT n,r` z operandu.
2. **`BIT`** ustawia `Z` i `PV` identycznie, `S` tylko dla bitu 7, `H=1`, `N=0`, `C` bez zmian.
3. **`RLCA`/`RRCA`/`RLA`/`RRA`** (bez prefiksu) nie ustawiają `S`/`Z`/`PV`, w odróżnieniu od `cb 00`–`cb 1F`.
4. **`SLL`** wstawia `1` do bitu 0, nie `0` (potwierdzone pełnymi wektorami).

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | rotacje i przesunięcia | ukończone | 64 pliki próbek; pełne wektory: 64 000/64 000 | `RLC`…`SRL`, także `SLL`; `(HL)` ma 15 T-state |
| 2026-10-22 | `BIT` | ukończone | 64 pliki próbek; pełne wektory: 64 000/64 000 | `(HL)` ma 12 T-state; `X`/`Y` z `WZ` |
| 2026-10-22 | `RES` / `SET` | ukończone | 128 plików próbek; pełne wektory: 128 000/128 000 | zapisują wynik dla `(HL)`, flagi bez zmian |
| 2026-10-22 | rotacje akumulatora | ukończone | 4 pliki próbek; pełne wektory: 4 000/4 000 | `S`/`Z`/`PV` zachowane |
