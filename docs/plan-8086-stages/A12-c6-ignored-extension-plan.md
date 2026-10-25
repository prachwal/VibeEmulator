# A12 — nieudokumentowane rozszerzenia `C6 /1–/7`

## Cel i gałąź

Rozszerzyć obsługę `MOV r/m8,imm8` (`C6`) na wszystkie wartości pola `ModR/M.reg`. Zgodnie z przypiętym repozytorium SingleStepTests pole `reg` jest formalnie niezdefiniowane, ale na testowanym 8086 jest ignorowane; nie jest to semantyka nowego opcode’u. Plan, kod i checkpoint będą osobnymi commitami na `A12-8086-pelna-weryfikacja`, po zamkniętym pakiecie SETMO.

## Źródła i zakres

- SingleStepTests/8086, commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, plik `v1/C6.json.gz`; README przypięte do tego commitu opisuje `C6/C7` z `ModR/M.reg != 0` jako oficjalnie undefined i pole ignorowane: <https://github.com/SingleStepTests/8086/blob/e71c68d215a6bb8c356bd4cb3842de3bef345ca9/README.md>.
- Intel 8086 Family User's Manual dokumentuje `C6 /0` jako `MOV r/m8,imm8`; inne wartości `/1–/7` nie mają zdefiniowanej formy instrukcji. Zestaw wektorów dostarcza zachowanie układu dla nieoficjalnych kodowań, nie zmieniając statusu ISA.
- Pełny plik zawiera 2 000 rekordów: `/0` ma 236 rekordów, już przechodzących; `/1–/7` ma 1 764 rekordy, obecnie pomijane. Wśród tych 1 764: 861 bez prefiksu i 903 z prefiksem segmentowym. Każda z 7 wartości `/1–/7` występuje we wszystkich 5 wariantach prefiksu (brak, ES, CS, SS, DS) i 4 trybach `ModR/M.mod` — 140 niepustych grup.
- Oczekiwany przyrost po dokładnym odblokowaniu tych rekordów: 1 764 passed; pełny korpus 582 734 / 646 000 (około 90,2%), bez mismatchy, timeoutów ani błędów parsowania.

## Zakres implementacji

1. Usunąć ograniczenie, które odrzuca `C6` wyłącznie z powodu pola `ModR/M.reg` różnego od zera. Zachować wspólną ścieżkę `/0`: dekodowanie adresu, pobranie immediate, wybór segmentu, zapis, kolejkę i cykle.
2. Nie zmieniać `C7`, pozostałych opcode’ów, zachowania `/0` ani formalnej klasyfikacji ISA. Macierz opisze `/1–/7` jako nieudokumentowane aliasy potwierdzone wektorami, nie jako oficjalną instrukcję.
3. Dodać deterministyczne lokalne fixture’y z każdej niepustej kombinacji rozszerzenie/prefiks/ModR/M.mod. Do diagnozy korzystać z fixture’ów i krótkich filtrów; pełny plik C6 i cały korpus uruchomić po ustabilizowaniu zachowania, po jednym razie.
4. Uzgodnić checklisty, macierz ISA, `Vectors/README.md`, raport źródłowy i wyjaśnienie pozostałych skipów. Usunąć `C6 /1–/7` z aktualnych kategorii skipów.

## Plan pracy

- [x] Zapisać plan i potwierdzić przypięty pin, formalny status oraz rozkład 1 764 rekordów.
- [x] Przygotować lokalne fixture’y obejmujące 140/140 niepustych grup i jeden test xUnit. Wynik: 1 463 rekordy, 1/1 test passed.
- [x] Wdrożyć ignorowanie `ModR/M.reg` dla C6 bez zmian semantyki `/0`; naprawić odkryte zawijanie pobierania przez `CS:FFFF`. Commit `d16021e`.
- [x] Uruchomić fixture’y oraz kontrolny pełny plik C6: 1 463/1 463 lokalnie i 2 000/2 000 pełnego źródła.
- [x] Uruchomić pełny korpus 646 000 po stabilizacji: 582 734 passed, 63 266 skipped, 0 mismatchy/timeoutów/parse errors (90,2%).
- [x] Przejść build Debug/Release, pełne testy Release, formatter, OpcodeInfo, walidator manifestu i `git diff --check`: buildy 0/0; testy 22 242/22 242 (Intel8086 19 442/19 442); OpcodeInfo 512/512; manifest 1 682/31 662.
- [x] Osobne commity planu `c91a464`, implementacji `d16021e` i dokumentacji `02cacb4`; raport i checklisty zapisane.

## Testy i kryteria

- [x] Fixture’y z 140/140 grup przechodzą w lokalnym teście xUnit (1 463/1 463).
- [x] Pełny plik C6 kontrolnie: 2 000/2 000, w tym 1 764 `/1–/7`; zero skipów/mismatchy/timeoutów/parse errors.
- [x] Kontrolny pełny C6: 2 000/2 000, bez regresji `/0`; pełny korpus końcowy 646 000/646 000.
- [x] Pełny korpus: 582 734/646 000 passed, 63 266 skipped; 0 mismatchy/timeoutów/parse errors.
- [x] Buildy Debug i Release: 0 ostrzeżeń, 0 błędów; pełne testy Release 22 242/22 242.
- [x] Formatter verify, OpcodeInfo 512/512, walidator manifestu 1 682/31 662 oraz diff check zielone.
- [x] Zapisać checkpoint dokumentacji osobno po planie `c91a464` i implementacji `d16021e`; commit `02cacb4`.

## Definicja ukończenia

Wszystkie 1 764 wektory `C6 /1–/7` przechodzą na wspólnej ścieżce `C6 /0`, bez regresji 236 legalnych wektorów `/0`; wynik całego korpusu i status nieudokumentowanych kodowań są zapisane, a plan, implementacja oraz checkpoint mają oddzielne commity.
