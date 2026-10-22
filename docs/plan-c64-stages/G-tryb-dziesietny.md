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

- [ ] Test dziesiętny Bruce’a Clarka (harness H5): `ERROR = 0`. **Nie uruchomiony** — binarium trzeba
      zasemblować (AS65/CA65); zakres wektorowy jest pełny, ale H5 zostaje otwarty.
- [x] Wektory `69`/`E9` (i pozostałych trybów `ADC`/`SBC`) **bez pominięć** — 320 000 testów pełnych wektorów
      (16 form × 2 warianty), 0 pominięć, 0 niezgodności.
- [x] Usunięcie `Skip`/wyjątku w `SingleStepVectors` — porównanie bez wyjątków (komentarz w kodzie).
- [x] Cykle zgodne z CSV: `+1` w 65C02 (dana `DecimalExtraCycle`), bez zmian w NMOS.

## Pułapki

1. **`N`/`Z`/`V` w BCD** nie wynikają z prostego rzutowania wyniku — bierz z wektorów.
2. **Nie zmieniać zachowania binarnego** (`D=0`): wszystkie dotychczasowe wektory muszą przejść bez zmian.
3. **`+1` cykl tylko w 65C02** — wariant jako dana (`CpuVariant`), nie `if` w akcji.
4. `SED`/`CLD` już istnieją; ten etap nie dotyka ich.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/1 | `Skip` dla `69`/`E9` z `D=1` wciąż aktywny | etap otwarty |
| 2026-10-21 | cały etap | 1/1 | pełne wektory 320 000 (16 form × 2 warianty), 0 pominięć, 0 niezgodności; próbka 309/309; testy 461/461 | reguły z wektorów: NMOS — N z sumy bez korekty $A0, V z przepełnienia binarnego na górnych nibble, Z binarne; 65C02 — N/Z z wyniku BCD, V/C jak NMOS; 65C02 +1 cykl (powtórka odczytu operandu, imm: stała per akcja); H5 nie uruchomiony |
