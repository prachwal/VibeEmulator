# Etap A3 — transfery 8-bit i adresowanie ModR/M

Poziom: **II. Zestaw bazowy, część 1** · Wektory: grupy ruchu bajtów z manifestu A1

## Zakres

Implementować bazowe formy transferu bajtowego 8086: `MOV` rejestr↔rejestr, rejestr↔pamięć, immediate→rejestr i immediate→pamięć; `XLAT`; `LEA` w części wyliczania adresu efektywnego. Bajtowy `XCHG` należy do A3, słowny do A5; `LEA` zamknąć tutaj jako instrukcję zapisującą 16-bitowy offset. Z przypiętego źródła wycięto próbkę LEA 20 rekordów; pełny podzbiór pamięciowych kodowań zawiera 996 rekordów.

## Kolejność prac

1. Dekoder ModR/M: `mod`, `reg`, `r/m`; rejestry 8-bit; rozróżnienie rejestru od pamięci.
2. Obliczenie 16-bitowego effective offset ze wszystkich kombinacji bazowych i indeksowych opisanych w dokumentacji 8086; displacement 0/8/16-bit; przypadek bezpośredni.
3. Wybór segmentu domyślnego i override z prefiksu, z testami niezależnymi od wykonania instrukcji.
4. Operandy rejestrowe oraz immediate; długość instrukcji i przesuwanie wskaźnika bytecode.
5. Transfer `MOV` pamięciowy i read/write cykl po cyklu.
6. Bajtowy `XCHG`, `XLAT` i `LEA` dopiero po wspólnych testach EA/segmentów.
7. Dodać każdy zaimplementowany plik do manifestu i uruchomić wszystkie rekordy próbki.

## Nowe komponenty testowe

- Testy tabeli rejestrów ModR/M dla byte registers, bez ręcznej kopii dekodowania w każdej instrukcji.
- Macierz EA: bazowy, indeksowy, baza+indeks, displacement, parzystość adresu, segment domyślny/override.
- Test pamięci: read/write sekwencja, bajty zmienione i nietknięte, operand o adresie granicznym.
- Wektory każdej formy `MOV`, `XLAT`, `LEA` dostępnej w przypiętym zestawie; dla nieobecnych danych oddzielny test z cytowanym źródłem producenta.

## Testy i kryteria

- [x] Manifest A3 zamknięty: 23 pliki, 458 rekordów próbek, 15 wartości opkodu; źródłowy inwentarz nadal zachowuje wszystkie 323 pliki.
- [x] Wspólny dekoder EA ma 38 testów, wszystkie przechodzą: osiem pól `r/m` dla mod=00/01/10, direct disp16, disp8 ze znakiem, zawijanie offsetu i 20-bitowego adresu fizycznego, domyślne DS/SS (w tym BP+SI/BP+DI dla mod=00) oraz cztery segment overrides.
- [x] Pełna macierz pamięciowych form A3 przechodzi wektory z porównaniem kolejności read/write i sygnałów: `88/8A` bez prefiksu 1533/1533, z prefiksem 1423/1423, `86` pamięć 1509/1509 oraz `C6 /0` pamięć 192/192. Wektory obejmują adresy obu parzystości; test end-to-end potwierdza zapis `C6 /0` przez zawinięcie granicy 1 MiB.
- [x] Aktywne wektory CPU A3 (`B0`–`B7`, `88/8A mod=11`, `86`, `C6 /0`, NOP regresji) porównują stan, RAM, kolejkę i każdy cykl/pin; manifest A3: 458/458.
- [x] Nieprefiksowane pamięciowe formy `MOV` (`88/8A`, wszystkie `mod != 11`) porównują pełne CPU/RAM/kolejkę/cykle: 753 + 780 = 1533/1533.
- [x] Pamięciowe formy `MOV` z prefiksami segmentowymi przechodzą pełne porównanie wektorów: 1423/1423, 0 skip, 0 mismatch.
- [x] `MOV imm8,r/m8` (`C6 /0`) przechodzi próbkę 78/78 oraz pełny podzbiór 236/236: 103 nieprefiksowane pamięciowe, 89 pamięciowych z prefiksem segmentowym, 26 rejestrowych i 18 rejestrowych z prefiksem; 0 skip, 0 mismatch.
- [x] Rejestrowa forma bajtowego `XCHG` (`86`, `mod=11`, także z prefiksem segmentowym) przechodzi próbkę 20/20 i pełny podzbiór: 491/491, CPU, RAM, kolejka i T-states.
- [x] Pamięciowa forma bajtowego `XCHG` (`86`, `mod!=11`, z prefiksami segmentowymi) przechodzi próbkę 20/20 i pełny podzbiór 1509/1509; razem cały plik `86`: 2000/2000, 0 skip, 0 mismatch.
- [x] Próbka `XLAT` (D7), w tym prefiksy ES/CS/SS/DS: 20/20 pełnych porównań CPU, RAM, kolejki i cykli/pinów.
- [x] Pełny plik `XLAT` z przypiętego SST: 2000/2000, 0 pominiętych, 0 niezgodnych.
- [x] Próbka pamięciowych form `LEA` (ModR/M `mod != 11`): 20/20, pełne porównanie CPU, RAM, kolejki i każdego cyklu/pinu.
- [x] Pełny podzbiór pamięciowych form `LEA` z przypiętego SST: 996/996, 0 pominiętych, 0 niezgodnych.
- [x] Dokumentacja podaje wyłącznie zmierzone wyniki fixture/testów i wskazuje źródło Intel dla reguł EA.

