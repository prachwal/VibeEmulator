# A12 — checklista do dokończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

To jest bieżąca lista otwartych prac. Po zamknięciu każdego punktu należy od razu zaznaczyć checkbox, dopisać commit i wynik
do rejestru oraz zaktualizować procent. Wskaźnik implementacji oznacza wyłącznie zgodność z przypiętym korpusem
SingleStepTests 8086 (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), a nie procent wszystkich cech procesora.

Szczegółowy, osobny tracker z aktualną checklistą i historią zmian po każdym punkcie: [A12-biezacy-rejestr-postepu.md](A12-biezacy-rejestr-postepu.md).

## Aktualny stan

- **Pokrycie korpusu: 100%** — 646 000 / 646 000 passed.
- **Pozostałe:** 0 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania.
- Ostatni raport pełnego przebiegu: [segment-prefiksowany CALL FAR 9A](A12-prefixed-call-far-9a-report.json).
- Nadal otwarte są niezależny test programu/ROM, jawny audyt ograniczeń sprzętowych A10/A11 i końcowy audyt A1–A12.

Szczegółowy rejestr odhaczania po każdym zamkniętym punkcie: [A12-biezacy-rejestr-postepu.md](A12-biezacy-rejestr-postepu.md).

## Otwarta lista

### 1. Segment-prefiksowane kombinacje źródłowe — zamknięte

- [x] Inwentaryzacja skipów zakończyła się przypiętą listą źródłową; ostatnia grupa to 1 011 segment-prefiksowanych `CALL FAR 9A`.
- [x] Plan osobny `c365fac`; 80 fixture’ów, 1 011/1 011 pełnego filtra i 2 000/2 000 pliku 9A.
- [x] Pełny korpus po implementacji: 646 000/646 000 (100%), zero skipów/rozbieżności/timeoutów/parse errors.
- [x] Raport: [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).

### 2. Nieudokumentowane rozszerzenia `8F /1–/7`

- [x] Pełne ślady P80C86A-2 potwierdzają 1 762/1 762 aliasy `POP r/m16`; jest to obserwacja układu, metadata upstream nadal klasyfikuje je jako undefined.
- [x] Fixture’y 1 461/1 461 w 140 grupach; pełny plik 8F 2 000/2 000.
- [x] Pełny korpus: 586 226/646 000 (**90,7%**), 59 774 skipy segment-prefiksowe, zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości zielone: Debug/Release, testy Release 22 244/22 244, format, OpcodeInfo 512/512, manifest 1 962/34 549.
- [x] Osobny checkpoint dokumentacji `7a7d82d`; hash zapisany w raporcie.

### 3. Prefiksy segmentowe `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`)

- [x] Dodać 320 fixture’ów w 16 grupach; lokalny test przechodzi 320/320.
- [x] Pełne pliki źródłowe E0–E3 przechodzą 8 000/8 000, w tym 3 943 rekordy segment-prefiksowane.
- [x] Pełny korpus po pakiecie: 593 185/646 000 (91,8%), 52 815 skipów, 0 mismatchy/timeoutów/parse errors.
- [x] Build Debug/Release, testy Release, format/verify, OpcodeInfo, manifest i `git diff --check` zielone.
- [x] Plan `e0b5209` i implementacja `5890848` są osobnymi commitami; checkpoint dokumentacji `52f27e0` zapisany osobno.


### 4. Niezależny test programu lub ROM

- [ ] Wybrać niezależny program/test z ustalonym pochodzeniem i licencją.
- [ ] Opisać reprodukowalne uruchomienie, wymagane urządzenia oraz mierzalne kryterium sukcesu.
- [ ] Dodać test do repo i udokumentować wynik; nie zastępować nim wektorów SingleStepTests.

### 5. Luki cyklowe A10/A11

- [ ] Ustalić źródłowe zachowanie i test dla kolejności T-state wejścia NMI/INTR.
- [ ] Ustalić zachowanie magistrali podczas HALT.
- [ ] Ustalić wznowienie WAIT po IRET.
- [ ] Ustalić READY przy transakcjach z efektami ubocznymi.
- [ ] Ustalić arbitraż RQ/GT.
- [ ] Ustalić opóźnienie po MOV/POP SS.
- [ ] Ustalić brakujące sekwencje prefiksów.
- [ ] Każdą nierozstrzygniętą pozycję oznaczyć jako ograniczenie z uzasadnieniem źródłowym; nie deklarować jej jako zgodnej.

### 6. Końcowy audyt A1–A12

- [ ] Sprawdzić łańcuch branchy A0–A12 i potwierdzić osobne, kompletne commity etapów.
- [ ] Uruchomić pełny korpus oraz buildy Debug/Release, testy Release, format, OpcodeInfo i walidator manifestu.
- [ ] Uzgodnić checklisty, macierz ISA, README wektorów, raporty i listę znanych ograniczeń.
- [ ] Zamknąć A12 dopiero po zapisaniu wyniku audytu i procentu z najnowszego pełnego przebiegu korpusu.

## Zasada aktualizacji po każdym punkcie

