# A12 — checklista do dokończenia

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`.

To jest bieżąca lista otwartych prac. Po zamknięciu każdego punktu należy od razu zaznaczyć checkbox, dopisać commit i wynik
do rejestru oraz zaktualizować procent. Wskaźnik implementacji oznacza wyłącznie zgodność z przypiętym korpusem
SingleStepTests 8086 (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`), a nie procent wszystkich cech procesora.

Szczegółowy, osobny tracker z aktualną checklistą i historią zmian po każdym punkcie: [A12-biezacy-rejestr-postepu.md](A12-biezacy-rejestr-postepu.md).

## Aktualny stan

- **Pokrycie korpusu: 100%** — 646 000 / 646 000 passed.
- **Pozostałe:** 0 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania.
- Ostatni raport pełnego przebiegu: [segment-prefiksowany CALL FAR 9A](A12-prefixed-call-far-9a-report.json).
- Program integracyjny i audyt jawnych ograniczeń A10/A11 są zamknięte. Pozostaje końcowa weryfikacja gałęzi/commitów i bramek jakości.

Szczegółowy rejestr odhaczania po każdym zamkniętym punkcie: [A12-biezacy-rejestr-postepu.md](A12-biezacy-rejestr-postepu.md).

## Otwarta lista

### 1. Segment-prefiksowane kombinacje źródłowe — zamknięte

- [x] Inwentaryzacja skipów zakończyła się przypiętą listą źródłową; ostatnia grupa to 1 011 segment-prefiksowanych `CALL FAR 9A`.
- [x] Plan osobny `c365fac`; 80 fixture’ów, 1 011/1 011 pełnego filtra i 2 000/2 000 pliku 9A.
- [x] Pełny korpus po implementacji: 646 000/646 000 (100%), zero skipów/rozbieżności/timeoutów/parse errors.
- [x] Raport: [A12-prefixed-call-far-9a-report.json](A12-prefixed-call-far-9a-report.json).

### 2. Nieudokumentowane rozszerzenia `8F /1–/7`

- [x] Pełne ślady P80C86A-2 potwierdzają 1 762/1 762 aliasy `POP r/m16`; jest to obserwacja układu, metadata upstream nadal klasyfikuje je jako undefined.
- [x] Fixture’y 1 461/1 461 w 140 grupach; pełny plik 8F 2 000/2 000.
- [x] Pełny korpus: 586 226/646 000 (**90,7%**), 59 774 skipy segment-prefiksowe, zero mismatchy/timeoutów/parse errors.
- [x] Bramki jakości zielone: Debug/Release, testy Release 22 244/22 244, format, OpcodeInfo 512/512, manifest 1 962/34 549.
- [x] Osobny checkpoint dokumentacji `7a7d82d`; hash zapisany w raporcie.

### 3. Prefiksy segmentowe `LOOP/LOOPE/LOOPNE/JCXZ` (`E0–E3`)

- [x] Dodać 320 fixture’ów w 16 grupach; lokalny test przechodzi 320/320.
- [x] Pełne pliki źródłowe E0–E3 przechodzą 8 000/8 000, w tym 3 943 rekordy segment-prefiksowane.
- [x] Pełny korpus po pakiecie: 593 185/646 000 (91,8%), 52 815 skipów, 0 mismatchy/timeoutów/parse errors.
- [x] Build Debug/Release, testy Release, format/verify, OpcodeInfo, manifest i `git diff --check` zielone.
- [x] Plan `e0b5209` i implementacja `5890848` są osobnymi commitami; checkpoint dokumentacji `52f27e0` zapisany osobno.


### 4. Niezależny test programu lub ROM

- [x] Dodano oryginalny program bez zewnętrznych ROM-ów, z licencją MIT i listingiem: [program A12](../../tests/VibeEmulator.Intel8086.Tests/Programs/A12/README.md).
- [x] Opisano powtarzalne uruchomienie, hostowy feeder kolejki, brak wymaganych urządzeń, limit 256 taktów i kryterium AX/IP/HLT.
- [x] Test `Intel8086IndependentProgramTests` przechodzi 1/1; pełny korpus pozostaje 646 000/646 000 (100%). Test programu nie zastępuje SST.

### 5. Luki cyklowe A10/A11

