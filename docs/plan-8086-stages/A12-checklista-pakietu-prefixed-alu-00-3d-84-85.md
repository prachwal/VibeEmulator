# A12 — checklista segment-prefiksowanego ALU `00–3D, 84/85`

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Aktualizuj checkbox i procent po każdym ukończonym punkcie. Procent pakietu jest osobny od udziału `passed` w korpusie.

## Wskaźniki bazowe

- Korpus przed pakietem: **92,1%** — 595 207/646 000 passed, 50 793 skipped, zero mismatchy/timeoutów/parse errors.
- Inwentarz źródłowy: 49 782 segment-prefiksowanych rekordów z 60 plików opcode; pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.
- Plan przed kodem: [A12-prefixed-alu-00-3d-84-85-plan.md](A12-prefixed-alu-00-3d-84-85-plan.md).
- Pakiet: **1/10 (10%)**.

## Testy i kryteria

- [x] Inwentarz, plan i checklista zapisane przed zmianą kodu osobnym commitem `d790ce0`. Korpus: 92,1%.
- [ ] Dodać 200 fixture’ów: po jednym dla każdego z 50 opkodów i każdego z ES/CS/SS/DS; lokalny test 200/200. Korpus bez pełnego przebiegu: 92,1%.
- [ ] Potwierdzić na źródłach profiles fetch T1–T4, segment danych, BHE, kolejkę, IP, flagi i wyniki dla każdej formy; bez zmian oczekiwań testów. Korpus: 92,1%.
- [ ] Dodać dekodowanie segment-prefiksu i przesunięcie operandów/immediate; wszystkie fixture’y przechodzą. Korpus: 92,1%.
- [ ] Poprawnie obsłużyć rejestrowe ModR/M i TEST/CMP read-only; rozszerzyć testy fixture’ów. Korpus: 92,1%.
- [ ] Poprawnie obsłużyć pamięciowe ALU, segment override i read-modify-write; rozszerzyć testy fixture’ów. Korpus: 92,1%.
- [ ] Pełny filtr prefiksowany: 49 782/49 782; zero skipów/mismatchy/timeoutów/parse errors.
- [ ] Pełne 60 plików źródłowych: 120 000/120 000 bez regresji; po jednym przebiegu korpus: oczekiwane 644 989/646 000 (99,8%), 1 011 skipów i zero błędów.
- [ ] Bramki jakości, walidator manifestu, macierz/README/raport/checklisty/stany A12 zielone i zsynchronizowane.
- [ ] Osobne commity implementacji i dokumentacji; hashe zapisane w raporcie, checklista zamknięta.

## Historia aktualizacji

| data | ukończony punkt | dowód | passed / 646 000 | pokrycie | postęp pakietu |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Inwentarz i plan przed kodem | pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; 49 782 rekordów / 60 opcode files; plan `d790ce0` | 595 207 | 92,1% | 10% (1/10) |

## Zasada aktualizacji

Po każdym punkcie odśwież pole Pakiet, zaznacz checkbox, dopisz dowód w historii i wpisz najnowszy `passed / 646 000`. Procent korpusu zmienia się wyłącznie po kompletnym pełnym raporcie; inne punkty zachowują ostatni potwierdzony wynik.
