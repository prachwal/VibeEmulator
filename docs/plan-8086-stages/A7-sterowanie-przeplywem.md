# Etap A7 — sterowanie przepływem

Poziom: **II. Zestaw bazowy, część 5** · Zależność: A3/A5/A6

## Zakres

Warunkowe skoki `Jcc`; `JMP` short/near/far; `CALL` near/far; `RET` near/far z korektą stosu; `LOOP`, `LOOPE/LOOPZ`, `LOOPNE/LOOPNZ`, `JCXZ`; `INT`, `INT3`, `INTO`, `IRET`; sterowanie flagami i kierunkiem: `CLC/STC/CMC`, `CLD/STD`, `CLI/STI`, `LAHF/SAHF`. Finalne formy muszą występować w 8086 i zostać sprawdzone w źródle A1/Intel.

## Kolejność

1. Dodać dekodowanie warunków jako jedną tablicę/funkcję pure z testem wszystkich kodów warunku.
2. `Jcc` i short/near branch: rozmiar displacement, fetch/prefetch flush oraz kierunek przepływu.
3. `JMP` near/far oraz aktualizacja CS:IP; prefetch queue flush na transferze sterowania.
4. `CALL` i `RET` near/far: stack frames, kolejność push/pop i stan CS/IP.
5. `LOOP*`/`JCXZ`: zachowanie CX, flags i branch taken/not-taken.
6. Interrupt instructions przez tablicę IVT; `IRET` odtwarza odpowiednie pola stosu.
7. Flagi kontrolne i transfer flag register; testować wpływ IF/TF z uwzględnieniem zakresu fixture.

## Plan wykonania A7

1. **A7.1 — predykaty Jcc (`70`–`7F`):** helper pure dla czterech flag OF/CF/ZF/SF/PF; próbki 20 rekordów/opkod, oba kierunki taken/not-taken, oracle wyprowadzony z displacement i finalnego IP. Zrealizowano helper (319/319); przypadek zerowego displacementu nie rozstrzyga kierunku.
2. **A7.2 — wykonanie Jcc short (`70`–`7F`):** dekodowanie displacementu signed 8-bit, stan taken/not-taken, flush/reload kolejki i cykle; aktywować próbki w CPU runnerze i porównywać każdy T-state.
3. **A7.3 — JMP (`EB`, `E9`, `EA`):** short, near relative i far immediate; IP/CS oraz flush kolejki.
4. **A7.4 — CALL/RET (`E8`, `9A`, `C3`, `C2`, `CB`, `CA`):** near/far ramki stosu, kolejność push/pop, korekta SP i cykle.
5. **A7.5 — LOOP/JCXZ (`E0`–`E3`):** CX, ZF dla LOOPZ/LOOPNZ oraz oba kierunki przepływu.
6. **A7.6 — INT/IRET (`CD`, `CC`, `CE`, `CF`):** IVT, zapis FLAGS/CS/IP, wejście na handler i odtworzenie przez IRET; zakres tylko synchroniczny fixture.
7. **A7.7 — transfer i kontrola FLAGS (`F5`, `F8`, `F9`, `FA`, `FB`, `FC`, `FD`, `9E`, `9F`):** zmieniane i zachowane flagi, pełny stan oraz cykle.

Każdy podetap aktywuje tylko formy potwierdzone w przypiętym manifeście 8086. Przed implementacją rodziny dopisać wektorową próbkę, przejścia dekodera i dokładne filtry; `A7Branch` i `A7Jump` obejmują obecnie wykonanie Jcc, JMP immediate i near CALL/RET w CPU.

## Testy i kryteria

- [x] Wszystkie 16 warunków ma przypadki taken i not-taken w próbce; predykat helpera zgadza się w 319/319 jednoznacznych wektorach. Wykonanie CPU sprawdza osobne kryterium niżej.
- [x] Jcc short taken/not-taken daje zgodny IP/CS, kolejkę, RAM i wszystkie T-states w próbce 320/320.
- [x] JMP short/near/far immediate `EB`/`E9`/`EA`: 60/60, pełny stan, RAM, kolejka i wszystkie T-states.
- [x] Próbki CALL/RET `E8`, `9A`, `C3`, `C2`, `CB`, `CA`: 120 rekordów z przypiętego źródła, przygotowane, lecz nieaktywne.
- [x] Near CALL `E8` oraz near RET `C3`/`C2`: próbki 60/60; porównanie pełnego stanu, stosu, kolejki i wszystkich T-states.
- [x] Far CALL/RET `9A`, `CB`, `CA`: próbki 60/60; porównanie pełnego stanu, stosu, kolejki i wszystkich T-states.
- [x] LOOP/LOOPE/LOOPNE/JCXZ (`E0`–`E3`): 80/80, CX/ZF, IP, kolejka i wszystkie T-states zgodne.
- [x] IRET (`CF`): próbka 20/20, odczyt IP/CS/FLAGS ze stosu, SP, kolejka i wszystkie T-states zgodne.
- [x] INT/INT3/INTO (`CD`, `CC`, `CE`): 60/60, IVT, pełna ramka stosu, flagi, kolejka i T-states zgodne.
- [x] Próbki `CD`, `CC`, `CE`, `CF`: po 20 rekordów przygotowane z przypiętego SHA, nieaktywne do implementacji i kontroli ramki stosu/IVT.
- [x] Transfery i sterowanie FLAGS (`F5`, `F8`–`FD`, `9E`, `9F`): 180/180; zachowanie innych flag i wszystkie T-states porównane.
- [x] Aktywne próbki A3–A7 przechodzą 6 638/6 638, 0 skipów; testy Intel8086: 7 329/7 329. Pełnych źródłowych zestawów nie uruchomiono.

