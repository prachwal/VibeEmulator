# Etap A11 — reset, przerwania i stany procesora

Poziom: **IV. Zachowanie procesora poza pojedynczą instrukcją** · Wektory SST: ograniczone

Wektory SingleStepTests/8086 nie ćwiczą wszystkich wejść asynchronicznych. A11 wymaga osobnych scenariuszy i dokumentacji producenta; status „brak mismatchów SST” nie jest dowodem dla resetu, INTR, NMI ani TF.

## Zakres

- RESET i stan startowy: wektor resetu, rejestry, kolejka oraz pierwszy fetch.
- `INTR`, `NMI`, `INTA`, `IF`, `TF` i granice przyjęcia sygnału.
- `HLT`, wyjście z HLT przez reset/przerwanie, pin/status bus.
- `WAIT`/`TEST` w modelu 8086 i połączenie z coprocessorem, wyłącznie zakresie zweryfikowanym.
- READY/wait states i arbitraż magistrali tylko po wskazaniu wejść/wyjść właściwych dla Maximum Mode w dokumentacji producenta. Nie przejmować HOLD/HLDA z innego trybu bez weryfikacji.
- REP restart/resume w kontekście przerwań, jeśli źródła określają dokładną architekturę.

README upstream jawnie wyklucza `WAIT` (9B) i `HLT` (F4); nie czekać na ich pliki podczas audytu A10. Ich semantyka i cykle wymagają źródła producenta oraz osobnych testów.

## Plan testów

1. Zbudować scenariusz deterministyczny, który ustawia linie wejściowe przez jawny interfejs; bez czasu ściennego.
2. Każdy test opisuje cykl asercji/zwolnienia sygnału, oczekiwany cykl akceptacji i źródło tej granicy.
3. IRQ: test IF clear/set, wejście po instrukcji, INTA i frame na stosie.
4. NMI: test zbocza/warunku aktywacji według dokumentacji, niezależny od IF.
5. Granice po STI i zapisie SS: źródłowe scenariusze odroczenia przerwań oraz interakcji TF, powiązane z instrukcjami A5/A7. Dokładne reguły pozostają niezweryfikowane do odczytania manuala.
6. TF: pojedynczy krok i stacking state po instrukcji; oddzielić od zwykłego wektora `INT`.
7. HLT: test utrzymania stanu i każdego udokumentowanego sposobu wznowienia.
8. RESET: asercja w różnych fazach, jeśli manual określa; test resetu symulatora nie jest automatycznie równoznaczny z resetem pinowym.
9. READY/arbitraż/WAIT: włączać tylko po rozstrzygnięciu ich wymagań oraz obserwowalnych pinów dla modelu Maximum Mode.

## Kryteria akceptacji

- [ ] Każdy sygnał ma opis poziomu/zbocza, cyklu próbkowania i źródła.
- [ ] Testy obejmują stos, IVT, IF/TF, kolejkę i bus cycles.
- [ ] Brakujące źródła zostają oznaczone „niezweryfikowane”, bez deklaracji pełnej zgodności.
- [ ] Linia zewnętrzna nie zmienia się nondeterministycznie w testach.
- [ ] Kontrakt `IInterruptLines` nie jest rozszerzony globalnie bez testów regresyjnych dla istniejących CPU.

## Pułapki

1. Instrukcja `INT n` to nie to samo co asynchroniczne `INTR`.
2. NMI/IRQ mogą zależeć od granicy instrukcji i bieżącego fetch/prefetch; nie przyjmować „natychmiast”.
3. Kształt interrupt frame i stos trzeba ustalić z manuala i potwierdzić independently.
4. WAIT bez zewnętrznego coprocessora może wymagać modelu wejścia TEST; nie zastępować go pustym NOP-em.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

| data | sygnał/scenariusz | testy | źródło | status |
|---|---|---|---|---|
| — | — | — | do przypięcia | niezweryfikowane |
