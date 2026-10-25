# A12 — segment-prefiksowane `LEA` (`8D`)

## Cel i branch

Obsłużyć segment-prefiksowane `LEA r16,m` zgodnie z przypiętymi śladami P80C86A-2. W 8086 override segmentu nie zmienia offsetu efektywnego obliczanego przez LEA, ale prefiks wpływa na pobranie instrukcji, kolejkę i T-state. Nie wolno wywnioskować zgodności z semantyki samego LEA. Pracować na `A12-8086-pelna-weryfikacja`; plan, implementacja i checkpoint dokumentacji będą osobnymi commitami.

## Źródło i inwentarz

- Repozytorium: `SingleStepTests/8086`, pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, plik `v1/8D.json.gz`.
- Plik pobrany do `/tmp/a12-prefixed-lea-8d/source/8D.json.gz`, gzip poprawny, SHA-256 `5736d94720e9367079b3f1c729b06644c7fe8662b50aed394cc063f45dcd29e`.
- 2 000 rekordów łącznie: 996 bez prefiksu oraz 1 004 segment-prefiksowane; ostatni pełny korpus jawnie pominął 1 004 formy 8D i nie raportował mismatchy.
- Prefiksy w źródle: ES `26`: 222; CS `2E`: 271; SS `36`: 260; DS `3E`: 251. W każdej grupie występują adresowania ModR/M `mod=00/01/10`; LEA nie przyjmuje `mod=11`.
- Bazowy wynik korpusu: 594 203/646 000 (92,0%), 51 797 skipów, 0 mismatchy, timeoutów i błędów parsowania. Raport: `/tmp/a12-eb-final-corpus-report.json`.
- Dotychczasowa implementacja LEA i jej dowody dla 996 form bez prefiksu są opisane w [planie A3](A3-transfery-8-bit-i-modrm.md); ten pakiet nie rozszerza tego twierdzenia bez ponownego pełnego filtra 8D.

## Zakres

1. Dodać lokalne, niezmienione fixture’y z pierwszymi 20 rekordami źródłowymi dla każdego prefiksu (80 przypadków) oraz manifest przypięty do SHA.
2. Odczytać ślady dla wszystkich profili LEA i czterech prefiksów; sprawdzić offset EA, zachowanie rejestrów/flags, bajty instrukcji, IP, kolejkę i każdy T-state.
3. Obsłużyć wyłącznie cztery segment-prefiksy przed opcode `8D`; nie zmieniać `LEA` z `mod=11` ani innych opkodów.
4. Przejść pełny filtr prefiksowany (1 004 przypadki) oraz cały plik 8D (2 000 przypadków). Zachować jawne skipy i rozbieżności, jeśli źródłowe ślady nie potwierdzą zachowania.
5. Po ustabilizowaniu wykonać pojedynczy pełny przebieg pinu 646 000 rekordów oraz zbiorczą bramkę jakości.

## Kolejność i kryteria

- [x] Plan i osobna checklista zapisane przed zmianą kodu; plan commit `b842f56`.
- [x] Fixture’y i test offline: 4 grupy, 80/80 rekordów identycznych z wycinkiem źródłowym.
- [x] Profile cykli LEA i segment override odtworzone z przypiętych rekordów; brak zmian oczekiwań.
- [x] Implementacja LEA z segment-prefiksem przechodzi 1 004/1 004 pełnego filtra.
- [x] Pełny plik źródłowy 8D przechodzi 2 000/2 000; jawnie raportować także 996 form bez prefiksu.
- [x] Pełny korpus 646 000 uruchomiony po stabilizacji: 595 207 passed, 50 793 skipped, zero błędów; uzgadnia się do 646 000.
- [x] Build Debug i Release, testy Release (22 968/22 968), format verify, OpcodeInfo 512/512, walidator manifestu 2 034/35 989 oraz `git diff --check` zielone.
- [x] Macierz ISA, raport, README wektorów, checklisty i stan wznowienia zsynchronizowane.
- [x] Implementacja zapisana osobno w `9b7cc06` po tym planie.
- [x] Checkpoint dokumentacji `330f1e3` zapisany osobnym commitem; hash w raporcie; niczego nie wypychano.

## Strategia pracy

Wąskie uruchomienia robić tylko do diagnozy rozbieżności. Po ustabilizowaniu wspólnego zakresu LEA wykonać jeden przebieg pełnego filtra 8D, jeden pełny przebieg korpusu i jedną zbiorczą bramkę jakości. Próbki lokalne nie zastępują pełnych źródeł.

## Polecenia

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~PrefixedLea --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-prefixed-lea-8d/vectors --manifest /tmp/a12-prefixed-lea-8d/manifest-prefixed.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-prefixed-lea-8d/prefixed-report.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-prefixed-lea-8d/vectors-all --manifest /tmp/a12-prefixed-lea-8d/manifest-all.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-prefixed-lea-8d/all-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn --no-restore
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Historia

- 2026-10-25: zakres zinwentaryzowany przed implementacją; 1 004 pominięte rekordy segment-prefiksowanego LEA, cztery grupy; bazowy pełny korpus 594 203/646 000 (92,0%).
