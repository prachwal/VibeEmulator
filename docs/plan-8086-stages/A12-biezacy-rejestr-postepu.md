# A12 — bieżący rejestr postępu

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

Ten plik jest osobną checklistą roboczą dla bieżących prac. Po **każdym** ukończonym punkcie należy zaznaczyć `[x]`, dodać wiersz do historii oraz wpisać aktualny procent. Punkt uznajemy za ukończony dopiero po spełnieniu jego kryterium. Procent implementacji CPU oznacza udział rekordów `passed` w przypiętym korpusie SingleStepTests 8086 (646 000 rekordów); nie jest oceną kompletności wszystkich cech 8086.

## Aktualny wskaźnik

- Korpus: **100%** — 646 000 / 646 000 `passed`; 0 `skipped`, mismatchy, timeoutów i błędów parsowania.
- Raport bieżącego pełnego przebiegu: [CALL FAR 9A](A12-prefixed-call-far-9a-report.json); 323/323 plików przypiętego źródła.
- Ostatni zamknięty pakiet E8–EA ma osobne commity implementacji `2e508ee` i dokumentacji `4c4890a`.
- Ostatni zamknięty pakiet E0–E3: **9/9 punktów (100%)**, commity `5890848`, `52f27e0`, `a8ffcc1`.
- Pakiet EB: **8/8 punktów (100%)**; implementacja, testy, korpus, bramki, dokumentacja i osobne commity gotowe. Szczegółowa lista: [checklista EB](A12-checklista-pakietu-EB.md). Procent checklisty jest niezależny od 100% korpusu.

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
- [x] Kontynuować pakiety obserwowanych prefiksów do wyzerowania skipów; segment-prefiksowany `CALL FAR 9A` zamknięto planem `c365fac`, pełny korpus wynosi 646 000/646 000.
- [ ] Dokończyć niezależny test programu/ROM, jawne ograniczenia cyklowe A10/A11 i końcowy audyt A1–A12.

## Bieżący pakiet — segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)

Plan przed implementacją: [A12-prefixed-direct-call-jump-e8-ea-plan.md](A12-prefixed-direct-call-jump-e8-ea-plan.md), commit `10898e9`. Pokrycie korpusu przed pakietem: **91,4%** (590 169/646 000); samo przygotowanie planu nie zmienia wyniku.

- [x] Potwierdzić branch, pin i inwentarz: 3 016 rekordów w 12 grupach; plan zatwierdzony przed kodem. Korpus: **91,4%** (bez nowego pełnego przebiegu).
- [x] Dodać fixture’y i test lokalny dla 12 grup: 240/240; procent korpusu przed pełnym przebiegiem pozostawał 91,4%.
- [x] Odtworzyć pełne ślady i zaimplementować E8/E9/EA; wąskie testy służyły diagnozie, a nie powtarzanej pełnej weryfikacji.
- [x] Po stabilizacji przejść pełne pliki E8–EA 6 000/6 000, filtr segment-prefiksowany 3 016/3 016 oraz jeden pełny korpus: 593 185/646 000 (91,8%), 52 815 skipów, 0 błędów.
- [x] Zaliczyć końcowe bramki jakości; dokumentację i osobne commity kończę w tym checkpointcie.

## Zamknięty pakiet — segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)

Plan `10898e9`, implementacja `2e508ee`, dokumentacja `4c4890a`; 240/240 fixture’ów, 3 016/3 016 prefiksowanych, pliki 6 000/6 000, korpus 593 185/646 000 (91,8%).

## Zamknięty pakiet — segment-prefiksowany krótki `JMP rel8` (`EB`)

Plan przed kodem: [A12-prefixed-short-jump-eb-plan.md](A12-prefixed-short-jump-eb-plan.md), commit `37ae6a6`. Inwentarz czterech grup: 1 018 rekordów. Ostatni pełny wynik korpusu przed EB: 593 185/646 000 (91,8%).