Próbki A3 aktywne w manifeście: **458 rekordów w 23 plikach**; wszystkie kryteria implementacyjne A3 są zamknięte.

## Pułapki

1. Rejestr `r/m` przy `mod=11` nie jest adresem pamięci.
2. `LEA` potrzebuje offsetu, nie odczytu spod efektywnego adresu.
3. Nie zakładać, że `XLAT` przechodzi przez dekoder ModR/M tylko dlatego, że używa adresowania. Współdzielenie obliczeń adresu potwierdzić dokumentacją w A1 (niezweryfikowane); `LEA` nie wykonuje odczytu operandu pamięciowego.
4. Segment override i segment domyślny wpływają na adres, nie na samo wyliczenie offsetu.
5. Nie kodować tabeli cykli opkodu jako substytutu sekwencji magistrali.

## Dziennik postępu

### Kryteria zamknięcia A3

A3 jest zaimplementowane w zakresie planu: immediate→rejestr, `MOV 88/8A`, `MOV C6 /0`, wszystkie formy bajtowego `XCHG 86`, dekoder EA, pamięciowe `LEA` i `XLAT`. Próbki A3 aktywne: 458/458; pełny podzbiór C6 /0: 236/236; pełne podzbiory pamięciowe 88/8A, XCHG i C6 są opisane poniżej.


### 2026-10-23 — dekoder adresu efektywnego ModR/M

Dodano współdzielony dekoder kombinacji bazowych/indeksowych, `disp8` ze znakiem, `disp16`, bezpośredniego `mod=00,r/m=110`, domyślnego DS/SS, override segmentu i 20-bitowego adresu fizycznego. Typ override przeniesiono do podstawowego API A3, aby A10 mógł używać tego samego enumu. Tabele kodowania i domyślnego segmentu odpowiadają sekcji adresowania efektywnego i tabeli 2-2 podręcznika Intel 8086 Family User's Manual (1979), odnotowanego w `docs/cpu/sources.md`.

Testy jednostkowe obejmują osiem kombinacji `r/m` dla mod=00/mod=01/mod=10, znaki i zawijanie offsetu, direct address, wybory segmentu i override oraz zawijanie adresu fizycznego 20-bit: **38/38 przechodzi**. Osobny test end-to-end potwierdza, że `C6 /0` zapisuje pod fizyczny adres zero po zawinięciu segmentu DS=`FFFF` i offsetu `0010`. W toku A3 wykryto, że domyślny segment dla `[BP+SI]` i `[BP+DI]` przy mod=00 też musi być SS; poprawiono wybór segmentu i dodano osiem przypadków mod=00. Testy adresu nie zaliczają wektorów cyklu CPU.

### 2026-10-23 — pierwsza grupa: `MOV r8, imm8`

Zaimplementowano pliki `B0`–`B7` (20 pierwszych rekordów z każdego) oraz bezpośrednie mapowanie AL/CL/DL/BL/AH/CH/DH/BH na rejestry słowne. Runner porównuje końcowe rejestry, RAM, kolejkę i wszystkie pola cyklu; rozpoznaje wariant T1/T2 pobrania kodu dla zaobserwowanego rozmiaru kolejki.

| forma | opkody | próbka | wynik |
|---|---|---:|---:|
| `MOV r8, imm8` | B0–B7 | 160 | 160/160 |
| `MOV r/m8,r8` / `MOV r8,r/m8`, tylko mod=11 | 88, 8A | 40 | 40/40 |
| NOP (regresja A2) | 90 | 20 | 20/20 |

A3 jest ukończony w zdefiniowanym zakresie manifestu i źródeł A1. `LEA` obsługuje pamięciowe kodowania ModR/M bez odczytu operandu z RAM; `XLAT` odczytuje `DS:BX+AL` albo segment nadpisany. Pełny zestaw B0–B7 dał 16 000/16 000, 88/8A mod=11 519/519, nieprefiksowane pamięciowe 88/8A 1533/1533, prefiksowane pamięciowe 88/8A 1423/1423, pamięciowe `LEA` 996/996, D7 2000/2000 i całe `86` 2000/2000. Mechanika cykli jest potwierdzona tylko dla wskazanych form i filtrów.

## Dziennik postępu

