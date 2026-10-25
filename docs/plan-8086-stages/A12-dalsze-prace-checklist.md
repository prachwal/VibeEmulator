# A12 — szczegółowa checklista prac do zakończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Po każdym zakończonym punkcie zaznacz checkbox, dopisz commit i wynik oraz przelicz pokrycie z pełnego korpusu. Grupuj powiązane opkody w pakiet; podczas implementacji używaj wąskich filtrów, a pełne przebiegi wykonuj raz po ustabilizowaniu pakietu.

Krótka lista pozostałych rezultatów do dowiezienia i rejestr procentu po zamknięciu pakietu: [A12-do-zakonczenia.md](A12-do-zakonczenia.md).
Główna checklista punktów do dokończenia: [A12-checklista-do-dokonczenia.md](A12-checklista-do-dokonczenia.md).

## Procent implementacji

Wskaźnik jest liczony jako `passed / 646 000 × 100` dla przypiętego korpusu SingleStepTests 8086 o SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Jest to odsetek rekordów tego zestawu, które przechodzą pełne porównanie stanu, pamięci, kolejki i cykli; nie jest deklaracją procentu wszystkich cech krzemu ani ukończenia całego procesora.

**Aktualnie: 91,8% — 593 185 / 646 000 passed.** Pozostaje 52 815 skipów segment-prefiksowych, 0 mismatchy, timeoutów i błędów parsowania. Wynik pochodzi z przebiegu po prefiksowanych `CALL/JMP` (`E8–EA`); raport: [A12-prefixed-direct-call-jump-e8-ea-report.json](A12-prefixed-direct-call-jump-e8-ea-report.json).

## Bieżąca inwentaryzacja skipów

Poniższe powody po pełnym przebiegu prefiksowanych `CALL/JMP` sumują się do 52 815:

| powód w raporcie | rekordy | praca |
|---|---:|---|
| prefiks segmentowy poza aktywną listą opkodów | 52 815 | analiza i obsługa pozostałych zaobserwowanych kombinacji prefiks/opcode, porcjowana według grup ISA |
| **Razem** | **52 815** | |

Uwaga: implementacja opcode’u może odblokować również jego rekordy obecnie przypisane do powodu „prefiks segmentowy”. Liczby w tabeli są klasyfikacją aktualnych skipów, nie sumą prognozowanych zysków per pakiet.

## Prace do wykonania

