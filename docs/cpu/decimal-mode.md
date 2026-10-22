# Tryb dziesiętny (BCD)

Źródła (przeczytane 2026-10-21): [Bruce Clark: Decimal Mode](http://www.6502.org/tutorials/decimal_mode.html)
(w tym Dodatek A: zachowanie nieudokumentowane), [NESdev: CPU](https://www.nesdev.org/wiki/CPU) (`oldid=20204`),
źródła testów Dormanna (`6502_functional_test.a65`, `6502_decimal_test.a65`, `65C02_extended_opcodes_test.a65c`).
Algorytmy poniżej są faktami z opracowania Clarka; szczegóły i dowody są w oryginale.

## Zasady

- Flaga `D` wybiera tryb **tylko** dla `ADC` i `SBC`; żadna inna instrukcja nie zależy od `D` (`ASL`, `INC` itd. zawsze
  liczą binarnie).
- `D` zmieniają: `CLD`, `SED`, `PLP`, `RTI` (oraz `BRK`/IRQ/NMI/RESET na 65C02, patrz [interrupts-and-reset.md](interrupts-and-reset.md)).
- W trybie dziesiętnym przeniesienie jest ustawione, gdy wynik przekracza `$99` (dodawanie), a przy odejmowaniu jest
  wyczyszczone, gdy wynik jest ujemny; odejmowanie zawija się (`$00 − $01 = $99`, `C = 0`).
- Wejścia `ADC`: `A = A + operand + C`; `SBC`: `A = A − operand − (1 − C)`.
- **Ricoh 2A03 (NES) nie ma trybu dziesiętnego** (NESdev: „lacks the MOS6502's decimal mode”). Flaga `D` da się ustawić, ale
  `ADC`/`SBC` liczą binarnie. *(Sformułowanie o możliwości ustawienia flagi: niezweryfikowane; NESdev mówi tylko, że tryb
  dziesiętny jest nieobecny.)*

## Algorytmy (wszystkie obliczenia w nieograniczonej precyzji, binarnie, bez znaku, chyba że zaznaczono)

### `ADC B`: `A = A + B + C`

**Sekwencja 1** (wynik akumulatora i `C` na wszystkich wariantach):

1. `AL = (A & $0F) + (B & $0F) + C`
2. jeśli `AL >= $0A`: `AL = ((AL + $06) & $0F) + $10`
3. `A = (A & $F0) + (B & $F0) + AL` (tu `A` może być `>= $100`)
4. jeśli `A >= $A0`: `A = A + $60`
5. wynik akumulatora = dolne 8 bitów `A`; `C = 1`, gdy `A >= $100`

**Sekwencja 2** (flagi `N` i `V` na NMOS; `V` także na 65C02): kroki 1–2 jak wyżej, potem
`A = (A & $F0) + (B & $F0) + AL` **w arytmetyce ze znakiem** (U2); `N` = bit 7 wyniku; `V = 1`, gdy `A < −128` lub `A > 127`.

### `SBC B`: `A = A − B + C − 1`

**Sekwencja 3** (NMOS i 65816):

1. `AL = (A & $0F) − (B & $0F) + C − 1`
2. jeśli `AL < 0`: `AL = ((AL − $06) & $0F) − $10`
3. `A = (A & $F0) − (B & $F0) + AL`
4. jeśli `A < 0`: `A = A − $60`
5. wynik akumulatora = dolne 8 bitów `A`

**Sekwencja 4** (65C02): `AL = (A & $0F) − (B & $0F) + C − 1`; `A = A − B + C − 1`; jeśli `A < 0`: `A = A − $60`;
jeśli `AL < 0`: `A = A − $06`; wynik = dolne 8 bitów `A`.

### Skąd bierze się każda flaga (`bin` = jak w trybie binarnym dla tych samych wejść; `dec` = z wyniku BCD w akumulatorze)

| operacja | wynik `A` | `C` | `N` | `V` | `Z` |
|---|---|---|---|---|---|
| `ADC`, 6502 | sekw. 1 | sekw. 1 | sekw. 2 | sekw. 2 | `bin` |
| `ADC`, 65C02 i 65816 | sekw. 1 | sekw. 1 | `dec` | sekw. 2 | `dec` |
| `SBC`, 6502 | sekw. 3 | `bin` | `bin` | `bin` | `bin` |
| `SBC`, 65C02 | sekw. 4 | `bin` | `dec` | `bin` | `dec` |
| `SBC`, 65816 | sekw. 3 | `bin` | `dec` | `bin` | `dec` |

## Co jest udokumentowane, a co nie

- NMOS 6502: **tylko `C` jest poprawne** w trybie dziesiętnym. `N`, `V` i `Z` przy poprawnych operandach BCD to
  zachowanie nieudokumentowane; zachowanie dla niepoprawnych operandów BCD (cyfry `A`–`F`) też.
- 65C02 i 65816: dokumentacja uznaje `N`, `V`, `Z` za poprawne, ale `V` w trybie dziesiętnym jest efektywnie
  nieudokumentowane (arytmetyka BCD jest bez znaku, a `V` ma sens dla arytmetyki ze znakiem); na wszystkich trzech
  układach przy tych samych wejściach `V` wychodzi tak samo.

## Cykle

Na **65C02** `ADC` i `SBC` w trybie dziesiętnym trwają o **1 cykl dłużej** (`ADC #` 3 zamiast 2; `SBC abs,X` z
przekroczeniem strony 6 zamiast 5). 65816 ma czas jak 6502. Zgadza się to z wektorami SingleStepTests: 18 opkodów
`ADC`/`SBC` w zestawie `wdc65c02` ma o jeden cykl więcej w części testów (przy ustawionym `D`).

**Weryfikacja wektorowa (2026-10-21, etap G):** pełne wektory 16 form `ADC`/`SBC` × 2 warianty (320 000 testów, bez
pominięć) potwierdzają powyższe algorytmy i dodatkowo uściślają to, czego nie obejmuje tabela Clarka:

- `ADC` NMOS: `N` = bit 7 sumy **bez korekty $A0** (a nie z wyniku w akumulatorze); `V` = przepełnienie binarne liczone
  na górnych nibble'ach (`(A&F0)+(B&F0)` z wynikiem `r2`); `Z` = zero sumy **binarnej**.
- `ADC`/`SBC` 65C02: `N` i `Z` z wyniku BCD (jak w tabeli); `V` i `C` jak w tabeli.
- Dodatkowy cykl 65C02: tryby pamięciowe **powtarzają odczyt operandu**; tryb `imm` czyta pod stałym adresem
  per akcja (`$7F` dla ADC, `$00` dla SBC) — to stała implementacji referencyjnej (prawdziwy 65C02 wykonuje odczyt
  wewnętrzny, którego adres nie jest obserwowalny), nie zachowanie sprzętu.

## Jak testy Dormanna traktują tryb dziesiętny

| test | zakres | konfiguracja |
|---|---|---|
| `6502_functional_test` | `ADC`/`SBC` tylko z **poprawnymi** operandami BCD; flagi `N`, `V`, `Z` ignorowane | `disable_decimal`: 0 = testuj, 1 = pomiń, 2 = pomiń także flagę `D` w statusie |
| `65C02_extended_opcodes_test` | poprawne operandy; `V` ignorowane | jw. |
| `6502_decimal_test.a65` (Bruce Clark, domena publiczna) | **wszystkie 262 144 przypadki** (2 instrukcje × 2 wartości `C` × 256 × 256), także niepoprawne BCD | `cputype` (0 = 6502, 1 = 65C02, 2 = 65816), `vld_bcd` (1 = tylko poprawny BCD), `chk_a`, `chk_n`, `chk_v`, `chk_z`, `chk_c` |

Test dziesiętny zapisuje 0 w `ERROR` przy sukcesie i 1 przy porażce; zajmuje ok. minuty przy 1 MHz. Domyślnie sprawdza
`A` i `C` (`chk_n = chk_v = chk_z = 0`), co odpowiada temu, co jest udokumentowane dla 6502. Makro `end_of_test`
wykonuje opkod `$DB` (STP 65C02); dla 6502 trzeba je zmienić.

Dla NMOS należy porównywać tylko `A` i `C` (poprawne zachowanie udokumentowane), dla 65C02 i 65816 także `N` i `Z`.

## Nie przeczytano

Pełny kod programu weryfikacyjnego z Dodatku B (jest też w repozytorium Dormanna jako `6502_decimal_test.a65`, pobrane;
przeczytano tylko nagłówek i konfigurację).
