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

- [ ] 82 pliki wektorów w repo i w manifeście, testy zielone (stan, `ram`, `ports`, `cycles`).
- [ ] Ręczne testy gałęzi bez powtórzenia (`BC=1`): w próbce `ed b0` **wszystkie 1000 testów ma 21 T-state**, więc
      16-T-state wariant końcowy może nie być pokryty losowymi wektorami.
- [ ] Decyzja o niezdefiniowanych `ED xx` zapisana w dzienniku.
- [ ] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia.

## Pułapki

1. **`OUT (C),0` na NMOS** zapisuje inną wartość niż na CMOS (niezweryfikowane — zestaw ma CMOS wyłączony; sprawdź `ed 71`).
2. **`IN r,(C)` z `r=6`** ustawia tylko flagi, nie zapisuje rejestru (`ed 70`; niezweryfikowane).
3. **Flagi `X`/`Y` w instrukcjach blokowych** zależą od wariantu powtórzenia (niezweryfikowane); jeśli wektory i
   dokumentacja się różnią, opisz to (R1 w README).
4. **`RETI` i `RETN`** kopiują `iff2` do `iff1` — oba warianty na magistrali wyglądają tak samo, różnią je tylko stany.
5. **`INIR`/`OTIR` i port**: adres portu zależy od `B` przed lub po dekrementacji — kolejność z `ports`, nie z pamięci.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