- [x] **Zebrać i uzgodnić bazę skipów.** Historyczny checkpoint bazowy miał 118 481 skipów. Aktualny raport po `MOV 89/8B` ma 87 550 skipów, zero mismatchy, timeoutów i błędów parsowania.
- [x] **Prefiksy segmentowe `D0–D3`.** Z 31 905 prefiksowanych rekordów `/0–/5,/7` wdrożono 27 906; 3 999 prefiksowanych `/6` pozostaje jawnie sklasyfikowane jako skip. Wszystkie 4 480 lokalnych fixture’ów, pełny filtr 64 000 rekordów (`56 000 passed`, `8 000 /6 skipped`) i pełny korpus przeszły bez mismatchy, timeoutów i błędów parsowania. Pełny korpus wzrósł o 27 906 rekordów do 555 425/646 000 (86,0%). Plan i bramki: [A12-prefiksy-segmentowe-D0-D3-plan.md](A12-prefiksy-segmentowe-D0-D3-plan.md); raport: [A12-prefiksy-segmentowe-D0-D3-report.json](A12-prefiksy-segmentowe-D0-D3-report.json). Plan jest w commicie `7cf43e3`, implementacja w `8db8f9d`.
- [x] **Pamięciowe `MOV 89/8B`.** Obsłużono wszystkie 3 025 pomijane pamięciowe rekordy obu kierunków (1 518 nieprefiksowanych i 1 507 segment-prefiksowanych). Pełny filtr 4 000/4 000 przechodzi; pełny korpus wzrósł o 3 025 do 558 450/646 000 (86,4%), zero mismatchy/timeoutów/parse errors. Lokalne prefiksowane fixture’y: 160/160; raport: [A12-mov-memory-89-8b-report.json](A12-mov-memory-89-8b-report.json); plan: [A12-mov-memory-89-8b-plan.md](A12-mov-memory-89-8b-plan.md). Osobne commity: plan `d483857`, implementacja `c449767`.
- [x] **`LES/LDS` (`C4/C5`).** `C4` = LES, `C5` = LDS. Pełne filtry 4 000/4 000, lokalne fixture’y 40/40, Intel8086 Release 18 271/18 271. Pełny korpus 562 450/646 000 (87,1%), 83 550 skipów, 0 mismatchy/timeoutów/parse errors. Debug/Release build 0 ostrzeżeń/błędów; pełna solucja Release 21 071/21 071; formatter verify, manifest i OpcodeInfo zielone. Plan: [A12-les-lds-c4-c5-plan.md](A12-les-lds-c4-c5-plan.md), raport: [A12-les-lds-c4-c5-report.json](A12-les-lds-c4-c5-report.json). Plan commit `5158550`; implementacja `1817148`.
- [x] **`POP r/m16` (`8F /0`).** Zaimplementowano formy rejestrowe i pamięciowe, adresowanie ModR/M, prefiksy segmentowe, parzyste/nieparzyste transfery i profil T-state z pinned SST. Lokalne fixture’y: 202/202; pełny filtr `/0`: 238/238; cały plik `8F`: 238 passed, 1 762 jawne skipped, 0 mismatchy/timeoutów/parse errors. W chwili zamknięcia pakietu korpus: 562 688/646 000 (87,1%), 83 312 skipped. Raport: [A12-pop-rm16-8f-report.json](A12-pop-rm16-8f-report.json); osobne commity: plan `4ca96d9`, implementacja `54f875e`.
- [x] **`MOV r/m16,imm16` (`C7 /0`).** Fixture’y: 226/226; pełny filtr `/0`: 270/270; cały plik `C7`: 270 passed, 1 730 jawnych skipped (`/1–/7`), 0 mismatchy/timeoutów/parse errors. Pełny korpus: 562 958/646 000 (87,1%), 83 042 skipped, 0 mismatchy/timeoutów/parse errors. Debug/Release 0 ostrzeżeń/błędów; testy 21 499/21 499; formatter, OpcodeInfo 512/512 i manifest zielone. Raport: [A12-mov-rm16-immediate-c7-report.json](A12-mov-rm16-immediate-c7-report.json); osobne commity: plan `d49f2bf`, implementacja `fbd6e43`.
- [x] **`SALC` (`D6`).** Fixture’y dla braku prefiksu i ES/CS/SS/DS: 100/100; cały plik 2 000/2 000. `AL` otrzymuje maskę CF, pozostałe flagi i AH pozostają bez zmian; czasy 3/4 T-state bez prefiksu i 5/6 z prefiksem dla CF=0/1. Pełny korpus: 564 958/646 000 (87,5%), 81 042 skipped, 0 mismatchy/timeoutów/parse errors. Solucja Release 22 239/22 239, buildy Debug/Release bez ostrzeżeń/błędów, formatter, OpcodeInfo i manifest zielone. Plan: [A12-salc-d6-plan.md](A12-salc-d6-plan.md), raport: [A12-salc-d6-report.json](A12-salc-d6-report.json).
- [x] **Segment-prefiksowane ESC (`D8–DF`).** Fixture’y 2 560/2 560; prefiksowany filtr źródłowy 8 012/8 012; kontrolny nieprefiksowany 7 988/7 988; zero skipów i rozbieżności. Pełny korpus: 572 970/646 000 (88,7%), 73 030 skipped, 0 mismatchy/timeoutów/parse errors. Buildy Debug/Release 0 ostrzeżeń/błędów; testy Release 22 240/22 240 (Intel8086 19 440/19 440); format, OpcodeInfo 512/512, walidator manifestu 1 462/28 599 oraz `git diff --check` zielone. Plan: [A12-prefiksy-segmentowe-esc-d8-df-plan.md](A12-prefiksy-segmentowe-esc-d8-df-plan.md); raport: [A12-prefiksy-segmentowe-esc-d8-df-report.json](A12-prefiksy-segmentowe-esc-d8-df-report.json). Osobne commity: plan `9ebc8c4`, implementacja `5ebb882`.
- [ ] **Pozostałe prefiksy segmentowe.** Kontynuować porcjowanie otwartych grup opcode, zachowując koszty prefiksu, wpływ na kolejkę i zachowanie przy rejestrowym operandzie. ESC D8–DF, SETMO D0–D3 /6, LOOP E0–E3 oraz CALL/JMP E8–EA mają pełne filtry; inne grupy pozostają otwarte. Podpakiet `FF /0,/1` przeszedł 1 983 prefiksowane rekordy, 640/640 fixture’ów i pełne pliki 4 000/4 000; wówczas nie zmienił korpusu. Plan: [A12-prefixed-inc-dec-ff01-plan.md](A12-prefixed-inc-dec-ff01-plan.md), commit `72d5a45`; raport i fixture’y: `089eeea`. Bieżący pełny wynik: 586 226/646 000 (90,7%).
- [x] **Nieudokumentowane `C6 /1–/7`.** Wszystkie 1 764 aliasy przechodzą pełny plik C6 (2 000/2 000). Fixture’y: 1 463/1 463 w 140 grupach. Plan `c91a464`, implementacja `d16021e`; raport: [A12-c6-ignored-extension-report.json](A12-c6-ignored-extension-report.json).
- [x] **Nieudokumentowane `D0–D3 /6` (SETMO/SETMOC).** Wszystkie 8 000 przypiętych wektorów przechodzi; plan `bd1ae29`, implementacja `aaed1ed`, checkpoint w osobnym commicie. Szczegóły: [plan](A12-setmo-d0-d3-6-plan.md), [raport](A12-setmo-d0-d3-6-report.json).
- [x] **Nieudokumentowane rozszerzenia `8F /1–/7`** — 1 762/1 762 przechodzą jako alias `POP r/m16` na P80C86A-2; metadata nadal oznacza je jako undefined. Fixture’y 1 461/1 461; pełny plik 2 000/2 000; plan `3fecba9`, implementacja `c6d1b4f`; raport [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json).
- [ ] **Niezależny test programu lub ROM.** Wybrać niezależne źródło z jasną licencją/pochodzeniem, określić uruchamianie i kryterium sukcesu, dodać reproducowalny test bez zastępowania nim wektorów SST.
- [ ] **Zamknąć znane luki cyklowe A11/A10.** Uzyskać źródłowe ślady dla kolejności T-state wejścia NMI/INTR, statusu magistrali HALT, wznowienia WAIT po IRET, odroczenia READY przy transakcjach z efektami ubocznymi, arbitrażu RQ/GT, opóźnienia po MOV/POP SS i brakujących sekwencji prefiksów. Każdą lukę rozstrzygnąć testem lub pozostawić jako jawne ograniczenie z uzasadnieniem źródłowym.
- [ ] **Audyt końcowy celu A1–A12.** Sprawdzić łańcuch branchy A0–A12 i osobny commit każdego etapu; uruchomić pełny korpus i wszystkie bramki z `AGENTS.md`; zsynchronizować README, macierz ISA, checklisty oraz raporty. Pełne ukończenie nie może być deklarowane przy nieopisanych skipach, mismatchach ani znanych lukach.

