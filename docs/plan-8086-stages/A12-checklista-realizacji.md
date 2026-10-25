# A12 — checklista realizacji

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

## Postęp

- **Pokrycie przypiętego korpusu:** 100% (646 000 / 646 000 rekordów zgodnych; zero skipów, mismatchy, timeoutów i błędów parsowania). To miernik zgodności z tym pinem SingleStepTests, nie deklaracja ukończenia wszystkich cech 8086.
- **Ostatni zamknięty pakiet ESC D8–DF:** 6/6 punktów (100%); fixture’y 2 560/2 560, filtry 8 012/8 012 i 7 988/7 988, oddzielne commity implementacji i checkpointu.
- **Ostatni pakiet C7 /1–/7:** 6/6 punktów (**100% checklisty**); pełny plik 2 000/2 000, bramki zielone; checkpoint `aeb0699`.
- **Poprzedni pakiet C6 /1–/7:** 6/6 punktów (100%); fixture’y 1 463/1 463, pełny plik 2 000/2 000.
- **Poprzedni pakiet E0–E3:** implementacja `5890848`; fixture’y 320/320, pełne pliki 8 000/8 000, korpus 590 169/646 000 (91,4%); checkpoint dokumentacji `52f27e0`; pakiet zamknięty 6/6.
- **Pakiet E8–EA:** fixture’y 240/240, filtr 3 016/3 016, pełne pliki 6 000/6 000; korpus 593 185/646 000 (91,8%); bramki zielone; implementacja `2e508ee`, checkpoint dokumentacji `4c4890a`; pakiet zamknięty 5/5.
- **Pakiet EB:** fixture’y 80/80, filtr 1 018/1 018, plik 2 000/2 000; korpus 594 203/646 000 (92,0%); regresja E8–EA zielona; bramki jakości i dokumentacja zielone; osobne commity `d3b57a9` / `e1663ff`; pakiet zamknięty. Checklista: [pakiet EB](A12-checklista-pakietu-EB.md), 8/8 (100%).

## Ostatni zamknięty pakiet: segment-prefiksowane ESC D8–DF

- [x] Zaimplementować prefiks ES/CS/SS/DS dla form pamięciowych i rejestrowych ESC; zachować nieprefiksowaną semantykę. Wynik: fixture’y końcowe 2 560/2 560.
- [x] Zweryfikować pełny filtr prefiksowany i kontrolny filtr nieprefiksowany. Wynik: 8 012/8 012 oraz 7 988/7 988; bez skipów i rozbieżności.
- [x] Uruchomić pełny korpus i odświeżyć wskaźnik zgodności. Wynik: 572 970/646 000 (88,7%), 73 030 skipów.
- [x] Uzgodnić macierz ISA, README wektorów, rozbicie skipów i raport; zachować istniejące dane próbki źródłowej oraz dodać dowody ESC. Wynik: macierz 1 462/28 599, próbka 5 700 zachowana (3 376 passed / 2 324 mismatched), wszystkie 8 opcode’ów ESC mają dowody pełnych filtrów; README i skipy wskazują 73 030.
- [x] Zaznaczyć P4 w planie po zapisaniu wyników wszystkich wymaganych bramek: Debug, Release, testy, format, OpcodeInfo, manifest i `git diff --check`. Wynik: buildy Debug/Release 0 ostrzeżeń i błędów; Release 22 240/22 240 (Intel8086 19 440/19 440); format verify; OpcodeInfo 512/512; manifest 1 462/28 599; diff check czysty.
- [x] Utworzyć osobny commit implementacji po planie, uzupełnić jego hash w raporcie i checklistach, a zapis stanu wykonać osobnym commitem dokumentacyjnym. Wynik: implementacja `5ebb8828924fe8a697534d2951b1909b6964aec0`; checkpoint dokumentacji `547d98a`.

## Ostatni zamknięty pakiet: SETMO/SETMOC `D0–D3 /6`

Plan i implementacja są osobnymi commitami na A12: [A12-setmo-d0-d3-6-plan.md](A12-setmo-d0-d3-6-plan.md).