1. Zaznaczyć tylko faktycznie zakończony punkt `[x]`.
2. Dodać wiersz do historii z nazwą punktu, commitami i wynikiem weryfikacji.
3. Przepisać `passed`, `skipped` i procent z nowego pełnego raportu korpusu. Liczyć `passed / 646 000 × 100`.
4. Gdy punkt nie zmienia korpusu (np. audyt albo ROM), wpisać **90,7% bez zmiany** albo nowszy potwierdzony wynik; nie szacować wzrostu.

## Historia zamkniętych punktów

| data | zamknięty punkt | commity | passed / 646 000 | pokrycie |
|---|---|---|---:|---:|
| 2026-10-25 | Ostatni pełny checkpoint przed tą listą: C7 `/1–/7` | plan `ffee9f8`; implementacja `d24a4d1`; dokumentacja `aeb0699` | 584 464 | 90,5% |
| 2026-10-25 | `8F /1–/7`: implementacja i pełna weryfikacja | 1 461 fixture’ów; pełny plik 2 000/2 000; commit `c6d1b4f`; checkpoint `7a7d82d` | 586 226 | 90,7% |
| 2026-10-25 | Segment-prefiksowane LOOP E0–E3: pełne filtry, korpus i bramki | plan `e0b5209`; implementacja `5890848`; checkpoint dokumentacji `52f27e0` zapisany osobno | 590 169 | 91,4% |
| 2026-10-25 | Segment-prefiksowany JMP EB: pełny korpus i zbiorcze bramki jakości | raport `/tmp/a12-eb-final-corpus-report.json`; Release 22 887/22 887; manifest 2 026/35 829 | 594 203 | 92,0% |
| 2026-10-25 | Segment-prefiksowane LEA 8D: pełne źródło, korpus i bramki | 1 004/1 004 prefiksowane; plik 2 000/2 000; raport A12-prefixed-lea-8d-report.json; Release 22 968/22 968 | 595 207 | 92,1% |


## Zamknięty pakiet EB

- [x] Fixture’y lokalne: 80/80.
- [x] Segment-prefiksowany filtr EB: 1 018/1 018; pełny plik EB: 2 000/2 000.
- [x] Kontrola regresji E8–EA+EB: 4 034/4 034 prefiksowane, pełne źródła 8 000/8 000.
- [x] Korpus po EB: 594 203/646 000 (92,0%), 51 797 skipów, 0 błędów.
- [x] Bramki jakości i synchronizacja dokumentacji: Debug/Release 0 ostrzeżeń/błędów; Release 22 887/22 887; format, OpcodeInfo 512/512, manifest 2 026/35 829 i `git diff --check` zielone.
- [ ] Osobne commity implementacji i checkpointu dokumentacji; dopisać hashe do raportu.

Postęp pakietu EB: **8/8 (100%)**. Szczegółowa osobna lista: [checklista EB](A12-checklista-pakietu-EB.md); plan: [EB](A12-prefixed-short-jump-eb-plan.md), raport: [EB](A12-prefixed-short-jump-eb-report.json).


## Bieżący pakiet LEA 8D

- [x] Plan przed kodem `b842f56`; cztery grupy prefiksów, 1 004 rekordy.
- [x] Fixture’y/test lokalny 80/80; pełny filtr prefiksowany 1 004/1 004 i plik 8D 2 000/2 000.
- [x] Pełny korpus 595 207/646 000 (92,1%), 50 793 skipped i 0 błędów; bramki jakości zielone (Release 22 968/22 968).
- [ ] Osobny commit dokumentacji; implementacja `9b7cc06` jest już zapisana; checklista pakietu **10/10 (100%)**: [szczegóły](A12-checklista-pakietu-prefixed-lea-8d.md).


## Zamknięty pakiet: segment-prefiksowane ALU

- [x] Inwentarz 49 782 rekordów ze źródłowego pinu i plan przed kodem zapisane.
- [x] Fixture’y 200/200, implementacja, filtr 49 782/49 782, pełne pliki 100 000/100 000 i korpus 644 989/646 000 przy zamknięciu.
- [x] Oddzielne commity planu `d790ce0`, implementacji `cab1fd2`, dokumentacji `fc9af2e`.

Checklista szczegółowa: [ALU `00–3D, 84/85`](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md), **11/11 (100%)**; pełny korpus 646 000/646 000 (100%).

| 2026-10-25 | Segment-prefiksowany CALL FAR 9A — pełny korpus | 1 011/1 011 prefiksowanych, 2 000/2 000 9A, 646 000/646 000 łącznie | 646 000 | 100% |

## Bieżący pakiet: segment-prefiksowany CALL FAR 9A

- [x] Plan `c365fac`, fixture’y 80/80, profil cycle-exact i implementacja.
- [x] Filtr prefiksowany 1 011/1 011; pełny plik 9A 2 000/2 000.
- [x] Pełny korpus 646 000/646 000 (100%); pełny Release 23 250/23 250 i wszystkie bramki jakości zielone.
- [x] Dokumentacja/macierze A12 zsynchronizowane; package report [9A](A12-prefixed-call-far-9a-report.json).
- [x] Osobny commit implementacji `3198647`; plan `c365fac`.
- [x] Osobny commit dokumentacji `cba7033`; checklista pakietu 9/9 (100%).
