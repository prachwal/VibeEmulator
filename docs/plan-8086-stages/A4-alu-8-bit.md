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

- [x] Tabela flag oparta na manifestach/metadanych przypiętego upstream; maski niezdefiniowanych flag helpera jawnie zapisane w `A4Alu/manifest.json` (AF `0x0010` dla operacji logicznych), a pełny runner CPU porównuje końcowe FLAGS dokładnie.
- [x] Testy jednostkowe i wektorowe pokrywają carry/borrow, auxiliary carry, parity, zero, sign i overflow.
- [x] Każdy z 25 aktywnych kodowań/form A4 ma próbkę i test; przypadki `CMP`/`TEST` porównują pełny stan końcowy i nie zapisują rejestru docelowego.
- [x] Cała aktywna próbka A3+A4 przechodzi; mismatch zawiera tożsamość fixture, hash i pierwszą różnicę cyklu/pola.
- [x] Brak testów Skip w aktywnym zakresie A3+A4.
- [ ] Wszystkie formy A4 z pełnego manifestu źródła — w tym operandy pamięciowe i grupy immediate do r/m — są zaimplementowane i zweryfikowane.

## Pułapki

1. Nie przenosić algorytmu flag z Mos6502/Z80: szerokość i określoność flag są specyficzne dla x86.
2. Nie maskować flag „na oko”; jeśli generator nie określa ich, udokumentować dowód z metadanych lub manuala.
3. `CMP` i `TEST` wykonują obliczenie flagowe, ale nie zapisują wyniku.
4. Immediate może być sign-extended w niektórych formach słownych; rozstrzygnąć źródłem w A6, a tu nie uogólniać reguły bajtowej.

## Kolejny podetap przed implementacją

1. Dodać `ADD r/m8,r8` (`00`) i `ADD r8,r/m8` (`02`) dla `mod=11`; fixture wybiera pierwsze 20 rekordów upstream z dokładnym opcode na początku `bytes` i rejestrowym ModR/M.
2. Z próbek ustalić T1/T2/T3, końcowe pobranie prefetch i kolejkę dla różnych długości kolejki wejściowej.
3. Użyć `Intel8086Alu.Execute`, nie duplikować obliczania flag. Runner ma porównać wszystkie rejestry, flagi, RAM, kolejkę i pola T-state.
4. Uruchomić cały przefiltrowany podzbiór źródłowy `00`/`02` mod=11; pozostałe kierunki/operacje i pamięć nadal pozostają osobnymi grupami.

## Dziennik postępu

### 2026-10-23 — primitive ALU i accumulator-immediate oracle

Dodano czystą funkcję 8-bit `Intel8086Alu.Execute` dla ADD/ADC/SUB/SBB/CMP/AND/OR/XOR/TEST. Wektorowe testy helpera korzystają z pierwszych 20 rekordów upstream dla form AL-immediate: `04`, `14`, `1C`, `2C`, `3C`, `24`, `0C`, `34` (160 rekordów). Dla wpisów z metadata `flags-mask=0xFFEF` test ignoruje wyłącznie AF (bit 4); rejestry i pozostałe określone flagi są porównywane. Wynik: **160/160** helper checks.

Zestawy `Vectors/A4Alu/` testują helper, nie cykle CPU. W kolejnym podetapie podłączono osiem form accumulator-immediate do rdzenia i aktywowano osobne, nieprefiksowane próbki end-to-end. A4 nadal pozostaje otwarty: formy pamięciowe, immediate do r/m oraz warianty z prefiksami nie są zaimplementowane.

### 2026-10-23 — ALU byte ModR/M, register-direct

Rdzeń wykonuje `ADD/OR/ADC/SBB/AND/SUB/XOR/CMP r/m8,r8` (`00/08/10/18/20/28/30/38`), warianty kierunku do rejestru (`02/0A/12/1A/22/2A/32/3A`) oraz `TEST r/m8,r8` (`84`) przy `mod=11`. Aktywowano pierwsze 20 pasujących upstream rekordów dla każdego z 17 kodowań (340 przypadków); pełny runner porównał rejestry, flagi, RAM, kolejkę i wszystkie T-states: **340/340**. Pełne przefiltrowane źródłowe podzbiory zawierają **4 355** rekordów i przeszły bez skipów/mismatchy. To nie obejmuje operandów pamięciowych ani immediate do r/m.

## Dziennik postępu

| data | rodzina | pass/manifest | testy próbki | pełne poza repo |
|---|---|---|---|---|
| 2026-10-23 | 8 operacji, formy AL-immediate | helper: 160/160 | CPU runner: 0 form A4 | pełne wektory nie uruchomiono; integracja core otwarta |
| 2026-10-23 | AL,imm8 (`04`,`0C`,`14`,`1C`,`24`,`2C`,`34`,`3C`), bez prefiksów | 160 próbek CPU, pierwsze 20 rekordów bez prefiksu na opcode | CPU runner 160/160; pełne źródłowe podzbiory 8 029/8 029; 0 skip/mismatch | pełen stan, flagi, kolejka i cykle zgodne; prefiksy i pozostałe formy A4 otwarte |
| 2026-10-23 | ALU byte ModR/M rejestrowe (`00`–`3A` wybrane, `84`) | 17 kodowań / 340 próbek | 340/340; test projektu: 920/920 | pełne filtrowane źródło: 4 355/4 355; 0 skip/mismatch | operandy pamięciowe i immediate do r/m nadal otwarte |