## Pułapki

1. Względny displacement jest liczony od punktu wskazanego przez architekturę; oprzeć na fixture i manualu.
2. Flow control opróżnia/przeładowuje kolejkę; ustawienie samego `IP` nie wystarcza.
3. Warunki skoków czytają różne kombinacje flag; błąd jednej flagi rozchodzi się na wiele opcode.
4. `INT`/`IRET` są synchroniczne instrukcje, nie test dowolnego czasu przyjęcia zewnętrznej linii.

## Dziennik postępu

### 2026-10-23 — predykaty warunków Jcc

Dodano funkcję `Intel8086Condition.IsSatisfied` obejmującą kody 0–F. Pierwsze 20 rekordów z każdego pliku `70`–`7F` daje 320 przypadków; jeden ma displacement 0, więc `IP` końcowe nie rozróżnia taken/not-taken i nie może służyć jako oracle. Pozostałe **319/319** zgodne z predykatem helpera. Test porównuje wynik warunku z kierunkiem wynikającym z końcowego IP.

W chwili raportu helpera CPU nie dekodował jeszcze Jcc; branch target, prefetch flush, CALL/RET/INT i cykle pozostawały otwarte. Pełnych wektorów nie uruchomiono.

### 2026-10-24 — JMP immediate short/near/far

Dodano wykonanie nieprefiksowanych `EB`, `E9` i `EA`; próbki po 20 wektorów na opkod przechodzą 60/60 z porównaniem pełnego stanu i wszystkich T-states. Cały aktywny runner: 6 638/6 638, 0 skipów; testy Intel8086: 7 329/7 329. Pełne pliki źródłowe JMP i prefiksy nie były uruchomione.

### 2026-10-24 — cycle-exact Jcc short

Dodano wykonanie `70`–`7F` taken/not-taken z signed displacement, flush/reload kolejki, spekulacyjnym fetch i target fetch. Aktywne próbki CPU: 320/320; cała próbka CPU A3–A7: 6 118/6 118, 0 skipów; testy Intel8086: 6 809/6 809. Pełnych plików źródłowych Jcc nie uruchomiono.

## Dziennik postępu

| data | rodzina | pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | warunki Jcc 70–7F (helper) | 16 plików / 319 jednoznacznych przypadków | 319/319 helper checks; CPU runner 0 | pełne wektory i transfery sterowania nieuruchomione |
| 2026-10-24 | Jcc short taken/not-taken | 16 plików / 320 przypadków | 320/320; aktywna próbka 6 178/6 178; Intel8086.Tests 6 869/6 869 | pełne źródłowe zestawy nieuruchomione; dalsze A7 rodziny otwarte |
| 2026-10-24 | JMP immediate `EB`/`E9`/`EA` | 3 pliki / 60 przypadków | 60/60; aktywna próbka 6 238/6 238; Intel8086.Tests 6 929/6 929 | nieprefiksowane formy; pełne źródłowe zestawy nieuruchomione |
| 2026-10-24 | near CALL/RET `E8`, `C3`, `C2` | 3 pliki / 60 przypadków | 60/60; aktywna próbka 6 298/6 298; Intel8086.Tests 6 989/6 989 | stos i cykle T-state porównane |
| 2026-10-24 | far CALL/RET `9A`, `CB`, `CA` | 3 pliki / 60 przypadków | 60/60; aktywna próbka 6 378/6 378; Intel8086.Tests 7 069/7 069 | stos, CS:IP i cykle T-state porównane |
| 2026-10-24 | LOOP/JCXZ `E0`–`E3` | 4 pliki / 80 przypadków | 80/80; aktywna próbka 6 398/6 398; Intel8086.Tests 7 089/7 089 | wszystkie T-states, CX i ZF porównane |
| 2026-10-24 | IRET `CF` | 1 plik / 20 przypadków | 20/20; aktywna próbka 6 458/6 458; Intel8086.Tests 7 149/7 149 | IP/CS/FLAGS, stos, queue i T-states zgodne |
| 2026-10-24 | INT/INT3/INTO `CD`/`CC`/`CE` | 3 pliki / 60 przypadków | 60/60; aktywna próbka 6 638/6 638; Intel8086.Tests 7 329/7 329 | IVT, zapis FLAGS/CS/IP i handler fetch porównane |
| 2026-10-24 | transfer FLAGS `F5`, `F8`–`FD`, `9E`, `9F` | 9 plików / 180 przypadków | 180/180; aktywna próbka 6 638/6 638; Intel8086.Tests 7 329/7 329 | flagi zachowane/zmienione zgodne; T-states zgodne |
| 2026-10-24 | próbki CALL/RET | `E8`, `9A`, `C3`, `C2`, `CB`, `CA`, po 20 | 120/120 przygotowanych z przypiętego źródła | nieaktywne; implementacja i cykle otwarte |

Próbki A7.6 `CD`, `CC`, `CE`, `CF` są przygotowane; zakres cykli w źródle rozciąga się od 4 do 66 T-states. Nie są aktywne i nie wpływają na liczniki weryfikacji.

W A7.6 aktywowano `CD`, `CC`, `CE`, `CF`: wejście przerwania i IRET przechodzą pełną próbkę 80/80 cykl-po-cyklu. Pełnych źródłowych zestawów nie uruchomiono.

A7.7: `CLC/STC/CMC`, `CLD/STD`, `CLI/STI`, `LAHF/SAHF` przechodzą 180/180 w próbkach. `SAHF` ma 4 T-states, pozostałe formy w tej próbce 2; wszystkie porównania obejmują FLAGS i kolejkę.
