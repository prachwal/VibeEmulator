# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; bieżąca zmiana domyka segment-prefiksowane `80`/`82`.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; zmiana bazuje na `b761ab4 feat(core): dodaj PUSHF i POPF do 8086`.
- Bieżący zakres obejmuje implementację, profile cykli, 64 lokalne pliki wektorów, manifest i aktualizacje dokumentacji. Commit po pełnych bramkach jakości; nie pushować.

## Ostatnia zmiana i dowody

Segment-prefiksowane grupy immediate `80`/`82` korzystają z profili T-state i kolejki wyprowadzonych z przypiętych wektorów źródłowych. Dla `82` tabela obejmuje wszystkie rozszerzenia `/0`–`/7`, `mod`/`r/m` i kolejki 5/6, wspólne dla prefiksów ES/CS/SS/DS. `80` używa tej samej ścieżki; pełne źródło weryfikuje wszystkie jego formy.

- 64 segment-prefiksowane próbki `80`/`82`, 20 wektorów na prefiks/opcode/rozszerzenie: 1 280 przypadków; 567 aktywnych wpisów / 11 202 rekordy w lokalnym manifeście.
- Pełne filtry źródłowe `80` i `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: po 16 000/16 000, 0 skipów i mismatchy.
- Intel8086 Release: 12 871/12 871.
- Pełne źródło: 323 pliki / 646 000 rekordów, **491 519 passed, 154 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-final-report.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Pełna solucja Debug/Release: 0 ostrzeżeń i błędów; pełny Release test: 15 671/15 671; formatter verify czysty; OpcodeInfo 512/512 zgodnych. Zmiana gotowa do commita.

## Co pozostało

1. Zatwierdzić segment-prefiksowane `80`/`82` osobnym commitem na obecnym branchu; bez pushowania.
2. Dalej mapować i redukować 154 481 jawnych skipów. Szczególnie istotne są brakujące formy FF, pozostałe kombinacje segment-prefix, F6/F7 i D0–D3.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
