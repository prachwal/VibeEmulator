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

- [x] 24 wpisy w katalogu (`OpcodeCatalog.Nmos.Shifts.cs`); 24 próbki wektorów w obu katalogach.
- [x] `--verify-code`: `mismatched: 0`, NMOS `missingInCode` **40 → 16**.
- [x] Wektory zielone dla obu wariantów (RMW różni się sekwencją dostępu — to jest sedno testu).
- [x] Test na różnicę wariantów: wektory `1E` (ASL, bez page cross) pokazują brak dummy read w 65C02
  i dummy read w NMOS; wektory `FE` (INC) pokazują dummy read w obu. Różnica jest w teście wektorowym, nie jednostkowym.

## Pułapki

1. **Akumulator vs pamięć.** Dwie ścieżki wykonania; pomylenie `OpKind` daje złe cykle i brak dummy read.
2. **RMW w `zp,X`** ma dodatkowy cykl indeksowania i dummy read spod bazy zerowej strony — to już jest w fazach,
   ale nowa akcja musi poprawnie działać i przy `zp`, i przy `zpx`/`abx`.
3. **`N`/`Z` przy `ROL`/`ROR`** liczone z wyniku, nie z operandu.
4. Nie dopisywać `INC A`/`DEC A` — te są 65C02 i już istnieją (`1A`, `3A`).

## Odkrycie w trakcie implementacji (do zapisania, nie do zgubienia)

65C02 RMW w `abs,X`/`abs,Y` **nie zachowuje się jednolicie** — dummy read zależy od akcji, nie tylko od trybu:

| akcja | NMOS | 65C02 bez page cross | 65C02 z page cross |
|---|---|---|---|
| INC/DEC | dummy read zawsze (7 cykli) | dummy read spod PC-1 (7 cykli) | dummy read spod PC-1 (7 cykli) |
| ASL/LSR/ROL/ROR | dummy read zawsze (7 cykli) | **brak** dummy read (6 cykli) | dummy read spod PC-1 (7 cykli) |

Dowód: wektory `FE` (INC, bez page cross, 7 cykli z dummy read spod PC-1) vs `1E` (ASL, bez page cross,
6 cykli, odczyt od razu spod adresu). CSV to potwierdza: INC/DEC `abx` mają `cykle_bazowe: 7` w obu zestawach,
ASL/LSR/ROL/ROR `abx` mają 7 w NMOS i 6 w 65C02 z `dodatkowe: +1 page cross`.

Implementacja: `StartIndexedAccess` rozróżnia po `_entry.Action` (nie po `OpKind`, bo rodzaj jest ten sam).
`_doubleReadRmw` służy jako znacznik wariantu 65C02. Formuła cykli w `CodeAnnotations` i korekta zestawu
w `CodeVerification` odzwierciedlają tę samą regułę.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | ASL/LSR/ROL/ROR + INC/DEC `zp,X`/`abs,X` | 24/24 | `--verify-code`: `mismatched: 0`, `missingInCode` NMOS = **16**; testy Mos6502 **402/402** (48 nowych przypadków wektorowych) | wektory: 24 pliki × 2 zestawy po 100 testów |
| 2026-10-21 | pełne wektory (10 000 na opkod) | 24/24 × 2 zestawy | NMOS: przeszło **240 000**, pominięto 0, 0 niezgodnych; 65C02: przeszło **240 000**, pominięto 0, 0 niezgodnych | runner poza repo (`/tmp`, nie commitowany), pliki z przypiętego commita, usuwane po przebiegu; uwaga: pierwsze uruchomienie padło na nieprzebudowanym runnerze (DLL z etapu A) — po rebuildzie zielono |

- [x] Pełne wektory (10 000 na opkod) — uruchomione poza repo: **24/24 opkody × 2 zestawy bez niezgodności**
  (240 000 + 240 000 testów przeszło, 0 pominięto, 0 niezgodnych).
