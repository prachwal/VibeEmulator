# A12 — checklista prac do zakończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Dokument jest bieżącym rejestrem otwartych prac A12. Po zakończeniu każdego punktu należy zaznaczyć jego checkbox, dopisać commit i wynik w sekcji „Historia postępu” oraz przeliczyć wskaźnik pokrycia z pełnego korpusu.

## Procent implementacji

Wskaźnik jest liczony jako `passed / 646 000 × 100` dla przypiętego korpusu SingleStepTests 8086 o SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Jest to odsetek rekordów tego zestawu, które przechodzą pełne porównanie stanu, pamięci, kolejki i cykli; nie jest deklaracją procentu wszystkich cech krzemu ani ukończenia całego procesora.

**Aktualnie: 87,5% — 564 958 / 646 000 passed.** Pozostałe kategorie: 81 042 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania. Wynik pochodzi z pełnego przebiegu korpusu po `SALC` (`D6`); szczegółowy raport: [A12-salc-d6-report.json](A12-salc-d6-report.json).

## Bieżąca inwentaryzacja skipów

Poniższe rozłączne powody po pełnym przebiegu SALC sumują się do 81 042:

| powód w raporcie | rekordy | praca |
|---|---:|---|
| prefiks segmentowy poza aktywną listą opkodów | 67 786 | analiza i obsługa wszystkich zaobserwowanych poprawnych kombinacji prefiks/opcode, porcjowana według grup ISA |
| `D0–D3 /6` z prefiksem i bez niego | 8 000 | ustalić zachowanie kodowania z wektorów i źródeł 8086; zaimplementować tylko zachowanie poparte dowodami lub jawnie udokumentować nieokreślone kodowanie |
| `C7` | 1 730 | nieobsługiwane rozszerzenia ModR/M `/1–/7`; `C7 /0` przechodzi pełny filtr |
| `8F` | 1 762 | nieobsługiwane rozszerzenia ModR/M `/1–/7`; poprawne `8F /0` działa dla form rejestrowych i pamięciowych |
| segment-prefiksowane `C6` poza `/0` | 903 | rozstrzygnąć legalność/nieokreśloność kodowania i zachowanie na pinie |
| `C6` bez prefiksu poza `/0` | 861 | rozstrzygnąć legalność/nieokreśloność kodowania i zachowanie na pinie |
| **Razem** | **81 042** | |

Uwaga: implementacja opcode’u może odblokować również jego rekordy obecnie przypisane do powodu „prefiks segmentowy”. Liczby w tabeli są klasyfikacją aktualnych skipów, nie sumą prognozowanych zysków per pakiet.

## Prace do wykonania

