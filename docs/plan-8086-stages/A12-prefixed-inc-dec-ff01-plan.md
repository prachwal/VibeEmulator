# A12 — segment-prefiksowane `INC/DEC r/m16` (`FF /0,/1`)

## Cel

Zweryfikować i pokryć lokalnymi fixture’ami istniejącą ścieżkę segment-prefiksowanego `FF /0,/1` dla form rejestrowych i pamięciowych. W formie pamięciowej prefiks wybiera segment celu; w rejestrowej nie zmienia operandu. W obu przypadkach wpływa na pobranie instrukcji, kolejkę i T-state. Jeżeli przypięte źródła potwierdzą istniejącą implementację, nie zmieniać kodu CPU; plan oraz fixture’y/raport zapisać w osobnych commitach na branchu `A12-8086-pelna-weryfikacja`.

## Źródła i inwentaryzacja

- Przypięte źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; pliki `v1/FF.0.json.gz` i `v1/FF.1.json.gz`, po 2 000 rekordów.
- Segment-prefiksowane `FF /0`: 981 rekordów; `FF /1`: 1 002; razem 1 983.
- Liczby prefiksowanych form `mod=00/01/10/11`: `/0` 257/236/227/261; `/1` 232/261/253/256.
- Prefiksowane formy `mod=11` występują: 517 rekordów razem i wykonują `INC/DEC r16`; prefiks nie wybiera tu segmentu. Form pamięciowych jest 1 466.
- W bazowym nieprefiksowanym filtrze A6 pełne `/0,/1` przeszło 2 017 rekordów: 506 form rejestrowych i 1 511 pamięciowych. Zmiana dotyczy segment-prefiksowanych 1 983 rekordów.
- Zachowanie i schedule opierać na pinowanych śladach SST, zwłaszcza przedrostkowym zużyciu kolejki, domyślnym SS dla BP, override pamięci i aligned/unaligned RMW.

## Zakres

1. Obsłużyć segment-prefiksowane `FF /0` (INC) i `/1` (DEC) dla rejestru i pamięci.
2. Dla `mod=11` wykonać zmianę rejestru i flags zgodnie z implementacją nieprefiksowaną, ale odtworzyć dodatkowy czas/prefetch prefiksu.
3. Dla pamięci wybrać adres EA oraz segment domyślny DS/SS albo segment wskazany prefiksem.
4. Zachować prawidłowe RMW, byte-lane BHE, zapis, dummy/read phases, kolejkę i wszystkie T-state.
5. Zachować SP, IP, FLAGS (poza flagami INC/DEC) oraz resztę stanu zgodnie z wektorami.
6. Nie rozszerzać pakietu na `FF /2–/7`; te grupy mają własne plany i statusy.

## Plan realizacji

### P0 — plan i źródło

- [x] Potwierdzić branch i czysty checkpoint po `C7 /0`.
- [x] Policzyć prefiksy, rozszerzenia i cztery formy ModR/M w `FF.0` oraz `FF.1`.
- [x] Zapisać plan przed implementacją w osobnym commicie.

### P1 — fixture’y i dekoder

- [x] Dodać po 20 niezmienionych rekordów z każdej kombinacji prefiks/opcode/mod: 32 grupy i 640 lokalnych fixture’ów, z numerami/hashami w manifeście.
- [x] Zweryfikować ścieżkę dekodera dla obu rozszerzeń; istniejąca implementacja obsługuje `FF /0,/1`, bez zmian w kodzie CPU.
- [x] Zweryfikować przekazanie prefiksu do EA i formy rejestrowe: 640/640 lokalnych fixture’ów przechodzi.
- [x] Zweryfikować źródłowy domyślny SS dla BP i DS dla pozostałych kombinacji w pełnych filtrach.
- [x] Potwierdzić prefiksowy queue status, fetch windows, idle oraz aligned/unaligned RMW w pełnych wektorach: 4 000/4 000.

### P2 — testy wektorowe

- [x] Uruchomić 640 fixture’ów: 640/640 passed, zero mismatchy/timeoutów/parse errors.
- [x] Segment-prefiksowane filtry: `/0` 981/981 i `/1` 1 002/1 002, zero skipów/mismatchy/timeoutów/parse errors.
- [x] Oba pełne pliki: 4 000/4 000 passed, zero skipów/mismatchy/timeoutów/parse errors.
- [x] Potwierdzić formy `mod=11` (517), pamięciowe adresy (1 466), BP/SS, każdy override oraz przypadki RMW parzyste i nieparzyste.

### P3 — pełny korpus i dokumentacja

- [x] Użyć pełnego raportu korpusu po C7: kod CPU nie zmienił się w tym podpakiecie, więc licznik 562 958/646 000 i 83 042 skipów pozostaje reprezentatywny.
- [x] Uaktualnić macierz ISA, README, raport i checklistę A12; pokrycie pełnego korpusu bez zmian.

### P4 — Testy i kryteria

- [x] Build Debug/Release po zmianach kodu C7: zero ostrzeżeń i błędów; ten podpakiet nie zmienia C#.
- [x] Pełne testy Release po dodaniu fixture’ów: 22 139/22 139.
- [x] Formatter verify i OpcodeInfo 512/512: bez zmian/rozbieżności; kod C# nie został zmodyfikowany.
- [x] Walidator manifestu (1 329 wpisów/25 939 wskazań) i `git diff --check` przechodzą.
- [x] Zapisać fixture’y i raport osobnym commitem po commicie planu `72d5a45`; commit `089eeea`. Nie wypychać.

## Kryterium ukończenia

Wszystkie 1 983 segment-prefiksowane rekordy `FF /0,/1` przechodzą porównanie stanu, pamięci, kolejki i T-state; oba pełne pliki `/0,/1` przechodzą 4 000/4 000, skipy poza tym zakresem są jawne, a pełny korpus i bramki są zapisane. Plan i osobny commit fixture’ów/raportu są zatwierdzone.

## Polecenia robocze

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors tests/VibeEmulator.Intel8086.Tests/Vectors --manifest /tmp/a12-ff01-prefixed-local-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-ff01-prefixed-local.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/a12-ff01-prefixed-filter-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-ff01-prefixed-full.json
```
