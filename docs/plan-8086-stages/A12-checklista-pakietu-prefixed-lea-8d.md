# A12 — checklista `LEA 8D` z prefiksem segmentowym

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Aktualizuj tę listę po każdym zakończonym punkcie. Procent pakietu i procent pokrycia korpusu to osobne wskaźniki.

## Wskaźniki początkowe

- Pokrycie pinu: **92,0%** — 594 203/646 000 passed, 51 797 skipped, 0 mismatchy/timeoutów/parse errors.
- Pakiet: **0/9 (0%)**; punkt planu będzie zamknięty po zapisaniu tego planu i checklisty w repo.
- Źródło: `SingleStepTests/8086` `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; `8D.json.gz`, 2 000 rekordów.
- Segment-prefiksowane rekordy: **1 004/1 004 pominięte** w ostatnim korpusie; grupy: ES 222, CS 271, SS 260, DS 251.
- Plan przed implementacją: [A12-prefixed-lea-8d-plan.md](A12-prefixed-lea-8d-plan.md).

## Punkty do dokończenia

- [ ] Zatwierdzić inwentaryzację i plan osobnym commitem przed zmianą kodu. Korpus: 92,0%.
- [ ] Dodać cztery fixture’y źródłowe i test offline (80/80); manifest bez ręcznych zmian wektorów. Po punkcie wpisać aktualny korpus.
- [ ] Odtworzyć cykle i zachowanie LEA/prefiksu ze wszystkich śladów grup źródłowych; zapisać dowody oraz minimalne przypadki diagnostyczne.
- [ ] Zaimplementować prefiksowane LEA i przejść wszystkie fixture’y; nie zmieniać zachowania bez prefiksu.
- [ ] Pełny filtr segment-prefiksowany 1 004/1 004; raport zero skipów/mismatchy/timeoutów/parse errors albo jawne wyjaśnienie ograniczenia.
- [ ] Pełny plik 8D 2 000/2 000, obejmujący również dotąd obsłużone 996 rekordów bez prefiksu.
- [ ] Pełny korpus po stabilizacji; wpisać liczby wszystkich pięciu kategorii i procent `passed / 646 000`.
- [ ] Końcowe bramki jakości i synchronizacja raportu, macierzy ISA, README oraz checklist.
- [ ] Osobny commit implementacji, osobny commit dokumentacji, hashe w raporcie; zamknąć checklistę.

## Historia aktualizacji

| data | zakończony punkt | dowód | passed / 646 000 | pokrycie korpusu | pakiet |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Źródło zinwentaryzowane i plan/checklista zapisane | 8D: 2 000 rekordów; segment-prefiksowane 1 004; plan wymaga osobnego commitu przed kodem | 594 203 | 92,0% | 0% (0/9) |

## Zasada aktualizacji

Po każdym odhaczeniu dopisz dowód do historii i ustaw aktualny licznik pakietu. Procent korpusu aktualizuj wyłącznie po pełnym raporcie 646 000 rekordów; przy punktach bez nowego przebiegu zachowaj 92,0% i oznacz wynik jako niezmieniony. Nigdy nie wliczaj skipped do passed.