- [x] **Zebrać i uzgodnić bazę skipów.** Historyczny checkpoint bazowy miał 118 481 skipów. Aktualny raport po `MOV 89/8B` ma 87 550 skipów, zero mismatchy, timeoutów i błędów parsowania.
- [x] **Prefiksy segmentowe `D0–D3`.** Z 31 905 prefiksowanych rekordów `/0–/5,/7` wdrożono 27 906; 3 999 prefiksowanych `/6` pozostaje jawnie sklasyfikowane jako skip. Wszystkie 4 480 lokalnych fixture’ów, pełny filtr 64 000 rekordów (`56 000 passed`, `8 000 /6 skipped`) i pełny korpus przeszły bez mismatchy, timeoutów i błędów parsowania. Pełny korpus wzrósł o 27 906 rekordów do 555 425/646 000 (86,0%). Plan i bramki: [A12-prefiksy-segmentowe-D0-D3-plan.md](A12-prefiksy-segmentowe-D0-D3-plan.md); raport: [A12-prefiksy-segmentowe-D0-D3-report.json](A12-prefiksy-segmentowe-D0-D3-report.json). Plan jest w commicie `7cf43e3`, implementacja w `8db8f9d`.
- [x] **Pamięciowe `MOV 89/8B`.** Obsłużono wszystkie 3 025 pomijane pamięciowe rekordy obu kierunków (1 518 nieprefiksowanych i 1 507 segment-prefiksowanych). Pełny filtr 4 000/4 000 przechodzi; pełny korpus wzrósł o 3 025 do 558 450/646 000 (86,4%), zero mismatchy/timeoutów/parse errors. Lokalne prefiksowane fixture’y: 160/160; raport: [A12-mov-memory-89-8b-report.json](A12-mov-memory-89-8b-report.json); plan: [A12-mov-memory-89-8b-plan.md](A12-mov-memory-89-8b-plan.md). Osobne commity: plan `d483857`, implementacja `c449767`.
- [x] **`LES/LDS` (`C4/C5`).** `C4` = LES, `C5` = LDS. Pełne filtry 4 000/4 000, lokalne fixture’y 40/40, Intel8086 Release 18 271/18 271. Pełny korpus 562 450/646 000 (87,1%), 83 550 skipów, 0 mismatchy/timeoutów/parse errors. Debug/Release build 0 ostrzeżeń/błędów; pełna solucja Release 21 071/21 071; formatter verify, manifest i OpcodeInfo zielone. Plan: [A12-les-lds-c4-c5-plan.md](A12-les-lds-c4-c5-plan.md), raport: [A12-les-lds-c4-c5-report.json](A12-les-lds-c4-c5-report.json). Plan commit `5158550`; implementacja `1817148`.
- [x] **`POP r/m16` (`8F /0`).** Zaimplementowano formy rejestrowe i pamięciowe, adresowanie ModR/M, prefiksy segmentowe, parzyste/nieparzyste transfery i profil T-state z pinned SST. Lokalne fixture’y: 202/202; pełny filtr `/0`: 238/238; cały plik `8F`: 238 passed, 1 762 jawne skipped, 0 mismatchy/timeoutów/parse errors. W chwili zamknięcia pakietu korpus: 562 688/646 000 (87,1%), 83 312 skipped. Raport: [A12-pop-rm16-8f-report.json](A12-pop-rm16-8f-report.json); osobne commity: plan `4ca96d9`, implementacja `54f875e`.
- [x] **`MOV r/m16,imm16` (`C7 /0`).** Fixture’y: 226/226; pełny filtr `/0`: 270/270; cały plik `C7`: 270 passed, 1 730 jawnych skipped (`/1–/7`), 0 mismatchy/timeoutów/parse errors. Pełny korpus: 562 958/646 000 (87,1%), 83 042 skipped, 0 mismatchy/timeoutów/parse errors. Debug/Release 0 ostrzeżeń/błędów; testy 21 499/21 499; formatter, OpcodeInfo 512/512 i manifest zielone. Raport: [A12-mov-rm16-immediate-c7-report.json](A12-mov-rm16-immediate-c7-report.json); osobne commity: plan `d49f2bf`, implementacja `fbd6e43`.
- [x] **`SALC` (`D6`).** Fixture’y dla braku prefiksu i ES/CS/SS/DS: 100/100; cały plik 2 000/2 000. `AL` otrzymuje maskę CF, pozostałe flagi i AH pozostają bez zmian; czasy 3/4 T-state bez prefiksu i 5/6 z prefiksem dla CF=0/1. Pełny korpus: 564 958/646 000 (87,5%), 81 042 skipped, 0 mismatchy/timeoutów/parse errors. Solucja Release 22 239/22 239, buildy Debug/Release bez ostrzeżeń/błędów, formatter, OpcodeInfo i manifest zielone. Plan: [A12-salc-d6-plan.md](A12-salc-d6-plan.md), raport: [A12-salc-d6-report.json](A12-salc-d6-report.json).
- [ ] **Pozostałe prefiksy segmentowe.** Kontynuować porcjowanie otwartych grup opcode, zachowując koszty prefiksu, wpływ na kolejkę i zachowanie przy rejestrowym operandzie. Podpakiet `FF /0,/1` przeszedł 1 983 prefiksowane rekordy, 640/640 fixture’ów i pełne pliki 4 000/4 000; wówczas nie zmienił korpusu. Plan: [A12-prefixed-inc-dec-ff01-plan.md](A12-prefixed-inc-dec-ff01-plan.md), commit `72d5a45`; raport i fixture’y: `089eeea`. Bieżący pełny wynik po SALC: 564 958/646 000 (87,5%).
- [ ] **Kodowania poza `/0` w `C6` i `D0–D3 /6`.** Rozstrzygnąć na podstawie pinu oraz dokumentacji, które kodowania są nielegalne lub nieokreślone, a które mają obserwowalne działanie; utrwalić decyzję testem/raportem. Nie zgadywać semantyki.
- [ ] **Niezależny test programu lub ROM.** Wybrać niezależne źródło z jasną licencją/pochodzeniem, określić uruchamianie i kryterium sukcesu, dodać reproducowalny test bez zastępowania nim wektorów SST.
- [ ] **Zamknąć znane luki cyklowe A11/A10.** Uzyskać źródłowe ślady dla kolejności T-state wejścia NMI/INTR, statusu magistrali HALT, wznowienia WAIT po IRET, odroczenia READY przy transakcjach z efektami ubocznymi, arbitrażu RQ/GT, opóźnienia po MOV/POP SS i brakujących sekwencji prefiksów. Każdą lukę rozstrzygnąć testem lub pozostawić jako jawne ograniczenie z uzasadnieniem źródłowym.
- [ ] **Audyt końcowy celu A1–A12.** Sprawdzić łańcuch branchy A0–A12 i osobny commit każdego etapu; uruchomić pełny korpus i wszystkie bramki z `AGENTS.md`; zsynchronizować README, macierz ISA, checklisty oraz raporty. Pełne ukończenie nie może być deklarowane przy nieopisanych skipach, mismatchach ani znanych lukach.

