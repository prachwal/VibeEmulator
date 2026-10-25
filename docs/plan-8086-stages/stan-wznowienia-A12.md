# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie. Najnowszy ukończony podetap pośredniego `CALL` (`FF /2,/3`) przechodzi 4 000/4 000 pełnych przypiętych wektorów. Pełne źródło przechodzi 515 519/646 000; pozostaje 130 481 jawnych skipów, 0 mismatchy.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; ostatni ukończony podetap segment-prefiksowanego `FF /6,/7`: plan `c65ca0e`, implementacja `faa641d`, checkpoint `7e57aa4`. Poprzedzająca go implementacja nieprefiksowanego `/7`: `ff22d92`.
- Podetap `MOV moffs` (`A0`–`A3`) jest zamknięty: plan `b0790e4`, implementacja `5e84a94`; osobne commity na A12.
- Podetap `XCHG r/m16,r16` (`87`) jest zakończony: plan `51d24a2`, implementacja `b0665fa`; osobne commity na branchu A12.
- Podetap `MOV` rejestry segmentowe ↔ `r/m16` (`8C`/`8E`) zakończono commitami planu `a0fe0cb` i implementacji `5fcffe8`; pełne 4 000/4 000.
- Pośredni `CALL r/m16` (`FF /2,/3`) ukończono: plan `b653718`, pełne źródło 4 000/4 000, pełny korpus +4 000 passed / −4 000 skips; raport `A12-indirect-call-report.json`.

## Ostatnia zmiana i dowody

Nieprefiksowane `FF /6` jest zaimplementowane dla rejestru i pamięci, z odczytem operandu słowa, zapisem do `SS:SP` i prefetch. Inwentaryzacja `FF.6.json.gz`: 2 000 rekordów, z czego 1 014 nieprefiksowanych (252 rejestrowe i 762 pamięciowe) oraz 986 prefiksowanych.

- 64 segment-prefiksowane próbki `80`/`82`, 20 wektorów na prefiks/opcode/rozszerzenie: 1 280 przypadków; 567 aktywnych wpisów / 11 202 rekordy w lokalnym manifeście.
- Pełne filtry źródłowe `80` i `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: po 16 000/16 000, 0 skipów i mismatchy.
- Próbki `FF /6`: 40/40; pełny nieprefiksowany filtr: 1 014/1 014, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff6-full-report.json`.
- Próbki `FF /7`: 40/40; pełny nieprefiksowany filtr: 996/996, 0 skipów/niezgodności/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-ff7-full-report.json`.
- Segment-prefiksowane `FF /6,/7`: próbki repo 320/320; pełne wektory 1 990/1 990, z czego `/6` 986/986 i `/7` 1 004/1 004; raport `/tmp/vibe-a12-ff-prefix-full-report.json`.
- `TEST AX,imm16` (`A9`): fixture repo 20/20; pełny plik 2 000/2 000, 0 skipów i rozbieżności; raport `/tmp/vibe-a12-a9-full-report.json`.
- `MOV AL/AX ↔ moffs16` (`A0`–`A3`): lokalne próbki 80/80; pełne filtry 8 000/8 000, 0 skipów/rozbieżności/timeoutów/błędów parsowania; raport `A12-moffs-report.json`.
- `XCHG r/m16,r16` (`87`): lokalna próbka 20/20; pełny filtr 2 000/2 000; pełne źródło 646 000: 507 519 passed, 138 481 skipped, 0 mismatchy/timeoutów/błędów parsowania; raport `A12-xchg-word-report.json` i `/tmp/vibe-a12-full-after-xchg87-final.json`.
- `MOV` Sreg (`8C`/`8E`): lokalne fixture 40/40; pełne filtry 4 000/4 000; pełny korpus 646 000: 511 519 passed, 134 481 skipped, 0 mismatchy/timeoutów/błędów parsowania; raport `A12-mov-segment-report.json` i `/tmp/vibe-a12-full-after-8c8e-20261025.json`.
- Intel8086 Release: 13 471/13 471; pełna solucja Release: 16 271/16 271.
- Pełne źródło po `FF /2,/3`: 323 pliki / 646 000 rekordów, **515 519 passed, 130 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-after-ff23.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Końcowe bramki po podetapie `8C`/`8E`: Debug/Release po 0 ostrzeżeń i błędów; pełna solucja Release 16 231/16 231; formatter verify czysty; OpcodeInfo 512/512 zgodnych; walidator manifestu 1 011 wpisów / 19 951 rekordów, źródło 323 pliki / 646 000 rekordów. Pełna macierz ISA zawiera dowody 8 000/8 000 dla A0–A3, 2 000/2 000 dla `87` i 4 000/4 000 dla `8C`/`8E`.

## Co pozostało

1. Zakończono podetap `TEST AX,imm16`: plan `00560a7`, implementacja `d94ae3c`. Zmiany są zatwierdzone osobno; niczego nie wypchnięto.
2. Podetap `FF /2,/3` zakończony: plan `b653718`; implementacja `4aecec5` przechodzi pełne 4 000/4 000 i wszystkie bramki jakości; plan `b653718` jest osobnym commitem.
3. Następny zaplanowany podetap: indirect JMP `FF /4,/5` w `A12-indirect-jump-ff45-plan.md`; pełne źródło zawiera 4 000 jawnych skipów tych form.
4. Po nim mapować pozostałe jawne skipy (130 481 przed JMP), w tym F6/F7, D0–D3 i inne prefiksy segmentowe.
4. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