- [x] Potwierdzić branch, pin, inwentarz i zapisać plan przed implementacją; plan `37ae6a6`. Korpus: 91,8% (bez nowego przebiegu).
- [x] Dodać cztery fixture’y i test lokalny: 80/80. Korpus: 91,8% (bez nowego przebiegu).
- [x] Odtworzyć ślady i obsłużyć prefiks segmentowy w dekoderze/skoku; fixture’y przechodzą.
- [x] Pełny filtr prefiksowany: 1 018/1 018; pełny plik EB: 2 000/2 000.
- [x] Pełny korpus: 594 203/646 000 (92,0%), 51 797 skipów, 0 mismatchy/timeoutów/parse errors.
- [x] Bramki jakości: Debug/Release 0 ostrzeżeń/błędów; testy Release 22 887/22 887; format/verify, OpcodeInfo 512/512, manifest 2 026/35 829 i `git diff --check` zielone.
- [x] Synchronizacja raportu, macierzy ISA, README wektorów, checklist i stanu wznowienia. Korpus 92,0%.
- [x] Osobny commit implementacji `d3b57a9`; plan `37ae6a6` pozostał osobno.
- [x] Osobny commit dokumentacji `e1663ff`; raport i checklista EB zawierają hashe i są zamknięte.

Postęp pakietu EB: **8/8 punktów (100%)**. Pełna checklista po każdym punkcie: [A12-checklista-pakietu-EB.md](A12-checklista-pakietu-EB.md).

## Zamknięty pakiet — segment-prefiksowane `LEA 8D`

Plan przed kodem: [A12-prefixed-lea-8d-plan.md](A12-prefixed-lea-8d-plan.md), commit `b842f56`. Checklista aktualizowana punkt po punkcie: [checklista pakietu](A12-checklista-pakietu-prefixed-lea-8d.md). Raport: [LEA 8D](A12-prefixed-lea-8d-report.json).

- [x] Pin i źródło: 2 000 rekordów; 996 bez prefiksu, 1 004 prefiksowane w czterech grupach.
- [x] Fixture’y lokalne: 80/80; profile fetch T1 odtworzone ze źródłowych śladów.
- [x] Pełny filtr prefiksowany 1 004/1 004 i plik 8D 2 000/2 000; zero błędów.
- [x] Pełny korpus: 595 207/646 000 (92,1%), 50 793 skipów, zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości: Debug/Release bez ostrzeżeń i błędów; Release 22 968/22 968; format verify, OpcodeInfo 512/512, manifest 2 034/35 989.
- [x] Synchronizacja raportu, macierzy ISA, README, checklist i stanu wznowienia.
- [x] Osobny commit implementacji `9b7cc06`; plan `b842f56`.
- [x] Osobny commit dokumentacji `330f1e3`; raport zawiera wszystkie hashe.

Pakiet LEA 8D zamknięty: **10/10 (100%)**; zgodność korpusu: **92,1%**.

| 2026-10-25 | LEA 8D: plan przed kodem | `b842f56`; 1 004 prefiksowane rekordy | 594 203 | 92,0% |
| 2026-10-25 | LEA 8D: fixture’y i implementacja | 80/80 fixture’ów; profil cykli oparty o pin | 594 203 | 92,0% |
| 2026-10-25 | LEA 8D: pełny filtr i plik | 1 004/1 004 prefiksowane; 2 000/2 000 pełny plik | 594 203 | 92,0% |
| 2026-10-25 | LEA 8D: pełny korpus | `/tmp/a12-lea-8d-full-corpus.json`; 595 207 passed, 50 793 skipped, zero błędów | 595 207 | 92,1% |
| 2026-10-25 | LEA 8D: zbiorcze bramki jakości i dokumenty zsynchronizowane | Debug/Release, Release 22 968/22 968, format verify, OpcodeInfo 512/512, manifest 2 034/35 989; macierz/README/checklisty aktualne | 595 207 | 92,1% |
| 2026-10-25 | LEA 8D: osobny commit implementacji | `9b7cc06`; plan `b842f56` osobno | 595 207 | 92,1% |
| 2026-10-25 | LEA 8D: osobny commit dokumentacji i zamknięcie pakietu | `330f1e3`; raport zawiera trzy hashe commitów | 595 207 | 92,1% |

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
| 2026-10-25 | Plan pakietu segment-prefiksowanego `JMP EB` | 1 018 rekordów / 4 grupy; plan `37ae6a6` przed zmianą kodu | 593 185 | 91,8% |
| 2026-10-25 | Fixture’y i test lokalny EB | 80/80 w czterech grupach; `Intel8086PrefixedShortJumpVectorTests` | 593 185 | 91,8% |
| 2026-10-25 | Implementacja EB i pełne filtry źródłowe | prefiks 1 018/1 018; plik EB 2 000/2 000; E8–EA regresja 4 034/4 034 | 593 185 | 91,8% |
| 2026-10-25 | Pełny korpus po EB | `/tmp/a12-eb-final-corpus-report.json`; +1 018 passed, 51 797 jawnych skipów, 0 błędów | 594 203 | 92,0% |
| 2026-10-25 | Bramki jakości i synchronizacja dokumentacji EB | Release 22 887/22 887; buildy 0 ostrzeżeń/błędów; format, OpcodeInfo 512/512, manifest 2 026/35 829; checklista pakietu EB 7/8 | 594 203 | 92,0% |
| 2026-10-25 | Osobny commit implementacji EB | `d3b57a9`; osobny plan `37ae6a6` | 594 203 | 92,0% |
| 2026-10-25 | Osobny commit dokumentacji EB i zamknięcie pakietu | `e1663ff`; 8/8 punktów checklisty | 594 203 | 92,0% |


