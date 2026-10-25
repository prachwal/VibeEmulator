# Plan podetapu A12: pośrednie `JMP r/m16` (`FF /4` i `/5`)

Status: implementacja ukończona i zweryfikowana; plan oraz implementacja są osobnymi commitami na branchu A12.

## Cel i zakres

Na branchu `A12-8086-pelna-weryfikacja` dodać indirect JMP z grupy `FF`: `/4` — near JMP r/m16 oraz `/5` — far JMP m16:16. Objąć pełne przypięte pliki `FF.4.json.gz` i `FF.5.json.gz`, po 2 000 rekordów każdy, wraz z obserwowanymi prefiksami segmentowymi, trybami adresowania, odczytami operandu, flush kolejki i fetch celem. JMP nie zapisuje ramki na stosie i nie zmienia FLAGS; potwierdzić stan w każdym rekordzie źródłowym.

Źródło: SingleStepTests/8086 SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, lokalnie `/tmp/vibe-8086-a12-complete/v1`. Ten plan obejmuje tylko `/4` i `/5`; inne rozszerzenia grupy `FF` są poza zakresem.

## Inwentaryzacja źródła

- `FF /4`: 2 000 rekordów; 998 bez prefiksu, 1 002 z prefiksem; ModR/M `mod=00/01/10/11`: 485/481/494/540; 540 form rejestrowych, 1 460 pamięciowych; każde `r/m` występuje; 737 rekordów ma nieparzysty pierwszy adres T1 odczytu operandu; kolejka początkowa: 978 rekordów z 5 bajtami, 1 022 z 6.
- `FF /5`: 2 000 rekordów; 1 007 bez prefiksu, 993 z prefiksem; ModR/M `mod=00/01/10/11`: 661/686/653/0; wszystkie formy są pamięciowe; każde `r/m` występuje; 1 010 rekordów ma nieparzysty pierwszy adres T1 odczytu; odczyty far pointera mają 2 transakcje T1 dla adresu parzystego i 4 dla nieparzystego; kolejka początkowa: 1 023 rekordy z 5 bajtami, 977 z 6.
- Przed implementacją pełny raport 646 000 rekordów klasyfikuje wszystkie 2 000 form `/4` i 2 000 form `/5` jako jawne skipy.
- Długości instrukcji, segment domyślny/override, sekwencja T-state, moment zmiany CS:IP i prefetch wyprowadzić z pełnych wektorów, nie przenosić założeń z `CALL` bez sprawdzenia.

## Kolejność prac

1. Zachować 20 pierwszych rekordów każdego pliku jako niezmienione fixture offline w `Vectors/A12Jump/`; rozszerzyć manifest testów i README.
2. Zaimplementować near indirect JMP `/4` dla `mod=11` i pamięci. Odczytać offset celu, wykonać obserwowany queue flush i target prefetch; nie zmieniać stosu ani FLAGS.
3. Zaimplementować far indirect JMP `/5` tylko dla operandów pamięciowych `mod=00/01/10`: odczytać offset i segment w obserwowanej kolejności, potem przeładować CS:IP i kolejkę. `/5 mod=11` nie występuje w przypiętym źródle i ma pozostać nieobsługiwane.
4. Obsłużyć prefiksy ES/CS/SS/DS, displacement, `DS:FFFF` wrap, oba parytety adresu, BHE, 20-bitowy fizyczny adres oraz fetch przy zawinięciu IP. Współdzielić z CALL tylko fazy potwierdzone zgodnymi wektorami.
5. Uruchomić lokalne 40 fixture, potem pełne filtry `/4` 2 000 i `/5` 2 000. Naprawiać rozbieżności według pierwszego T-state; nie zmieniać danych ani masek wektora.
6. Uruchomić cały korpus 646 000. Oczekiwany przyrost względem checkpointu po CALL: +4 000 passed, −4 000 skipped, mismatchy/timeouty/parse errors = 0.
7. Zaktualizować raport, macierz ISA, README fixture'ów, `## Testy i kryteria`, główny plan A12 oraz stan wznowienia. Plan i implementacja mają osobne commity na A12; bez push.

## Testy i kryteria

- [x] Fixture to pierwsze 20 rekordów `FF.4.json.gz` i `FF.5.json.gz` z przypiętego SHA, bez modyfikacji; lokalny harness porównuje CPU, RAM, kolejkę i każdy T-state.
- [x] Inwentaryzacja i raport potwierdzają zakres ModR/M, prefiksy, klasy operandów, parytet odczytu i transakcje pointera far.
- [x] Pełny filtr `FF.4.json.gz`: 2 000 passed; 0 skipped, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny filtr `FF.5.json.gz`: 2 000 passed; 0 skipped, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny korpus: +4 000 passed / −4 000 skipped do checkpointu 515 519/130 481; 0 mismatchy, timeoutów i błędów parsowania.
- [x] Buildy Debug/Release, testy Release, format verify, walidator manifestu i OpcodeInfo przechodzą bez ostrzeżeń/błędów.
- [x] Szczegółowy raport: [`A12-indirect-jump-ff45-report.json`](A12-indirect-jump-ff45-report.json).
- [x] Plan i implementacja są osobnymi commitami na branchu A12; bez push.

## Polecenia weryfikacyjne

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-a12-ff45-full-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-ff45-full-report.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-8086-a12-full-current-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-full-after-ff45.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
