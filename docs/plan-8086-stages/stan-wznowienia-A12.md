# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Ostatni zamknięty pakiet: segment-prefiksowane `LEA 8D`, fixture’y 80/80, filtr prefiksowany 1 004/1 004 i plik 2 000/2 000. Korpus: 595 207/646 000 (92,1%), 50 793 skipów, 0 mismatchy/timeoutów/parse errors. Plan LEA `b842f56`; raport: [A12-prefixed-lea-8d-report.json](A12-prefixed-lea-8d-report.json). Korpus ukończony; buildy, testy Release 22 968/22 968, format, OpcodeInfo 512/512 i manifest 2 034/35 989 są zielone. Checklista LEA: 10/10 (100%); plan/implementacja/dokumentacja w commitach `b842f56`, `9b7cc06`, `330f1e3`. Następny pakiet ALU `00–3D, 84/85` ma 49 782 rekordy źródłowe; plan `d790ce0` przed kodem, checklista 1/10 (10%).

Pakiet C7 zaliczył testy Release 22 243/22 243 (Intel8086 19 443/19 443), buildy Debug/Release bez ostrzeżeń i błędów, format, OpcodeInfo 512/512 oraz manifest 1 822/33 088. Pakiet zamknięty checklistą 6/6 (100%). Checklista: [A12-checklista-realizacji.md](A12-checklista-realizacji.md); raport: [A12-c7-ignored-extension-report.json](A12-c7-ignored-extension-report.json).

