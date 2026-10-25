# A12 — prefiksy segmentowe dla `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`)

## Cel i branch

Domknąć segment-prefiksowane formy krótkich skoków `E0–E3` na podstawie pełnych śladów przypiętego P80C86A-2. Prefiks nie wybiera segmentu dla tych instrukcji, ale wpływa na IP, pobieranie kolejki, moment zmiany `CX`, decyzję skoku i T-state. To wspólny pakiet wykonania `LOOP`; plan i implementację zatwierdzić osobno na branchu `A12-8086-pelna-weryfikacja`.

## Źródło i zakres

- Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, pełne pliki `v1/E0.json.gz`–`v1/E3.json.gz` po 2 000 rekordów.
- Inwentarz raportu prefiksowego A10: 3 943 rekordy z jednym prefiksem segmentowym: `E0` 975, `E1` 979, `E2` 969, `E3` 1 020.
- Rozkład według prefiksu ES/CS/SS/DS: `E0` 234/234/255/252; `E1` 267/220/241/251; `E2` 228/255/243/243; `E3` 238/250/258/274.
- Lokalnie istnieją już nieprefiksowane próbki `A7Loop/E0.json`–`E3.json`; nie zmieniać rekordów ani kolejności tych fixture’ów.
- Wszystkie 16 grup prefiks/opcode obecnie zgłaszają rozbieżności w raporcie A10. Pełny filtr i lokalne próbki są źródłem prawdy dla implementacji; nie zakładać, że wystarczy dodać stałe dwa cykle.

## Zakres implementacji

1. Dodać po maksymalnie 20 niezmienionych rekordów źródłowych dla każdej z 16 grup prefiks/opcode (do 320 fixture’ów) oraz wpisy do manifestu.
2. Odtworzyć indeksowanie bajtu displacement przy prefiksie i obliczenie celu/IP dla każdej formy.
3. Zachować semantykę `CX` (`E0–E2` zmniejszają, `E3` nie zmienia), FLAGS i stan niepowiązanych rejestrów.
4. Zmierzyć z pełnych wektorów zachowanie prefiksu dla ścieżki skoku i braku skoku, obu stanów ZF, `CX=0/1`, granic IP i wariantów kolejki.
5. Odtworzyć każdą fazę T-state, kolejkę prefetch, adres celu, status magistrali i moment aktualizacji stanu. Zmiany ograniczyć do wspólnej obsługi prefiksowanego `E0–E3`.
6. Utrzymywać checklisty i procent po każdym zamkniętym punkcie; pełny filtr i pełny korpus uruchomić jeden raz po ustabilizowaniu wszystkich czterech opkodów.

## Plan pracy

- [x] Potwierdzić branch, czysty stan po pakiecie `8F` i pin źródła.
- [x] Policzyć pełne rekordy prefiksowane według opcodu i prefiksu; zapisać zakres przed kodem.
- [x] Dodać fixture’y 16 grup i manifest powiązany z pinem; zachować istniejące nieprefiksowane próbki (320 rekordów).
- [x] Porównać pełne ślady z nieprefiksowanymi odpowiednikami; kod pokrywa ścieżki `CX/ZF`, obie długości kolejki i displacement po prefiksie.
- [x] Zaimplementować `E0–E3` z jednym prefiksem segmentowym na podstawie pełnych śladów.
- [x] Uruchomić lokalne fixture’y (320/320) i pełne filtry czterech źródeł (8 000/8 000; zero skipów i rozbieżności).
- [x] Wykonać jeden pełny przebieg korpusu: 590 169 passed / 55 831 skipped / 646 000 = 91,4%; zero mismatchy, timeoutów i błędów parsowania.
- [x] Zaliczyć build Debug/Release, pełne testy Release, formatter, OpcodeInfo, manifest i `git diff --check`.
- [x] Zaktualizować `## Testy i kryteria`, checklisty A12, macierz ISA, README wektorów i raport JSON.
- [x] Zapisać osobny commit implementacji `5890848` po commicie planu `e0b5209`.
- [x] Zapisać checkpoint dokumentacji `52f27e0` w osobnym commicie; nie wypychać.

## Testy i kryteria

- [x] Fixture’y lokalne: 16 grup, 320/320; oryginalne rekordy bez zmian.
- [x] Pełne `E0–E3`: 8 000/8 000 passed, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Każda kombinacja prefiks/opcode z niepustego źródła przechodzi; testowane są taken/not-taken, ZF/CX, warianty kolejki i granice adresowania w pełnych wektorach.
- [x] Pełny korpus 646 000: 590 169 passed, 55 831 skipped (91,4%), zero mismatchy/timeoutów/parse errors.
- [x] Debug/Release build bez ostrzeżeń, testy Release, formatter, OpcodeInfo, manifest i `git diff --check` zielone.
- [x] Osobne commity planu `e0b5209`, implementacji `5890848` i checkpointu `52f27e0`; pakiet zamknięty checklistą 6/6.

## Polecenia weryfikacyjne

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~Loop --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/a12-loop-e0-e3-prefixed-manifest.json --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/a12-loop-e0-e3-prefixed.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Historia

- 2026-10-25: plan zapisany przed kodem (`e0b5209`); 3 943 prefiksowane rekordy, 16 grup, cztery opkody.
- 2026-10-25: implementacja `5890848`; lokalnie 320/320, pełne źródła 8 000/8 000, korpus 590 169/646 000 (91,4%). Bramki jakości zielone; checkpoint dokumentacji `52f27e0` zapisany osobno; pakiet zamknięty checklistą 6/6.