- [x] Zatwierdzić plan z dowodami pinu i zakresu przed zmianą kodu. Wynik: plan `A12-setmo-d0-d3-6-plan.md`, zakres 8 000 rekordów i 80 grup fixture’ów; kod niezmieniony.
- [x] Dodać deterministyczne fixture’y i testy lokalne dla wszystkich niepustych grup opcode/prefiks/ModR/M. Wynik: 80/80 grup, 1 600/1 600 fixture’ów; lokalny test xUnit 1/1 passed, bez mismatchy.
- [x] Zaimplementować wektorowe zachowanie `SETMO`/`SETMOC`, w tym FLAGS, CL=0, prefiksy, pamięć i T-state. Wynik diagnostyczny fixture’ów: 1 600/1 600; pełny filtr 8 000 i korpus pozostają do uruchomienia.
- [x] Sprawdzić cztery pełne pliki (8 000/8 000) i jeden pełny korpus po stabilizacji. Wynik: 8 000/8 000; korpus 580 970/646 000 (89,9%), 65 030 skipów, 0 mismatchy/timeoutów/parse errors.
- [x] Zaliczyć bramki, uzgodnić macierz, README, raport i skipy; zapisać nowe pokrycie. Wynik: Debug/Release 0 ostrzeżeń/błędów; solucja Release 22 241/22 241 (Intel8086 19 441/19 441); format verify, OpcodeInfo 512/512, manifest 1 542/30 199, diff check czysty; korpus 580 970/646 000 (89,9%).
- [x] Zatwierdzić implementację oraz checkpoint dokumentacji oddzielnie; dopisać hashe. Wynik: plan `bd1ae29`; implementacja `aaed1ed`; checkpoint dokumentacji zapisany osobno.

Postęp pakietu SETMO/SETMOC: **6/6 (100%)**.

## Ostatni zamknięty pakiet: nieudokumentowane `C6 /1–/7`

Plan: [A12-c6-ignored-extension-plan.md](A12-c6-ignored-extension-plan.md); plan i implementacja mają osobne commity.

- [x] Zapisać plan i ustalić pinned status oraz zakres 1 764 nieudokumentowanych rekordów.
- [x] Dodać fixture’y z 140/140 grup: 1 463 rekordy; lokalny test xUnit 1/1 passed.
- [x] Obsłużyć ignorowanie pola `ModR/M.reg`; naprawić zawijanie pobierania kodu `CS:FFFF`, wykryte przez wektor.
- [x] Zweryfikować cały plik C6: 2 000/2 000, w tym wszystkie 1 764 rekordy `/1–/7`.
- [x] Uruchomić pełny korpus: 582 734/646 000 (90,2%), 63 266 skipów, zero mismatchy/timeoutów/parse errors.
- [x] Zapisać osobny checkpoint dokumentacji z raportem, macierzą, README i historią. Plan `c91a464`; implementacja `d16021e`; checkpoint `02cacb4`.

Postęp pakietu C6: **6/6 (100%)**. Raport: [A12-c6-ignored-extension-report.json](A12-c6-ignored-extension-report.json).

## Bieżący pakiet: nieudokumentowane `C7 /1–/7`

Plan: [A12-c7-ignored-extension-plan.md](A12-c7-ignored-extension-plan.md); plan `ffee9f8`, implementacja `d24a4d1`.

- [x] Zapisać plan i ustalić pinned status oraz rozkład 1 730 aliasów.
- [x] Dodać fixture’y: 140/140 grup, 1 426/1 426 rekordów; test xUnit 1/1.
- [x] Otworzyć `/1–/7` na wspólnej ścieżce `/0`; dodać 67 profili T-state z pełnych wektorów bez konfliktów.
- [x] Zweryfikować pełne C7: 2 000/2 000; cały korpus: 584 464/646 000 (90,5%), 61 536 skipów, 0 mismatchy/timeoutów/parse errors.
- [x] Zaliczyć bramki: Debug/Release 0/0; testy Release 22 243/22 243; format; OpcodeInfo 512/512; manifest 1 822/33 088.
- [x] Zapisać oddzielny checkpoint dokumentacji z raportem i aktualną checklistą. Plan `ffee9f8`; implementacja `d24a4d1`; checkpoint `aeb0699`.

Postęp pakietu C7: **6/6 (100%)**. Raport: [A12-c7-ignored-extension-report.json](A12-c7-ignored-extension-report.json).

## Bieżący pakiet: nieudokumentowane `8F /1–/7`

Plan: [A12-8f-ignored-extension-plan.md](A12-8f-ignored-extension-plan.md); raport: [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json). Plan `3fecba9`, korekta inwentarza `a2d5203`, implementacja `c6d1b4f`.