Lista punktów do dokończenia i obowiązkowy rejestr procentu: [A12-checklista-do-dokonczenia.md](A12-checklista-do-dokonczenia.md). Checklista realizacji pakietów: [A12-checklista-realizacji.md](A12-checklista-realizacji.md). Pełna inwentaryzacja skipów i szczegółowa historia: [A12-dalsze-prace-checklist.md](A12-dalsze-prace-checklist.md). Aktualnie 92,1% rekordów pełnego pinu przechodzi. Szczegółowa checklista aktualizacji po każdym punkcie: [A12-biezacy-rejestr-postepu.md](A12-biezacy-rejestr-postepu.md); najnowszy raport: [A12-prefixed-lea-8d-report.json](A12-prefixed-lea-8d-report.json); zamknięta checklista LEA: [A12-checklista-pakietu-prefixed-lea-8d.md](A12-checklista-pakietu-prefixed-lea-8d.md), 10/10 (100%); nowa checklista ALU: [A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md), 1/10 (10%).

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`; zamknięte pakiety: `POP r/m16` plan `4ca96d9` / implementacja `54f875e`, oraz `MOV r/m16,imm16` plan `d49f2bf` / implementacja `fbd6e43`. Walidacja segment-prefiksowanego `FF /0,/1` ma plan `72d5a45` i dowody w commicie `089eeea`: 640 fixture’ów oraz pełne pliki przechodzą, bez zmian kodu CPU; pełny wynik pozostaje 562 958/646 000 (87,1%).
- Podetap `MOV moffs` (`A0`–`A3`) jest zamknięty: plan `b0790e4`, implementacja `5e84a94`; osobne commity na A12.
- Podetap `XCHG r/m16,r16` (`87`) jest zakończony: plan `51d24a2`, implementacja `b0665fa`; osobne commity na branchu A12.
- Podetap `MOV` rejestry segmentowe ↔ `r/m16` (`8C`/`8E`) zakończono commitami planu `a0fe0cb` i implementacji `5fcffe8`; pełne 4 000/4 000.
- Pamięciowe `MOV 89/8B`: pełny filtr 4 000/4 000, pełny korpus 558 450/646 000 (86,4%); osobne commity: plan `d483857`, implementacja `c449767`.
- `LES/LDS` (`C4/C5`): pełne filtry 4 000/4 000, fixture’y 40/40, Intel8086 18 271/18 271; w chwili zakończenia pakietu pełny korpus 562 450/646 000 (87,1%). Plan `5158550`; implementacja `1817148`.
- Ostatni zamknięty pakiet `POP r/m16` (`8F /0`): 202/202 lokalne fixture’y, pełny filtr /0 238/238, cały plik 8F 238 passed + 1 762 jawne skipped; korpus wtedy 562 688/646 000 (87,1%), 83 312 skipped, zero mismatchy/timeoutów/parse errors. Build Debug/Release zielony, testy Release 21 273/21 273, formatter verify, OpcodeInfo 512/512 i manifest zielone. Plan `4ca96d9`, implementacja `54f875e`.
- Pośredni `CALL r/m16` (`FF /2,/3`) ukończono: plan `b653718`, pełne źródło 4 000/4 000, pełny korpus +4 000 passed / −4 000 skips; raport `A12-indirect-call-report.json`.

## Następny pakiet

Kontynuować inwentaryzację pozostałych 55 831 skipów segment-prefiksowych. E0–E3 są już obsłużone. Wybrać kolejny rozłączny pakiet, zapisać plan przed kodem, a podczas implementacji używać wąskich filtrów; pełny filtr, korpus i bramki uruchomić raz po stabilizacji pakietu.

## Ostatnia zmiana i dowody

Segment-prefiksowane `LOOP/LOOPE/LOOPNE/JCXZ`: 16 grup fixture’ów 320/320; cztery pliki źródłowe 8 000/8 000; pełny korpus 590 169 passed, 55 831 skipped, zero rozbieżności/timeoutów/błędów parsowania. Bramki Debug/Release, Release tests, formatter, OpcodeInfo, manifest i diff check zielone. Implementacja `5890848`; dokumentacja: checkpoint `52f27e0`.

`8F /1–/7`: pełne ślady potwierdzają 1 762 aliasy `POP r/m16` na P80C86A-2. Fixture’y 1 461/1 461; plik 2 000/2 000; 261 nowych profili, 178 współdzielonych, 0 konfliktów; korpus 586 226/646 000 (90,7%). Raport: [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json).
`MOV r/m16,imm16` (`C7 /0`) jest zaimplementowane dla rejestru i pamięci, z ModR/M, displacement, 16-bit immediate, segment override i harmonogramem T-state z 157 profili bez konfliktów w pinowanych rekordach. Fixture’y 226/226; pełny filtr `/0` 270/270; cały plik 270 passed, 1 730 jawnych skipów `/1–/7`; pełny korpus 562 958 passed, 83 042 skipped i zero mismatchy/timeoutów/parse errors (87,1%). Build Debug/Release, testy 21 499/21 499, format verify, OpcodeInfo 512/512 i manifest są zielone; plan `d49f2bf` i implementacja `fbd6e43` są osobnymi commitami. Raport: [`A12-mov-rm16-immediate-c7-report.json`](A12-mov-rm16-immediate-c7-report.json).

Segment-prefiksowane `FF /0,/1` miały już implementację na branchu. Dodane 640 fixture’ów przechodzi, pełne źródła `FF.0`/`FF.1` przechodzą 4 000/4 000; z tego 1 983 rekordy są prefiksowane. Dowody są w commicie `089eeea`.

`SALC` (`D6`) jest zaimplementowane dla wersji bez prefiksu i czterech prefiksów segmentowych. Fixture’y: 100/100; pełne źródło: 2 000/2 000; pełny korpus: 564 958/646 000 (87,5%), 81 042 skipped i 0 mismatchy/timeoutów/parse errors. Pełna solucja Release: 22 239/22 239. Raport: [A12-salc-d6-report.json](A12-salc-d6-report.json); plan: [A12-salc-d6-plan.md](A12-salc-d6-plan.md).

Segment-prefiksowane ESC (`D8–DF`) przechodzi fixture’y 2 560/2 560, pełny prefiksowany filtr 8 012/8 012 i kontrolę bez prefiksu 7 988/7 988. Pełny korpus po pakiecie: 572 970/646 000 (88,7%), 73 030 skipped, 0 mismatchy/timeoutów/parse errors. Raport: [A12-prefiksy-segmentowe-esc-d8-df-report.json](A12-prefiksy-segmentowe-esc-d8-df-report.json); plan: [A12-prefiksy-segmentowe-esc-d8-df-plan.md](A12-prefiksy-segmentowe-esc-d8-df-plan.md).


`POP r/m16` (`8F /0`) obsługuje rejestr, pamięć, segment override i pełne T-state oparte na pinned SST. 238/238 pełnych wektorów `/0` przechodzi, lokalne fixture’y 202/202; 1 762 rozszerzenia `/1–/7` pozostają jawnie pominięte. Pełny korpus wynosi 562 688 passed, 83 312 skipped, zero mismatchy/timeoutów/parse errors (87,1%). Raport: [`A12-pop-rm16-8f-report.json`](A12-pop-rm16-8f-report.json).

Pamięciowe `MOV 89/8B` obsługują słowo w obu kierunkach, adresowania ModR/M, DS/SS i wszystkie cztery segment-prefiksy. Nowe fixture’y przechodzą 160/160; pełne źródło 4 000/4 000; pełny korpus 646 000: 558 450 passed, 87 550 skipped, 0 mismatchy/timeoutów/parse errors. Raport: [`A12-mov-memory-89-8b-report.json`](A12-mov-memory-89-8b-report.json).

`LES/LDS` (`C4/C5`) czyta czterobajtowy wskaźnik z pamięci: offset do wybranego rejestru ogólnego, segment odpowiednio do ES/DS. Próbki 40/40, pełne filtry 4 000/4 000, Intel8086 18 271/18 271; pełny korpus 562 450 passed, 83 550 skipped, 0 mismatchy/timeoutów/parse errors (87,1%). Raport: [`A12-les-lds-c4-c5-report.json`](A12-les-lds-c4-c5-report.json).

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
- Intel8086 Release: 13 511/13 511; pełna solucja Release: 16 311/16 311.
- Pełne źródło po `FF /4,/5`: 323 pliki / 646 000 rekordów, **519 519 passed, 126 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**; raport `/tmp/vibe-a12-full-after-ff45.json`.
- `TEST r/m,imm` (`F6/F7 /0,/1`): lokalne fixture’y 80/80, pełne filtry 8 000/8 000, pełny korpus +8 000 passed / −8 000 skipów, bez mismatchy; raport `A12-test-rm-immediate-f6f7-report.json`.
- Poprzedni wynik po `PUSHF`/`POPF`: 475 612 passed, 170 388 skipped; segment-prefiksowane `80`/`82` dodały 15 907 zgodnych przypadków.
- Dane źródłowe: `/tmp/vibe-8086-a12-complete/v1`; tymczasowy profil pełnego źródła dla `80`: `/tmp/vibe-a12-full-80-report.json`; dla `82`: `/tmp/vibe-a12-full-82-report.json`.
- Końcowe bramki po podetapie `8C`/`8E`: Debug/Release po 0 ostrzeżeń i błędów; pełna solucja Release 16 231/16 231; formatter verify czysty; OpcodeInfo 512/512 zgodnych; walidator manifestu 1 011 wpisów / 19 951 rekordów, źródło 323 pliki / 646 000 rekordów. Pełna macierz ISA zawiera dowody 8 000/8 000 dla A0–A3, 2 000/2 000 dla `87` i 4 000/4 000 dla `8C`/`8E`.

## Co pozostało

1. Zakończono podetap `TEST AX,imm16`: plan `00560a7`, implementacja `d94ae3c`. Zmiany są zatwierdzone osobno; niczego nie wypchnięto.
2. Podetap `FF /2,/3` zakończony: plan `b653718`; implementacja `4aecec5` przechodzi pełne 4 000/4 000 i wszystkie bramki jakości; plan `b653718` jest osobnym commitem.
3. Podetap indirect JMP `FF /4,/5` zakończony osobnymi commitami planu `4a2729a` i implementacji `92c5353`; pełne 4 000/4 000, korpus +4 000 passed / −4 000 skips.
4. Podetap `TEST r/m,imm` (`F6/F7 /0,/1`) zamknięty commitami planu `7e495ce` i implementacji `efdd9c8`; wyniki pełnych filtrów, korpusu i bramek jakości zapisano.
5. Kontynuować implementację i klasyfikację pozostałych 73 030 skipów według [checklisty A12](A12-dalsze-prace-checklist.md); pakiet `POP r/m16` (`8F /0`) jest zaimplementowany w osobnym commicie `54f875e`.
6. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

## Rytm pracy dla krótszych iteracji

- Grupować powiązane zmiany implementacji, fixture’y i aktualizacje dokumentacji w jeden pakiet A12.
- Po zmianie uruchamiać szybki, wąski filtr właściwy dla opkodu; pełny filtr źródłowy uruchamiać po ustabilizowaniu całego pakietu.
- Pełny korpus, komplet testów i buildy Debug/Release uruchamiać raz przed commitem pakietu, a nie po każdej poprawce w obrębie pakietu.
- Po nieudanym wąskim filtrze diagnozować jego minimalny przypadek i ponawiać ten filtr. Nie uruchamiać ponownie całego korpusu, dopóki zmiana nie jest gotowa.
- Każde kryterium planu aktualizować po tej końcowej bramce; nie oznaczać testów pominiętych jako zaliczone.

### Testy i kryteria

- [x] `SALC` (`D6`): fixture’y 100/100; pełne źródło 2 000/2 000; korpus 646 000: 564 958 passed, 81 042 skipped, 0 mismatchy/timeoutów/parse errors (87,5%).
- [x] Solucja Release 22 239/22 239; build Debug/Release bez ostrzeżeń/błędów; formatter verify, manifest 1 334/26 039 i OpcodeInfo 512/512 zielone.
- [x] Plan SALC i implementacja są w osobnych commitach: `c0c9e6e`, `519dc83`.
- [ ] Pozostałe prefiksy segmentowe, niezależny program/ROM, luki cyklowe A10/A11 oraz audyt końcowy A1–A12 pozostają otwarte; postęp śledzi [checklista](A12-dalsze-prace-checklist.md).

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
