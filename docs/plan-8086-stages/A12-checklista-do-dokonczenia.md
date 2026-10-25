# A12 — checklista do dokończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

To jest bieżąca lista otwartych prac. Po zamknięciu każdego punktu należy od razu zaznaczyć checkbox, dopisać commit i wynik
do rejestru oraz zaktualizować procent. Wskaźnik implementacji oznacza wyłącznie zgodność z przypiętym korpusem
SingleStepTests 8086 (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), a nie procent wszystkich cech procesora.

## Stan bazowy

- **Pokrycie korpusu: 90,5%** — 584 464 / 646 000 rekordów passed.
- **Pozostałe:** 61 536 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania.
- Podział skipów: 59 774 rekordy z prefiksami segmentowymi i 1 762 rozszerzenia `8F /1–/7`.
- Źródłowy raport ostatniego pełnego przebiegu: [C7 — raport](A12-c7-ignored-extension-report.json).

## Otwarta lista

### 1. Pozostałe prefiksy segmentowe

- [ ] Inwentaryzować pomijane kombinacje prefiks/opcode i pogrupować je na niezależne pakiety.
- [ ] Dla każdego pakietu zapisać plan przed zmianą kodu, dodać fixture’y, wdrożyć zachowanie i uruchomić pełny filtr źródłowy.
- [ ] Po ustabilizowaniu każdego pakietu wykonać pełny przebieg korpusu, bramki jakości oraz osobne commity planu, implementacji i checkpointu.
- [ ] Powtarzać pakiety aż wszystkie poprawne, zaobserwowane kombinacje będą obsłużone albo jawnie udokumentowane jako ograniczenia.

### 2. Nieudokumentowane rozszerzenia `8F /1–/7`

- [ ] Ustalić na podstawie źródeł i wektorów, czy rozszerzenia są ignorowane, nielegalne, czy mają inne zachowanie.
- [ ] Dodać fixture’y i implementację albo udokumentować źródłowo uzasadniony skip.
- [ ] Zweryfikować pełny plik `8F` i ponownie policzyć pełny korpus.

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
4. Gdy punkt nie zmienia korpusu (np. audyt albo ROM), wpisać **90,5% bez zmiany** albo nowszy potwierdzony wynik; nie szacować wzrostu.

## Historia zamkniętych punktów

| data | zamknięty punkt | commity | passed / 646 000 | pokrycie |
|---|---|---|---:|---:|
| 2026-10-25 | Ostatni pełny checkpoint przed tą listą: C7 `/1–/7` | plan `ffee9f8`; implementacja `d24a4d1`; dokumentacja `aeb0699` | 584 464 | 90,5% |

