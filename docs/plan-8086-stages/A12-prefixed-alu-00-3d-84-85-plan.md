# A12 — segment-prefiksowane operacje ALU (`00–3D`, `84/85`)

## Cel i branch

Na branchu `A12-8086-pelna-weryfikacja` rozszerzyć istniejącą implementację ALU 8086 o wszystkie przypięte ślady z jednym segment override przed 50 źródłowymi opcode’ami: 32 formy binarnej ALU ModR/M, 16 form akumulatora z immediate oraz `TEST r/m8,r8` / `TEST r/m16,r16` (`84/85`). Pakiet obejmuje zarówno `mod=11`, jak i adresowanie pamięciowe, odczyt oraz read-modify-write, wszystkie cztery segmenty, bajty instrukcji i cykle prefetch. To przenosi istniejącą semantykę bezprefiksową na wariant prefiksowany; nie zakłada, że segment jest używany przy operandzie rejestrowym.

Plan i osobna checklista są zapisywane przed zmianą kodu. Implementację i checkpoint dokumentacji zapisać osobnymi commitami, kontynuując łańcuch A12; nie wypychać branchu.

## Źródło i inwentarz

- Repozytorium/pin: `SingleStepTests/8086`, `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.
- Źródłowe pliki `v1`: 60 gzipów opcode’ów po 2 000 rekordów, pobrane w `/tmp/vibe-8086-a12-complete/v1/`.
- W korpusie po LEA 8D dokładnie 49 782 rekordy z tymi opcode’ami zostały pominięte wyłącznie z powodu segmentowego prefiksu; wszystkie pozostałe przypadki ALU w tych plikach są już zaliczane.
- Rozkład segmentów: ES `26` 12 554; CS `2E` 12 586; SS `36` 12 171; DS `3E` 12 471.
- Liczba pomijanych rekordów źródłowych na opcode:

| opcode | rekordy segment-prefiksowane |
|---|---:|
| `00` | 1014 |
| `01` | 978 |
| `02` | 977 |
| `03` | 975 |
| `04` | 986 |
| `05` | 1013 |
| `08` | 977 |
| `09` | 975 |
| `0A` | 986 |
| `0B` | 1013 |
| `0C` | 996 |
| `0D` | 1033 |
| `10` | 981 |
| `11` | 1012 |
| `12` | 1000 |
| `13` | 993 |
| `14` | 982 |
| `15` | 987 |
| `18` | 976 |
| `19` | 994 |
| `1A` | 1027 |
| `1B` | 987 |
| `1C` | 988 |
| `1D` | 971 |
| `20` | 974 |
| `21` | 1014 |
| `22` | 995 |
| `23` | 995 |
| `24` | 1005 |
| `25` | 996 |
| `28` | 1002 |
| `29` | 992 |
| `2A` | 1000 |
| `2B` | 1000 |
| `2C` | 997 |
| `2D` | 1002 |
| `30` | 986 |
| `31` | 994 |
| `32` | 1005 |
| `33` | 970 |
| `34` | 990 |
| `35` | 1016 |
| `38` | 994 |
| `39` | 997 |
| `3A` | 1022 |
| `3B` | 984 |
| `3C` | 1027 |
| `3D` | 996 |
| `84` | 998 |
| `85` | 1010 |
| **Razem** | **49782** |

- Kategorie (sumują się do 49 782): 32 binarne formy ALU ModR/M `00–03`, `08–0B`, …, `38–3B`; 16 form `AL/AX, immediate` `04–05`, `0C–0D`, …, `3C–3D`; TEST ModR/M `84/85`. Tabela wylicza tylko opkody z segment-prefiksowanymi przypadkami w tym pinie; pliki zawierają również rekordy pozostałych form i służą do regresji.
- Ostatni pełny pin: 595 207/646 000 passed (92,1%), 50 793 skipped, zero mismatchy, timeoutów i błędów parsowania. Raport: [LEA 8D](A12-prefixed-lea-8d-report.json). Spodziewany przyrost po zgodnym przebiegu wszystkich 49 782 rekordów: 644 989/646 000 (99,8%), jeżeli cała rodzina przejdzie i nie pojawią się regresje.

## Zakres implementacji

1. Rozpoznać segment prefiks przed opcode’ami ALU 00–3D oraz 84/85, ustawić wariant prefiksu i poprawnie odczytać ModR/M / immediate z kolejki.
2. Dla `mod=11` zachować wynik, flagi i operand register zgodny z istniejącą logiką; prefiks nadal wpływa na T-state i kolejkę.
3. Dla `mod=00/01/10` wybrać ES/CS/SS/DS zgodnie z prefiksem, zachować domyślny segment DS/SS, BHE, parzystość, read-only dla CMP/TEST oraz read-modify-write dla operacji zapisujących.
4. Odtworzyć fetch T1–T4, queue status/data, IP, końcową kolejkę i długość cyklu na podstawie przypiętych pełnych śladów; nie dodawać stałej liczby cykli bez potwierdzenia każdego profilu.
5. Utrzymać dotychczasowe ścieżki bez prefiksu bez regresji.

## Testy i kryteria

- [ ] Plan/inwentarz/checklista zapisane przed kodem; plan ma własny commit.
- [ ] Dodać 160 deterministycznych fixture’ów: 40 na prefix, reprezentujących klasy rejestrowe, memory read-only, memory read-modify-write, accumulator byte/word immediate i TEST; test lokalny przechodzi 160/160.
- [ ] Wykonać pełny filtr segment-prefiksowany dla wszystkich 60 plików: **49 782/49 782**, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [ ] Uruchomić całe 60 plików źródłowych jako regresję: **120 000/120 000**, jawnie podać liczbę bezprefiksowanych/prefiksowanych i brak błędów.
- [ ] Po stabilizacji uruchomić jeden pełny korpus 646 000. Oczekiwany pułap przy pełnej zgodności: 644 989/646 000 (99,8%), 1 011 skipów (prefiksowane CALL FAR 9A), zero innych skipów/mismatchy/timeoutów/parse errors.
- [ ] Bramki: build Debug i Release, testy Release, `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn`, OpcodeInfo `--verify-code`, walidator manifestu i `git diff --check` zielone.
- [ ] Uaktualnić `## Testy i kryteria`, macierz ISA, raport pakietu, README wektorów, checklistę A12 i stan wznowienia po każdym zakończonym punkcie, ze wskazaniem % korpusu.
- [ ] Osobny commit implementacji i osobny commit dokumentacji; raport zawiera hashe planu, implementacji i checkpointu.

## Optymalizacja przebiegów

Najpierw zebrać deterministyczne fixture’y i używać wyłącznie wąskich filtrów do diagnozy. Nie uruchamiać pełnego korpusu przy kolejnych poprawkach. Po ustabilizowaniu całej rodziny uruchomić jeden przebieg pełnego filtra, jeden przebieg 60 plików regresyjnych, jeden pełny korpus i jedną zbiorczą bramkę jakości.

## Polecenia

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~PrefixedAlu --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-prefixed-alu/vectors --manifest /tmp/a12-prefixed-alu/prefixed-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-prefixed-alu/prefixed-report.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-prefixed-alu/vectors-all --manifest /tmp/a12-prefixed-alu/all-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-prefixed-alu/all-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Status

- Inwentarz potwierdzony z pełnego pinu, wszystkie 49 782 rekordy odpowiadają aktywnym skipom prefiksowym.
- Baza przed pakietem: 595 207 passed/646 000 (92,1%).
- Checklista pakietu: [A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md).
