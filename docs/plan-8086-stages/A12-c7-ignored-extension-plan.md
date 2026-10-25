# A12 — nieudokumentowane rozszerzenia `C7 /1–/7`

## Cel i gałąź

Otworzyć aliasy `MOV r/m16,imm16` (`C7`) dla wszystkich wartości pola `ModR/M.reg`. Formalną instrukcją jest `C7 /0`; pinned README SingleStepTests opisuje `C6/C7 /1–/7` jako niezdefiniowane w ISA, ale ignorowane przez testowany układ. Plan, implementacja i checkpoint będą osobnymi commitami na `A12-8086-pelna-weryfikacja`, po C6.

## Źródła i rozmiar

- SingleStepTests/8086, commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, `v1/C7.json.gz`; pinned README stwierdza, że przy `C6/C7` pole `ModR/M.reg != 0` jest ignorowane: <https://github.com/SingleStepTests/8086/blob/e71c68d215a6bb8c356bd4cb3842de3bef345ca9/README.md>.
- Intel 8086 Family User's Manual dokumentuje `C7 /0` jako `MOV r/m16,imm16`; zachowanie `/1–/7` traktujemy jako nieudokumentowany alias z przypiętych pomiarów, nie oficjalną formę ISA.
- Pełne `C7` ma 2 000 rekordów: `/0` 270, już obsłużonych; `/1–/7` 1 730, obecnie pomijanych (864 bez prefiksu, 866 z prefiksem segmentowym). Jest 140 niepustych grup rozszerzenie/prefiks/`ModR/M.mod`, zakres grup od 1 do 47 rekordów.
- Oczekiwane pokrycie po odblokowaniu: 584 464 / 646 000 (około 90,5%), 61 536 skipów, zero mismatchy/timeoutów/parse errors.

## Zakres

1. Dla `C7` ignorować wyłącznie pole `ModR/M.reg`, zachowując istniejącą ścieżkę `/0`: 16-bit displacement/immediate, zapis słowa, adresowanie, segment override, kolejka, BHE i T-state.
2. Nie zmieniać `C6`, ani semantyki oficjalnego `/0`; utrzymać klasyfikację `/1–/7` jako nieudokumentowanego zachowania konkretnego źródła.
3. Dodać fixture’y z każdej niepustej grupy. Fixture test i krótka diagnostyka służą do szybkiej iteracji; pełne `C7` i korpus uruchomić po ustabilizowaniu, po jednym razie.
4. Sprawdzić zawijanie `CS:IP` i parzyste/nieparzyste adresy słowa. C6 ujawniło, że filtrowanie nowych aliasów może odsłonić istniejące braki kolejki w przypadkach granicznych.
5. Uzgodnić macierz ISA, skipi, README, raport i checklisty.

## Plan pracy

- [x] Zapisać plan z pinned źródłem, formalnym statusem i rozkładem 1 730 rekordów.
- [ ] Przygotować deterministyczne fixture’y dla 140/140 grup i pojedynczy test xUnit.
- [ ] Wdrożyć aliasy `/1–/7` na wspólnej ścieżce `/0` bez regresji.
- [ ] Zaliczyć fixture’y, pełne źródło C7 (2 000/2 000) i cały korpus po stabilizacji.
- [ ] Zaliczyć build Debug/Release, pełne testy Release, format verify, OpcodeInfo, walidator manifestu i diff check.
- [ ] Zatwierdzić implementację osobno, zaktualizować checklistę po każdym punkcie i wykonać osobny checkpoint dokumentacji.

## Testy i kryteria

- [ ] Fixture’y obejmują 140/140 grup; wszystkie lokalne rekordy przechodzą.
- [ ] Pełny C7 przechodzi 2 000/2 000, w tym 1 730 `/1–/7`, bez skipów/mismatchy/timeoutów/parse errors.
- [ ] Pełny korpus: 584 464 passed, 61 536 skipped; zero mismatchy/timeoutów/parse errors.
- [ ] Debug/Release: 0 ostrzeżeń i błędów; pełne testy Release bez pominięć.
- [ ] Formatter verify, OpcodeInfo, walidator manifestu oraz `git diff --check` przechodzą.
- [ ] Plan, implementacja i checkpoint są oddzielnymi commitami; raport i checklisty zawierają hashe.

## Definicja ukończenia

Wszystkie 1 730 rekordów `C7 /1–/7` przechodzą pełne porównanie na wspólnej implementacji `C7 /0`; 270 rekordów `/0` pozostaje zgodnych, a korpus, status ISA, checklisty i osobne commity są aktualne.
