# A12 — segment-prefiksowany krótki skok `JMP rel8` (`EB`)

## Cel i branch

Obsłużyć segmentowy prefix `26/2E/36/3E` przed krótkim, względnym skokiem `EB` według pełnych śladów przypiętego P80C86A-2. Zakres obejmuje wyłącznie pojedynczy prefix segmentowy. Plan, implementację i checkpoint dokumentacji zapisać osobno na branchu `A12-8086-pelna-weryfikacja`.

## Źródło i inwentarz

- Repozytorium `SingleStepTests/8086`, commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, źródło `v1/EB.json.gz`, 2 000 rekordów.
- Audyt A10 wskazuje 1 018 segment-prefiksowanych przypadków EB w czterech grupach: ES (`26`) 259, CS (`2E`) 249, SS (`36`) 251, DS (`3E`) 259.
- W poprzednim audycie wszystkie 1 018 były rozbieżne, ponieważ dekoder odrzucał tę formę z prefixem; nie był to dowód błędnego cyklu `JMP EB`.
- Bazowy pełny korpus po E8–EA: 593 185/646 000 (91,8%), 52 815 jawnych skipów, zero mismatchy/timeoutów/parse errors.

## Zakres analizy i implementacji

1. Dodać po maksymalnie 20 niezmienionych rekordów źródłowych do każdej z czterech grup oraz przypiąć manifest do SHA.
2. Zweryfikować prefix fetch, dekodowanie displacement ze znakiem, IP po prefixie i opcodzie, zawijanie offsetu 16-bit oraz kierunki skoku.
3. Porównać T-state, status magistrali, QS, kolejkę prefetch i pobranie celu dla profili zależnych od liczby bajtów w kolejce.
4. Wykorzystać wspólną obsługę skoków tylko tam, gdzie pełne ślady EB to potwierdzają; zachować niezmienione nieprefiksowane `EB` i prefiksowane `E8–EA`.
5. Nie wprowadzać stałej korekty zegarów bez poparcia pełnymi wektorami.

## Plan i checklista

- [x] Potwierdzić branch, pin i inwentarz: 1 018 rekordów w czterech grupach; zapisać plan przed implementacją.
- [x] Dodać lokalne fixture’y i test porównujący 80 rekordów ze źródłem; 80/80 przechodzi.
- [x] Odtworzyć profile pełnych śladów i zaimplementować segment-prefiksowane `EB`.
- [x] Przejść filtr 1 018/1 018 oraz pełny plik EB 2 000/2 000; zero skipów, mismatchy, timeoutów i parse errors.
- [x] Uruchomić jeden pełny korpus: 594 203/646 000 (92,0%), 51 797 skipów, zero błędów.
- [x] Uruchomić komplet bramek jakości raz dla stabilnego pakietu: Debug/Release 0 ostrzeżeń i błędów, Release 22 887/22 887, format/verify, OpcodeInfo 512/512, manifest 2 026/35 829 i `git diff --check` zielone.
- [x] Uaktualnić raport, checklisty, macierz ISA, README wektorów i stan wznowienia; wynik korpusu 594 203/646 000 (92,0%).
- [ ] Zapisać osobne commity implementacji i dokumentacji; wpisać hashe i zamknąć checklistę.

## Testy i kryteria

- [x] Lokalne fixture’y: cztery grupy i 80/80 rekordów przechodzi.
- [x] Segment-prefiksowany pełny filtr: 1 018/1 018, zero skipów i rozbieżności.
- [x] Pełny plik EB: wszystkie rekordy prawidłowo porównane; brak nieoczekiwanych skipów, mismatchy, timeoutów i parse errors.
- [x] Kontrolne nieprefiksowane `EB` i wcześniejsze E8–EA zachowują zgodność.
- [x] Pełny korpus: jawny raport, zero mismatchy/timeoutów/parse errors.
- [x] Build Debug/Release, testy Release, format/verify, OpcodeInfo, walidator manifestu i `git diff --check` zielone.

## Strategia uruchamiania testów

W trakcie implementacji najpierw domknąć powiązaną zmianę prefixu, kolejki i cykli. Wąski test lub filtr uruchamiać, gdy potrzebny jest dowód dla diagnozowanej rozbieżności; po ustabilizowaniu uruchomić pełny filtr źródłowy, pełny korpus i komplet bramek po jednym razie.

## Polecenia

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~PrefixedShortJump --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/a12-eb-full/vectors --manifest /tmp/a12-eb-full/manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-eb-prefixed-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Historia

- 2026-10-25: plan przed implementacją; cztery grupy EB zawierają 1 018 rekordów. Bazowy pełny korpus 593 185/646 000 (91,8%).
