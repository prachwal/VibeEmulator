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

Każdy podetap aktywuje tylko formy potwierdzone w przypiętym manifeście 8086. Przed implementacją rodziny dopisać wektorową próbkę, przejścia dekodera i dokładne filtry; `A7Branch` obejmuje obecnie wykonanie Jcc w CPU.

## Testy i kryteria

- [x] Wszystkie 16 warunków ma przypadki taken i not-taken w próbce; predykat helpera zgadza się w 319/319 jednoznacznych wektorach. Wykonanie CPU sprawdza osobne kryterium niżej.
- [x] Jcc short taken/not-taken daje zgodny IP/CS, kolejkę, RAM i wszystkie T-states w próbce 320/320.
- [ ] Near/far JMP/CALL/RET porównuje kompletną pamięć stosu i sekwencję dostępów.
- [ ] Przerwania programowe i IRET porównują IVT/stack z fixture.
- [x] Aktywne próbki A3–A7 przechodzą 6 118/6 118, 0 skipów; testy Intel8086: 6 809/6 809. Pełnych źródłowych zestawów nie uruchomiono.

## Pułapki

1. Względny displacement jest liczony od punktu wskazanego przez architekturę; oprzeć na fixture i manualu.
2. Flow control opróżnia/przeładowuje kolejkę; ustawienie samego `IP` nie wystarcza.
3. Warunki skoków czytają różne kombinacje flag; błąd jednej flagi rozchodzi się na wiele opcode.
4. `INT`/`IRET` są synchroniczne instrukcje, nie test dowolnego czasu przyjęcia zewnętrznej linii.

## Dziennik postępu

### 2026-10-23 — predykaty warunków Jcc

Dodano funkcję `Intel8086Condition.IsSatisfied` obejmującą kody 0–F. Pierwsze 20 rekordów z każdego pliku `70`–`7F` daje 320 przypadków; jeden ma displacement 0, więc `IP` końcowe nie rozróżnia taken/not-taken i nie może służyć jako oracle. Pozostałe **319/319** zgodne z predykatem helpera. Test porównuje wynik warunku z kierunkiem wynikającym z końcowego IP.

W chwili raportu helpera CPU nie dekodował jeszcze Jcc; branch target, prefetch flush, CALL/RET/INT i cykle pozostawały otwarte. Pełnych wektorów nie uruchomiono.

### 2026-10-24 — cycle-exact Jcc short

Dodano wykonanie `70`–`7F` taken/not-taken z signed displacement, flush/reload kolejki, spekulacyjnym fetch i target fetch. Aktywne próbki CPU: 320/320; cała próbka CPU A3–A7: 6 118/6 118, 0 skipów; testy Intel8086: 6 809/6 809. Pełnych plików źródłowych Jcc nie uruchomiono.

## Dziennik postępu

| data | rodzina | pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | warunki Jcc 70–7F (helper) | 16 plików / 319 jednoznacznych przypadków | 319/319 helper checks; CPU runner 0 | pełne wektory i transfery sterowania nieuruchomione |
| 2026-10-24 | Jcc short taken/not-taken | 16 plików / 320 przypadków | 320/320; aktywna próbka 6 118/6 118; Intel8086.Tests 6 809/6 809 | pełne źródłowe zestawy nieuruchomione; dalsze A7 rodziny otwarte |
