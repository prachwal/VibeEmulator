# A12 — checklista do dokończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

To jest bieżąca lista otwartych prac. Po zamknięciu każdego punktu należy od razu zaznaczyć checkbox, dopisać commit i wynik
do rejestru oraz zaktualizować procent. Wskaźnik implementacji oznacza wyłącznie zgodność z przypiętym korpusem
SingleStepTests 8086 (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), a nie procent wszystkich cech procesora.

## Stan bazowy

- **Pokrycie korpusu: 90,7%** — 586 226 / 646 000 rekordów passed.
- **Pozostałe:** 59 774 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania.
- Podział skipów: 59 774 rekordy z prefiksami segmentowymi; rozszerzenia `8F /1–/7` przechodzą po implementacji.
- Źródłowy raport ostatniego pełnego przebiegu: [8F — raport](A12-8f-ignored-extension-report.json).

## Otwarta lista

### 1. Pozostałe prefiksy segmentowe

- [ ] Inwentaryzować pomijane kombinacje prefiks/opcode i pogrupować je na niezależne pakiety.
- [ ] Dla każdego pakietu zapisać plan przed zmianą kodu, dodać fixture’y, wdrożyć zachowanie i uruchomić pełny filtr źródłowy.
- [ ] Po ustabilizowaniu każdego pakietu wykonać pełny przebieg korpusu, bramki jakości oraz osobne commity planu, implementacji i checkpointu.
- [ ] Powtarzać pakiety aż wszystkie poprawne, zaobserwowane kombinacje będą obsłużone albo jawnie udokumentowane jako ograniczenia.

### 2. Nieudokumentowane rozszerzenia `8F /1–/7`

- [x] Pełne ślady P80C86A-2 potwierdzają 1 762/1 762 aliasy `POP r/m16`; jest to obserwacja układu, metadata upstream nadal klasyfikuje je jako undefined.
- [x] Fixture’y 1 461/1 461 w 140 grupach; pełny plik 8F 2 000/2 000.
- [x] Pełny korpus: 586 226/646 000 (**90,7%**), 59 774 skipy segment-prefiksowe, zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości zielone: Debug/Release, testy Release 22 244/22 244, format, OpcodeInfo 512/512, manifest 1 962/34 549.
- [x] Osobny checkpoint dokumentacji `7a7d82d`; hash zapisany w raporcie.


### 3. Niezależny test programu lub ROM

- [ ] Wybrać niezależny program/test z ustalonym pochodzeniem i licencją.
- [ ] Opisać reprodukowalne uruchomienie, wymagane urządzenia oraz mierzalne kryterium sukcesu.
- [ ] Dodać test do repo i udokumentować wynik; nie zastępować nim wektorów SingleStepTests.

### 4. Luki cyklowe A10/A11

- [ ] Ustalić źródłowe zachowanie i test dla kolejności T-state wejścia NMI/INTR.
- [ ] Ustalić zachowanie magistrali podczas HALT.
- [ ] Ustalić wznowienie WAIT po IRET.
- [ ] Ustalić READY przy transakcjach z efektami ubocznymi.
- [ ] Ustalić arbitraż RQ/GT.
- [ ] Ustalić opóźnienie po MOV/POP SS.
- [ ] Ustalić brakujące sekwencje prefiksów.
- [ ] Każdą nierozstrzygniętą pozycję oznaczyć jako ograniczenie z uzasadnieniem źródłowym; nie deklarować jej jako zgodnej.

### 5. Końcowy audyt A1–A12

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

