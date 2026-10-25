# Plan podetapu A12: `XCHG r/m16,r16` (`87`)

Status: plan zatwierdzany osobno; implementacja jeszcze nierozpoczęta.

## Cel i zakres

Uzupełnić 16-bitowy wariant wymiany `XCHG` dla opkodu `87` na aktywnym branchu
`A12-8086-pelna-weryfikacja`. Obsłużyć formę rejestrową (`mod=11`) oraz wszystkie
formy pamięciowe ModR/M, w tym displacement 8/16-bit, adresowanie `[disp16]`,
segment domyślny DS/SS i pojedynczy prefiks ES/CS/SS/DS. Implementacja ma zachować
stan i kolejkę prefetch oraz odtworzyć każdy cykl T-state z przypiętych wektorów.

Nie zmieniać zachowania `86` ani `90`–`97` poza współdzielonym kodem, jeśli pełne
wektory potwierdzą zgodność. Nie wyprowadzać cykli z samych tabel czasowych.

## Źródła i inwentaryzacja

- SingleStepTests 8086, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`,
  pełny plik `v1/87.json.gz`, 2 000 rekordów.
- Wstępny rozkład: 496 rejestrowych i 1 504 pamięciowe; 979 bez prefiksu,
  1 021 z prefiksem segmentowym (ES 254, CS 258, SS 263, DS 246).
- Przypięty pełny runner klasyfikuje wszystkie 2 000 jako skip przed
  implementacją, komunikat `Opcode 87 is not implemented yet`.
- Wektory zawierają sekwencję odczytu i zapisu dla pamięciowego XCHG; testować
  adresy parzyste/nieparzyste, granice offsetu, pełny stan rejestrów, RAM, kolejkę,
  wartości i piny magistrali dla każdego T-state.
- Źródłowy wektor zawiera też rekordy z bajtem `F0` po `87` jako następnym bajtem
  instrukcji; to nie jest prefiks LOCK, tylko bajt ModR/M. Klasyfikować prefiksy
  przed opcode, nie przez wyszukiwanie `F0` w całej instrukcji.

## Kolejność prac

1. Zbadać rozkład pełnych wektorów względem `mod`, `reg`, `r/m`, prefiksu,
   początkowej kolejki, adresu parzystego i długości cykli; zachować kilka
   dokładnych rekordów regresyjnych z każdego profilu.
2. Dodać pierwsze 20 niezmienionych rekordów upstream do `A12Group/87.json`,
   aktywnego manifestu i README wektorów.
3. Rozszerzyć stan dekodera i wymianę rejestrową o słowa bez zmiany `86`.
4. Dodać transfer pamięciowy słowa: odczyt obu bajtów, zapis starego słowa
   rejestru na ten sam adres, oddzielne transfery dla nieparzystego adresu,
   prawidłowe BHE, wrap offsetu i cykle kolejki/prefetch.
5. Obsłużyć prefiksy segmentowe zgodnie z `87.json.gz`, w tym krótkie kolejki;
   segment nadpisujący nie zmienia bezpośredniej formy rejestrowej.
6. Uruchomić próbkę repo i pełny filtr `87`; analizować pierwszą różnicę, poprawiać
   rdzeń i powtarzać filtr, aż wszystkie rekordy przejdą bez skipów i mismatchy.
7. Ponownie uruchomić pełny korpus 646 000, zaktualizować macierz ISA, raport,
   checklistę A12 i stan wznowienia. Zachować osobno commit planu i implementacji.

## Testy i kryteria

- [ ] Zapisano rozkład źródła 2 000 rekordów oraz dokładne rekordy brzegowe z hashami.
- [ ] Dodano 20 wiernych rekordów `87` do manifestu; testy Intel8086 sprawdzają
  stan końcowy i pełną sekwencję T-state.
- [ ] Rejestrowe `87`: pełny filtr przechodzi bez skipów, mismatchy, timeoutów i
  błędów parsowania.
- [ ] Pamięciowe `87`: pełny filtr przechodzi dla wszystkich trybów ModR/M,
  segmentów, parzystości adresu, displacement i profili kolejki/prefetch.
- [ ] Pełne `87.json.gz`: 2 000/2 000 zgodnych; raport wskazuje SHA źródła i runnera.
- [ ] Pełne źródło 646 000 przebiega ponownie; zmiana passed/skipped jest zgodna
  z pokryciem 2 000 rekordów, mismatched/timeout/parse errors wynoszą 0.
- [ ] Intel8086 i pełna solucja Release przechodzą; build Debug/Release bez
  ostrzeżeń; `dotnet format --verify-no-changes`, manifest i OpcodeInfo są zielone.
- [ ] Plan i implementacja są osobnymi commitami na `A12-8086-pelna-weryfikacja`;
  nic nie jest wypychane.

## Polecenia weryfikacyjne

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
python3 -c 'import json; p="tests/VibeEmulator.Intel8086.Tests/Vectors/manifest.json"; m=json.load(open(p)); m["active"]=[{"file":"87.json","opcode":"87","records":2000}]; json.dump(m,open("/tmp/vibe-a12-87-manifest.json","w"),indent=2)'
dotnet run --project tools/Intel8086VectorRunner/VibeEmulator.Intel8086VectorRunner.csproj -c Release -- \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-a12-87-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 --report /tmp/vibe-a12-87-full-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