## Bieżący pakiet — segment-prefiksowane ALU `00–3D, 84/85`

Plan i checklista zapisane przed kodem osobnym commitem `d790ce0`. Inwentarz po sprawdzeniu pinu: **49 782 rekordy w 50 plikach ALU**, cztery segmenty. Pełny filtr prefiksowany: 49 782/49 782; regresja 50 pełnych plików: 100 000/100 000; pełny korpus po ALU: 644 989/646 000 (99,8%). Następny pakiet 9A usunął ostatnie 1 011 skipów; aktualnie 646 000/646 000 (100%), zero mismatchy/timeoutów/parse errors. Raport: [pełny korpus](A12-prefixed-call-far-9a-report.json).

- [x] Zapisać inwentarz, plan i osobną checklistę przed implementacją.
- [x] Fixture’y 200/200; profile cykli; dekoder i ścieżki rejestrowe/pamięciowe.
- [x] Pełny filtr 49 782/49 782 i regresja 50 plików ALU 100 000/100 000.
- [x] Pełny korpus: 644 989/646 000 (99,8%), 1 011 skipów `9A`, zero błędów.
- [x] Bramki jakości: Debug/Release 0/0; Release 23 169/23 169; format verify, OpcodeInfo 512/512, manifest 2 042/36 389, `git diff --check`; macierz i README zaktualizowane.
- [x] Osobne commity: implementacja `cab1fd2`, dokumentacja `fc9af2e`; plan `d790ce0`.

Szczegóły i aktualizacje punkt po punkcie: [checklista pakietu](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md), **11/11 (100%)**; [plan](A12-prefixed-alu-00-3d-84-85-plan.md).

| 2026-10-25 | ALU prefiksowane: bramki jakości i synchronizacja | Debug/Release 0/0; testy Release 23 169/23 169; format/verify, OpcodeInfo 512/512, manifest 2 042/36 389; macierz i README | 644 989 | 99,8% |
| 2026-10-25 | Osobny commit implementacji ALU prefiksowanego | `cab1fd2`; plan osobno `d790ce0` | 644 989 | 99,8% |
| 2026-10-25 | Osobny commit dokumentacji i zamknięcie pakietu ALU prefiksowanego | `fc9af2e`; raport zawiera hashe planu `d790ce0`, implementacji `cab1fd2` i dokumentacji `fc9af2e` | 644 989 | 99,8% |
| 2026-10-25 | Prefiksowany `CALL FAR 9A`: pełny korpus i bramki | 646 000/646 000; Release 23 250/23 250; format, OpcodeInfo, manifest i diff check zielone | 646 000 | 100% |
| 2026-10-25 | Osobny commit implementacji CALL FAR 9A | `3198647`; plan `c365fac` | 646 000 | 100% |
| 2026-10-25 | Osobny commit dokumentacji i zamknięcie pakietu 9A | `cba7033`; raport zapisuje hashe planu, implementacji i dokumentacji | 646 000 | 100% |