- [x] Audyt NMI/INTR: czas sumaryczny, stos/IVT i INTA są sprawdzone; pełna kolejność T-state bez niezależnego śladu pozostaje ograniczeniem.
- [x] Audyt HALT: wejście/wznowienie jest testowane; długotrwała magistrala/piny bez śladu pozostają ograniczeniem.
- [x] Audyt WAIT po IRET: architektoniczny adres powrotu jest testowany; autonomiczny fetch zależny od hosta pozostaje ograniczeniem.
- [x] Audyt READY: Tw jest testowane; efekt `IAddressBus` w T3 nie jest odraczany i pozostaje ograniczeniem dla urządzeń ze skutkami ubocznymi.
- [x] Audyt RQ/GT: brak interfejsu i arbitrażu oznaczono jako niezaimplementowane.
- [x] Audyt MOV/POP SS: odroczenie po POP SS ma test; MOV SS nie jest obsługiwane, interakcja TF pozostaje ograniczeniem.
- [x] Audyt sekwencji prefiksów: brakujące kombinacje oraz ograniczenia interrupted-string opisano względem pinu i manuali.
- [x] Każde nierozstrzygnięte zachowanie jest w [rejestrze ograniczeń A10/A11](A12-audyt-ograniczen-a10-a11.md) wraz z dowodem i powodem pozostawienia luki.

### 6. Końcowy audyt A1–A12

- [x] Sprawdzić łańcuch branchy A0–A12: każdy następny branch zawiera poprzedni; plany w bazowym commicie `ffc9557`, zmiany implementacyjne w commitach właściwych etapów. Szczegóły i tipy: [audyt branchy](A12-audyt-branchy-i-commitow.md).
- [x] Użyć najnowszego pełnego raportu pinu 646 000/646 000 z kroku 9A (kod CPU od tego przebiegu bez zmian); końcowe buildy Debug/Release 0/0, testy Release 23 252/23 252, formatter verify, OpcodeInfo 512/512 i walidator manifestu 2 050/36 549.
- [x] Uzgodnić checklisty, macierz ISA, README wektorów, raport programu, branch audit i listę jawnych ograniczeń.
- [x] Zapisać procent na podstawie najnowszego pełnego przebiegu: **100% pinu (646 000/646 000)**; nie jest to procent wszystkich funkcji sprzętowych.

## Zasada aktualizacji po każdym punkcie

1. Zaznaczyć tylko faktycznie zakończony punkt `[x]`.
2. Dodać wiersz do historii z nazwą punktu, commitami i wynikiem weryfikacji.
3. Przepisać `passed`, `skipped` i procent z nowego pełnego raportu korpusu. Liczyć `passed / 646 000 × 100`.
4. Gdy punkt nie zmienia korpusu (np. audyt albo ROM), wpisać **100% bez zmiany** albo ostatni inny potwierdzony wynik z raportu pełnego; nie szacować wzrostu.

## Historia zamkniętych punktów

| data | zamknięty punkt | commity | passed / 646 000 | pokrycie |
|---|---|---|---:|---:|
| 2026-10-25 | Ostatni pełny checkpoint przed tą listą: C7 `/1–/7` | plan `ffee9f8`; implementacja `d24a4d1`; dokumentacja `aeb0699` | 584 464 | 90,5% |
| 2026-10-25 | `8F /1–/7`: implementacja i pełna weryfikacja | 1 461 fixture’ów; pełny plik 2 000/2 000; commit `c6d1b4f`; checkpoint `7a7d82d` | 586 226 | 90,7% |
| 2026-10-25 | Segment-prefiksowane LOOP E0–E3: pełne filtry, korpus i bramki | plan `e0b5209`; implementacja `5890848`; checkpoint dokumentacji `52f27e0` zapisany osobno | 590 169 | 91,4% |
| 2026-10-25 | Segment-prefiksowany JMP EB: pełny korpus i zbiorcze bramki jakości | raport `/tmp/a12-eb-final-corpus-report.json`; Release 22 887/22 887; manifest 2 026/35 829 | 594 203 | 92,0% |
| 2026-10-25 | Segment-prefiksowane LEA 8D: pełne źródło, korpus i bramki | 1 004/1 004 prefiksowane; plik 2 000/2 000; raport A12-prefixed-lea-8d-report.json; Release 22 968/22 968 | 595 207 | 92,1% |


## Zamknięty pakiet EB

