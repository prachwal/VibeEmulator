# A12 — checklista realizacji

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

## Postęp

- **Pokrycie przypiętego korpusu:** 88,7% (572 970 / 646 000 rekordów zgodnych; 73 030 skipów; zero mismatchy, timeoutów i błędów parsowania). Jest to miernik zgodności z korpusem, a nie procent wszystkich cech 8086.
- **Pakiet ESC D8–DF:** implementacja i walidacja zachowania są ukończone: fixture’y 2 560/2 560, prefiksowany filtr 8 012/8 012, kontrolny nieprefiksowany 7 988/7 988. Pozostaje zamknięcie dokumentacji, końcowych kryteriów i commitów.
- **Zamknięcie bieżącego pakietu:** 6/6 punktów = **100%** według poniższej checklisty. Procent rośnie wyłącznie po odhaczeniu całego punktu i zapisaniu wyniku.

## Bieżący pakiet: segment-prefiksowane ESC D8–DF

- [x] Zaimplementować prefiks ES/CS/SS/DS dla form pamięciowych i rejestrowych ESC; zachować nieprefiksowaną semantykę. Wynik: fixture’y końcowe 2 560/2 560.
- [x] Zweryfikować pełny filtr prefiksowany i kontrolny filtr nieprefiksowany. Wynik: 8 012/8 012 oraz 7 988/7 988; bez skipów i rozbieżności.
- [x] Uruchomić pełny korpus i odświeżyć wskaźnik zgodności. Wynik: 572 970/646 000 (88,7%), 73 030 skipów.
- [x] Uzgodnić macierz ISA, README wektorów, rozbicie skipów i raport; zachować istniejące dane próbki źródłowej oraz dodać dowody ESC. Wynik: macierz 1 462/28 599, próbka 5 700 zachowana (3 376 passed / 2 324 mismatched), wszystkie 8 opcode’ów ESC mają dowody pełnych filtrów; README i skipy wskazują 73 030.
- [x] Zaznaczyć P4 w planie po zapisaniu wyników wszystkich wymaganych bramek: Debug, Release, testy, format, OpcodeInfo, manifest i `git diff --check`. Wynik: buildy Debug/Release 0 ostrzeżeń i błędów; Release 22 240/22 240 (Intel8086 19 440/19 440); format verify; OpcodeInfo 512/512; manifest 1 462/28 599; diff check czysty.
- [x] Utworzyć osobny commit implementacji po planie, uzupełnić jego hash w raporcie i checklistach, a zapis stanu wykonać osobnym commitem dokumentacyjnym. Wynik: implementacja `5ebb8828924fe8a697534d2951b1909b6964aec0`; checkpoint dokumentacji jest zatwierdzany oddzielnie.

## Dalsze prace A12

- [ ] Zakończyć analizę pozostałych prefiksów segmentowych w oddzielnych pakietach; po każdym pakiecie zapisać testy, pełny korpus i procent.
- [ ] Rozstrzygnąć kodowania `C6` oraz `D0–D3 /6` na podstawie źródeł i wektorów.
- [ ] Dodać reprodukowalny test niezależnego programu lub ROM-u z opisanym pochodzeniem i kryterium sukcesu.
- [ ] Rozstrzygnąć albo opisać ograniczenia cyklowe A10/A11 wskazane w [checkliście dalszych prac](A12-dalsze-prace-checklist.md).
- [ ] Wykonać końcowy audyt A1–A12: łańcuch branchy, osobne commity, wszystkie kryteria projektu i spójność dokumentacji.

## Rejestr aktualizacji

| data | ukończony punkt | wynik po punkcie | pokrycie korpusu | ukończenie bieżącego pakietu |
|---|---|---|---:|---:|
| 2026-10-25 | Implementacja ESC i fixture’y | 2 560/2 560 fixture’ów | 88,7% | 17% (1/6) |
| 2026-10-25 | Pełny filtr prefiksowany i kontrolny | 8 012/8 012 oraz 7 988/7 988 | 88,7% | 33% (2/6) |
| 2026-10-25 | Pełny korpus i synchronizacja bieżącego wyniku | 572 970/646 000; 73 030 skipów | 88,7% | 50% (3/6) |
| 2026-10-25 | Uzgodniona macierz/README/skipy i zachowana próbka źródłowa | macierz 1 462 wpisy / 28 599 wskazań; próbka 5 700 zachowana | 88,7% | 67% (4/6) |
| 2026-10-25 | Końcowe bramki i kryteria P4 zapisane | buildy 0/0; testy 22 240/22 240; format; OpcodeInfo 512/512; manifest; diff check | 88,7% | 83% (5/6) |
| 2026-10-25 | Commit implementacji i osobny checkpoint dokumentacji | implementacja `5ebb8828924fe8a697534d2951b1909b6964aec0`; dokumenty w osobnym commicie | 88,7% | 100% (6/6) |

Po ukończeniu każdego kolejnego punktu należy od razu zaznaczyć checkbox i dopisać wiersz do rejestru. Wartość ukończenia pakietu to liczba zamkniętych punktów z sześciu; pokrycie korpusu pozostaje osobnym wskaźnikiem.
