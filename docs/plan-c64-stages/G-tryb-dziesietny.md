# Etap G — tryb dziesiętny

Poziom: **A** (poprawność) · Opkody: **0** · Nowe fazy: 0 · Nowe akcje: 0

Etap nie zmienia liczby opkodów, ale **zdejmuje jedyny stały `Skip`** w testach wektorowych: `ADC`/`SBC` z `D=1`
(9 883 przypadki NMOS i 9 975 65C02).

## Zakres

- `ADC` i `SBC` z `D=1`: korekta BCD, semantyka `N`, `Z`, `V`, `C`.
- Różnica wariantów: NMOS i 65C02 różnią się **flagami** po operacji dziesiętnej (które dokładnie — z wektorów
  i [../../docs/cpu/decimal-mode.md](../../cpu/decimal-mode.md)).
- 65C02: `+1` cykl dla `ADC`/`SBC` w trybie dziesiętnym (potwierdzone wektorami; kolumna `wektory_cykle`
  w CSV pokazuje `4/5`, `5/6` itd.).

## Kryteria akceptacji

- [ ] Test dziesiętny Bruce’a Clarka (harness H5): `ERROR = 0`.
- [ ] Wektory `69`/`E9` (i pozostałych trybów `ADC`/`SBC`) **bez pominięć**.
- [ ] Usunięcie `Skip`/wyjątku w `SingleStepVectors` wraz z komentarzem, że etap 2e jest zamknięty.
- [ ] Cykle zgodne z CSV: `+1` w 65C02, bez zmian w NMOS.

## Pułapki

1. **`N`/`Z`/`V` w BCD** nie wynikają z prostego rzutowania wyniku — bierz z wektorów.
2. **Nie zmieniać zachowania binarnego** (`D=0`): wszystkie dotychczasowe wektory muszą przejść bez zmian.
3. **`+1` cykl tylko w 65C02** — wariant jako dana (`CpuVariant`), nie `if` w akcji.
4. `SED`/`CLD` już istnieją; ten etap nie dotyka ich.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/1 | `Skip` dla `69`/`E9` z `D=1` wciąż aktywny | etap otwarty |
