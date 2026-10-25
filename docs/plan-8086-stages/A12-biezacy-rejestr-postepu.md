# A12 — bieżący rejestr postępu

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

Ten plik jest osobną checklistą roboczą dla bieżących prac. Po **każdym** ukończonym punkcie należy zaznaczyć `[x]`, dodać wiersz do historii oraz wpisać aktualny procent. Punkt uznajemy za ukończony dopiero po spełnieniu jego kryterium. Procent implementacji CPU oznacza udział rekordów `passed` w przypiętym korpusie SingleStepTests 8086 (646 000 rekordów); nie jest oceną kompletności wszystkich cech 8086.

## Aktualny wskaźnik

- Korpus: **91,8%** — 593 185 / 646 000 `passed`.
- Pozostało: 52 815 `skipped`; 0 `mismatched`, timeoutów i błędów parsowania.
- Raport tego przebiegu: `/tmp/a12-e8-ea-full-corpus-report.json`.
- To wynik po zamkniętym pakiecie E8–EA; implementacja i dokumentacja mają osobne commity `2e508ee` i `4c4890a`.
- Ostatni zamknięty pakiet E0–E3: **9/9 punktów (100%)**, commity `5890848`, `52f27e0`, `a8ffcc1`.
- Bieżący pakiet E8–EA: **5/5 punktów (100%)**; implementacja, filtry, korpus, bramki i dokumentacja zakończone. Commity implementacji `2e508ee` i dokumentacji `4c4890a`. Procent checklisty pakietu jest niezależny od 91,8% korpusu.

## Ukończony pakiet — E0–E3 z prefiksem segmentowym

- [x] Potwierdzić branch, pin źródła oraz zakres 16 grup prefiks/opcode; plan przed kodem zapisany w [planie E0–E3](A12-prefixed-loop-e0-e3-plan.md). Korpus: **90,7%** (586 226/646 000).
- [x] Dodać do repo 16 grup fixture’ów, 320 rekordów oraz wpisy manifestu; zachować dotychczasowe próbki. Korpus: **90,7%** (ostatni pełny wynik w tym momencie).
- [x] Dodać test lokalnych fixture’ów; **320/320** przechodzi. Korpus: **90,7%** (bez nowego pełnego przebiegu).
- [x] Dostosować dekoder i wykonanie LOOP do prefiksu, displacement, IP, kolejki oraz ścieżek skoku/braku skoku; diagnostyczny pełny filtr potwierdza implementację. Korpus po pełnym przebiegu: **91,4%** (590 169/646 000).
- [x] Uruchomić pełne źródła E0–E3: **8 000/8 000 passed**, 0 skipów, rozbieżności, timeoutów i błędów parsowania.
- [x] Dokończyć końcową bramkę Release, formattera, OpcodeInfo, walidatora manifestu i `git diff --check`; wszystkie wyniki zielone. Procent korpusu: **91,4%**.
- [x] Zapisać raport E0–E3 w repo i uzupełnić `## Testy i kryteria`, checklisty A12, README wektorów, macierz ISA i stan wznowienia. Procent: **91,4%** (590 169/646 000).
- [x] Zatwierdzić implementację osobnym commitem `5890848`, po wcześniejszym osobnym commicie planu `e0b5209`. Procent: **91,4%** (590 169/646 000).
- [x] Zatwierdzić checkpoint dokumentacji `52f27e0` oraz zamknąć checklistę checkpointu. Procent: **91,4%** (590 169/646 000).
- [ ] Wybrać następny pakiet z pozostałych 55 831 skipów, zinwentaryzować jego wektory i zapisać plan przed zmianą kodu. Procent po wyborze: aktualizować wyłącznie po pełnym przebiegu korpusu.
- [ ] Kontynuować pakiety pozostałych prefiksów, luki cyklowe A10/A11 i niezależny test programu/ROM, aktualizując ten rejestr po każdym zamkniętym punkcie.

## Bieżący pakiet — segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)

Plan przed implementacją: [A12-prefixed-direct-call-jump-e8-ea-plan.md](A12-prefixed-direct-call-jump-e8-ea-plan.md), commit `10898e9`. Pokrycie korpusu przed pakietem: **91,4%** (590 169/646 000); samo przygotowanie planu nie zmienia wyniku.

- [x] Potwierdzić branch, pin i inwentarz: 3 016 rekordów w 12 grupach; plan zatwierdzony przed kodem. Korpus: **91,4%** (bez nowego pełnego przebiegu).
- [x] Dodać fixture’y i test lokalny dla 12 grup: 240/240; procent korpusu przed pełnym przebiegiem pozostawał 91,4%.
- [x] Odtworzyć pełne ślady i zaimplementować E8/E9/EA; wąskie testy służyły diagnozie, a nie powtarzanej pełnej weryfikacji.
- [x] Po stabilizacji przejść pełne pliki E8–EA 6 000/6 000, filtr segment-prefiksowany 3 016/3 016 oraz jeden pełny korpus: 593 185/646 000 (91,8%), 52 815 skipów, 0 błędów.
- [x] Zaliczyć końcowe bramki jakości; dokumentację i osobne commity kończę w tym checkpointcie.

