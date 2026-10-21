# Etap B — przesunięcia, rotacje i RMW indeksowane

Poziom: **A** · Opkody: **24** · Nowe tryby: 0 · Nowe fazy: 0 · Nowe akcje: 4

Po etapie: **135/151** oficjalnych opkodów NMOS (89,4%).

## Zakres

| grupa | opkody | tryby | akcja |
|---|---|---|---|
| ASL | `06 0A 0E 16 1E` | zp, **A**, abs, zpx, abx | `Asl` |
| LSR | `46 4A 4E 56 5E` | jw. | `Lsr` |
| ROL | `26 2A 2E 36 3E` | jw. | `Rol` |
| ROR | `66 6A 6E 76 7E` | jw. | `Ror` |
| INC | `F6 FE` | zpx, abx | `Inc` (istnieje) |
| DEC | `D6 DE` | zpx, abx | `Dec` (istnieje) |

## Nowe elementy

- `CpuAction.Asl`, `Lsr`, `Rol`, `Ror`.
- Wariant **akumulatorowy** (`0A 4A 2A 6A`) używa `OpKind.Accumulator` (dummy read spod `PC`, 2 cykle) — nowa
  akcja musi być obsłużona w `ApplyImplied`, nie w `ApplyReadToRegister`.
- Wariant **pamięciowy** to `OpKind.ReadModifyWrite` — przechodzi przez `RmwOld`/`RmwNew`, więc styl RMW
  (dwa zapisy NMOS / dwa odczyty 65C02) działa bez zmian.

## Semantyka

- `ASL`: `C` ← bit 7, wynik ← `M << 1`; `LSR`: `C` ← bit 0, wynik ← `M >> 1`.
- `ROL`/`ROR`: `C` wchodzi do bitu 0 / 7, a wypadający bit trafia do `C`.
- `N`/`Z` z wyniku. Kolejność ma znaczenie: najpierw odczytaj stary `C` dla ROL/ROR, potem ustaw nowy.

## Kryteria akceptacji

- [ ] 24 wpisy w katalogu; 24 próbki wektorów w obu katalogach.
- [ ] `--verify-code`: NMOS `missingInCode` **40 → 16**.
- [ ] Wektory zielone dla obu wariantów (RMW różni się sekwencją dostępu — to jest sedno testu).
- [ ] Test na różnicę wariantów: ten sam `INC abs` daje dwa zapisy (NMOS) i dwa odczyty + zapis (65C02).

## Pułapki

1. **Akumulator vs pamięć.** Dwie ścieżki wykonania; pomylenie `OpKind` daje złe cykle i brak dummy read.
2. **RMW w `zp,X`** ma dodatkowy cykl indeksowania i dummy read spod bazy zerowej strony — to już jest w fazach,
   ale nowa akcja musi poprawnie działać i przy `zp`, i przy `zpx`/`abx`.
3. **`N`/`Z` przy `ROL`/`ROR`** liczone z wyniku, nie z operandu.
4. Nie dopisywać `INC A`/`DEC A` — te są 65C02 i już istnieją (`1A`, `3A`).

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/24 | `missingInCode` NMOS = 40 | etap otwarty |
