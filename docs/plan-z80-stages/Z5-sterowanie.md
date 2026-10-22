# Etap Z5 — skoki, wywołania, powroty, HALT, DI/EI

Poziom: **A** · Pliki wektorów: **45** · Nowe elementy: warunki, `ei`, HALT

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| `JP nn`, `JP cc,nn` | `C3`; `C2 CA D2 DA E2 EA F2 FA` | 9 |
| `JP (HL)` | `E9` | 1 |
| `JR e`, `JR cc,e` | `18`; `20 28 30 38` | 5 |
| `DJNZ e` | `10` | 1 |
| `CALL nn`, `CALL cc,nn` | `CD`; `C4 CC D4 DC E4 EC F4 FC` | 9 |
| `RET`, `RET cc` | `C9`; `C0 C8 D0 D8 E0 E8 F0 F8` | 9 |
| `RST p` | `C7 CF D7 DF E7 EF F7 FF` | 8 |
| `HALT` | `76` | 1 |
| `DI`, `EI` | `F3 FB` | 2 |

## Nowe elementy

- Warunki `NZ Z NC C PO PE P M` — jedna funkcja na podstawie pola 3-bitowego i `F`.
- Zachowanie „warunek niespełniony” skraca instrukcję (liczba T-state z wektorów, dwa warianty na opkod).
- `ei` w stanie: opóźnienie włączenia przerwań o jedną instrukcję po `EI`. Pole `ei` wektora pokazuje stan przed/po.
- `HALT`: z próbki `76` — 4 T-state, `pc` po instrukcji = `pc`+1. Zachowanie **w czasie** (powtarzanie pobrań NOP do
  przerwania) wektory jednej instrukcji nie pokrywają — patrz Z10.

## Kryteria akceptacji

- [ ] 45 plików wektorów w repo i w manifeście, testy zielone.
- [ ] Ręczne testy dla gałęzi, które losowe wektory mogą rzadko trafiać (`DJNZ` z `B=1`, `JR` przez granicę strony).
- [ ] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia.

## Pułapki

1. **`WZ`** ustawiają `JP`, `CALL`, `RET`, `JR`, `DJNZ` (także gdy warunek niespełniony — niezweryfikowane); pole `wz` decyduje.
2. **`JP (HL)`** to w istocie `LD PC,HL` — nie czyta pamięci pod `HL`.
3. **`RST`** używa starego `PC`+1 jako adresu powrotu; adres docelowy to `p`·8.
4. **Nie dodawaj tu przyjmowania przerwań.** `EI`/`DI` zmieniają flagi `iff1`/`iff2`/`ei`, ale akceptacja IRQ to Z10.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
