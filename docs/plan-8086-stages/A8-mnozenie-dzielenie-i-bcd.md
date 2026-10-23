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
- [ ] CWD (`99`): CPU runner, w tym przypadki różnych długości (5/6 T-states).
- [ ] MUL/IMUL i DIV/IDIV: formy rejestrowe i pamięciowe, przypadki graniczne oraz cykle.
- [ ] DAA/DAS/AAA/AAS/AAM/AAD: wykonanie przez CPU i określone flagi; AAM/AAD obecnie tylko helpery 80/80.
- [ ] ESC: fixture i zachowanie magistrali udokumentowane albo jawnie wyłączone na podstawie audytu źródła.
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

CBW (`98`) jest też zaimplementowane w CPU: próbka 20/20 porównuje kompletny stan końcowy, RAM, kolejkę i wszystkie T-states. Pozostałe helper-only wyniki A8 nie są wykonaniami przez CPU; nadal brakuje CWD, MUL/IMUL/DIV/IDIV, DAA/DAS/AAA/AAS, AAM/AAD jako instrukcji, oraz ESC. A8 pozostaje otwarty. AAM z radix 0 rzuca w helperze `DivideByZeroException`; ścieżka wyjątku procesora nadal do wdrożenia.

## Dziennik postępu

| data | rodzina | pass/manifest | wynik próbki | wyjątki/uwagi |
|---|---|---|---|---|
| 2026-10-23 | CBW/CWD/AAM/AAD (helpery) | 4 pliki / 80 przypadków | 80/80 helper checks; CPU CBW 20/20 | pełne wektory i pozostała arytmetyka nieuruchomione |
