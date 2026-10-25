# Plan podetapu A12: pośrednie `CALL r/m16` (`FF /2` i `/3`)

Status: implementacja ukończona i zweryfikowana; plan oraz implementacja są w osobnych commitach na branchu A12.

## Cel i zakres

Na branchu `A12-8086-pelna-weryfikacja` dodać obie formy pośredniego CALL z grupy
`FF`: `/2` — near indirect CALL oraz `/3` — far indirect CALL. Objąć wszystkie
zaobserwowane formy ModR/M, pojedyncze prefiksy segmentowe, odczyt operandu,
zapis adresu powrotu na stos, zmianę IP lub CS:IP, opróżnienie kolejki i każdy
obserwowany T-state. FLAGS i RAM poza dostępami stosu/odczytem operandu mają
pozostać zgodne z przypiętymi rekordami.

Zakres to pełne przypięte pliki `FF.2.json.gz` i `FF.3.json.gz` — po 2 000
rekordów. Pozostałe rozszerzenia `FF /0,/1,/4,/5` oraz zachowanie nieobecne w
źródle nie należą do tego podetapu.

## Źródła i inwentaryzacja

- SingleStepTests 8086, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`;
  dane dostępne lokalnie w `/tmp/vibe-8086-a12-complete/v1`.
- `FF /2`: 2 000 rekordów; 982 bez prefiksu, 1 018 z prefiksem. Rozkład ModR/M
  `mod=00/01/10/11`: 488/518/509/485; wszystkie osiem kodowań `r/m` występuje
  w `mod=11` (51–71 rekordów każde). Odczyt operandów pod nieparzystym adresem
  występuje w 771 rekordach.
- `FF /3`: 2 000 rekordów; 964 bez prefiksu, 1 036 z prefiksem. Rozkład `mod`
  `00/01/10/11`: 697/676/627/0; brak bezpośredniego operandu rejestrowego.
  Odczyt operandów pod nieparzystym adresem występuje w 968 rekordach.
- Pełny runner przed implementacją klasyfikuje wszystkie 4 000 rekordów jako
  skip: rozszerzenia `/2` i `/3` nie są obsługiwane.
- W repo są już nieprefiksowane i prefiksowane ścieżki `FF /6,/7`, odczyt słowa
  przez `TickMemoryWordIncDec` i bezpośrednie `CALL`/`RET` w `TickJump`. Trzeba
  sprawdzić je i współdzielić tylko te fazy, które zgadzają się z wektorami.
  Nie kopiować harmonogramu CALL immediate bez weryfikacji.

## Kolejność prac

1. Dodać po 20 pierwszych niezmienionych rekordów `FF.2` i `FF.3` do aktywnego
   manifestu; sprawdzić, że próbka zawiera wszystkie formy, a dodatkowe przypadki
   graniczne dobrać z pełnego źródła i wskazać przez `test_num`/hash.
2. Zaimplementować `FF /2` near CALL: pobranie słowa rejestru/pamięci, zachowanie
   kolejnego IP, push na `SS:SP`, nowy IP, opróżnienie kolejki i prefiksy.
3. Dodać `FF /3` far CALL: odczyt offsetu i segmentu z pamięci, oba zapisy stosu,
   kolejność transferów, nowy CS:IP i kolejkę. Nie dopuszczać `mod=11`, którego
   źródło nie zawiera.
4. Uwzględnić słowo pod nieparzystym adresem, kolejność bajtów, BHE, oddzielone
   transakcje, prefetch, domyślny segment DS/SS oraz nadpisanie segmentu.
5. Najpierw uruchomić próbki lokalne, następnie pełne filtry 2 000/2 000 dla obu
   rozszerzeń. Każdy mismatch naprawić na podstawie pierwszej różnicy T-state i
   ponowić pełny filtr.
6. Ponowić cały korpus 646 000: oczekiwane +4 000 passed, −4 000 skipped,
   niezmienione 0 mismatchy, timeoutów i błędów parsowania. Skontrolować pełne
   wyniki `FF /6,/7`, `8C/8E` i wcześniejsze CALL/RET.
7. Zaktualizować macierz ISA, raport, `## Testy i kryteria`, README fixture'ów,
   plan nadrzędny i plik wznowienia. Plan i implementację zatwierdzić osobno;
   bez push.

## Testy i kryteria

- [x] Próbki to pierwsze 20 rekordów każdego pełnego pliku upstream, bez zmian;
  harness porównuje CPU, RAM, kolejkę i każdy T-state.
- [x] Inwentaryzacja i raport potwierdzają wszystkie rozkłady ModR/M, prefiksy,
  formy rejestrowe/pamięciowe i przypadki nieparzystego adresu.
- [x] `FF.2.json.gz`: 2 000 passed, 0 skipped, mismatchy, timeoutów i parse errors.
- [x] `FF.3.json.gz`: 2 000 passed, 0 skipped, mismatchy, timeoutów i parse errors.
- [x] Pełny korpus: 646 000 rekordów, zmiana +4 000 passed/−4 000 skipped;
  mismatchy, timeouty i parse errors = 0.
- [x] Intel8086 i pełna solucja Release przechodzą; buildy Debug/Release mają
  0 ostrzeżeń; formatter, manifest i OpcodeInfo są zielone.
- [x] Plan i implementacja są osobnymi commitami na branchu A12; bez push.

## Polecenia weryfikacyjne

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-a12-ff23-full-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-ff23-full-report.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-8086-a12-full-current-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-full-after-ff23.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