| data | rodzina/formy | opkody pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | próbka `MOV r8,imm8` B0–B7 | 8/323 plików aktywnych | 160/160 plus regresja NOP 20/20 | pozostała część A3 otwarta |
| 2026-10-23 | pełne `MOV r8,imm8` B0–B7 | 16 000/16 000; SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` | wszystkie stany, RAM, kolejka i T-states zgodne; 0 skip/mismatch | NOP pełne 2 000/2 000 w tym samym uruchomieniu; pozostałe formy A3 otwarte |
| 2026-10-23 | `MOV` bajtowy ModR/M mod=11 (`88`,`8A`) | 40 próbek, pierwsze 20 pasujących rekordów/opcode | 40/40; pełny podzbiór 243+276=519/519 | tylko rejestr–rejestr; fixture z pamięcią nieaktywne |
| 2026-10-24 | `LEA` pamięciowy ModR/M (`8D`, `mod != 11`) | 20 próbek | 20/20; pełny podzbiór 996/996, 0 skip, 0 mismatch | offset EA trafia do pola `reg`; brak odczytu operandu z RAM; cykle i kolejka zgodne z przypiętym SST |
| 2026-10-24 | `XLAT` (`D7`) i prefiksy segmentowe | 20 próbek | 20/20; pełny plik 2000/2000, 0 skip, 0 mismatch | poprawny odczyt DS/ES/CS/SS:`BX+AL`, pełne T-states i kolejka prefetch |
| 2026-10-24 | nieprefiksowane `MOV` pamięciowe (`88/8A`, `mod != 11`) | 40 próbek; pełne podzbiory 753 + 780 | 40/40; pełny podzbiór 1533/1533, 0 skip, 0 mismatch | aktywne próbki w `A3MovMemory/`; segment-prefiksowane MOV potwierdzone osobnym pełnym przebiegiem, bajtowy XCHG nadal otwarty |
| 2026-10-24 | segment-prefiksowane `MOV` pamięciowe (`88/8A`, `mod != 11`) | 40 próbek | 40/40; pełny podzbiór 1423/1423, 0 skip, 0 mismatch | aktywne próbki w `A3MovMemory/*-prefix.json`; cztery prefiksy ES/CS/SS/DS |
| 2026-10-24 | bajtowy `XCHG` (`86`, wszystkie mody i segment overrides) | 40 próbek; pełny plik 2000 rekordów (491 rejestrowych + 1509 pamięciowych) | 40/40; pełny plik 2000/2000, 0 skip, 0 mismatch | pełne porównanie stanu, RAM, kolejki i T-states/pinów |
| 2026-10-24 | `MOV imm8,r/m8` (`C6 /0`, rejestr/pamięć, segment overrides) | 78 próbek; pełny filtrowany podzbiór 236 rekordów (192 pamięciowe + 44 rejestrowe) | 78/78; pełne 236/236, 0 skip, 0 mismatch | rozszerzenia ModR/M inne niż `/0` pozostają poza zdefiniowaną formą instrukcji |

### Analiza źródłowych wektorów pamięciowych `MOV 88/8A`

Przefiltrowano przypięte pliki SST `88.json.gz` i `8A.json.gz` po rzeczywistym położeniu opkodu (rekordy z prefiksem mają opkod pod `bytes[1]`) oraz `ModR/M.mod != 11`. Plik `88` zawiera 753, a `8A` 780 nieprefiksowanych rekordów pamięciowych. Profile MEMR/MEMW T1 różnią się zależnie od `mod/rm` i liczby bajtów w początkowej kolejce:

| kierunek i forma | kolejka 5 | kolejka 6 |
|---|---:|---:|
| `88`, mod=00 `[SI]` (rm=4), MEMW | cykl 12 | cykl 12 |
| `88`, mod=00 `[BX+SI]` (rm=0), MEMW | cykl 14 | cykl 14 |
| `88`, mod=01 `[SI+disp8]` (rm=4), MEMW | cykl 16 | cykl 16 |
| `88`, mod=10 `[SI+disp16]` (rm=4), MEMW | cykl 16 | cykl 17 |
| `8A`, mod=00 `[SI]` (rm=4), MEMR | cykl 9 | cykl 8 |
| `8A`, mod=00 `[BX+SI]` (rm=0), MEMR | cykl 10 | cykl 10 |
| `8A`, mod=01 `[SI+disp8]` (rm=4), MEMR | cykl 12 | cykl 12 |
| `8A`, mod=10 `[SI+disp16]` (rm=4), MEMR | cykl 12 | cykl 13 |

To są pozycje T1 z wybranych profili źródłowych. Próbki prefiksowane i nieprefiksowane (po 20 na opcode) są aktywne; pełne podzbiory pamięciowe 88/8A bez prefiksu przechodzą **1533/1533**, a z prefiksami **1423/1423**, 0 pominiętych i 0 niezgodnych. Porównanie obejmuje CPU, RAM, kolejkę oraz wszystkie T-states/piny. Bajtowy `XCHG` jest zweryfikowany osobno: 2000/2000.