- [x] Fixture’y lokalne: 80/80.
- [x] Segment-prefiksowany filtr EB: 1 018/1 018; pełny plik EB: 2 000/2 000.
- [x] Kontrola regresji E8–EA+EB: 4 034/4 034 prefiksowane, pełne źródła 8 000/8 000.
- [x] Korpus po EB: 594 203/646 000 (92,0%), 51 797 skipów, 0 błędów.
- [x] Bramki jakości i synchronizacja dokumentacji: Debug/Release 0 ostrzeżeń/błędów; Release 22 887/22 887; format, OpcodeInfo 512/512, manifest 2 026/35 829 i `git diff --check` zielone.
- [x] Osobne commity EB: implementacja `d3b57a9`, checkpoint dokumentacji `e1663ff`; raport i checklista zawierają hashe.

Postęp pakietu EB: **8/8 (100%)**. Szczegółowa osobna lista: [checklista EB](A12-checklista-pakietu-EB.md); plan: [EB](A12-prefixed-short-jump-eb-plan.md), raport: [EB](A12-prefixed-short-jump-eb-report.json).


## Bieżący pakiet LEA 8D

- [x] Plan przed kodem `b842f56`; cztery grupy prefiksów, 1 004 rekordy.
- [x] Fixture’y/test lokalny 80/80; pełny filtr prefiksowany 1 004/1 004 i plik 8D 2 000/2 000.
- [x] Pełny korpus 595 207/646 000 (92,1%), 50 793 skipped i 0 błędów; bramki jakości zielone (Release 22 968/22 968).
- [x] Osobny commit dokumentacji `330f1e3`, po implementacji `9b7cc06`; checklista pakietu **10/10 (100%)**: [szczegóły](A12-checklista-pakietu-prefixed-lea-8d.md).


## Zamknięty pakiet: segment-prefiksowane ALU

- [x] Inwentarz 49 782 rekordów ze źródłowego pinu i plan przed kodem zapisane.
- [x] Fixture’y 200/200, implementacja, filtr 49 782/49 782, pełne pliki 100 000/100 000 i korpus 644 989/646 000 przy zamknięciu.
- [x] Oddzielne commity planu `d790ce0`, implementacji `cab1fd2`, dokumentacji `fc9af2e`.

Checklista szczegółowa: [ALU `00–3D, 84/85`](A12-checklista-pakietu-prefixed-alu-00-3d-84-85.md), **11/11 (100%)**; pełny korpus 646 000/646 000 (100%).

| 2026-10-25 | Segment-prefiksowany CALL FAR 9A — pełny korpus | 1 011/1 011 prefiksowanych, 2 000/2 000 9A, 646 000/646 000 łącznie | 646 000 | 100% |
| 2026-10-25 | Oryginalny program integracyjny i audyt ograniczeń A10/A11; korpus bez zmiany | plan `79bb04c`; test `2dc4968`; niezależny test 1/1; ograniczenia [A12-audyt-ograniczen-a10-a11.md](A12-audyt-ograniczen-a10-a11.md) | 646 000 | 100% |
| 2026-10-25 | Wstępne końcowe bramki i audyt A0–A12 | Debug/Release 0 ostrzeżeń/błędów; Release 23 251/23 251; formatter verify; OpcodeInfo 512/512; manifest 2 050/36 549; [audyt branchy](A12-audyt-branchy-i-commitow.md) | 646 000 | 100% |
| 2026-10-25 | Zamknięcie niezmierzonego kryterium A2 i powtórzone zbiorcze bramki | `Intel8086HotPathAllocationTests`: 1 000 warmup, 10 000 pomiarowych wykonań `Tick()`, 0 B; implementacja testu `2ed7eec`; Debug/Release 0/0; Release 23 252/23 252; format, OpcodeInfo, manifest zielone; korpus bez zmiany | 646 000 | 100% |

## Bieżący pakiet: segment-prefiksowany CALL FAR 9A

- [x] Plan `c365fac`, fixture’y 80/80, profil cycle-exact i implementacja.
- [x] Filtr prefiksowany 1 011/1 011; pełny plik 9A 2 000/2 000.
- [x] Pełny korpus 646 000/646 000 (100%); pełny Release 23 250/23 250 i wszystkie bramki jakości zielone.
- [x] Dokumentacja/macierze A12 zsynchronizowane; package report [9A](A12-prefixed-call-far-9a-report.json).
- [x] Osobny commit implementacji `3198647`; plan `c365fac`.
- [x] Osobny commit dokumentacji `cba7033`; checklista pakietu 9/9 (100%).
