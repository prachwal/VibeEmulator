# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; trwa implementacja nieprefiksowanego `FF /6` (`PUSH r/m16`).

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; ostatni commit planu: `1a439e7 docs(8086): ogranicz FF /6 do form bez prefiksu`; implementacja `FF /6` jest niezatwierdzona.
- Poprzedni commit implementacyjny: `50b7234 feat(core): obsłuż segment-prefiksowane grupy ALU 80/82`.

## Ostatnia zmiana i dowody

Nieprefiksowane `FF /6` jest zaimplementowane dla rejestru i pamięci, z odczytem operandu słowa, zapisem do `SS:SP` i prefetch. Inwentaryzacja `FF.6.json.gz`: 2 000 rekordów, z czego 1 014 nieprefiksowanych (252 rejestrowe i 762 pamięciowe) oraz 986 prefiksowanych.

- 64 segment-prefiksowane próbki `80`/`82`, 20 wektorów na prefiks/opcode/rozszerzenie: 1 280 przypadków; 567 aktywnych wpisów / 11 202 rekordy w lokalnym manifeście.
- Pełne filtry źródłowe `80` i `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: po 16 000/16 000, 0 skipów i mismatchy.
- Próbki `FF /6`: 40/40; pełny nieprefiksowany filtr: 1 014/1 014, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff6-full-report.json`.
- Intel8086 Release: 12 911/12 911; pełna solucja Release: 15 711/15 711.
- Pełne źródło: 323 pliki / 646 000 rekordów, **492 533 passed, 153 467 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-after-ff6.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Build Debug/Release: 0 ostrzeżeń i błędów; formatter verify czysty; OpcodeInfo 512/512 zgodnych. Pełne A12 nadal nie jest zamknięte.

## Co pozostało

1. Zatwierdzić implementację `FF /6` w osobnym commicie po planie `1a439e7`.
2. Dalej mapować i redukować pozostałe jawne skipy (obecnie 153 467). Kluczowe zakresy obejmują prefiksowane `FF /6`, `FF /2`–`/5`, F6/F7, D0–D3 i inne prefiksy segmentowe.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
