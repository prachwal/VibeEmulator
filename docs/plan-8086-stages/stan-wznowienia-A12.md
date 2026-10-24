# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; ostatnia zmiana robocza dodaje rejestrowe `XCHG AX,r16`.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`, rodzic bazowy zachowuje etapy A0–A11. Commit bazowy przed bieżącą zmianą: `d4dd69d fix(core): odtworz granice IDIV procesora 8086`.
- Zmiany `XCHG AX,r16` i aktualizacja dokumentacji są jeszcze niezacommitowane; po zielonej weryfikacji zapisać je w osobnym commicie. Nie pushować.
- Zmodyfikowane pliki źródłowe: `Intel8086Core.XchgRegister.cs`, `Intel8086Core.Decode.cs`, `Intel8086Core.cs`. Dodano dokładne próbki 20 rekordów dla każdego opkodu `91`–`97` i wpisy manifestu.

## Ostatnia zmiana i wynik

`XCHG AX,r16` (`91`–`97`) wykonuje wymianę AX z wybranym rejestrem 16-bitowym i zajmuje trzy T-states. Każdy z siedmiu plików ma 20 lokalnych rekordów upstream. Zmiana usuwa 14 000 skipów z pełnego przebiegu.

- Pełne źródło SingleStepTests/8086, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: 323 pliki, 646 000 rekordów; **463 571 passed, 182 429 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**.
- Pełny filtr `91`–`97`: 14 000/14 000; próbki lokalne: 140/140.
- Intel8086 Release: 11 391/11 391; pełna solucja Release: 14 191/14 191.
- W tej turze Debug i Release budowały się bez ostrzeżeń/błędów. Pierwsze `dotnet format` zakończyło się błędem restore; ponowić z `MSBuildEnableWorkloadResolver=false` i `--no-restore`, a następnie ponownie uruchomić OpcodeInfo.
- Raport pełnego przebiegu: `/tmp/vibe-8086-a12-full-after-xchg-91-97.json`; dokładny filtr: `/tmp/vibe8086-xchg-91-97-full.json`.

## Co pozostało

1. Dokończyć formatter verify bez restore i `dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code`.
2. Zaktualizować checklisty `## Testy i kryteria`, raport A12 i README wektorów wynikami bieżącej tur.
3. Sprawdzić `git diff --check`, zatwierdzić zmianę `XCHG AX,r16` oraz dokumentację osobnym commitem na A12.
4. Kontynuować mapowanie i redukcję 182 429 jawnych skipów. A12 pozostaje otwarty; osobnym warunkiem jest niezależny test programowy/ROM z ustalonym pochodzeniem, licencją i kryterium sukcesu.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
