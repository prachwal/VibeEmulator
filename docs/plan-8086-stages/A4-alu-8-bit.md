# Etap A4 — ALU 8-bit i flagi

Poziom: **II. Zestaw bazowy, część 2** · Zależność: A3

## Zakres

Grupy arytmetyczne bajtów: `ADD`, `ADC`, `SUB`, `SBB`, `CMP`; logiczne: `AND`, `OR`, `XOR`, `TEST`. Uwzględnić warianty rejestr/rejestr, rejestr/pamięć, accumulator/immediate i grupy immediate do r/m obecne w ISA 8086. Przypisać konkretne bajty z manifestu A1, nie z pamięci.

## Kolejność

1. Zdefiniować prywatne, testowalne operacje ALU z operand width, carry-in i wynikiem/flag delta.
2. Dodać `ADD` i `ADC`; weryfikować wynik i flagi dla 0, 1, wartości granicznych, carry-in i overflow.
3. Dodać `SUB`/`SBB`/`CMP`; `CMP` nie zapisuje wyniku do operandu.
4. Dodać logiczne `AND`/`OR`/`XOR`/`TEST`, w tym reguły nieokreślonych flag według przypiętego źródła i dokumentacji producenta.
5. Dodać dekodowanie grup immediate ModR/M do istniejącego adresowania A3.
6. Zweryfikować przypadki pamięciowy read/modify/write i cykle, nie tylko końcowy wynik.

## Testy i kryteria

- [ ] Tabela expected flags oparta na README upstream/Intel manual; jawnie wymienić maskę porównania.
- [ ] Testy jednostkowe dla carry, borrow, auxiliary carry, parity, zero, sign, overflow.
- [ ] Każdy opcode/form z manifestu A4 w próbce i teście; `CMP`/`TEST` asercjonują brak zapisu.
- [ ] Cała próbka A3+A4 zielona; mismatch ma nazwę fixture, opcode, cycle i expected/actual.
- [ ] Bez wyjątków Skip bez przykładu, różnicy i odnośnika do etapu naprawy.

## Pułapki

1. Nie przenosić algorytmu flag z Mos6502/Z80: szerokość i określoność flag są specyficzne dla x86.
2. Nie maskować flag „na oko”; jeśli generator nie określa ich, udokumentować dowód z metadanych lub manuala.
3. `CMP` i `TEST` wykonują obliczenie flagowe, ale nie zapisują wyniku.
4. Immediate może być sign-extended w niektórych formach słownych; rozstrzygnąć źródłem w A6, a tu nie uogólniać reguły bajtowej.

## Dziennik postępu

### 2026-10-23 — primitive ALU i accumulator-immediate oracle

Dodano czystą funkcję 8-bit `Intel8086Alu.Execute` dla ADD/ADC/SUB/SBB/CMP/AND/OR/XOR/TEST. Wektorowe testy helpera korzystają z pierwszych 20 rekordów upstream dla form AL-immediate: `04`, `14`, `1C`, `2C`, `3C`, `24`, `0C`, `34` (160 rekordów). Dla wpisów z metadata `flags-mask=0xFFEF` test ignoruje wyłącznie AF (bit 4); rejestry i pozostałe określone flagi są porównywane. Wynik: **160/160** helper checks.

Zestawy `Vectors/A4Alu/` testują helper, nie cykle CPU. W kolejnym podetapie podłączono osiem form accumulator-immediate do rdzenia i aktywowano osobne, nieprefiksowane próbki end-to-end. A4 nadal pozostaje otwarty: formy ModR/M, pamięciowe i TEST poza AL nie są zaimplementowane.

## Dziennik postępu

| data | rodzina | pass/manifest | testy próbki | pełne poza repo |
|---|---|---|---|---|
| 2026-10-23 | 8 operacji, formy AL-immediate | helper: 160/160 | CPU runner: 0 form A4 | pełne wektory nie uruchomiono; integracja core otwarta |
| 2026-10-23 | AL,imm8 (`04`,`0C`,`14`,`1C`,`24`,`2C`,`34`,`3C`), bez prefiksów | 160 próbek CPU, pierwsze 20 rekordów bez prefiksu na opcode | CPU runner 160/160; pełne źródłowe podzbiory 8 029/8 029; 0 skip/mismatch | pełen stan, flagi, kolejka i cykle zgodne; prefiksy i pozostałe formy A4 otwarte |
