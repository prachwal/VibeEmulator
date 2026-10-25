# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie. Najnowszy podetap `MOV AL/AX ↔ moffs16` (`A0`–`A3`) przechodzi 8 000/8 000 pełnych przypiętych wektorów. Pełne źródło przechodzi 505 519/646 000; pozostaje 140 481 jawnych skipów, 0 mismatchy.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; ostatni ukończony podetap segment-prefiksowanego `FF /6,/7`: plan `c65ca0e`, implementacja `faa641d`, checkpoint `7e57aa4`. Poprzedzająca go implementacja nieprefiksowanego `/7`: `ff22d92`.
- Podetap `MOV moffs` (`A0`–`A3`) jest zamknięty: plan `b0790e4`, implementacja `5e84a94`; osobne commity na A12.
- Następny podetap: plan `XCHG r/m16,r16` (`87`) w `A12-xchg-rm16-plan.md`; 2 000 źródłowych przypadków, w tym 1 504 pamięciowe i 1 021 segment-prefiksowanych.

## Ostatnia zmiana i dowody

Nieprefiksowane `FF /6` jest zaimplementowane dla rejestru i pamięci, z odczytem operandu słowa, zapisem do `SS:SP` i prefetch. Inwentaryzacja `FF.6.json.gz`: 2 000 rekordów, z czego 1 014 nieprefiksowanych (252 rejestrowe i 762 pamięciowe) oraz 986 prefiksowanych.

- 64 segment-prefiksowane próbki `80`/`82`, 20 wektorów na prefiks/opcode/rozszerzenie: 1 280 przypadków; 567 aktywnych wpisów / 11 202 rekordy w lokalnym manifeście.
- Pełne filtry źródłowe `80` i `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: po 16 000/16 000, 0 skipów i mismatchy.
- Próbki `FF /6`: 40/40; pełny nieprefiksowany filtr: 1 014/1 014, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff6-full-report.json`.
- Próbki `FF /7`: 40/40; pełny nieprefiksowany filtr: 996/996, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff7-full-report.json`.
- Segment-prefiksowane `FF /6,/7`: próbki repo 320/320; pełne wektory 1 990/1 990, z czego `/6` 986/986 i `/7` 1 004/1 004; raport `/tmp/vibe-a12-ff-prefix-full-report.json`.
- `TEST AX,imm16` (`A9`): fixture repo 20/20; pełny plik 2 000/2 000, 0 skipów i rozbieżności; raport `/tmp/vibe-a12-a9-full-report.json`.
- `MOV AL/AX ↔ moffs16` (`A0`–`A3`): lokalne próbki 80/80; pełne filtry 8 000/8 000, 0 skipów/rozbieżności/timeoutów/błędów parsowania; raport `A12-moffs-report.json`.
- Intel8086 Release: 13 371/13 371; pełna solucja Release: 16 171/16 171.
- Pełne źródło po `A0`–`A3`: 323 pliki / 646 000 rekordów, **505 519 passed, 140 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-after-moffs.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Build Debug/Release: po 0 ostrzeżeń i błędów; pełna solucja Release 16 171/16 171 (Intel8086 13 371/13 371); formatter verify czysty; OpcodeInfo 512/512 zgodnych; walidator manifestu 1 008 wpisów / 19 891 rekordów, źródło 323 pliki / 646 000 rekordów. Pełna macierz ISA zawiera teraz dowody 2 000/2 000 dla A0–A3.

## Co pozostało

1. Zakończono podetap `TEST AX,imm16`: plan `00560a7`, implementacja `d94ae3c`. Zmiany są zatwierdzone osobno; niczego nie wypchnięto.
2. Dalej mapować i redukować pozostałe jawne skipy (obecnie 140 481), w tym `FF /2`–`/5`, F6/F7, D0–D3 i inne prefiksy segmentowe.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