## Reguła pracy i weryfikacji

Pracować pakietami opcode. W obrębie pakietu używać fixture’ów i wąskich filtrów do szybkiej diagnozy; pełny filtr źródłowy i cały korpus uruchamiać po ustabilizowaniu pakietu. Przed commitem pakietu wykonać wymagane buildy, testy, formatter, walidator manifestu i OpcodeInfo. Po każdym zakończonym punkcie aktualizować checkbox i wskaźnik `%` na podstawie nowego pełnego raportu. Jeżeli pozostałe rekordy są nielegalnymi kodowaniami, osobno udowodnić i udokumentować tę klasyfikację — nie ukrywać ich jako zaliczonych.

## Historia postępu

| data | ukończony punkt | commit | passed / 646 000 | pokrycie |
|---|---|---|---:|---:|
| 2026-10-25 | Inwentaryzacja skipów i ustalenie pomiaru bazowego | `efdd9c8` | 527 519 | 81,7% |
| 2026-10-25 | Prefiksy segmentowe `D0–D3` | plan `7cf43e3`; implementacja `8db8f9d` | 555 425 | 86,0% |
| 2026-10-25 | Pamięciowe `MOV 89/8B` | plan `d483857`; implementacja `c449767` | 558 450 | 86,4% |
| 2026-10-25 | `LES/LDS` (`C4/C5`) — pełne filtry, korpus i bramki końcowe | plan `5158550`; implementacja `1817148` | 562 450 | 87,1% |
| 2026-10-25 | `POP r/m16` (`8F /0`) — lokalne fixture’y, pełny filtr `/0`, klasyfikacja `/1–/7` i pełny korpus | plan `4ca96d9`; implementacja `54f875e` | 562 688 | 87,1% |
| 2026-10-25 | `MOV r/m16,imm16` (`C7 /0`) — fixture’y, pełny filtr, jawne skipy `/1–/7` i pełny korpus | plan `d49f2bf`; implementacja `fbd6e43` | 562 958 | 87,1% |
| 2026-10-25 | Segment-prefiksowane `FF /0,/1` — lokalne fixture’y i pełne pliki potwierdziły istniejącą obsługę; bez zmiany korpusu | plan `72d5a45`; dowody `089eeea` | 562 958 | 87,1% |
| 2026-10-25 | `SALC` (`D6`) — pięć grup fixture’ów i wszystkie rekordy źródłowe; pełny korpus bez mismatchy | plan `c0c9e6e`; implementacja `519dc83` | 564 958 | 87,5% |