- [x] Zapisać plan przed kodem i zweryfikować źródło: 1 762 rekordy, 140 grup.
- [x] Dodać fixture’y i test lokalny: 1 461/1 461.
- [x] Potwierdzić pełnymi śladami aliasowanie na P80C86A-2; dodać 261 profili, bez konfliktów.
- [x] Zweryfikować pełny plik `8F`: 2 000/2 000.
- [x] Zweryfikować korpus i bramki: 586 226/646 000 (90,7%), 59 774 skipy; testy Release 22 244/22 244; buildy Debug/Release, format, OpcodeInfo 512/512 i manifest 1 962/34 549 zielone.
- [x] Zapisać osobny checkpoint dokumentacji `7a7d82d` i uzupełnić hash w raporcie.

Postęp pakietu `8F`: **6/6 (100%)**. Raport: [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json).

### Rejestr postępu pakietu `8F /1–/7`

| punkt | wynik | pokrycie korpusu | pakiet |
|---|---|---:|---:|
| Plan i inwentaryzacja | 1 762 rekordy, 140 grup; plan `3fecba9`, korekta `a2d5203` | 90,5% | 17% (1/6) |
| Fixture’y | 1 461/1 461 z 140 grup; test lokalny 1/1 | 90,5% | 33% (2/6) |
| Analiza i implementacja | 1 762 aliasy potwierdzone pełnymi śladami; 261 profili dodanych, 178 współdzielonych, 0 konfliktów; `c6d1b4f` | 90,5% | 50% (3/6) |
| Pełne źródło i korpus | `8F` 2 000/2 000; korpus 586 226/646 000, 59 774 skipy, 0 rozbieżności | 90,7% | 67% (4/6) |
| Bramki i synchronizacja dokumentów | Debug/Release, testy 22 244/22 244, format, OpcodeInfo 512/512, manifest 1 962/34 549; checkpoint `7a7d82d`; hash raportu uzupełniony | 90,7% | 83% (5/6) |
| Checkpoint i aktualizacja hashy | osobny commit dokumentacji `7a7d82d`; raport oraz checklisty zamknięte | 90,7% | 100% (6/6) |

## Poprzedni pakiet: segment-prefiksowane `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`)

Plan: [A12-prefixed-loop-e0-e3-plan.md](A12-prefixed-loop-e0-e3-plan.md); raport: [A12-prefixed-loop-e0-e3-report.json](A12-prefixed-loop-e0-e3-report.json). Plan `e0b5209`, implementacja `5890848`.

- [x] Plan przed kodem i inwentaryzacja: 3 943 rekordy segment-prefiksowane w 16 grupach.
- [x] Fixture’y i test lokalny: 320/320.
- [x] Implementacja segment-prefiksu i cykli na podstawie pełnych źródłowych śladów.
- [x] Pełne pliki E0–E3: 8 000/8 000, zero skipów i rozbieżności.
- [x] Pełny korpus i bramki jakości: 590 169/646 000 (91,4%), 55 831 skipów; wszystkie bramki zielone.
- [x] Osobny checkpoint dokumentacji `52f27e0` i wpis commita w raporcie.

Postęp pakietu E0–E3: **6/6 punktów (100%)**.

## Ostatni zamknięty pakiet: segment-prefiksowany `CALL FAR 9A`

- [x] Fixture’y 80/80; prefiksowany filtr 1 011/1 011; pełny plik 9A 2 000/2 000.
- [x] Pełny korpus 646 000/646 000 (100%), 0 skipów/mismatchy/timeoutów/parse errors.
- [x] Bramki jakości: Debug/Release 0/0, Release 23 250/23 250, format verify, OpcodeInfo 512/512, manifest 2 050/36 549.
- [x] Plan `c365fac`; oddzielne commity implementacji i dokumentacji w raporcie [9A](A12-prefixed-call-far-9a-report.json).

## Dalsze prace A12

Punktowa lista otwartych prac i rejestr procentu po każdym zakończeniu: [A12-checklista-do-dokonczenia.md](A12-checklista-do-dokonczenia.md).

