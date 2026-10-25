# A12 — nieudokumentowane rozszerzenia `8F /1–/7`

## Cel i branch

Rozstrzygnąć zachowanie pól `ModR/M.reg` `/1–/7` dla instrukcji `8F` na podstawie przypiętych pomiarów P80C86A-2,
a jeśli wektory potwierdzą wspólne zachowanie z `/0`, odblokować je bez zmiany semantyki `POP r/m16`, cykli ani
pozostałych form. To są kodowania oznaczone przez upstream jako `undefined`, więc ewentualne aliasowanie będzie opisane
jako zachowanie zmierzonego układu, nie jako oficjalna forma ISA. Plan, implementacja i checkpoint dokumentacji będą
osobnymi commitami na branchu `A12-8086-pelna-weryfikacja`.

## Źródło i inwentaryzacja

- Źródło: `SingleStepTests/8086`, pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, `v1/8F.json.gz` — 2 000 rekordów.
- README źródła opisuje pomiary z fizycznego Intel P80C86A-2 782003 J20 przez ArduinoX86/Intel 8288; `metadata.json`
  w pinie klasyfikuje `8F /0` jako `normal`, a `/1–/7` jako `undefined`. README nie rozszerza stwierdzenia o ignorowanym
  polu `reg` dla C6/C7 na opcode 8F. Nie zakładać więc aliasowania na podstawie C6/C7.
- `/0` ma 238 rekordów (116 bez prefiksu, 122 z segment-prefiksem) i jest już obsłużone.
- Rozszerzenia do zbadania: `/1` 250 (128 bez prefiksu, 122 z prefiksem), `/2` 253 (116/137), `/3` 258 (134/124),
  `/4` 240 (126/114), `/5` 239 (112/127), `/6` 260 (121/139), `/7` 262 (124/138). Razem 1 762, z czego 861
  bez prefiksu i 901 z prefiksem.
- Każde rozszerzenie ma wszystkie 20 grup prefiksu (brak/ES/CS/SS/DS) × `ModR/M.mod` (00/01/10/11): 140 grup
  niepustych, każda z 3–39 rekordami. Próbka deterministyczna: pierwsze maksymalnie 20 z każdej grupy, razem
  1 461 fixture’ów.
- Inwentarz lokalny: `tests/VibeEmulator.Intel8086.Tests/Vectors/source-inventory.json`; statusy upstream:
  `tests/VibeEmulator.Intel8086.Tests/Vectors/metadata.json`. Pełne archiwum źródłowe jest poza repo w
  `/tmp/vibe-8086-a12-complete/v1/8F.json.gz`; lokalne próbki pozostają niezmienionymi rekordami źródła.
- Baza przed pakietem: 584 464/646 000 passed (90,5%), 61 536 skipped. Po wdrożeniu wszystkie 1 762 rozszerzenia `8F` przeszły; najnowszy pełny wynik to 586 226/646 000 (90,7%), 59 774 skipy segment-prefiksowe, zero mismatchy, timeoutów i błędów parsowania.

## Zakres i decyzja implementacyjna

1. Dla `/1–/7` porównać końcowe rejestry, RAM, kolejkę oraz każdy T-state z aktualnym wykonaniem `8F /0` na tych samych
   klasach adresowania i prefiksach. Szczególnie sprawdzić, czy cykle/efekty są zgodne z `/0`, a nie tylko nazwy rekordów.
2. Jeśli pełne wektory potwierdzą aliasowanie pola `/0`, dopuścić `/1–/7` na ścieżce `POP r/m16` i użyć indeksu profilu
   z `ModR/M.reg` wyzerowanym. Zachować bajt ModR/M w kolejce, odczyty pamięci, zapis celu, kolejkę oraz wszystkie T-state.
3. Jeśli choć jedna forma różni się od `/0`, nie wymuszać aliasu. Odrębnie sklasyfikować różnice; wdrożyć tylko semantykę,
   którą pełne pomiary pozwolą odtworzyć, a nierozstrzygnięte kodowania zachować jako jawne skipy.
4. Dodać fixture’y ze wszystkich 140 grup; wektorów nie modyfikować. Wąskie fixture’y stosować do diagnozy; pełny plik
   i pełny korpus uruchomić po ustabilizowaniu pakietu, po jednym razie.
5. Uzgodnić README, manifest, macierz ISA, powód skipów, raport JSON, tę checklistę i stan wznowienia.

## Plan pracy

- [x] Sprawdzić pin, status metadata, bajty/dekodowanie i rozkład `/1–/7` według prefiksu oraz `mod`.
- [x] Dodać pierwsze maksymalnie 20 fixture’ów ze wszystkich 140 grup oraz manifest wiążący rekordy z pinem: 1 461/1 461.
- [x] Porównać pełne ślady `/1–/7` z `/0`: wszystkie 1 762 rekordy zachowują się jak `POP r/m16` na zmierzonym P80C86A-2.
- [x] Wdrożyć aliasowanie i profile: 261 nowych profili, 178 współdzielonych, 0 konfliktów; metadata upstream nadal oznacza rozszerzenia jako undefined.
- [x] Potwierdzić fixture’y 1 461/1 461 i pełny `8F.json.gz`: 2 000/2 000, bez skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Uruchomić pełny korpus: 586 226/646 000 (90,7%), 59 774 jawne skipy segment-prefiksowe, zero pozostałych błędów.
- [x] Zaliczyć bramki: Debug/Release 0 ostrzeżeń i błędów; testy Release 22 244/22 244; formatter; OpcodeInfo 512/512; manifest 1 962/34 549; diff check.
- [x] Zaktualizować checklisty, macierz ISA, README, raport oraz stan wznowienia.
- [x] Zapisać osobny checkpoint dokumentacji `7a7d82d` po planie `3fecba9` i implementacji `c6d1b4f`; hash checkpointu dopisano do raportu.

## Testy i kryteria

- [x] Fixture’y: 1 461/1 461 (140 grup, do 20 na grupę).
- [x] Pełny `8F`: 2 000/2 000 passed; zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny korpus: 646 000 rekordów; 586 226 passed (90,7%), 59 774 skipped, 0 mismatchy/timeoutów/parse errors.
- [x] Buildy Debug i Release bez ostrzeżeń/błędów; testy Release, formatter, OpcodeInfo, manifest oraz diff check zielone.
- [x] Osobne commity: plan `3fecba9`, korekta inwentarza `a2d5203`, implementacja `c6d1b4f`, checkpoint dokumentacji `7a7d82d`.

## Historia

- 2026-10-25: checkpoint dokumentacji `7a7d82d`; checklisty zamknięte 6/6 (100%).

- 2026-10-25: inwentaryzacja 1 762 rozszerzeń w 140 grupach; próbka 1 461 rekordów. Pierwotne oszacowanie 56 grup/1 120 rekordów skorygowano przed kodem. Plan `3fecba9`, korekta `a2d5203`.
- 2026-10-25: pełne ślady potwierdziły aliasowanie wszystkich `/1–/7` na P80C86A-2; implementacja `c6d1b4f`, fixture’y 1 461/1 461, pełne `8F` 2 000/2 000, korpus 586 226/646 000 (90,7%), 59 774 skipy, zero rozbieżności. Raport: [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json).

