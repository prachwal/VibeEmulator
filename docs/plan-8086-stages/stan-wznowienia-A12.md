# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; podetap segment-prefiksowanego `FF /6` i `/7` jest zaimplementowany, pełne wektory przechodzą, a kontrola jakości tej tury jest zakończona. Kolejny podetap należy wybrać z pozostałych jawnych skipów.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; nieprefiksowane `FF /6` jest zatwierdzone w `d9ba663`, plan aliasu `/7` w `92d969f`, a jego implementacja w `ff22d92 feat(core): obsluz alias FF /7 dla PUSH rm16`.
- Plan poprzedniego podetapu: `1a439e7 docs(8086): ogranicz FF /6 do form bez prefiksu`.

## Ostatnia zmiana i dowody

Nieprefiksowane `FF /6` jest zaimplementowane dla rejestru i pamięci, z odczytem operandu słowa, zapisem do `SS:SP` i prefetch. Inwentaryzacja `FF.6.json.gz`: 2 000 rekordów, z czego 1 014 nieprefiksowanych (252 rejestrowe i 762 pamięciowe) oraz 986 prefiksowanych.

- 64 segment-prefiksowane próbki `80`/`82`, 20 wektorów na prefiks/opcode/rozszerzenie: 1 280 przypadków; 567 aktywnych wpisów / 11 202 rekordy w lokalnym manifeście.
- Pełne filtry źródłowe `80` i `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: po 16 000/16 000, 0 skipów i mismatchy.
- Próbki `FF /6`: 40/40; pełny nieprefiksowany filtr: 1 014/1 014, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff6-full-report.json`.
- Próbki `FF /7`: 40/40; pełny nieprefiksowany filtr: 996/996, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff7-full-report.json`.
- Segment-prefiksowane `FF /6,/7`: próbki repo 320/320; pełne wektory 1 990/1 990, z czego `/6` 986/986 i `/7` 1 004/1 004; raport `/tmp/vibe-a12-ff-prefix-full-report.json`.
- Intel8086 Release: 13 271/13 271; pełna solucja po tej zmianie jeszcze do uruchomienia.
- Pełne źródło: 323 pliki / 646 000 rekordów, **495 519 passed, 150 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-after-ff-prefix.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Build Debug/Release: 0 ostrzeżeń i błędów; pełna solucja Release 16 071/16 071 (Intel8086 13 271/13 271); formatter verify czysty; OpcodeInfo 512/512 zgodnych; walidator manifestu 1 003 wpisy / 19 791 rekordów, źródło 323 pliki / 646 000 rekordów. Python audit matrix: 12/12.

## Co pozostało

1. Zatwierdzić implementację segment-prefiksowanego `FF /6,/7` osobno po planie `c65ca0e`; po commicie uzupełnić hash w tym pliku i zapisać checkpoint.
2. Dalej mapować i redukować pozostałe jawne skipy (obecnie 150 481), w tym `FF /2`–`/5`, F6/F7, D0–D3 i inne prefiksy segmentowe.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
