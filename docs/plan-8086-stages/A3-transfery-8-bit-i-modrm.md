# Etap A3 — transfery 8-bit i adresowanie ModR/M

Poziom: **II. Zestaw bazowy, część 1** · Wektory: grupy ruchu bajtów z manifestu A1

## Zakres

Implementować bazowe formy transferu bajtowego 8086: `MOV` rejestr↔rejestr, rejestr↔pamięć, immediate→rejestr i immediate→pamięć; `XLAT`; `LEA` w części wyliczania adresu efektywnego. Bajtowy `XCHG` należy do A3, słowny do A5; `LEA` zamknąć tutaj jako instrukcję zapisującą 16-bitowy offset, mimo nazwy etapu. Dokładne bajty/opkody i dostępność plików dopisać z manifestu A1 przed kodem.

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

## Kryteria akceptacji

- [ ] Manifest A3 zamknięty z liczbą plików/opkodów po A1.
- [ ] Wszystkie kombinacje EA przypisane do testów; brak duplikowanego kodu adresowania.
- [ ] Wektory próbki porównują stan, RAM, kolejkę i każdy cykl/pin.
- [ ] Brak opisu czasu „typowo”; liczby pochodzą z fixture lub źródła Intel.

## Pułapki

1. Rejestr `r/m` przy `mod=11` nie jest adresem pamięci.
2. `LEA` potrzebuje offsetu, nie odczytu spod efektywnego adresu.
3. Nie zakładać, że `XLAT` przechodzi przez dekoder ModR/M tylko dlatego, że używa adresowania. Współdzielenie obliczeń adresu potwierdzić dokumentacją w A1 (niezweryfikowane); `LEA` nie wykonuje odczytu operandu pamięciowego.
4. Segment override i segment domyślny wpływają na adres, nie na samo wyliczenie offsetu.
5. Nie kodować tabeli cykli opkodu jako substytutu sekwencji magistrali.

## Dziennik postępu

### 2026-10-23 — pierwsza grupa: `MOV r8, imm8`

Zaimplementowano pliki `B0`–`B7` (20 pierwszych rekordów z każdego) oraz bezpośrednie mapowanie AL/CL/DL/BL/AH/CH/DH/BH na rejestry słowne. Runner porównuje końcowe rejestry, RAM, kolejkę i wszystkie pola cyklu; rozpoznaje wariant T1/T2 pobrania kodu dla zaobserwowanego rozmiaru kolejki.

| forma | opkody | próbka | wynik |
|---|---|---:|---:|
| `MOV r8, imm8` | B0–B7 | 160 | 160/160 |
| `MOV r/m8,r8` / `MOV r8,r/m8`, tylko mod=11 | 88, 8A | 40 | 40/40 |
| NOP (regresja A2) | 90 | 20 | 20/20 |

A3 **nie jest ukończony**: ModR/M z operandem pamięci, segment override, `XCHG`, `XLAT` oraz `LEA` pozostają niezaimplementowane. Pełny zestaw B0–B7 dał 16 000/16 000, a osobny filtr wszystkich rejestrowych wariantów 88/8A dał 519/519. Mechanika cykli nadal jest ograniczona do zweryfikowanych form; nie ekstrapolować jej na inne instrukcje.

## Dziennik postępu

| data | rodzina/formy | opkody pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | próbka `MOV r8,imm8` B0–B7 | 8/323 plików aktywnych | 160/160 plus regresja NOP 20/20 | pozostała część A3 otwarta |
| 2026-10-23 | pełne `MOV r8,imm8` B0–B7 | 16 000/16 000; SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` | wszystkie stany, RAM, kolejka i T-states zgodne; 0 skip/mismatch | NOP pełne 2 000/2 000 w tym samym uruchomieniu; pozostałe formy A3 otwarte |
| 2026-10-23 | `MOV` bajtowy ModR/M mod=11 (`88`,`8A`) | 40 próbek, pierwsze 20 pasujących rekordów/opcode | 40/40; pełny podzbiór 243+276=519/519 | tylko rejestr–rejestr; fixture z pamięcią nieaktywne |