## Pozostałe prace A12 po bieżącym pakiecie

- [ ] Klasyfikować i obsługiwać pozostałe prefiksy segmentowe w osobnych, opartych na źródłach pakietach; dla każdego utrzymać oddzielny plan, implementację i checkpoint dokumentacji.
- [ ] Ustalić źródłowe zachowanie wejścia NMI/INTR, magistrali w HALT, wznowienia WAIT po IRET, READY, arbitrażu RQ/GT, opóźnienia po MOV/POP SS i brakujących sekwencji prefiksów; brak dowodu opisać jako ograniczenie.
- [ ] Dodać niezależny test programu lub ROM z pochodzeniem, licencją, reprodukowalnymi poleceniami i mierzalnym kryterium sukcesu.
- [ ] Wykonać audyt gałęzi i commitów A0–A12 oraz końcowy pełny przebieg korpusu i wszystkich bramek jakości.
- [ ] Uzgodnić checklisty, macierz ISA, README, raporty i listę ograniczeń; zamknąć A12 dopiero po zapisaniu końcowego raportu.

## Reguła aktualizacji po każdym punkcie

1. Zaznacz `[x]` tylko po zebraniu dowodu spełnienia kryterium.
2. Natychmiast dopisz w historii datę, zamknięty punkt, commit lub raport, wynik i procent korpusu.
3. Procent licz jako `passed / 646 000 × 100`, zaokrąglając do jednego miejsca; źródłem są wyłącznie pełne raporty korpusu.
4. Gdy punkt nie zmienia zachowania CPU, zachowaj ostatni potwierdzony procent i zaznacz, że nie wykonano nowego pełnego przebiegu.
5. Licznik checklisty podawaj osobno od procentu korpusu.

## Historia aktualizacji

| data | ukończony punkt | dowód / commit | passed / 646 000 | implementacja |
|---|---|---|---:|---:|
| 2026-10-25 | Plan i inwentaryzacja E0–E3 przed implementacją | `e0b5209`, pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` | 586 226 | 90,7% |
| 2026-10-25 | 16 grup fixture’ów dodane; test lokalny 320/320 | test `Intel8086PrefixedLoopVectorTests` | 586 226 | 90,7% |
| 2026-10-25 | Implementacja przechodzi pełny filtr E0–E3 | 8 000/8 000; 0 skipów i rozbieżności | 590 169 | 91,4% |
| 2026-10-25 | Pełny korpus po E0–E3 | `/tmp/a12-loop-e0-e3-full-corpus-report.json`; 55 831 skipów, 0 błędów | 590 169 | 91,4% |
| 2026-10-25 | Końcowe bramki jakości pakietu | Debug/Release bez ostrzeżeń i błędów; testy Release zielone; format/verify, OpcodeInfo 512/512, manifest 1 994/35 189, `git diff --check` | 590 169 | 91,4% |
| 2026-10-25 | Osobny commit implementacji E0–E3 | `5890848`; plan `e0b5209` pozostaje osobnym wcześniejszym commitem | 590 169 | 91,4% |
| 2026-10-25 | Raport i dokumentacja A12 zsynchronizowane | raport JSON, plan, README wektorów, macierz ISA, checklisty i stan wznowienia zaktualizowane | 590 169 | 91,4% |
| 2026-10-25 | Osobny checkpoint dokumentacji | commit `52f27e0`; hash checkpointu wpisany do raportu | 590 169 | 91,4% |
| 2026-10-25 | Wybór i plan kolejnego pakietu prefiksów | E8–EA: 3 016 rekordów / 12 grup; plan `10898e9`; bez pełnego przebiegu korpusu | 590 169 | 91,4% |
| 2026-10-25 | Fixture’y i test lokalny E8–EA | 240/240 w 12 grupach; test `Intel8086PrefixedDirectCallJumpVectorTests` | 590 169 | 91,4% |
| 2026-10-25 | Implementacja i pełne filtry E8–EA | 3 016/3 016 prefiksowanych, pliki E8–EA 6 000/6 000; zero skipów i rozbieżności | 593 185 | 91,8% |
| 2026-10-25 | Pełny korpus po E8–EA | `/tmp/a12-e8-ea-full-corpus-report.json`; 52 815 jawnych skipów, 0 błędów | 593 185 | 91,8% |
| 2026-10-25 | Zbiorcze bramki jakości E8–EA | Debug/Release bez ostrzeżeń i błędów; Release 22 806/22 806; format/verify, OpcodeInfo 512/512, manifest 2 018/35 669, `git diff --check` | 593 185 | 91,8% |
| 2026-10-25 | Osobne commity implementacji i dokumentacji E8–EA; pakiet zamknięty | implementacja `2e508ee`; checkpoint dokumentacji `4c4890a`; raport zawiera oba hashe | 593 185 | 91,8% |
