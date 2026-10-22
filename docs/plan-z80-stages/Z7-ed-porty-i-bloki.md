# Etap Z7 — prefiks `ED`, porty I/O, instrukcje blokowe

Poziom: **B** · Pliki wektorów: **82** (80 `ed NN` + `DB` + `D3`) · Nowe: magistrala portów, prefiks `ED`, powtarzanie

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| `IN A,(n)`, `OUT (n),A` | `DB D3` (bez prefiksu) | 2 |
| `ed 40`–`ed 7F` | `IN r,(C)`, `OUT (C),r`, `SBC/ADC HL,rr`, `LD (nn),rr`, `LD rr,(nn)`, `NEG`, `RETN`/`RETI`, `IM`, `LD I,A`/`LD R,A`/`LD A,I`/`LD A,R`, `RRD`/`RLD` oraz ich nieudokumentowane duplikaty | 64 |
| blokowe | `A0–A3` (`LDI CPI INI OUTI`), `A8–AB` (`LDD CPD IND OUTD`), `B0–B3` (`LDIR CPIR INIR OTIR`), `B8–BB` (`LDDR CPDR INDR OTDR`) | 16 |

W zestawie **nie ma** plików dla pozostałych `ED xx` (`ED 00`–`3F`, `80`–`9F` poza blokowymi, `C0`–`FF`). Zachowanie
niezdefiniowanych `ED xx` jest więc **poza pokryciem wektorów** — decyzja do wpisania w dzienniku (NOP 8 T-state to
częste założenie, **niezweryfikowane**).

## Nowe elementy

- Użycie `IPortBus`: M-cykl I/O (4 T-state w próbce `db`, adres portu `A<<8 | n` dla `IN A,(n)`).
- Pole `ports` wektora: kolejność i wartości dostępów do portów porównywane razem z `cycles`.
- Powtarzanie: końcowy `pc` równy początkowemu, gdy instrukcja się powtarza (zmierzone na `ed b0`, test 0); 21 T-state.
- Pole `p` (`LD A,I`/`LD A,R` a przerwanie) — semantykę z wektorów.

## Kryteria akceptacji

- [x] 82 pliki wektorów w repo i w manifeście, próbki 20/opkod przechodzą (stan, `ram`, `ports`, `cycles`).
- [x] Ręczne testy bez powtórzenia (`BC=1` / `B=1`) dla `LDIR`, `CPIR`, `INIR`, `OTIR`; każda instrukcja trwa 16 T-state.
- [x] Niezdefiniowane `ED xx` traktujemy jako 8-T-state NOP **(niezweryfikowane)**; źródło przypiętych wektorów nie zawiera tych opkodów.
- [x] Pełne wektory poza repo: 82 000/82 000.
- [x] Definicja ukończenia: wszystkie 80 dostępnych `ED` i oba nieprefiksowane portowe opkody przechodzą pełny stan, RAM, piny, cykle i transakcje portów.

## Pułapki

1. **`OUT (C),0`** zapisuje `0` w generatorze wektorów; generator ma CMOS wyłączony. Zachowanie CMOS nie jest pokryte.
2. **`IN (C)` (`r=6`)** pobiera wartość i ustawia flagi bez zapisu do rejestru; potwierdzone pełnymi wektorami `ED 70`.
3. **Flagi `X`/`Y` w powtarzanych instrukcjach blokowych** pochodzą z PC; potwierdzone wektorami i przypiętym generatorem.
4. **`RETI` i `RETN`** kopiują `iff2` do `iff1`; pełne wektory obu aliasów przechodzą.
5. **`INIR`/`OTIR` i port**: adres portu uwzględnia dekrementację `B` dla OUT; potwierdzone wpisami `ports` w wektorach.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | `ED 40`–`ED 7F` | ukończone | 64 próbki; 64 000 pełnych rekordów | I/O, ALU 16-bit, pamięć, NEG, RETN/RETI, IM, I/R, RRD/RLD; źródło: [generator z przypiętego commita](https://github.com/SingleStepTests/z80/blob/ebe1875d48f374bcfd4b505d8eb8ee751568b5f7/generation/z80_test_generator.js) |
| 2026-10-22 | instrukcje blokowe | ukończone | 16 próbek; 16 000 pełnych rekordów; 4 testy końcowej iteracji | transfer, porównanie i I/O; przypadki powtarzania oraz końcowe |
| 2026-10-22 | `IN A,(n)` / `OUT (n),A` | ukończone | 2 próbki; 2 000 pełnych rekordów | zachowany 8-bitowy przyrost dolnego bajtu WZ dla OUT |
