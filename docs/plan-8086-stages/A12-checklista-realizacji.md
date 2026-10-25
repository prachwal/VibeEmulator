# A12 — checklista realizacji

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

## Postęp

- **Pokrycie przypiętego korpusu:** 90,2% (582 734 / 646 000 rekordów zgodnych; 63 266 skipów; zero mismatchy, timeoutów i błędów parsowania). Jest to miernik zgodności z korpusem, a nie procent wszystkich cech 8086.
- **Ostatni zamknięty pakiet ESC D8–DF:** 6/6 punktów (100%); fixture’y 2 560/2 560, filtry 8 012/8 012 i 7 988/7 988, oddzielne commity implementacji i checkpointu.
- **Ostatni zamknięty pakiet C6 /1–/7:** 6/6 punktów (**100%**); fixture’y 1 463/1 463, pełny plik 2 000/2 000.
- **Poprzedni pakiet SETMO/SETMOC D0–D3 /6:** 6/6 punktów (100%); źródłowy zakres to 8 000 rekordów.

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

## Dalsze prace A12

- [ ] Zakończyć analizę pozostałych prefiksów segmentowych w oddzielnych pakietach; po każdym pakiecie zapisać testy, pełny korpus i procent.
- [x] Rozstrzygnąć nieudokumentowane `C6 /1–/7` i `D0–D3 /6`; dowody i status są zapisane w macierzy ISA oraz raportach.
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

Po ukończeniu każdego kolejnego punktu należy od razu zaznaczyć checkbox i dopisać wiersz do rejestru. Wartość ukończenia pakietu to liczba zamkniętych punktów z sześciu; pokrycie korpusu pozostaje osobnym wskaźnikiem.
