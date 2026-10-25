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
- [ ] Przygotować lokalne fixture’y obejmujące 140/140 niepustych grup i jeden test xUnit.
- [ ] Wdrożyć ignorowanie `ModR/M.reg` dla C6 bez zmian semantyki `/0`.
- [ ] Uruchomić test fixture’ów, następnie pełne 1 764 wektory `/1–/7` oraz kontrolny pełny plik C6 (2 000 rekordów).
- [ ] Uruchomić pełny korpus 646 000 raz po stabilizacji; oczekiwany wynik 582 734 passed, 63 266 skipped, 0 mismatchy/timeoutów/parse errors.
- [ ] Przejść build Debug/Release, pełne testy Release, `dotnet format ... --verify-no-changes`, OpcodeInfo, walidator manifestu i `git diff --check`.
- [ ] Zapisać osobny commit implementacji, uzupełnić raport/checklisty, a checkpoint dokumentacji zapisać oddzielnym commitem.

## Testy i kryteria

- [ ] Fixture’y z 140/140 grup przechodzą w lokalnym teście xUnit.
- [ ] Pełny filtr `/1–/7`: 1 764/1 764, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [ ] Kontrolny pełny C6: 2 000/2 000, bez regresji `/0`.
- [ ] Pełny korpus: wynik zgodny z rozkładem; 0 mismatchy, timeoutów i błędów parsowania.
- [ ] Buildy Debug i Release: 0 ostrzeżeń, 0 błędów; pełne testy Release zielone.
- [ ] Formatter verify, OpcodeInfo, walidator manifestu oraz diff check zielone.
- [ ] Commit planu poprzedza commit implementacji; checkpoint dokumentacji jest osobny i zawiera aktualne hashe.

## Definicja ukończenia

Wszystkie 1 764 wektory `C6 /1–/7` przechodzą na wspólnej ścieżce `C6 /0`, bez regresji 236 legalnych wektorów `/0`; wynik całego korpusu i status nieudokumentowanych kodowań są zapisane, a plan, implementacja oraz checkpoint mają oddzielne commity.
