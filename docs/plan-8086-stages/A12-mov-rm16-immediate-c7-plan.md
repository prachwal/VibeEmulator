# A12 — `MOV r/m16,imm16` (`C7 /0`)

## Cel

Dodać cycle-exact wykonanie `C7 /0`: dekodowanie ModR/M i displacementu, pobranie natychmiastowej wartości słowa z kolejki, zapis do rejestru lub pamięci, segment override oraz wszystkie T-state magistrali i kolejki prefetch. Plan i implementacja mają pozostać osobnymi commitami na branchu `A12-8086-pelna-weryfikacja`.

## Źródła i inwentaryzacja

- Przypięte źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, plik `v1/C7.json.gz` (2 000 rekordów).
- Rozszerzenie ModR/M `/0` ma 270 rekordów: 124 bez prefiksu i 146 segment-prefiksowanych. Formy ModR/M: `mod=00` 72, `mod=01` 62, `mod=10` 70, `mod=11` 66.
- Liczby `/0` per prefiks i `mod`: bez prefiksu 32/31/35/26; ES 8/12/11/8; CS 11/7/7/13; SS 9/6/10/14; DS 12/6/7/5 (`mod=00/01/10/11`). Każda z 20 grup ma od 5 do 35 rekordów; w fixture’ach zachować wszystkie rekordy, maksymalnie pierwsze 20 z grupy.
- Pozostałe 1 730 rekordów używają rozszerzeń `/1–/7`. Nie przypisywać im semantyki `MOV`; zostawić je jawnie pominięte do odrębnego rozstrzygnięcia kodowań.
- Zachowanie i kolejność cykli ustalać z rekordów SST. Dokumentację CPU w `docs/cpu/sources.md` wykorzystywać jako kontekst, a nie zastępować nią brakujących śladów magistrali.

## Zakres

1. Obsłużyć tylko `C7 /0`, wszystkie `mod=00/01/10/11` i `r/m=0..7`.
2. Dla `mod=11` zapisać immediate word do wskazanego rejestru; dla pamięci obliczyć adres zgodnie ze wspólnym dekoderem EA, w tym BP-domyślne SS i displacement.
3. Pobierać immediate word we właściwej kolejności względem opcode/ModR/M/displacement; zachować dokładny wzrost IP i flagi/rejestry niezwiązane z celem.
4. Dla celu pamięciowego obsłużyć domyślny segment i wszystkie cztery segment override; natychmiastowa wartość nie podlega odczytowi z pamięci operandu.
5. Odtworzyć aligned/unaligned zapis słowa, BHE, kolejkę, CODE prefetch i idle z pinowanych T-state SST.
6. Nie obejmować `C6` ani rozszerzeń `/1–/7` kodu `C7`; skipy muszą pozostać jawne.

## Plan realizacji

### P0 — plan i źródło

- [x] Sprawdzić branch, wcześniejszy czysty checkpoint i przypięte źródło.
- [x] Sklasyfikować 2 000 rekordów `C7`, policzyć `/0`, prefiksy i formy ModR/M.
- [x] Zapisać plan w osobnym commicie przed implementacją.

### P1 — fixture’y i implementacja

- [ ] Dodać niezmienione upstream rekordy `/0` dla każdej kombinacji pięciu prefiksów (brak/ES/CS/SS/DS) i czterech mod, maksymalnie pierwsze 20 z grupy; zapisać testNumbers oraz hashe w manifeście.
- [ ] Dodać izolowany stan transferu `C7 /0`; poprawnie rozdzielić `/0` od `/1–/7`.
- [ ] Zdekodować ModR/M, displacement i dwubajtowy immediate z kolejki bez side effectów odczytu celu.
- [ ] Zaimplementować zapis rejestrowy `mod=11` i transfer pamięciowy dla `mod=00/01/10`, zachowując EA helpery i wybrany segment.
- [ ] Odtworzyć z wektorów fazy zapisu, CODE fetch, prefetch, zużycie kolejki, IP i finalny stan instrukcji.

### P2 — weryfikacja źródłowa

- [ ] Uruchomić lokalne fixture’y i diagnozować mismatchy na hashu/T-state, nie rozszerzać tolerancji porównania.
- [ ] Uruchomić pełny filtr wszystkich 270 rekordów `/0`; oczekiwane 270 passed, zero skipów, mismatchy, timeoutów i parse errors.
- [ ] Uruchomić cały `C7.json.gz`; rozszerzenia `/1–/7` mają pozostać jawnie skipped, bez mismatchy.
- [ ] Sprawdzić przypadki rejestrowe, displacement, BP/SS, override celu, aligned/unaligned EA i immediate, granice segmentu oraz pokrycie `mod=11`.

### P3 — korpus i dokumentacja

- [ ] Uruchomić jeden pełny korpus 646 000 rekordów z manifestem checkpointu po `8F`.
- [ ] Zaktualizować tabelę skipów, procent `passed / 646 000`, macierz ISA, README wektorów, raport i checklistę A12.

### P4 — Testy i kryteria

- [ ] `dotnet build VibeEmulator.slnx -c Debug --nologo` — bez ostrzeżeń i błędów.
- [ ] `dotnet build VibeEmulator.slnx -c Release --nologo` — bez ostrzeżeń i błędów.
- [ ] `dotnet test VibeEmulator.slnx -c Release --nologo` — wszystkie testy zielone.
- [ ] `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn` — bez zmian.
- [ ] `dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code` — bez rozbieżności.
- [ ] Zweryfikować manifest, sumy raportu i `git diff --check`.
- [ ] Zapisać implementację osobnym commitem po commicie planu; nie wypychać.

## Kryterium ukończenia

Wszystkie 270 poprawnych rekordów `/0` przechodzą w pełnym porównaniu stanu, pamięci, kolejki i T-state; całe pozostałe rozszerzenia `/1–/7` są widoczne jako skipy; pełny korpus i bramki P4 są zapisane w raporcie i checkliście. Plan i implementacja mają osobne commity.

## Polecenia robocze

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors tests/VibeEmulator.Intel8086.Tests/Vectors --manifest /tmp/a12-c7-local-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-c7-local.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/a12-c7-filter-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-c7-full.json
```
