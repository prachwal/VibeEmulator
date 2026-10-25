# A12 — lista do zakończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

Główna, punktowa checklista otwartych prac i aktualizacji procentu: [A12-checklista-do-dokonczenia.md](A12-checklista-do-dokonczenia.md).

## Postęp implementacji

**90,5% korpusu SingleStepTests:** 584 464 / 646 000 rekordów przechodzi pełne porównanie CPU, pamięci, kolejki i cykli. Pozostaje 61 536 jawnie pominiętych rekordów; 0 rozbieżności, timeoutów i błędów parsowania. To miernik zgodności z przypiętym korpusem (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), nie procent wszystkich cech procesora.

Po każdym zamkniętym pakiecie należy zaznaczyć punkt poniżej, wpisać commit i wynik, a procent odświeżyć na podstawie pełnego przebiegu 646 000 rekordów. Nie zmieniać procentu na podstawie próbek ani samych testów jednostkowych.

## Do dokończenia

- [x] **Segment-prefiksowane ESC `D8–DF`** — fixture’y 2 560/2 560, pełny filtr prefiksowany 8 012/8 012, kontrolny nieprefiksowany 7 988/7 988, pełny korpus 572 970/646 000 (88,7%), zero mismatchy/timeoutów/parse errors. Buildy, pełne testy, format, OpcodeInfo, manifest i diff check są zielone. Plan: [A12-prefiksy-segmentowe-esc-d8-df-plan.md](A12-prefiksy-segmentowe-esc-d8-df-plan.md); osobne commity: plan `9ebc8c4`, implementacja `5ebb882`.
- [x] **`SETMO/SETMOC` (`D0–D3 /6`)** — fixture’y 1 600/1 600, pełny filtr 8 000/8 000, pełny korpus 580 970/646 000 (89,9%), 65 030 skipów i 0 mismatchy/timeoutów/parse errors. Plan `bd1ae29`, implementacja `aaed1ed`, checkpoint dokumentacji w oddzielnym commicie. Raport: [A12-setmo-d0-d3-6-report.json](A12-setmo-d0-d3-6-report.json).
- [ ] **Pozostałe prefiksy segmentowe** — kontynuować analizę otwartych opkodów w rozłącznych pakietach; dla każdego pakietu zapisać liczbę fixture’ów, pełny filtr, wpływ na skipy i commit. Wynik/commit: —.
- [x] **Nieudokumentowane rozszerzenia `C6 /1–/7`** — wszystkie 1 764 rekordy przechodzą; plik C6 2 000/2 000. Plan `c91a464`, implementacja `d16021e`, checkpoint `02cacb4`. Raport: [A12-c6-ignored-extension-report.json](A12-c6-ignored-extension-report.json).
- [x] **Nieudokumentowane rozszerzenia `C7 /1–/7`** — pełny C7 2 000/2 000; fixture’y 1 426/1 426; pełny korpus 584 464/646 000 (90,5%), 61 536 skipów, 0 mismatchy/timeoutów/parse errors. Plan `ffee9f8`, implementacja `d24a4d1`, checkpoint `aeb0699`. Raport: [A12-c7-ignored-extension-report.json](A12-c7-ignored-extension-report.json).
- [ ] **Nieudokumentowane rozszerzenia `8F /1–/7`** — przed implementacją zebrano rozkład 1 762 wektorów (861 bez prefiksu, 901 prefiksowanych; 140 grup). Metadata określa je jako `undefined`; zachowanie sprzętu wymaga porównania pełnych śladów. Plan: [A12-8f-ignored-extension-plan.md](A12-8f-ignored-extension-plan.md).
- [ ] **Niezależny program lub ROM** — wybrać test z jawnym pochodzeniem i licencją, opisać reprodukowalne uruchomienie i kryterium sukcesu. Wynik/commit: —.
- [ ] **Znane luki cyklowe A10/A11** — rozstrzygnąć albo jawnie udokumentować: kolejność T-state NMI/INTR, magistrala HALT, WAIT po IRET, READY przy efektach ubocznych, arbitraż RQ/GT, opóźnienie po MOV/POP SS i brakujące sekwencje prefiksów. Wynik/commit: —.
- [ ] **Końcowy audyt A1–A12** — zweryfikować łańcuch branchy i osobny commit każdego etapu, uruchomić wszystkie bramki z `AGENTS.md`, uzgodnić checklisty, macierz ISA, README, raporty i jawne pozostałe ograniczenia. Wynik/commit: —.

## Rejestr ukończonych punktów

| data | punkt | commit(y) | passed / 646 000 | % |
|---|---|---|---:|---:|
| 2026-10-25 | `SALC` (`D6`), pełny plik i pełny korpus bez rozbieżności | `c0c9e6e`, `519dc83`, `6784b2b` | 564 958 | 87,5% |
| 2026-10-25 | Segment-prefiksowane ESC (`D8–DF`), fixture’y, oba pełne filtry i bramki zielone | plan `9ebc8c4`; implementacja `5ebb882` | 572 970 | 88,7% |
| 2026-10-25 | `SETMO/SETMOC` (`D0–D3 /6`), pełny filtr, korpus i bramki | plan `bd1ae29`; implementacja `aaed1ed`; checkpoint osobno | 580 970 | 89,9% |
| 2026-10-25 | `C6 /1–/7`, pełny plik, korpus i bramki | plan `c91a464`; implementacja `d16021e`; checkpoint `02cacb4` | 582 734 | 90,2% |
| 2026-10-25 | `C7 /1–/7`, pełny plik, korpus i bramki | plan `ffee9f8`; implementacja `d24a4d1`; checkpoint `aeb0699` | 584 464 | 90,5% |
| 2026-10-25 | Inwentaryzacja i plan `8F /1–/7` przed kodem | 1 762 rekordy, 140 grup; plan `3fecba9` osobny przed implementacją | 584 464 | 90,5% |

Szczegółowy rejestr wcześniejszych pakietów i rozbicie skipów: [A12-dalsze-prace-checklist.md](A12-dalsze-prace-checklist.md). Stan wznowienia: [stan-wznowienia-A12.md](stan-wznowienia-A12.md).
