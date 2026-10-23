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

| data | rodzina/formy | opkody pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| — | — | — | nie uruchomiono | opkody do przypięcia po A1 |
