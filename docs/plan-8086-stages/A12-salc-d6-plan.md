# A12 — `SALC` (`D6`)

## Cel

Zaimplementować i zweryfikować jednobajtowy, nieudokumentowany na 8086 opcode `D6` na podstawie przypiętych wektorów SingleStepTests. Zakres obejmuje wersję bez prefiksu i cztery prefiksy segmentowe. Plan i implementacja pozostają osobnymi commitami na branchu `A12-8086-pelna-weryfikacja`.

## Źródła i inwentaryzacja

- Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, plik `v1/D6.json.gz`, 2 000 rekordów.
- Inwentaryzacja źródła: 981 rekordów `D6` bez prefiksu; 1 019 z prefiksem segmentowym: ES 237, CS 269, SS 247, DS 266.
- Wszystkie 2 000 rekordów potwierdzają `AL = 0xFF` dla `CF=1` i `AL = 0x00` dla `CF=0`; pozostała część `AX` i FLAGS jest zachowana. Nie przypisywać tej semantyki na podstawie samej nazwy.
- Długość śladów wynosi 3–6 T-state; należy zachować przebieg prefetch i opóźnienie zależne od obecności prefiksu, zgodnie z rekordami SST.
- Kontekst dla nazwy/klasy instrukcji: Intel 8086 Family User’s Manual (1979) nie określa `D6`; opublikowane zestawienia nieoficjalnych opkodów opisują `SALC` jako `AL := CF ? FFh : 00h` bez zmiany FLAGS. Źródłem wykonawczym i cykli w tym pakiecie pozostaje przypięty plik SST. Odnośnik kontekstowy: [OS/2 Museum — Undocumented 8086 Opcodes](https://www.os2museum.com/wp/undocumented-8086-opcodes-part-i/).

## Zakres

1. Rozpoznawać `D6` bez prefiksu i po jednym prefiksie `ES`, `CS`, `SS` lub `DS`.
2. Ustawić tylko AL na maskę Carry (`00h` albo `FFh`); zachować AH, FLAGS i niepowiązane rejestry.
3. Odtworzyć IP, kolejkę prefetch, T-state oraz wynikowy stan według wszystkich rekordów `D6.json.gz`.
4. Dodać do repo deterministyczne fixture’y: maksymalnie pierwsze 20 rekordów dla każdej z pięciu grup (łącznie do 100), bez zmieniania rekordów upstream.
5. Nie zmieniać zachowania pozostałych nieudokumentowanych kodowań `D0–D3 /6`.

## Plan realizacji

### P0 — plan i źródło

- [x] Potwierdzić czysty branch i przypięty plik `D6`.
- [x] Policzyć formy z prefiksem/bez prefiksu, zachowanie AL/FLAGS oraz rozkład długości śladów.
- [x] Zapisać plan w commicie przed zmianą rdzenia.

### P1 — fixture’y i rdzeń

- [ ] Dodać do manifestu maksymalnie 20 pierwszych wektorów dla braku prefiksu i każdego prefiksu segmentowego; zachować numery i hashe źródłowe.
- [ ] Dodać obsługę `D6` w dekoderze i ścieżce wykonania bez efektów ubocznych na FLAGS.
- [ ] Pozwolić dekoderowi przyjąć prefiks segmentowy przed `D6`; prefiks nie zmienia operandów, ale jego bajt i czas muszą być uwzględnione.
- [ ] Odtworzyć z SST moment aktualizacji AL, zużycie kolejki i T-state, bez tabeli uproszczonych cykli.

### P2 — weryfikacja źródłowa

- [ ] Uruchomić fixture’y lokalne (cel: 100/100, chyba że grupa ma mniej rekordów).
- [ ] Uruchomić cały plik `D6`: 2 000/2 000, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [ ] Potwierdzić pokrycie obu wartości CF, zachowania pozostałych flag, czterech prefiksów, różnych długości kolejki i wszystkich długości śladu.

### P3 — pełny korpus i dokumentacja

- [ ] Uruchomić cały pinned corpus 646 000 po ustabilizowaniu kodu; zaktualizować passed/skipped i procent.
- [ ] Zaktualizować macierz ISA, inwentarz skipów, README wektorów, raport A12 i osobną checklistę prac.

### P4 — bramki i commity

- [ ] Build Debug i Release: zero ostrzeżeń/błędów.
- [ ] Pełne testy Release oraz formatter verify bez błędów.
- [ ] OpcodeInfo, walidator manifestu i `git diff --check` przechodzą.
- [ ] Zapisać implementację, fixture’y i raport w oddzielnym commicie po planie; nie wypychać.

## Kryterium ukończenia

Pełne 2 000 rekordów `D6` przechodzą porównanie rejestrów, flag, pamięci, kolejki i każdego T-state; fixture’y repo obejmują wszystkie pięć grup wejściowych; pełny wynik korpusu i wymagane bramki są zapisane. Plan i implementacja mają oddzielne commity.