- [x] Zakończyć segment-prefiksowane kombinacje występujące w pinned korpusie; filtr 9A 1 011/1 011, cały korpus 646 000/646 000 (100%), zero skipów.
- [x] Rozstrzygnąć nieudokumentowane `C6/C7 /1–/7` i `D0–D3 /6`; dowody i status są zapisane w macierzy ISA oraz raportach.
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
| 2026-10-25 | Plan SETMO/SETMOC zapisany przed kodem | 8 000 źródłowych rekordów, 80/80 grup fixture’ów, kod bez zmian | 88,7% | 17% (1/6) |
| 2026-10-25 | Fixture’y i test lokalny SETMO/SETMOC | 1 600/1 600 fixture’ów, xUnit 1/1 passed | 88,7% | 33% (2/6) |
| 2026-10-25 | Wdrożone `SETMO/SETMOC` | wszystkie fixture’y lokalne zgodne; pełne źródło i pełny korpus jeszcze nieuruchomione | 88,7% | 50% (3/6) |
| 2026-10-25 | Pełny filtr SETMO i pełny korpus | 8 000/8 000; 580 970/646 000; 65 030 skipów; 0 mismatchy | 89,9% | 67% (4/6) |
| 2026-10-25 | Bramki i dokumentacja SETMO | buildy 0/0; testy 22 241/22 241; format; OpcodeInfo; manifest 1 542/30 199; diff check | 89,9% | 83% (5/6) |
| 2026-10-25 | Osobny commit implementacji i checkpoint dokumentacji SETMO | plan `bd1ae29`; implementacja `aaed1ed`; dokumentacja w osobnym commicie | 89,9% | 100% (6/6) |
| 2026-10-25 | Plan pakietu C6 | plan `c91a464`; pinned zakres 1 764/1 764 aliasów `/1–/7` | 89,9% | 17% (1/6) |
| 2026-10-25 | Fixture’y i test lokalny C6 | 140 grup, 1 463/1 463 fixture’ów; xUnit 1/1 | 89,9% | 33% (2/6) |
| 2026-10-25 | Implementacja C6 i poprawka zawijania `CS:FFFF` | wspólna ścieżka `/0`; fixture’y 1 463/1 463 | 89,9% | 50% (3/6) |
| 2026-10-25 | Pełny plik C6 i pełny korpus | 2 000/2 000; korpus 582 734/646 000, 63 266 skipów | 90,2% | 67% (4/6) |
| 2026-10-25 | Bramki jakości C6 | Debug/Release 0/0; testy 22 242/22 242; format; OpcodeInfo; manifest 1 682/31 662 | 90,2% | 83% (5/6) |
| 2026-10-25 | Oddzielne commity C6 i checkpoint | plan `c91a464`; implementacja `d16021e`; dokumentacja `02cacb4` | 90,2% | 100% (6/6) |
| 2026-10-25 | Plan pakietu C7 | plan `ffee9f8`; pinned zakres 1 730 aliasów `/1–/7` | 90,2% | 17% (1/6) |
| 2026-10-25 | Fixture’y i test lokalny C7 | 140 grup, 1 426/1 426 fixture’ów; xUnit 1/1 | 90,2% | 33% (2/6) |
| 2026-10-25 | Implementacja aliasów C7 i profili | 67 brakujących profili; fixture’y 1 426/1 426 | 90,2% | 50% (3/6) |
| 2026-10-25 | Pełny plik C7 i pełny korpus | 2 000/2 000; korpus 584 464/646 000, 61 536 skipów | 90,5% | 67% (4/6) |
| 2026-10-25 | Bramki jakości C7 | Debug/Release 0/0; testy 22 243/22 243; format; OpcodeInfo; manifest 1 822/33 088 | 90,5% | 83% (5/6) |
| 2026-10-25 | Osobny checkpoint C7 | plan `ffee9f8`; implementacja `d24a4d1`; dokumentacja `aeb0699` | 90,5% | 100% (6/6) |
| 2026-10-25 | Plan E0–E3 | plan `e0b5209`; zakres 3 943 prefiksowanych rekordów / 16 grup | 90,7% | 17% (1/6) |
| 2026-10-25 | Fixture’y i implementacja E0–E3 | 320/320 lokalnie; implementacja `5890848` | 90,7% | 50% (3/6) |
| 2026-10-25 | Pełne filtry i korpus E0–E3 | 8 000/8 000; 590 169/646 000; 55 831 skipów | 91,4% | 67% (4/6) |
| 2026-10-25 | Bramki jakości E0–E3 | Debug/Release, testy, format/verify, OpcodeInfo 512/512, manifest 1 994/35 189, diff check | 91,4% | 83% (5/6) |

