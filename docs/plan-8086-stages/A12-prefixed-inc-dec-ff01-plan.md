# A12 — segment-prefiksowane `INC/DEC r/m16` (`FF /0,/1`)

## Cel

Uzupełnić istniejące wykonanie `FF /0,/1` o segment-prefiksowane formy rejestrowe i pamięciowe. W formie pamięciowej prefiks wybiera segment celu; w formie rejestrowej nie zmienia operandu. W obu przypadkach nadal wpływa na pobranie instrukcji, kolejkę i T-state. Plan oraz implementację zapisać w osobnych commitach na branchu `A12-8086-pelna-weryfikacja`.

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

- [ ] Dodać po 20 niezmienionych rekordów z każdej kombinacji prefiks/opcode/mod: 32 grupy i 640 lokalnych fixture’ów, z numerami/hashami w manifeście.
- [ ] W dekoderze prefixu dopuścić `FF /0,/1` jawnie i zachować `/0,/1` walidację dla tej ścieżki.
- [ ] Przekazać wybrany prefiks do adresowania EA, nie zmieniając ścieżki rejestrowej poza timingiem.
- [ ] Zweryfikować źródłowy domyślny SS dla BP i DS dla pozostałych kombinacji.
- [ ] Odtworzyć prefiksowy queue status, fetch windows, idle i aligned/unaligned RMW z profili źródłowych.

### P2 — testy wektorowe

- [ ] Uruchomić 640 fixture’ów; każdy mismatch diagnozować po hash/T-state.
- [ ] Uruchomić segment-prefiksowane filtry: `/0` 981/981 oraz `/1` 1 002/1 002, zero skipów, mismatchy, timeoutów i parse errors.
- [ ] Uruchomić oba pełne pliki 4 000/4 000: istniejące 2 017 rekordów bez prefiksu i nowe 1 983 prefiksowane.
- [ ] Potwierdzić formy `mod=11` (517), pamięciowe adresy (1 466), BP/SS, każdy override oraz przypadki RMW parzyste i nieparzyste.

### P3 — pełny korpus i dokumentacja

- [ ] Uruchomić raz pełny korpus 646 000 rekordów z manifestem po C7.
- [ ] Uaktualnić liczniki skipów, procent, macierz ISA, README, raport i checklistę A12.

### P4 — Testy i kryteria

- [ ] `dotnet build VibeEmulator.slnx -c Debug --nologo` — bez ostrzeżeń i błędów.
- [ ] `dotnet build VibeEmulator.slnx -c Release --nologo` — bez ostrzeżeń i błędów.
- [ ] `dotnet test VibeEmulator.slnx -c Release --nologo` — cały zestaw zielony.
- [ ] `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn` — bez zmian.
- [ ] OpcodeInfo `--verify-code` — bez rozbieżności.
- [ ] Walidator manifestu, raporty i `git diff --check` przechodzą.
- [ ] Zapisać implementację osobnym commitem po planie; nie wypychać.

## Kryterium ukończenia

Wszystkie 1 983 segment-prefiksowane rekordy `FF /0,/1` przechodzą porównanie stanu, pamięci, kolejki i T-state, oba pełne pliki `/0,/1` przechodzą 4 000/4 000, skipy poza tym zakresem są jawne, pełny korpus i końcowe bramki są zapisane. Plan i implementacja mają osobne commity.

## Polecenia robocze

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors tests/VibeEmulator.Intel8086.Tests/Vectors --manifest /tmp/a12-ff01-prefixed-local-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-ff01-prefixed-local.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/a12-ff01-prefixed-filter-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-ff01-prefixed-full.json
```
