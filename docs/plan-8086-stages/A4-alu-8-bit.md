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
- [x] Brak testów Skip i mismatchy w aktywnym zakresie A3+A4.
- [x] Pamięciowe ModR/M dla `00/02`, `08/0A`, `10/12`, `18/1A`, `20/22`, `28/2A`, `30/32`, `38/3A`, `84`: próbki 20/opcode przechodzą pełny stan, kolejkę i wszystkie T-states (340/340).
- [x] Pełne nieprefiksowane podzbiory pamięciowe SST dla tych 17 kodowań przechodzą bez skipów/mismatchy (**12 731/12 731**).
- [x] Wszystkie formy A4 mają aktywne próbki i implementację: pamięciowy ModR/M `00/02`, `08/0A`, `10/12`, `18/1A`, `20/22`, `28/2A`, `30/32`, `38/3A`, `84`; grupa immediate `80 /0–/7`; `TEST AL,imm8` (`A8`).
- [x] Pełne nieprefiksowane podzbiory wszystkich form A4 przechodzą bez skipów/mismatchy; odczyt, zapis, brak zapisu `CMP/TEST`, kolejka i piny są porównywane.
- [x] Wektory operandów pamięciowych i `CMP/TEST` pokrywają carry/borrow, AF, parity, zero, sign i overflow; pełny runner porównuje rejestry, RAM, kolejkę i wszystkie cykle.
- [x] Wszystkie nieprefiksowane formy A4 z pełnego podzbioru źródłowego — w tym operandy pamięciowe i grupy immediate do r/m — są zaimplementowane i zweryfikowane. Prefiksy segmentowe należą do A10.

## Pułapki

1. Nie przenosić algorytmu flag z Mos6502/Z80: szerokość i określoność flag są specyficzne dla x86.
2. Nie maskować flag „na oko”; jeśli generator nie określa ich, udokumentować dowód z metadanych lub manuala.
3. `CMP` i `TEST` wykonują obliczenie flagowe, ale nie zapisują wyniku.
4. Immediate może być sign-extended w niektórych formach słownych; rozstrzygnąć źródłem w A6, a tu nie uogólniać reguły bajtowej.

## Kolejność implementacji (zrealizowana)

1. [x] Dodać nieprefiksowane pamięciowe formy 17 kodowań ALU ModR/M oraz `TEST 84`; rozdzielić odczyt, zapis i `CMP/TEST`.
2. [x] Oprzeć profile T-state, displacement/immediate i prefetch na SST oraz porównać każdą magistralę w runnerze.
3. [x] Dodać `80 /0–/7` dla rejestru/pamięci oraz `A8 TEST AL,imm8`; `CMP` nie zapisuje operandu.
4. [x] Współdzielić `Intel8086Alu.Execute` bez powielania reguł flag.
5. [x] Uruchomić aktywną próbkę i pełne filtrowane podzbiory; zaktualizować inwentarz oraz tę checklistę.

## Dziennik postępu

### 2026-10-23 — primitive ALU i accumulator-immediate oracle

Dodano czystą funkcję 8-bit `Intel8086Alu.Execute` dla ADD/ADC/SUB/SBB/CMP/AND/OR/XOR/TEST. Wektorowe testy helpera korzystają z pierwszych 20 rekordów upstream dla form AL-immediate: `04`, `14`, `1C`, `2C`, `3C`, `24`, `0C`, `34` (160 rekordów). Dla wpisów z metadata `flags-mask=0xFFEF` test ignoruje wyłącznie AF (bit 4); rejestry i pozostałe określone flagi są porównywane. Wynik: **160/160** helper checks.

Zestawy `Vectors/A4Alu/` testują helper, nie cykle CPU. W kolejnym podetapie podłączono osiem form accumulator-immediate do rdzenia i aktywowano osobne, nieprefiksowane próbki end-to-end. A4 nadal pozostaje otwarty: formy pamięciowe, immediate do r/m oraz warianty z prefiksami nie są zaimplementowane.

### 2026-10-23 — ALU byte ModR/M, register-direct

Rdzeń wykonuje `ADD/OR/ADC/SBB/AND/SUB/XOR/CMP r/m8,r8` (`00/08/10/18/20/28/30/38`), warianty kierunku do rejestru (`02/0A/12/1A/22/2A/32/3A`) oraz `TEST r/m8,r8` (`84`) przy `mod=11`. Aktywowano pierwsze 20 pasujących upstream rekordów dla każdego z 17 kodowań (340 przypadków); pełny runner porównał rejestry, flagi, RAM, kolejkę i wszystkie T-states: **340/340**. Pełne przefiltrowane źródłowe podzbiory zawierają **4 355** rekordów i przeszły bez skipów/mismatchy. To nie obejmuje operandów pamięciowych ani immediate do r/m.

### 2026-10-24 — operandy pamięciowe i immediate do r/m

Dodano 340 próbek pamięciowych dla 17 kodowań ModR/M, `80 /0–/7` dla rejestru i pamięci oraz `A8 TEST AL,imm8`. Aktywna próbka A3+A4 przechodzi **1638/1638**. Pełne filtrowane, nieprefiksowane zestawy wszystkich form A4 przechodzą **35 167/35 167**: AL-immediate 8 029, ModR/M rejestrowe 4 355, ModR/M pamięciowe 12 731 oraz `80` i `A8` 10 052. Bez skipów i mismatchy; porównano FLAGS, rejestry, RAM, kolejkę i wszystkie cykle T-state. Prefiksy segmentowe pozostają w zakresie A10.

## Dziennik postępu

| data | rodzina | pass/manifest | testy próbki | pełne poza repo |
|---|---|---|---|---|
| 2026-10-23 | 8 operacji, formy AL-immediate | helper: 160/160 | CPU runner: 0 form A4 | pełne wektory nie uruchomiono; integracja core otwarta |
| 2026-10-23 | AL,imm8 (`04`,`0C`,`14`,`1C`,`24`,`2C`,`34`,`3C`), bez prefiksów | 160 próbek CPU, pierwsze 20 rekordów bez prefiksu na opcode | CPU runner 160/160; pełne źródłowe podzbiory 8 029/8 029; 0 skip/mismatch | pełen stan, flagi, kolejka i cykle zgodne; prefiksy i pozostałe formy A4 otwarte |
| 2026-10-23 | ALU byte ModR/M rejestrowe (`00`–`3A` wybrane, `84`) | 17 kodowań / 340 próbek | 340/340; test projektu: 920/920 | pełne filtrowane źródło: 4 355/4 355; 0 skip/mismatch | operandy pamięciowe i immediate do r/m nadal otwarte |
