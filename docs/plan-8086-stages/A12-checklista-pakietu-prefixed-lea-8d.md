# A12 — checklista `LEA 8D` z prefiksem segmentowym

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Aktualizuj tę listę po każdym zakończonym punkcie. Procent pakietu i procent pokrycia korpusu to osobne wskaźniki.

## Wskaźniki początkowe

- Pokrycie pinu: **92,0%** — 594 203/646 000 passed, 51 797 skipped, 0 mismatchy/timeoutów/parse errors.
- Pakiet: **9/10 (90,0%)**.
- Źródło: `SingleStepTests/8086` `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; `8D.json.gz`, 2 000 rekordów.
- Segment-prefiksowane rekordy: **1 004/1 004 pominięte** w ostatnim korpusie; grupy: ES 222, CS 271, SS 260, DS 251.
- Plan przed implementacją: [A12-prefixed-lea-8d-plan.md](A12-prefixed-lea-8d-plan.md).

## Punkty do dokończenia

- [x] Zatwierdzić inwentaryzację i plan osobnym commitem `b842f56` przed zmianą kodu. Korpus: 92,0%.
- [x] Dodać cztery fixture’y źródłowe, manifest i test offline; wszystkie 80/80 przechodzą. Korpus: 92,0% (bez nowego przebiegu).
- [x] Odtworzyć profile fetch T1 z 1 004 źródłowych śladów; rozdzielić zależności `mod/rm` i początkowej kolejki 5/6 bajtów; profile wynikają z pełnego pinu.
- [x] Zaimplementować prefiksowane LEA; fixture’y przechodzą 80/80, w tym wszystkie cztery segmenty.
- [x] Pełny filtr segment-prefiksowany 1 004/1 004: zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny plik 8D 2 000/2 000, w tym 996 rekordów bez prefiksu; połączony przebieg filtr+plik: 3 004/3 004 porównań, zero błędów.
- [x] Pełny korpus po stabilizacji: 595 207/646 000 passed, 50 793 skipped, 0 mismatchy/timeoutów/parse errors; +1 004 passed, pokrycie 92,1%. Raport `/tmp/a12-lea-8d-full-corpus.json`.
- [x] Końcowe bramki jakości i synchronizacja raportu, macierzy ISA, README oraz checklist. Buildy bez ostrzeżeń/błędów; Release 22 968/22 968; format verify, OpcodeInfo 512/512 i manifest 2 034/35 989 zielone.
- [x] Zatwierdzić implementację osobnym commitem `9b7cc06`; plan `b842f56` jest wcześniejszy i oddzielny.
- [ ] Zatwierdzić dokumentację osobnym commitem, wpisać jego hash do raportu i zamknąć checklistę.

## Historia aktualizacji

| data | zakończony punkt | dowód | passed / 646 000 | pokrycie korpusu | pakiet |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Plan i inwentaryzacja przed kodem | `b842f56`; źródło 8D ma 2 000 rekordów, w tym 1 004 prefiksowane | 594 203 | 92,0% | 10% (1/10) |
| 2026-10-25 | Fixture’y i test lokalny | 4 grupy; 80/80 testów, pliki źródłowe przypięte do SHA | 594 203 | 92,0% | 20% (2/10) |
| 2026-10-25 | Profile cykli z pełnego pinu | profile 48 kombinacji `mod/rm/queue`, źródło 1 004 śladów | 594 203 | 92,0% | 30% (3/10) |
| 2026-10-25 | Implementacja prefiksowanego LEA | fixture’y 80/80; kod w `9b7cc06` | 594 203 | 92,0% | 40% (4/10) |
| 2026-10-25 | Pełny filtr segment-prefiksowany | 1 004/1 004; zero skipów/mismatchy/timeoutów/parse errors | 594 203 | 92,0% | 50% (5/10) |
| 2026-10-25 | Pełny plik źródłowy | 8D 2 000/2 000; 3 004/3 004 porównań w przebiegu łączonym | 594 203 | 92,0% | 60% (6/10) |
| 2026-10-25 | Pełny korpus 646 000 | 595 207 passed, 50 793 skipped, 0 błędów; raport `/tmp/a12-lea-8d-full-corpus.json` | 595 207 | 92,1% | 70% (7/10) |
| 2026-10-25 | Zbiorcze bramki i synchronizacja dokumentacji | Debug/Release bez ostrzeżeń/błędów; Release 22 968/22 968; format verify, OpcodeInfo, manifest i dokumenty zsynchronizowane | 595 207 | 92,1% | 80% (8/10) |
| 2026-10-25 | Osobny commit implementacji | `9b7cc06`; plan `b842f56` pozostaje osobny | 595 207 | 92,1% | 90% (9/10) |

## Zasada aktualizacji

Po każdym odhaczeniu dopisz dowód do historii i ustaw aktualny licznik pakietu. Procent korpusu aktualizuj wyłącznie po pełnym raporcie 646 000 rekordów; przy punktach bez nowego przebiegu zachowaj 92,1% i oznacz wynik jako niezmieniony. Nigdy nie wliczaj skipped do passed.
