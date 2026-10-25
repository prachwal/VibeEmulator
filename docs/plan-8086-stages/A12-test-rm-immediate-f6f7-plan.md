# A12 — `TEST r/m, immediate` (`F6/F7 /0` i `/1`)

## Cel

Usunąć jawne skipy dla wszystkich czterech grup `F6.0`, `F6.1`, `F7.0`, `F7.1` z przypiętego źródła SingleStepTests 8086. Rozszerzenie `/1` jest obserwowanym aliasem `/0`; oba kodowania pozostają osobno w próbkach i pełnym porównaniu. Operacja wykonuje AND operandu z natychmiastową wartością, aktualizuje flagi zgodnie z wektorami i nie zapisuje wyniku do operandu.

## Inwentaryzacja źródła

Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, katalog `v1/`. Bieżący runner zgłasza dla każdego z czterech plików 2 000 jawnych skipów z powodem `Only F6/F7 /2,/3 and register-direct /4-/7 forms are implemented.`

| Plik | szerokość | rekordy | rejestrowe (`mod=11`) | pamięciowe (`mod!=11`) | bez prefiksu | z prefiksem segmentowym |
|---|---:|---:|---:|---:|---:|---:|
| `F6.0.json.gz` | bajt | 2 000 | 532 | 1 468 | 977 | 1 023 |
| `F6.1.json.gz` | bajt, alias `/0` | 2 000 | 488 | 1 512 | 1 007 | 993 |
| `F7.0.json.gz` | słowo | 2 000 | 483 | 1 517 | 1 022 | 978 |
| `F7.1.json.gz` | słowo, alias `/0` | 2 000 | 552 | 1 448 | 996 | 1 004 |
| **Razem** |  | **8 000** | **2 055** | **5 945** | **4 002** | **3 998** |

Prefiksy segmentowe w każdym pliku obejmują ES (`26`), CS (`2E`), SS (`36`) i DS (`3E`). Wektory testują wszystkie wartości `r/m` oraz `mod=00/01/10/11`; dla pamięci obejmują displacementy i adres bezpośredni. Weryfikacja musi zachować maski FLAGS przypisane do źródła/manifestu i porównywać rejestry, RAM, kolejkę oraz każdy T-state.

## Zakres implementacji

1. Dodać `/0` i alias `/1` do dekodowania F6/F7, w tym odczyt immediate byte dla F6 i immediate word dla F7 z kolejki prefetch.
2. Użyć istniejącej semantyki ALU `Test`, bez zapisu wyniku do rejestru ani pamięci; zweryfikować wszystkie bity FLAGS objęte maską wektorów.
3. Obsłużyć rejestrowy i pamięciowy operand, wszystkie cztery prefiksy segmentowe oraz alias `/1`; dla operandów rejestrowych prefiks nie zmienia wartości rejestru.
4. Uzupełnić profile czasu, fetchu immediate, odczytu pamięci i kolejki wyłącznie na podstawie pełnych przypiętych wektorów. Nie przenosić harmonogramu z `80/82` bez potwierdzenia w F6/F7.
5. Dodać małe, niezmienione próbki upstream dla każdego pliku, manifest i opis źródła/licencji. Minimalny zestaw lokalny: 20 rekordów z każdego pliku (80 łącznie); pełne 8 000 pozostaje testem poza repo.
6. Uaktualnić checklistę A12, macierz ISA, raport podetapu i `stan-wznowienia-A12.md`; podać nowy wynik całego korpusu i dokładną liczbę nadal pomijanych rekordów.

## Kolejność prac

1. Zatwierdzić ten plan osobnym commitem na branchu `A12-8086-pelna-weryfikacja`.
2. Wyeksportować próbki z lokalnego checkoutu źródła i potwierdzić ich SHA oraz niezmienność rekordów.
3. Dodać testy/regresje i wdrożyć `/0,/1`; uruchomić najpierw próbki, następnie pełne cztery filtry.
4. Uruchomić pełne 646 000 rekordów. Skontrolować zmianę passed/skipped i zachować raport; zero mismatchy, timeoutów i błędów parsowania jest warunkiem zaliczenia.
5. Uruchomić testy Intel8086, testy solucji Release, buildy Debug/Release, formatter, walidator manifestu oraz OpcodeInfo. Zaktualizować wszystkie checklisty, a implementację i stan zatwierdzić osobnym commitem.

## Ryzyka i kryteria interpretacji

- Alias `/1` wynika z metadata i rzeczywistych wektorów pinu; nie uznawać go za wspierany tylko na podstawie nazwy instrukcji.
- Cykle pamięciowego `TEST` obejmują odczyt operandu, ale nigdy zapis. Fixture musi wykrywać niezamierzony `MEMW`.
- Immediate i displacement współdzielą kolejkę. Nie zakładać, że wszystkie bajty są dostępne od razu; pełne rekordy mają rozstrzygnąć fetch przy krótkiej kolejce i zawijaniu IP.
- Prefiks segmentowy wpływa na adres pamięci, lecz nie powinien zmieniać rejestrowego operandu. Porównać wszystkie cztery wartości prefiksu.
- Sukces 8 000 wektorów nie zamyka A12; inne jawne skipy i niezależny test programu/ROM pozostają osobnymi wymaganiami.

## Testy i kryteria

- [ ] Cztery próbki upstream (po 20 rekordów) zostały porównane bajt w bajt ze źródłem SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; manifest wskazuje 80 rekordów.
- [ ] Próbki F6/F7 `/0,/1` przechodzą test harness, 80/80, w tym kontrole stanu, RAM, FLAGS, kolejki i wszystkich T-state.
- [ ] Pełne filtry `F6.0`, `F6.1`, `F7.0`, `F7.1` przechodzą 8 000/8 000; 0 skipów, mismatchy, timeoutów i błędów parsowania; raport źródłowy zapisany.
- [ ] Pokryte są oba rozmiary immediate, rejestry, pamięć ze wszystkimi trybami ModR/M, cztery prefiksy segmentowe, `/1` alias, odczyt bez zapisu i flagi według maski źródła.
- [ ] Pełny korpus 646 000 uruchomiony po zmianie; passed wzrasta o 8 000, skipped spada o 8 000, bez nowych mismatchy, timeoutów ani błędów parsowania.
- [ ] Testy projektu Intel8086 i całej solucji Release przechodzą; build Debug/Release bez ostrzeżeń i błędów; formatter verify, walidator manifestu i OpcodeInfo są zielone.
- [ ] Dokumentacja A12, raport, macierz ISA, stan wznowienia oraz wszystkie `## Testy i kryteria` są zgodne z uzyskanymi wynikami.
- [ ] Plan i implementacja mają osobne commity na branchu A12; niczego nie wypchnięto.
