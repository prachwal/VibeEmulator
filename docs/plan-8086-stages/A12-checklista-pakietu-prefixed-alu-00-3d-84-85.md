# A12 — checklista segment-prefiksowanego ALU `00–3D, 84/85`

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Aktualizuj checkbox i procent po każdym ukończonym punkcie. Procent pakietu jest osobny od udziału `passed` w korpusie.

## Wskaźniki bazowe

- Korpus przed pakietem: **92,1%** — 595 207/646 000 passed, 50 793 skipped, zero mismatchy/timeoutów/parse errors.
- Inwentarz źródłowy: 49 782 segment-prefiksowanych rekordów z 50 plików opcode; pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.
- Plan przed kodem: [A12-prefixed-alu-00-3d-84-85-plan.md](A12-prefixed-alu-00-3d-84-85-plan.md).
- Pakiet: **11/11 (100%)**. Commity: plan `d790ce0`, implementacja `cab1fd2`, dokumentacja `fc9af2e`.
- Osobny plik kontrolny dla tego pakietu; aktualizuj go po każdym zamkniętym punkcie, wraz z dowodem i procentem.
- Pełny filtr ALU segment-prefiksowanego: **49 782/49 782**, 0 skipów, 0 mismatchy, 0 timeoutów, 0 błędów parsowania. Pełny korpus: **644 989/646 000 (99,8%)**, 1 011 skipów wyłącznie dla prefiksowanego `CALL FAR 9A`, zero mismatchy/timeoutów/parse errors. Raport: [A12-prefixed-alu-00-3d-84-85-report.json](A12-prefixed-alu-00-3d-84-85-report.json).

## Testy i kryteria

- [x] Inwentarz, plan i checklista zapisane przed zmianą kodu osobnym commitem `d790ce0`. Korpus: 92,1%.
- [x] Dodać 200 fixture’ów: po jednym dla każdego z 50 opkodów i każdego z ES/CS/SS/DS; lokalny test 200/200. Korpus bez pełnego przebiegu: 92,1%.
- [x] Potwierdzić na źródłach profiles fetch T1–T4, segment danych, BHE, kolejkę, IP, flagi i wyniki dla każdej formy; bez zmian oczekiwań testów. Korpus: 92,1%.
- [x] Dodać dekodowanie segment-prefiksu i przesunięcie operandów/immediate; wszystkie fixture’y przechodzą. Korpus: 92,1%.
- [x] Poprawnie obsłużyć rejestrowe ModR/M i TEST/CMP read-only; rozszerzyć testy fixture’ów. Korpus: 92,1%.
- [x] Poprawnie obsłużyć pamięciowe ALU, segment override i read-modify-write; rozszerzyć testy fixture’ów. Korpus: 92,1%.
- [x] Pełny filtr prefiksowany: 49 782/49 782; zero skipów/mismatchy/timeoutów/parse errors.
- [x] Pełne 50 plików źródłowych: 100 000/100 000 bez regresji. Korpus pozostaje do osobnego przebiegu.
- [x] Pełny korpus: 644 989/646 000 (99,8%), 1 011 skipów wyłącznie dla prefiksowanego `CALL FAR 9A`; zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości zielone: build Debug/Release 0 ostrzeżeń/błędów; testy Release 23 169/23 169; format verify; OpcodeInfo 512/512; manifest 2 042/36 389; `git diff --check`. Macierz ISA, README, raport i checklisty zsynchronizowane.
- [x] Osobne commity: plan `d790ce0`, implementacja `cab1fd2`, dokumentacja `fc9af2e`; hashe zapisane w raporcie.

## Historia aktualizacji

| data | ukończony punkt | dowód | passed / 646 000 | pokrycie | postęp pakietu |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Inwentarz i plan przed kodem | pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; 49 782 rekordów / 50 opcode files; plan `d790ce0` | 595 207 | 92,1% | 9% (1/11) |
| 2026-10-25 | 200 fixture’ów lokalnych | 50 opkodów × 4 prefiksy; test xUnit 200/200; bez zmiany oczekiwanych danych | 595 207 | 92,1% | 18% (2/11) |
| 2026-10-25 | Profile cykli, fetch i kolejki | Pełne przypięte rekordy ALU; porównanie każdego T-state, stanu rejestrów, RAM i kolejki; granica IP poprawiona na podstawie trzech rekordów | 595 207 | 92,1% | 27% (3/11) |
| 2026-10-25 | Prefiks i dekodowanie operandów | 49 782 rekordy segment-prefiksowane; dekodowanie ModR/M i immediate zgodne w pełnym filtrze | 595 207 | 92,1% | 36% (4/11) |
| 2026-10-25 | Formy rejestrowe, TEST i CMP | Rejestrowe ModR/M i formy read-only sprawdzone w pełnym filtrze | 595 207 | 92,1% | 45% (5/11) |
| 2026-10-25 | Pamięciowe ALU i read-modify-write | Segment override, odczyt/zapis, BHE i RAM sprawdzone w pełnym filtrze | 595 207 | 92,1% | 55% (6/11) |
| 2026-10-25 | Pełny filtr segment-prefiksowany | 49 782/49 782; 0 skipped, 0 mismatched, 0 timeouts, 0 parse errors; raport `/tmp/a12-prefixed-alu/prefixed-report-2.json` | 595 207 | 92,1% | 64% (7/11) |
| 2026-10-25 | Regresja pełnych plików ALU | 50/50 plików źródłowych, 100 000/100 000 rekordów; 0 skipped/mismatched/timeouts/parse errors; raport `/tmp/a12-prefixed-alu/all-report.json` | 595 207 | 92,1% | 73% (8/11) |
| 2026-10-25 | Pełny korpus | 644 989/646 000; 1 011 jawnych skipów 9A, zero błędów; raport `A12-prefixed-alu-00-3d-84-85-report.json` | 644 989 | 99,8% | 82% (9/11) |
| 2026-10-25 | Bramki jakości i synchronizacja dokumentów | Debug/Release 0/0; Release 23 169/23 169; format verify; OpcodeInfo 512/512; manifest 2 042/36 389; `git diff --check`; README i macierz ISA | 644 989 | 99,8% | 91% (10/11) |
| 2026-10-25 | Osobny commit implementacji | `cab1fd2`; plan `d790ce0` i dokumentacja pozostają osobnymi commitami | 644 989 | 99,8% | 91% (10/11) |
| 2026-10-25 | Osobny commit dokumentacji i zamknięcie pakietu | `fc9af2e`; raport zapisuje hashe planu, implementacji i dokumentacji | 644 989 | 99,8% | 100% (11/11) |

## Zasada aktualizacji

Po każdym punkcie odśwież pole Pakiet, zaznacz checkbox, dopisz dowód w historii i wpisz najnowszy `passed / 646 000`. Procent korpusu zmienia się wyłącznie po kompletnym pełnym raporcie; inne punkty zachowują ostatni potwierdzony wynik.
