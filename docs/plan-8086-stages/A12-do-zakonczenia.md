# A12 — lista do zakończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

Główna, punktowa checklista otwartych prac i aktualizacji procentu: [A12-checklista-do-dokonczenia.md](A12-checklista-do-dokonczenia.md).

## Postęp implementacji

**100% korpusu SingleStepTests:** 646 000 / 646 000 rekordów przechodzi pełne porównanie CPU, pamięci, kolejki i cykli; zero skipów, rozbieżności, timeoutów i błędów parsowania. To miernik zgodności z przypiętym korpusem (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), nie procent wszystkich cech procesora. Niezależny program integracyjny oraz rejestr ograniczeń A10/A11 są gotowe; końcowy audyt branchy i bramki jakości pozostają otwarte.

Po każdym zamkniętym pakiecie należy zaznaczyć punkt poniżej, wpisać commit i wynik, a procent odświeżyć na podstawie pełnego przebiegu 646 000 rekordów. Nie zmieniać procentu na podstawie próbek ani samych testów jednostkowych.

## Do dokończenia

- [x] **Segment-prefiksowane ESC `D8–DF`** — fixture’y 2 560/2 560, pełny filtr prefiksowany 8 012/8 012, kontrolny nieprefiksowany 7 988/7 988, pełny korpus 572 970/646 000 (88,7%), zero mismatchy/timeoutów/parse errors. Buildy, pełne testy, format, OpcodeInfo, manifest i diff check są zielone. Plan: [A12-prefiksy-segmentowe-esc-d8-df-plan.md](A12-prefiksy-segmentowe-esc-d8-df-plan.md); osobne commity: plan `9ebc8c4`, implementacja `5ebb882`.
- [x] **`SETMO/SETMOC` (`D0–D3 /6`)** — fixture’y 1 600/1 600, pełny filtr 8 000/8 000, pełny korpus 580 970/646 000 (89,9%), 65 030 skipów i 0 mismatchy/timeoutów/parse errors. Plan `bd1ae29`, implementacja `aaed1ed`, checkpoint dokumentacji w oddzielnym commicie. Raport: [A12-setmo-d0-d3-6-report.json](A12-setmo-d0-d3-6-report.json).
- [x] **Segment-prefiksowany krótki `JMP rel8` (`EB`)** — fixture’y 80/80, prefiksowany filtr 1 018/1 018, plik 2 000/2 000; korpus 594 203/646 000 (92,0%), 51 797 skipów, zero mismatchy/timeoutów/parse errors. Plan `37ae6a6`; raport [A12-prefixed-short-jump-eb-report.json](A12-prefixed-short-jump-eb-report.json). Bramki jakości zielone (22 887 testów Release); osobne commity implementacji/dokumentacji: `d3b57a9` / `e1663ff`; pakiet zamknięty.
- [x] **Segment-prefiksowane kombinacje obecne w pinned SST** — ostatnia grupa `CALL FAR 9A`: fixture’y 80/80, filtr 1 011/1 011, plik 9A 2 000/2 000, pełny korpus 646 000/646 000 (100%). Plan `c365fac`; raport [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).
- [x] **Nieudokumentowane rozszerzenia `C6 /1–/7`** — wszystkie 1 764 rekordy przechodzą; plik C6 2 000/2 000. Plan `c91a464`, implementacja `d16021e`, checkpoint `02cacb4`. Raport: [A12-c6-ignored-extension-report.json](A12-c6-ignored-extension-report.json).
- [x] **Nieudokumentowane rozszerzenia `C7 /1–/7`** — pełny C7 2 000/2 000; fixture’y 1 426/1 426; pełny korpus 584 464/646 000 (90,5%), 61 536 skipów, 0 mismatchy/timeoutów/parse errors. Plan `ffee9f8`, implementacja `d24a4d1`, checkpoint `aeb0699`. Raport: [A12-c7-ignored-extension-report.json](A12-c7-ignored-extension-report.json).
- [x] **Nieudokumentowane rozszerzenia `8F /1–/7`** — pełne ślady P80C86A-2 potwierdzają 1 762 aliasy `POP r/m16`; fixture’y 1 461/1 461, pełny plik 2 000/2 000, korpus 586 226/646 000 (90,7%), 59 774 skipy. Metadata nadal oznacza rozszerzenia jako undefined. Plan `3fecba9`, korekta `a2d5203`, implementacja `c6d1b4f`, checkpoint `7a7d82d`; raport [A12-8f-ignored-extension-report.json](A12-8f-ignored-extension-report.json).
- [x] **Segment-prefiksowane `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`)** — 320/320 fixture’ów, pełne źródła 8 000/8 000, pełny korpus 590 169/646 000 (91,4%), 55 831 skipów i zero mismatchy/timeoutów/parse errors. Bramki jakości zielone; plan `e0b5209`, implementacja `5890848`, checkpoint dokumentacji `52f27e0`. Raport: [A12-prefixed-loop-e0-e3-report.json](A12-prefixed-loop-e0-e3-report.json).
- [x] **Segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`)** — fixture’y 240/240, filtr segment-prefiksowany 3 016/3 016, pełne E8–EA 6 000/6 000; korpus 593 185/646 000 (91,8%), 52 815 skipów, zero mismatchy/timeoutów/parse errors; bramki jakości zielone. plan `10898e9`, implementacja `2e508ee`, checkpoint dokumentacji `4c4890a`; raport [A12-prefixed-direct-call-jump-e8-ea-report.json](A12-prefixed-direct-call-jump-e8-ea-report.json).
- [x] **Niezależny program integracyjny** — oryginalny listing z licencją MIT, test powtarzalny 1/1; [opis](../../tests/VibeEmulator.Intel8086.Tests/Programs/A12/README.md), plan `79bb04c`, implementacja `2dc4968`.
- [x] **Audyt ograniczeń cyklowych A10/A11** — wszystkie nierozstrzygnięte obszary mają status i podstawę dowodową; [rejestr](A12-audyt-ograniczen-a10-a11.md). Luki pozostają ograniczeniami implementacji.
- [x] **Łańcuch branchy A0–A12** — każdy branch następnego etapu zawiera poprzedni; plany A0–A11 są w bazowym commicie `ffc9557`, commity etapów zachowano. Szczegóły: [audyt](A12-audyt-branchy-i-commitow.md).
- [x] **Końcowe bramki A1–A12** — Debug/Release 0 ostrzeżeń/błędów; pełne testy Release 23 251/23 251; formatter verify; OpcodeInfo 512/512; manifest 2 050/36 549; README, macierze i checklisty zsynchronizowane. Pełny korpus: najnowszy raport 646 000/646 000 (100%), bez zmian w CPU od tego przebiegu.

- [x] **Segment-prefiksowany `CALL FAR 9A` — ostatnia grupa pominięć.** 80/80 fixture’ów, filtr 1 011/1 011, pełny plik 2 000/2 000, cały korpus 646 000/646 000 (100%). Plan `c365fac`, implementacja `3198647`, dokumentacja `cba7033`; raport: [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).

## Rejestr ukończonych punktów

| data | punkt | commit(y) | passed / 646 000 | % |
|---|---|---|---:|---:|
| 2026-10-25 | `SALC` (`D6`), pełny plik i pełny korpus bez rozbieżności | `c0c9e6e`, `519dc83`, `6784b2b` | 564 958 | 87,5% |
| 2026-10-25 | Segment-prefiksowane ESC (`D8–DF`), fixture’y, oba pełne filtry i bramki zielone | plan `9ebc8c4`; implementacja `5ebb882` | 572 970 | 88,7% |
| 2026-10-25 | `SETMO/SETMOC` (`D0–D3 /6`), pełny filtr, korpus i bramki | plan `bd1ae29`; implementacja `aaed1ed`; checkpoint osobno | 580 970 | 89,9% |
| 2026-10-25 | `C6 /1–/7`, pełny plik, korpus i bramki | plan `c91a464`; implementacja `d16021e`; checkpoint `02cacb4` | 582 734 | 90,2% |
| 2026-10-25 | `C7 /1–/7`, pełny plik, korpus i bramki | plan `ffee9f8`; implementacja `d24a4d1`; checkpoint `aeb0699` | 584 464 | 90,5% |
| 2026-10-25 | `8F /1–/7` — pełne źródło, implementacja i korpus | 1 461 fixture’ów; pełny plik 2 000/2 000; implementacja `c6d1b4f`; checkpoint `7a7d82d` | 586 226 | 90,7% |

Szczegółowy rejestr wcześniejszych pakietów i rozbicie skipów: [A12-dalsze-prace-checklist.md](A12-dalsze-prace-checklist.md). Stan wznowienia: [stan-wznowienia-A12.md](stan-wznowienia-A12.md).


Zamknięte pakiety końcowe: segment-prefiksowane ALU (49 782/49 782) i `CALL FAR 9A` (1 011/1 011); pełny korpus wynosi 646 000/646 000. Raport 9A: [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).


Pakiet ALU `00–3D, 84/85` zamknięty 11/11 (100%); osobne commity planu `d790ce0`, implementacji `cab1fd2` i dokumentacji `fc9af2e`. Korpus po nim: 644 989/646 000; pakiet 9A podniósł wynik do 646 000/646 000.

| 2026-10-25 | Segment-prefiksowany CALL FAR 9A, pełny korpus i bramki | plan `c365fac`; 1 011/1 011 prefiksowane, 2 000/2 000 9A; Release 23 250/23 250 | 646 000 | 100% |
