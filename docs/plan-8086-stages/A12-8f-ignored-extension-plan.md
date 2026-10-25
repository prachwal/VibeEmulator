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
- Aktualna baza przed pakietem: 584 464/646 000 passed (90,5%), 61 536 skipped, zero mismatchy, timeoutów i błędów
  parsowania. Z raportu C7 wynika, że 1 762 skipy są przypisane do `8F`.

## Zakres i decyzja implementacyjna

1. Dla `/1–/7` porównać końcowe rejestry, RAM, kolejkę oraz każdy T-state z aktualnym wykonaniem `8F /0` na tych samych
   klasach adresowania i prefiksach. Szczególnie sprawdzić, czy cykle/efekty są zgodne z `/0`, a nie tylko nazwy rekordów.
2. Jeśli pełne wektory potwierdzą aliasowanie pola `/0`, dopuścić `/1–/7` na ścieżce `POP r/m16` i użyć indeksu profilu
   z `ModR/M.reg` wyzerowanym. Zachować bajt ModR/M w kolejce, odczyty pamięci, zapis celu, kolejkę oraz wszystkie T-state.
3. Jeśli choć jedna forma różni się od `/0`, nie wymuszać aliasu. Odrębnie sklasyfikować różnice; wdrożyć tylko semantykę,
   którą pełne pomiary pozwolą odtworzyć, a nierozstrzygnięte kodowania zachować jako jawne skipy.
4. Dodać fixture’y ze wszystkich 56 grup; wektorów nie modyfikować. Wąskie fixture’y stosować do diagnozy; pełny plik
   i pełny korpus uruchomić po ustabilizowaniu pakietu, po jednym razie.
5. Uzgodnić README, manifest, macierz ISA, powód skipów, raport JSON, tę checklistę i stan wznowienia.

## Plan pracy

- [x] Sprawdzić pin, status metadata, bajty/dekodowanie i rozkład `/1–/7` według prefiksu oraz `mod`.
- [ ] Dodać pierwsze maksymalnie 20 fixture’ów ze wszystkich 140 grup oraz manifest wiążący rekordy z pinem.
- [ ] Zmierzyć i porównać zachowanie `/1–/7` z `/0`; rozstrzygnąć, które kodowania można wiernie obsłużyć.
- [ ] Wdrożyć ustaloną semantykę i profile T-state albo zapisać uzasadnioną klasyfikację nierozstrzygniętych form.
- [ ] Potwierdzić fixture’y i pełny `8F.json.gz`: 2 000 rekordów, bez mismatchy, timeoutów i błędów parsowania; skipy raportować jawnie.
- [ ] Uruchomić pełny korpus 646 000 i przeliczyć passed/skipped/% tylko z tego przebiegu.
- [ ] Zaliczyć build Debug/Release, testy Release, format, OpcodeInfo, walidator manifestu i `git diff --check`.
- [ ] Zaktualizować checklisty, macierz ISA, README, raport oraz stan wznowienia.
- [ ] Zapisać implementację i dokumentację osobno po tym planie; nie wypychać.

## Testy i kryteria

- [ ] Fixture’y: 1 461/1 461 (140 grup, do 20 na grupę), lub jawnie wyjaśniony brak rekordów w grupie po ponownej kontroli źródła.
- [ ] Pełny `8F`: wszystkie zaimplementowane formy passed; jawne skipped tylko dla zachowania nieodtwarzalnego lub
  wyłączonego zakresem; zero mismatchy, timeoutów i błędów parsowania.
- [ ] Pełny korpus: raportuje 646 000 rekordów, passed, skipped, mismatchy, timeouty, parse errors i procent.
- [ ] Buildy Debug i Release bez ostrzeżeń/błędów; testy Release, formatter, OpcodeInfo, manifest oraz diff check zielone.
- [ ] Osobne commity planu, implementacji i checkpointu dokumentacji.

## Historia

- 2026-10-25: inwentaryzacja zakończona. Wynik przed implementacją: 584 464/646 000 (90,5%); plan i dokładne rozkłady
  zapisane przed zmianą kodu. Pełne grupowanie wykazało 140 grup i próbkę 1 461 rekordów (pierwotne oszacowanie
  56 grup/1 120 rekordów było niepoprawne i skorygowano je przed implementacją).

