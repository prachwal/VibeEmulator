# A12 — segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)

## Cel i branch

Obsłużyć jeden segment-prefix przed bezpośrednim `CALL rel16`, `JMP rel16` i `JMP ptr16:16` na podstawie pełnych śladów przypiętego P80C86A-2. Pakiet obejmuje tylko segmentowe prefiksy `26/2E/36/3E`; sekwencje wieloprefiksowe i inne formy transferu sterowania pozostają osobnym zakresem. Plan i implementację zapisać osobno na branchu `A12-8086-pelna-weryfikacja`.

## Źródło i inwentarz

- Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, pliki `v1/E8.json.gz`–`v1/EA.json.gz` po 2 000 rekordów.
- Poprzedni pełny audyt prefiksów A10 wykazał 3 016 jedno-prefiksowych rekordów w 12 grupach, wszystkie 3 016 oznaczonych wtedy jako rozbieżne: `E8` 1 008, `E9` 1 005, `EA` 1 003.
- Liczby grup ES/CS/SS/DS:

| opcode | 26 ES | 2E CS | 36 SS | 3E DS | razem |
|---|---:|---:|---:|---:|---:|
| `E8` CALL rel16 | 267 | 247 | 256 | 238 | 1 008 |
| `E9` JMP rel16 | 256 | 251 | 259 | 239 | 1 005 |
| `EA` JMP ptr16:16 | 264 | 230 | 264 | 245 | 1 003 |
| **Razem** | **787** | **728** | **779** | **722** | **3 016** |

- Inwentarz A10 to punkt wyjścia; przed zakończeniem pakietu należy ponownie wygenerować dokładny filtr z bieżącego kodu. Nie przedstawiać historycznego statusu A10 jako wyniku po E0–E3.
- Nieprefiksowane implementacje i próbki A7 `E8`, `E9`, `EA` pozostają bez zmian i stanowią kontrolę regresji.

## Zakres analizy i implementacji

1. Dodać maksymalnie 20 oryginalnych rekordów źródłowych w każdej z 12 grup prefiks/opcode, z manifestem przypiętym do SHA.
2. Zmierzyć położenie displacement/offset/segmentu w kolejce po prefiksie i wszystkie wartości końcowe CS:IP, stosu i FLAGS.
3. Dla `E8` porównać kolejność zapisu CS/IP na stos, moment obliczenia celu, prefetch oraz parzysty i nieparzysty SP.
4. Dla `E9` i `EA` porównać pełny cykl zmiany IP/CS oraz czyszczenie i napełnianie kolejki. Prefiks segmentowy nie może zostać omyłkowo potraktowany jako wybór segmentu operandu.
5. Odtworzyć wszystkie T-state, status magistrali, QS, adresy i dane cykli. Nie dodawać stałej liczby zegarów bez potwierdzenia profili pełnych wektorów.
6. Utrzymać niezmienione fixture’y nieprefiksowane i uzyskać zero rozbieżności dla każdej niepustej grupy prefiks/opcode.

## Plan i checklista

- [x] Potwierdzić branch, czyste drzewo po zamknięciu E0–E3 oraz pin źródła.
- [x] Wybrać spójny zakres i policzyć 3 016 rekordów w 12 grupach z raportu A10; zapisać plan przed zmianą kodu.
- [x] Dodać 12 fixture’ów grupowych i test lokalny; porównać rekordy i SHA z przypiętym źródłem (240/240).
- [x] Przejrzeć pełne ślady E8/E9/EA, rozdzielając CALL, near JMP i far JMP.
- [x] Zaimplementować wszystkie 12 grup prefiks/opcode bez regresji form nieprefiksowanych.
- [x] Przejść lokalne fixture’y oraz pełne filtry: prefiksowane 3 016/3 016 i E8–EA 6 000/6 000; zero skipów/mismatchy/timeoutów/parse errors.
- [x] Po stabilizacji uruchomić jeden pełny korpus 646 000: 593 185 passed, 52 815 skipped, 0 błędów (91,8%).
- [x] Zaliczyć buildy Debug/Release, testy Release 22 806/22 806, `dotnet format` i verify, OpcodeInfo 512/512, manifest 2 018/35 669 oraz `git diff --check`.
- [x] Zaktualizować raport JSON, `## Testy i kryteria`, checklisty A12, macierz ISA, README wektorów i stan wznowienia.
- [x] Zapisać osobne commity implementacji `2e508ee` i checkpointu dokumentacji `4c4890a`; raport zawiera oba hashe.

## Testy i kryteria

- [x] Fixture’y lokalne: 12 grup / 240 rekordów, 240/240 przechodzi.
- [x] Pełne pliki E8–EA: 6 000/6 000, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Wszystkie 3 016 segment-prefiksowanych rekordów przechodzi; raport obejmuje E8/E9/EA oraz 12 grup i porównanie wszystkich T-state.
- [x] Kontrolne nieprefiksowane A7 E8/E9/EA pozostają zielone w pełnym filtrze źródłowym.
- [x] Pełny korpus 646 000: 593 185 passed, 52 815 jawnych skipów, zero mismatchy/timeoutów/parse errors.
- [x] Build, pełne testy, format, OpcodeInfo, manifest i diff check zielone; plan `10898e9` poprzedza implementację.

## Polecenia

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~CallJump --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-e8-ea-full/vectors --manifest /tmp/a12-e8-ea-full/manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-e8-ea-prefixed-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Historia

- 2026-10-25: plan przed implementacją; przypięty zakres A10 to 3 016 rekordów / 12 grup. Bazowy korpus po zamknięciu poprzedniego pakietu: 590 169/646 000 (91,4%).
- 2026-10-25: fixture’y, pełne filtry i jeden końcowy przebieg korpusu zaliczone; wynik wzrósł do 593 185/646 000 (91,8%). Bramki jakości zielone; osobne commity implementacji `2e508ee` i checkpointu dokumentacji `4c4890a`. Pakiet zamknięty.