| 2026-10-25 | Osobny checkpoint dokumentacji E0–E3 | raport zawiera hash `52f27e0`; pakiet zamknięty | 91,4% | 100% (6/6) |

Po ukończeniu każdego kolejnego punktu należy od razu zaznaczyć checkbox i dopisać wiersz do rejestru. Wartość ukończenia pakietu to liczba zamkniętych punktów z sześciu; pokrycie korpusu pozostaje osobnym wskaźnikiem.

## Najnowszy pakiet: segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)

- [x] Fixture’y: 12 grup, 240/240.
- [x] Pełny filtr segment-prefiksowany: 3 016/3 016.
- [x] Pełne pliki E8–EA: 6 000/6 000.
- [x] Korpus: 593 185/646 000 (91,8%), 52 815 skipów, zero błędów.
- [x] Bramki jakości: buildy Debug/Release, Release 22 806/22 806, format/verify, OpcodeInfo, manifest i diff check.
- [x] Osobne commity implementacji `2e508ee` i dokumentacji `4c4890a`; raport zawiera hashe.

Postęp pakietu E8–EA: **5/5 punktów (100%)**. Raport: [A12-prefixed-direct-call-jump-e8-ea-report.json](A12-prefixed-direct-call-jump-e8-ea-report.json).

## Bieżący pakiet: segment-prefiksowany `JMP rel8` (`EB`)

- [x] Fixture’y lokalne: 80/80.
- [x] Pełny filtr EB: 1 018/1 018; cały plik EB: 2 000/2 000.
- [x] Kontrola regresji E8–EA+EB: 4 034/4 034 prefiksowane i 8 000/8 000 pełnych plików.
- [x] Pełny korpus: 594 203/646 000 (92,0%), 50 793 skipów, zero błędów.
- [ ] Bramki jakości, raport i osobne commity.

Postęp pakietu EB: **7/8 punktów (87,5%)**; pozostały osobne commity implementacji i dokumentacji.


## Bieżący pakiet: segment-prefiksowane LEA 8D

- [x] Fixture’y 80/80; pełny filtr prefiksowany 1 004/1 004 i pełny plik 8D 2 000/2 000.
- [x] Pełny korpus 595 207/646 000 (92,1%), 50 793 skipów, zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości: Release 22 968/22 968, buildy bez ostrzeżeń/błędów, format verify, OpcodeInfo 512/512, manifest 2 034/35 989.
- [ ] Commity implementacji i dokumentacji; szczegółowa checklista pakietu [LEA 8D](A12-checklista-pakietu-prefixed-lea-8d.md), 10/10 (100%).


## Zamknięty pakiet: segment-prefiksowane ALU `00–3D, 84/85`

- [x] Inwentaryzacja 49 782 rekordów / 50 plików ALU i plan zapisane przed kodem.
- [x] Fixture’y 200/200, implementacja, pełny filtr 49 782/49 782 i regresja 50 plików 100 000/100 000.
- [x] Pełny korpus: 644 989/646 000 (99,8%), 1 011 skipów `9A`, bez mismatchy/timeoutów/parse errors.
- [x] Bramki jakości i synchronizacja dokumentów: Debug/Release, Release 23 169/23 169, format, OpcodeInfo 512/512, manifest 2 042/36 389.
- [x] Osobne commity: implementacja `cab1fd2`, dokumentacja `fc9af2e`; plan `d790ce0`.

Pakiet zamknięty: **11/11 (100%)**; szczegóły: [checklista ALU](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md).


## Bieżący pakiet: segment-prefiksowany `CALL FAR 9A`

- [x] Plan przed kodem `c365fac`; 1 011 rekordów w czterech prefiksach.
- [x] Fixture’y 80/80; pełny filtr 1 011/1 011; cały plik 9A 2 000/2 000.
- [x] Pełny korpus 646 000/646 000 (100%), zero skipów i błędów.
- [x] Bramki jakości i dokumentacja A12 zielone; Debug/Release 0/0, Release 23 250/23 250, format verify, OpcodeInfo 512/512, manifest 2 050/36 549.
- [x] Osobny commit implementacji `3198647`; plan `c365fac` zapisano wcześniej.
- [ ] Osobny commit dokumentacji i zamknięcie checklisty.

Postęp pakietu 9A: **8/9 (89%)**; raport [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).
