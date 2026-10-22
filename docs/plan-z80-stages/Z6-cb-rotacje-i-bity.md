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

- [ ] 260 plików wektorów w repo i w manifeście, testy zielone.
- [ ] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia.

## Pułapki

1. **`BIT n,(HL)`: `X`/`Y` z górnego bajtu `WZ`** (niezweryfikowane — wektory mają pole `wz` właśnie po to); dla `BIT n,r` z operandu.
2. **`BIT`** ustawia `Z` i `PV` identycznie, `S` tylko dla bitu 7, `H=1`, `N=0`, `C` bez zmian.
3. **`RLCA`/`RRCA`/`RLA`/`RRA`** (bez prefiksu) nie ustawiają `S`/`Z`/`PV`, w odróżnieniu od `cb 00`–`cb 1F`.
4. **`SLL`** wstawia `1` do bitu 0, nie `0` (niezweryfikowane — weryfikuje wektor).

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
