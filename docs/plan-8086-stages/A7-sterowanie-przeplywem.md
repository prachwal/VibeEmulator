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

## Kryteria akceptacji

- [ ] Wszystkie warunki true/false pokryte testami jednostkowymi i istniejącymi wektorami.
- [ ] Branch taken/not-taken daje zgodny IP/CS, queue i cykle.
- [ ] Near/far call/return porównuje komplet stack memory i sekwencję accessów.
- [ ] Przerwania programowe i IRET porównują IVT/stack z test fixture.
- [ ] A3–A7 próbki przechodzą; testy nie rozszerzają automatycznie twierdzeń na asynchroniczne IRQ/NMI.

## Pułapki

1. Względny displacement jest liczony od punktu wskazanego przez architekturę; oprzeć na fixture i manualu.
2. Flow control opróżnia/przeładowuje kolejkę; ustawienie samego `IP` nie wystarcza.
3. Warunki skoków czytają różne kombinacje flag; błąd jednej flagi rozchodzi się na wiele opcode.
4. `INT`/`IRET` są synchroniczne instrukcje, nie test dowolnego czasu przyjęcia zewnętrznej linii.

## Dziennik postępu

### 2026-10-23 — predykaty warunków Jcc

Dodano funkcję `Intel8086Condition.IsSatisfied` obejmującą kody 0–F. Pierwsze 20 rekordów z każdego pliku `70`–`7F` daje 320 przypadków; jeden ma displacement 0, więc `IP` końcowe nie rozróżnia taken/not-taken i nie może służyć jako oracle. Pozostałe **319/319** zgodne z predykatem helpera. Test porównuje wynik warunku z kierunkiem wynikającym z końcowego IP.

CPU nie dekoduje jeszcze Jcc; branch target, prefetch flush, CALL/RET/INT i cykle pozostają otwarte. Pełnych wektorów nie uruchomiono.

## Dziennik postępu

| data | rodzina | pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | warunki Jcc 70–7F (helper) | 16 plików / 319 jednoznacznych przypadków | 319/319 helper checks; CPU runner 0 | pełne wektory i transfery sterowania nieuruchomione |
