# Etap A8 — mnożenie, dzielenie i BCD

Poziom: **II. Zestaw bazowy, część 6** · Zależność: A4–A7

## Zakres

`MUL`, `IMUL`, `DIV`, `IDIV`; konwersje `CBW`, `CWD`; korekty BCD/ASCII `DAA`, `DAS`, `AAA`, `AAS`, `AAM`, `AAD`; `ESC` (opcode escape do koprocesora) tylko w zakresie faktycznie obecnym w zestawie i udokumentowanego zachowania CPU. `BOUND` jest wyłączony z baseline; potwierdzić jego wprowadzenie po 8086 w manualu 80186 przed zamknięciem A1. Wszystkie formy dokładnie potwierdzić przed etapem na dokumentacji Intel oraz manifeście.

## Kolejność

1. Konwersje sign extension (`CBW`, `CWD`) z testami granic rejestrów i braku nieuzasadnionych zmian flag.
2. `MUL`/`IMUL` byte/word: wynik w parze rejestrów, flagi, overflow przypadki.
3. Obsłużyć REP przed IDIV przed aktywacją tych wektorów: README upstream opisuje zmianę znaku ilorazu i losowe dodanie prefiksu do części przypadków. Ten mechanizm poprzedza A9/A10; nie pomijać prefiksowanych rekordów.
4. `DIV`/`IDIV`: dzielnik 0, quotient overflow, wynik dodatni/ujemny, ścieżka wyjątku Type 0.
5. Dodać instrukcje BCD/ASCII pojedynczo, z macierzą wejściowych nibble i flag.
6. `ESC` jeśli fixture/test dokumentuje zachowanie magistrali; zachowanie zewnętrznego 8087 poza zakresem.

Flagi nieokreślone zapisane przez wyjątek na stosie pozostają częścią RAM i transferów. A1/A2 mają wybrać jawny profil: dokładne zachowanie badanego CPU albo maskowanie wyłącznie udowodnionych bitów flag, także w zidentyfikowanych zapisach stosu. Nie wyłączać porównania całej pamięci podczas wyjątku. Źródło: [README upstream, Per-Instruction Notes / Tricky Bits](https://github.com/SingleStepTests/8086/blob/e71c68d215a6bb8c356bd4cb3842de3bef345ca9/README.md).

## Testy i kryteria

- [ ] Lista instrukcji porównana z pierwotną dokumentacją 8086; żadnych opcode z 80186+.
- [ ] Każda forma z manifestu ma fixture i osobny test ekstremalnych danych.
- [x] CBW (`98`): próbka upstream 20/20 przez CPU runner; rejestry, IP, kolejka i każdy T-state zgodne.
- [x] CWD (`99`): próbka upstream 20/20 przez CPU runner; warianty 5/6 T-states, DX, IP, kolejka i każdy T-state zgodne.
- [x] Helpery MUL/IMUL byte/word: 80/80 próbek rejestrowych; wynik podwójnej szerokości dla word i CF/OF zgodne z metadata.
- [x] `MUL` register-direct `/4` (`F6/F7`): 40/40 z pełnym stanem, kolejką i T-states.
- [x] Rejestrowe `IMUL /5` (`F6/F7`): 40/40 ze stanem, flagami, kolejką i T-states.
- [ ] Pamięciowe `MUL/IMUL`: przypadki graniczne i wszystkie cykle.
- [x] Helpery DIV/IDIV byte/word: 80/80 próbek rejestrowych, quotient/remainder oraz klasyfikacja divide fault.
- [ ] DIV/IDIV CPU: formy rejestrowe i pamięciowe, przypadki graniczne, wyjątek Type 0 i wszystkie cykle.
- [x] DAA/DAS/AAA/AAS (`27/2F/37/3F`): próbki 80/80 przez CPU, stan, kolejka i każdy T-state; ignorowane flagi zgodne z metadata upstream.
- [x] AAD (`D5`): próbka CPU 20/20; natychmiastowy radix, AX/FLAGS, kolejka i T-states zgodne.
- [ ] AAM (`D4`): helper 20/20; wykonanie CPU, divide error radix 0, cykle i immediate nieukończone.
- [ ] ESC: fixture i zachowanie magistrali udokumentowane albo jawnie wyłączone na podstawie audytu źródła.
- [x] Cała aktywna próbka CPU A3–A8: 6 738/6 738; Intel8086.Tests: 7 749/7 749. Pełne upstream zestawy A8 nieuruchomione.
- [ ] Wyjątek divide testuje wektorowanie, stos oraz końcowy fetch wskazany przez fixture.
- [ ] Flagi undefined są jawnie maskowane tylko wtedy, gdy źródło to uzasadnia.
- [ ] Pełne testy nie blokują się: per-case timeout i raport o odrzuconym/przekroczonym przypadku.

## Pułapki

1. Wyniki multiply są szersze niż operand; wynik ucięty do jednego rejestru to błąd.
2. Dzielenie ze znakiem wymaga właściwego rozszerzenia dividend, a nie tylko rzutowania dzielnika.
3. Divide exception zmienia przepływ wykonania; zwykłe zakończenie instrukcji nie jest oczekiwaną granicą testu.
4. BCD flags mogą być częściowo nieokreślone; nie porównywać ich bez źródła.
5. `ESC` nie implementuje instrukcji koprocesora; emuluje wyłącznie zachowanie CPU, które da się udowodnić.

## Dziennik postępu

### 2026-10-23 — CBW/CWD i AAM/AAD (helpery)

Dodano czyste helpery konwersji `CBW`/`CWD` oraz wyniku `AAM`/`AAD`. Pierwsze 20 rekordów z każdego pliku `98`, `99`, `D4`, `D5` daje **80/80** zgodności wartości rejestrów i sprawdzanych flag. Dla `D4`/`D5` ignored mask `0x0811` odpowiada metadata upstream `flags-mask=0xF7EE` (nieokreślone CF/AF/OF).

CBW (`98`) i CWD (`99`) są zaimplementowane w CPU: próbki po 20/20 porównują stan końcowy, RAM, kolejkę i wszystkie T-states. DAA/DAS/AAA/AAS (`27/2F/37/3F`) przechodzą 80/80 przez CPU. Helpery MUL/IMUL byte/word oraz DIV/IDIV byte/word przechodzą po 80/80 próbek rejestrowych. Byte DIV używa pełnej dzielnej AX. Divide fault helpera rozpoznaje zero dzielnika i quotient overflow, ale CPU nie wektoryzuje jeszcze Type 0. Rejestrowe unsigned `MUL /4` (`F6/F7`) przechodzi 40/40 cycle-exact; `memory MUL/IMUL oraz DIV/IDIV CPU nie są jeszcze zrealizowane; nadal brakuje AAM jako instrukcji oraz ESC; AAD `D5` jest zweryfikowane CPU 20/20. A8 pozostaje otwarty.

## Dziennik postępu

| data | rodzina | pass/manifest | wynik próbki | wyjątki/uwagi |
|---|---|---|---|---|
| 2026-10-24 | A8: konwersje, korekty, MUL/IMUL/DIV/IDIV | 16 plików / 320 przypadków | helpery 320/320; CPU 220/220 dla CBW/CWD/DAA/DAS/AAA/AAS/MUL/IMUL /4,/5/AAD | pełne upstream wektory nieuruchomione; CPU MUL/IMUL /4,/5 80/80, pozostałe memory MUL/IMUL/DIV/ESC/AAM CPU otwarte |
