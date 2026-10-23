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

| data | rodzina | pass/manifest | testy próbki | pełne poza repo |
|---|---|---|---|---|
| — | — | — | nie uruchomiono | nie uruchomiono |
