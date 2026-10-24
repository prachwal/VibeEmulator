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
- [x] Każda aktywna forma ma fixture i testy brzegowe, w tym dzielenie przez zero, overflow i minimalny iloraz ze znakiem.
- [x] CBW (`98`): próbka upstream 20/20 przez CPU runner; rejestry, IP, kolejka i każdy T-state zgodne.
- [x] CWD (`99`): próbka upstream 20/20 przez CPU runner; warianty 5/6 T-states, DX, IP, kolejka i każdy T-state zgodne.
- [x] Helpery MUL/IMUL byte/word: 80/80 próbek rejestrowych; wynik podwójnej szerokości dla word i CF/OF zgodne z metadata.
- [x] `MUL` register-direct `/4` (`F6/F7`): 40/40 z pełnym stanem, kolejką i T-states.
- [x] Rejestrowe `IMUL /5` (`F6/F7`): 40/40 ze stanem, flagami, kolejką i T-states.
- [x] Pamięciowe `MUL/IMUL`: próbki 80/80 przez CPU, odczyty bajtów/słów, wynik i cykle.
- [x] Helpery DIV/IDIV byte/word: 80/80 próbek rejestrowych, quotient/remainder oraz klasyfikacja divide fault.
- [x] `DIV/IDIV` CPU: próbki rejestrowe i pamięciowe 160/160; dzielnik 0 i overflow przechodzą przez Type 0, stos, wektor i kolejkę.
- [x] DAA/DAS/AAA/AAS (`27/2F/37/3F`): próbki 80/80 przez CPU, stan, kolejka i każdy T-state; ignorowane flagi zgodne z metadata upstream.
- [x] AAD (`D5`): próbka CPU 20/20; natychmiastowy radix, AX/FLAGS, kolejka i T-states zgodne.
- [x] AAM (`D4`): 20/20 zwykłych przypadków i 12/12 radix 0 przez CPU; radix 0 wektoryzuje Type 0.
- [x] ESC (`D8`–`DF`): 160/160; wykonanie CPU nie symuluje koprocesora 8087.
- [x] Dedykowana próbka A8 CPU: 793/793; Intel8086.Tests: 8 234/8 234. Aktywny runner A3–A8: 6 738/6 738. Pełne upstream zestawy A8 nieuruchomione.
- [x] Wyjątek divide testuje wektorowanie, stos oraz końcowy fetch wskazany przez fixture.
- [x] Flagi undefined są maskowane wyłącznie maskami przypiętych metadanych wektorów.
- [x] Runner ma limit T-state na przypadek oraz raportuje timeout i przypadki odrzucone.

## Pułapki

1. Wyniki multiply są szersze niż operand; wynik ucięty do jednego rejestru to błąd.
2. Dzielenie ze znakiem wymaga właściwego rozszerzenia dividend, a nie tylko rzutowania dzielnika.
3. Divide exception zmienia przepływ wykonania; zwykłe zakończenie instrukcji nie jest oczekiwaną granicą testu.
4. BCD flags mogą być częściowo nieokreślone; nie porównywać ich bez źródła.
5. `ESC` nie implementuje instrukcji koprocesora; emuluje wyłącznie zachowanie CPU, które da się udowodnić.

## Dziennik postępu

### 2026-10-24 — A8 ukończony w zakresie aktywnych próbek

Próbka z manifestu A8 przechodzi 793/793. CPU wykonuje CBW/CWD, korekty BCD/ASCII, AAM/AAD, rejestrowe i pamięciowe MUL/IMUL/DIV/IDIV, wyjątek Type 0 (w tym dzielenie przez zero i overflow), prefiksowane próbki IDIV oraz ESC. Testy Intel8086 przechodzą 8 234/8 234; aktywny runner A3–A8 przechodzi 6 738/6 738. Pełnych upstream zestawów nie uruchomiono.

Wyniki obejmują próbki przypiętego zestawu, nie wszystkie 2 000 przypadków każdego opkodu. Bity flag porównywane są z maską z manifestu. `ESC` sprawdza wyłącznie zachowanie CPU/magistrali, nie wykonuje instrukcji 8087.

## Dziennik postępu

| data | rodzina | pass/manifest | wynik próbki | wyjątki/uwagi |
|---|---|---|---|---|
| 2026-10-24 | A8: arytmetyka specjalna, Type 0 i ESC | 35 plików / 793 przypadki | A8 CPU 793/793; Intel8086.Tests 8 234/8 234; runner A3–A8 6 738/6 738 | pełne upstream zestawy nieuruchomione; zakres ograniczony do przypiętych próbek |