- [x] **Segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`).** Fixture’y 240/240, filtr segment-prefiksowany 3 016/3 016, pełne pliki 6 000/6 000; pełny korpus 593 185/646 000 (91,8%), 52 815 skipów; wszystkie bramki zielone. Plan `10898e9`; raport [A12-prefixed-direct-call-jump-e8-ea-report.json](A12-prefixed-direct-call-jump-e8-ea-report.json).
- [x] **Segment-prefiksowane `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`).** Fixture’y 320/320; pełne źródła 8 000/8 000, w tym 3 943 prefiksowane; pełny korpus 590 169/646 000 (91,4%), 55 831 skipów; bramki jakości zielone. Plan `e0b5209`, implementacja `5890848`; raport [A12-prefixed-loop-e0-e3-report.json](A12-prefixed-loop-e0-e3-report.json).

## Reguła pracy i weryfikacji

Pracować pakietami powiązanych opkodów, żeby wspólnie analizować dekodowanie i cykle. Podczas implementacji uruchamiać test dotkniętej ścieżki oraz wąski filtr. Pełny filtr źródłowy, jeden przebieg korpusu i komplet bramek uruchomić po ustabilizowaniu pakietu, przed commitem; nie powtarzać ich po każdej poprawce. Po każdym zakończonym punkcie aktualizować checkbox i wskaźnik `%` na podstawie nowego pełnego raportu. Jeżeli pozostałe rekordy są nielegalnymi kodowaniami, osobno udowodnić i udokumentować tę klasyfikację — nie ukrywać ich jako zaliczonych.

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
| 2026-10-25 | Segment-prefiksowane ESC (`D8–DF`) — fixture’y, 8 012 prefiksowanych i 7 988 kontrolnych wektorów; pełny korpus bez mismatchy | plan `9ebc8c4`; implementacja `5ebb882` | 572 970 | 88,7% |
| 2026-10-25 | `SETMO/SETMOC` (`D0–D3 /6`) — 1 600 fixture’ów, pełne 8 000/8 000; pełny korpus bez mismatchy | plan `bd1ae29`; implementacja `aaed1ed`; checkpoint osobno | 580 970 | 89,9% |
| 2026-10-25 | Nieudokumentowane C6 `/1–/7` — 1 463 fixture’y/140 grup, pełny plik 2 000/2 000 i korpus bez mismatchy | plan `c91a464`; implementacja `d16021e`; checkpoint `02cacb4` | 582 734 | 90,2% |
| 2026-10-25 | Nieudokumentowane C7 `/1–/7` — 1 426 fixture’ów/140 grup, pełny plik 2 000/2 000; korpus bez mismatchy | plan `ffee9f8`; implementacja `d24a4d1`; checkpoint `aeb0699` | 584 464 | 90,5% |
| 2026-10-25 | Nieudokumentowane `8F /1–/7` — fixture’y 1 461/1 461, pełny plik 2 000/2 000; checkpoint dokumentacji `7a7d82d` | plan `3fecba9`; korekta `a2d5203`; implementacja `c6d1b4f` | 586 226 | 90,7% |
| 2026-10-25 | Segment-prefiksowane `LOOP` (`E0–E3`): pełne filtry, korpus i bramki | fixture’y 320/320; plan `e0b5209`; implementacja `5890848`; dokumentacja `52f27e0` | 590 169 | 91,4% |
| 2026-10-25 | Segment-prefiksowane `CALL/JMP` (`E8–EA`) | fixture’y 240/240; prefiks 3 016/3 016; pliki 6 000/6 000; raport i bramki zielone | 593 185 | 91,8% |

