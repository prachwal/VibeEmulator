# A12 — lista do zakończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

## Postęp implementacji

**88,7% korpusu SingleStepTests:** 572 970 / 646 000 rekordów przechodzi pełne porównanie CPU, pamięci, kolejki i cykli. Pozostaje 73 030 jawnie pominiętych rekordów; 0 rozbieżności, timeoutów i błędów parsowania. To miernik zgodności z przypiętym korpusem (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), nie procent wszystkich cech procesora.

Po każdym zamkniętym pakiecie należy zaznaczyć punkt poniżej, wpisać commit i wynik, a procent odświeżyć na podstawie pełnego przebiegu 646 000 rekordów. Nie zmieniać procentu na podstawie próbek ani samych testów jednostkowych.

## Do dokończenia

- [x] **Segment-prefiksowane ESC `D8–DF`** — fixture’y 2 560/2 560, pełny filtr prefiksowany 8 012/8 012, kontrolny nieprefiksowany 7 988/7 988, pełny korpus 572 970/646 000 (88,7%), zero mismatchy/timeoutów/parse errors. Buildy, pełne testy, format, OpcodeInfo, manifest i diff check są zielone. Plan: [A12-prefiksy-segmentowe-esc-d8-df-plan.md](A12-prefiksy-segmentowe-esc-d8-df-plan.md); osobne commity: plan `9ebc8c4`, implementacja `5ebb882`.
- [ ] **Pozostałe prefiksy segmentowe** — kontynuować analizę otwartych opkodów w rozłącznych pakietach; dla każdego pakietu zapisać liczbę fixture’ów, pełny filtr, wpływ na skipy i commit. Wynik/commit: —.
- [ ] **Nieobsługiwane rozszerzenia `C6` oraz `D0–D3 /6`** — ustalić ze źródeł i wektorów, które kodowania są nielegalne/nieokreślone, a które mają obserwowalne zachowanie; opisać dowody bez zgadywania semantyki. Wynik/commit: —.
- [ ] **Niezależny program lub ROM** — wybrać test z jawnym pochodzeniem i licencją, opisać reprodukowalne uruchomienie i kryterium sukcesu. Wynik/commit: —.
- [ ] **Znane luki cyklowe A10/A11** — rozstrzygnąć albo jawnie udokumentować: kolejność T-state NMI/INTR, magistrala HALT, WAIT po IRET, READY przy efektach ubocznych, arbitraż RQ/GT, opóźnienie po MOV/POP SS i brakujące sekwencje prefiksów. Wynik/commit: —.
- [ ] **Końcowy audyt A1–A12** — zweryfikować łańcuch branchy i osobny commit każdego etapu, uruchomić wszystkie bramki z `AGENTS.md`, uzgodnić checklisty, macierz ISA, README, raporty i jawne pozostałe ograniczenia. Wynik/commit: —.

## Rejestr ukończonych punktów

| data | punkt | commit(y) | passed / 646 000 | % |
|---|---|---|---:|---:|
| 2026-10-25 | `SALC` (`D6`), pełny plik i pełny korpus bez rozbieżności | `c0c9e6e`, `519dc83`, `6784b2b` | 564 958 | 87,5% |
| 2026-10-25 | Segment-prefiksowane ESC (`D8–DF`), fixture’y, oba pełne filtry i bramki zielone | plan `9ebc8c4`; implementacja `5ebb882` | 572 970 | 88,7% |

Szczegółowy rejestr wcześniejszych pakietów i rozbicie skipów: [A12-dalsze-prace-checklist.md](A12-dalsze-prace-checklist.md). Stan wznowienia: [stan-wznowienia-A12.md](stan-wznowienia-A12.md).
