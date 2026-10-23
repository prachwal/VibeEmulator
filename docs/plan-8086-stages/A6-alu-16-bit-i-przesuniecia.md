# Etap A6 — ALU 16-bit, inkrementacja i przesunięcia

Poziom: **II. Zestaw bazowy, część 4** · Zależność: A4/A5

## Zakres

Warianty słowne `ADD/ADC/SUB/SBB/CMP/AND/OR/XOR/TEST`; `INC/DEC` bajtowe i słowne; `NOT/NEG` bajtowe i słowne; przesunięcia i rotacje `SHL/SAL/SHR/SAR`, `ROL/ROR/RCL/RCR` dla obu szerokości; unary/multiply group decoding wyłącznie dla podoperacji przypisanych do tego etapu. Liczbę/podział opkodów utrzymywać zgodnie z manifestem A1.

## Kolejność

1. Uogólnić helper ALU z A4 na szerokości dopuszczone przez dekoder, bez boxing/alokacji w ścieżce `Tick`.
2. Dodać słowne add/sub/compare i wszystkie formy operandów z manifestu, w tym sign extension immediate tam, gdzie wynika z kodowania.
   Metadane D2/D3 mają statyczne maski, które według README upstream nie sprawdzają poprawności OF dla CL=0/1; dodać osobne asercje tych przypadków ze źródłem, a nie uznawać zielonej maski za pełne pokrycie.
3. Dodać `INC/DEC`, osobno asercjonując flagi zachowane i zmienione.
4. Dodać `NOT/NEG`, oddzielając zachowanie flag dla obu instrukcji.
5. Dodać rotacje przez/bez carry; potem przesunięcia arytmetyczne/logiczne.
6. Dla count=1 i count z CL porównywać dokładnie fixture; formy z dowolnym immediate pozostają poza zakresem bazowym do weryfikacji ISA w A1. Metadane upstream oznaczają C0/C1 jako aliasy, więc nie wolno nadawać tym bajtom znaczenia nowszych procesorów.

## Testy i kryteria

- [ ] Każda instrukcja/form ma wektor próbki albo oznaczoną lukę źródłową.
- [ ] Testy flag obejmują boundary wartości i kontrolują flagi zachowywane.
- [ ] Operandy pamięciowe mają kompletny log read/write/cycles, łącznie z RMW.
- [ ] Rozbieżności wynikające z nieokreślonych flag są maskowane wyłącznie na podstawie wiarygodnego źródła.
- [x] Aktywne próbki CPU A3–A6: 4 838/4 838 rekordów przeszły po rebase; testy Intel8086: 5 210/5 210. Pełne zestawy poza aktywną próbką opisano osobno.
- [x] Częściowe kryterium 1: osiem word AX-immediate form przechodzi próbki CPU 160/160 i pełny filtrowany zbiór 7 986/7 986.
- [x] Częściowe kryterium 2: siedemnaście word ALU register-direct form przechodzi próbki CPU 340/340 i pełny filtrowany zbiór `mod=11` 4 180/4 180; formy pamięciowe pozostają poza zakresem.
- [x] Częściowe kryterium 3: rejestrowe `INC/DEC r16` przechodzi 320 próbek i pełne 32 000 rekordów; wartości CF zgodne we wszystkich fixture.
- [x] Częściowe kryterium 4: grupy `FF /0` i `FF /1` z rejestrowym operandem word przechodzą próbki 40/40 i pełny filtr `mod=11` 506/506.
- [x] Częściowe kryterium 5: `FE /0,/1 mod=11` przechodzi próbki 40/40 i pełny filtr 561/561; oba źródłowe podzbiory mają trzy T-states.
- [x] Częściowe kryterium 6: nieprefiksowane pamięciowe `FF /0,/1 mod=00/01/10` przechodzi próbki 40/40 i pełny filtr źródła 1 511/1 511; zweryfikowano stan, RAM, flagi, kolejkę oraz T-states, w tym word-transfer wyrównany i rozdzielony dla nieparzystego adresu. Prefiksowane FF pozostaje poza tym filtrem.
- [x] Częściowe kryterium 7: `NOT/NEG` (`F6/F7 /2,/3`) przechodzi próbki 160/160 oraz pełny filtrowany zbiór rejestrowy i nieprefiksowany pamięciowy 3 983/3 983 (987 + 2 996); prefiksowane formy pamięciowe pozostają poza filtrem.
- [x] Częściowe kryterium 8: rejestrowe count=1 (`D0/D1 /0–/5,/7`) przechodzi próbki 280/280 i pełny filtrowany zbiór 3 549/3 549; maska AF `0xFFEF` używana jest tylko dla rozszerzeń oznaczonych tak w przypiętych metadata.
- [x] Częściowe kryterium 9: rejestrowe `D2/D3 /0–/5,/7` przechodzi próbki 280/280 i pełny filtrowany zbiór 3 478/3 478; profil źródła: CL 0–62 i długość 8–256 T-state; wektory zawierają maski OF/AF z upstream metadata.
- [x] Częściowe kryterium 10: dekoder EA wybiera SS dla `mod=00 r/m=010/011` (BP+SI/BP+DI) i DS dla bezpośredniego `mod=00 r/m=110`; testy dekodera 32/32.
- [x] Częściowe kryterium 11: `FE /0,/1` rejestrowe i pamięciowe, z prefiksem segmentowym i bez niego, przechodzi próbki 120/120 i pełny filtr źródła 4 000/4 000; stan, RAM, kolejka i T-states porównane.
- [x] Częściowe kryterium 12: nieprefiksowane pamięciowe `FF /0,/1` przechodzi próbki 40/40 i pełny filtr źródła 1 511/1 511; bez skipów i mismatchy w historycznym raporcie podetapu.
- [x] Częściowe kryterium 13: pamięciowe `F6/F7 /2,/3 mod=00/01/10` bez prefiksów przechodzą próbki 80/80 i pełny filtr 2 996/2 996; porównano stan, RAM, flags, kolejkę i każdy T-state w historycznym raporcie.
- [x] Częściowe kryterium 14: nieprefiksowane pamięciowe `D0–D3 /0–/5,/7 mod=00/01/10` przechodzą próbki 560/560 i pełny filtr 21 067/21 067; obejmuje count=0 i nieparzyste adresy word. Wynik szczegółowy do potwierdzenia po rebase.
- [x] Częściowe kryterium 15: nieprefiksowane pamięciowe word-ALU ModR/M (`01/03`, `09/0B`, `11/13`, `19/1B`, `21/23`, `29/2B`, `31/33`, `39/3B`, `85`; `mod=00/01/10`) przechodzi próbki 340/340 i pełny filtr 12 937/12 937; uwzględniono RMW, read-only CMP/TEST i kierunek do rejestru.
- [x] Częściowe kryterium 16: rejestrowe `81/83 /0–/7` przechodzi próbki CPU 320/320; `83` ma cztery T-states z overlapped prefetch zgodnym z wektorami. Pełny filtrowany wynik 4 034/4 034 pochodzi z historycznego uruchomienia przed rebase i wymaga ponowienia; pamięciowe formy `mod=00/01/10` pozostają otwarte.

## Następny podetap przed implementacją

1. Wdrożyć pamięciowe `FF /0,/1` word-RMW; dla nieparzystego adresu odtworzyć osobne transfery bajtów.
3. Porównać stan, RAM, kolejkę i każdy T-state, w tym zawinięcie 20-bitowego busa.
4. Rozszerzyć pozostałe operacje A6 o formy pamięciowe zgodnie z manifestem albo opisać udokumentowane luki źródłowe.

## Pułapki

1. Nie używać reguł count-maskowania z 80186 lub 80286; wariantem jest 8086 i sprawdza się go wektorami/manualem.
2. Count 0 i count większy od szerokości mogą mieć różne skutki dla flag; nie ekstrapolować z jednego przypadku.
3. Carry flag rotacji i overflow flag zależą od count; testować bity osobno.
4. `INC/DEC` nie mogą przypadkowo nadpisywać flagi, którą instrukcja zachowuje.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

### 2026-10-23 — operacje ALU 16-bit i formy AX-immediate

Dodano przeciążenie word `Intel8086Alu.Execute` dla ADD/ADC/SUB/SBB/CMP/AND/OR/XOR/TEST. Próbka helpera obejmuje po 20 rekordów upstream dla `05`, `15`, `1D`, `2D`, `3D`, `25`, `0D`, `35`: **160/160 wyników i określonych flag**. Maskowanie AF wynika z `flags-mask=0xFFEF` metadanych upstream dla instrukcji logicznych.

W tamtym podetapie testowano wyłącznie helper; dekoder/cykle nie były jeszcze objęte testami.

### 2026-10-23 — word AX-immediate w wykonaniu CPU

Dodano osiem form `ADD/ADC/SBB/SUB/AND/OR/XOR/CMP AX,imm16` (`05`, `0D`, `15`, `1D`, `25`, `2D`, `35`, `3D`). Próbka end-to-end ma po 20 rekordów na opcode: **160/160**. Pełny filtr przypiętego źródła: **7 986/7 986**, 0 skipów i mismatchy; porównanie obejmuje stan końcowy, kolejkę i wszystkie cztery T-states. To nie obejmuje form ModR/M, pamięci, `INC/DEC`, `NOT/NEG` ani rotacji/przesunięć.

## Dziennik postępu

| data | grupa | pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | word ALU AX-immediate (helper) | 8 plików / 160 przypadków | 160/160 helper checks; CPU runner 0 | pełne wektory i pozostałe rodziny nieuruchomione |
| 2026-10-23 | word ALU AX-immediate (CPU) | 8 plików / 160 przypadków | 160/160; pełny filtr 7 986/7 986 | pozostałe A6 formy niezaimplementowane |
| 2026-10-23 | rejestrowe NOT/NEG (`F6/F7 /2,/3`) | 4 filtrowane grupy / 80 próbek | 80/80; pełny filtr 987/987 | formy pamięciowe otwarte |
| 2026-10-23 | rejestrowe count=1 (`D0/D1 /0–/5,/7`) | 14 filtrowanych grup / 280 próbek | 280/280; pełny filtr 3 549/3 549 | metadata maskuje AF wyłącznie dla `/4,/5,/7`; CL i pamięć otwarte |
| 2026-10-23 | rejestrowe shift/rotate `D2/D3` z CL | 14 filtrowanych grup / 280 próbek | 280/280; pełny filtr 3 478/3 478 | metadata maskuje OF oraz AF dla shift; 0–62 CL, długość 8–256 T-state |

## Następny podetap

Rozszerzyć `FE /0,/1 mod=00/01/10` o prefiksy segmentowe; następnie wdrożyć `FF /0,/1` dla pamięci word z rozdziałem transferu przy adresie nieparzystym.

### 2026-10-23 — próba prefiksów segmentowych FE

Wdrożono prefiksy segment override dla rejestrowych i pamięciowych `FE /0,/1`, w tym profil prefetch, displacement i segment magistrali. Aktywne próbki: 120/120; pełne pliki źródłowe `FE.0.json` i `FE.1.json`: **4 000/4 000**, zero skipów/mismatchy; Intel8086 tests: **2 942/2 942**. Weryfikacja porównuje stan, RAM, kolejkę i każdy T-state. Pełny łączny wynik CPU A1–A6 po dodaniu tych prefiksowanych przypadków wynosi **104 079**; pozostałe formy pamięciowe A6, w tym `FF`, nadal są otwarte.


### 2026-10-23 — word ALU register-direct ModR/M (`mod=11`)

Dodano 17 kodowań: `01/03`, `09/0B`, `11/13`, `19/1B`, `21/23`, `29/2B`, `31/33`, `39/3B`, `85`. Próbki 20/opcode porównują stan końcowy, kolejkę i trzy T-states: **340/340**. Pełny filtrowany upstream: **4 180/4 180**, 0 skipów i mismatchy.


### 2026-10-23 — rejestrowe `INC/DEC r16` (`40`–`4F`)

Przypięte fixture ma 2 T-states i zużywa dwa bajty kolejki (opkod i kolejny bajt); każdy z 16 plików zawiera 2 000 rekordów. Zaimplementowano zachowanie słów i zachowanie CF przez helper. Próbka 320/320 i pełne 32 000/32 000 przechodzą bez skipów ani mismatchy.


### 2026-10-23 — grupy `FF /0` i `/1` rejestrowe

Przypięte źródło zawiera 251 rekordów `FF /0 mod=11` i 255 rekordów `FF /1 mod=11`, po trzy T-states. Próbki 40/40 i pełny filtr 506/506 przechodzą bez skipów ani mismatchy.


### 2026-10-23 — rejestrowe `INC/DEC r/m8` (`FE /0,/1`)

Przypięte źródło zawiera 258 przypadków `FE /0 mod=11` i 303 `FE /1 mod=11`, wszystkie po trzy T-states. Próbki 40/40 i pełny filtr 561/561 przechodzą bez skipów ani mismatchy. Testy projektu Intel8086: 4 403/4 403. Do odtworzenia buildu w tym środowisku potrzebne było `-m:1`; runner xUnit wymagał uruchomienia poza ograniczeniem lokalnych socketów.


### 2026-10-23 — audyt pamięciowych grup FE/FF

W przypiętych plikach FE jest 737 przypadków `/0` oraz 717 `/1` z `mod!=11`: każda forma ma dwa T-states read i dwa write; długość całej instrukcji zależy od displacement/prefetch (20–28 T-states). FF ma 768 przypadków `/0` i 743 `/1` pamięciowych. Słowo wyrównane używa dwóch T-states read i dwóch write; adres nieparzysty rozdziela read/write na po dwa bajty (łącznie osiem T-states pamięci). Pełna długość instrukcji w źródle: 20–36 T-states. To profil źródła, nie wynik wykonania CPU; implementacja RMW i kompletne porównanie magistrali pozostają otwarte.

| grupa | `mod=00` | `mod=01` | `mod=10` | transfer pamięci |
|---|---:|---:|---:|---|
| FE `/0` | 231 | 230 | 276 | 4 T-states R/R/W/W; instrukcja 20–28 T-states |
| FE `/1` | 246 | 227 | 244 | 4 T-states R/R/W/W; instrukcja 20–28 T-states |
| FF `/0` | 247 (126 parzyste /121 nieparzyste) | 235 (119/116) | 286 (138/148) | word: 4 T-states parzyste, 8 nieparzyste; instrukcja 20–36 T-states |
| FF `/1` | 244 (131/113) | 239 (118/121) | 260 (136/124) | word: 4 T-states parzyste, 8 nieparzyste; instrukcja 20–36 T-states |

Wartości parzyste/nieparzyste dotyczą fizycznego adresu po segmentacji i 20-bitowym zawinięciu; `FF` odd-address rozbija zarówno odczyt, jak i zapis na dwa bajty.

Rozkład całkowitej długości instrukcji FE, policzony po rekordach `mod!=11` z przypiętych plików `FE.0.json.gz` i `FE.1.json.gz`:

| rozszerzenie | 20 T | 21 T | 22 T | 23 T | 24 T | 25 T | 26 T | 27 T | 28 T |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `/0` | 48 | 64 | 66 | 53 | 180 | 73 | 85 | 131 | 37 |
| `/1` | 40 | 59 | 84 | 63 | 184 | 61 | 87 | 104 | 35 |

W profilach występują różne położenia cykli `CODE` względem `MEMR`; nie ma jednej stałej pozycji odczytu operandu. Powtarzająca się sekwencja dostępu to `MEMR T1/T2`, stan pasywny `T3/T4`, następnie `MEMW T1/T2` i pasywny `T3`. Prefetch i stany pasywne wypełniają pozostały czas zgodnie z kolejką oraz trybem adresowania. To uzasadnia pozostawienie kryterium implementacji magistrali niezaznaczonego do czasu porównania całego rekordu cykl po cyklu.

### 2026-10-23 — rejestrowe `NOT/NEG` (`F6/F7 /2,/3`)

Dodano bajtowe `F6 /2 NOT`, `F6 /3 NEG` oraz słowne `F7 /2 NOT`, `F7 /3 NEG` dla `mod=11`. Próbki po 20 rekordów na grupę przechodzą 80/80. Pełne filtrowane zbiory źródłowe: `F6 /2` 245, `F6 /3` 237, `F7 /2` 270 i `F7 /3` 235 — razem **987/987**, 0 pominięć i mismatchy. Testy projektu Intel8086: **2 260/2 260**. Formy pamięciowe nie są częścią tego wyniku.

### 2026-10-23 — rejestrowe rotacje i przesunięcia count=1 (`D0/D1`)

Dodano `/0` ROL, `/1` ROR, `/2` RCL, `/3` RCR, `/4` SHL/SAL, `/5` SHR i `/7` SAR dla operandów rejestrowych 8- i 16-bitowych z count=1 (`D0/D1`). Próbki po 20 rekordów dla 14 kodowań przechodzą 280/280. Pełny filtrowany zbiór przypiętego źródła: **3 549/3 549**, 0 pominięć i mismatchy; testy projektu Intel8086 w tamtym kroku: **2 540/2 540**. Dla SHL/SHR/SAR manifest przekazuje `flags-mask=0xFFEF` z metadata upstream, więc AF nie jest porównywane. Count z CL (`D2/D3`) pozostaje otwarty.

Audyt grup rejestrowych `D2/D3 /0–/5,/7` wykazał 1 720 rekordów D2 i 1 758 D3. W źródle CL ma wartości 0–62; długość wynosi 8 T-state dla CL=0 oraz `8+4×CL` dla CL>0 (maksymalnie 256). Implementacja iteruje operację dla każdego count, wykonuje fetch zgodnie z początkową długością kolejki i odtwarza kolejkę po fetchu. Próbki przechodzą 280/280, pełny filtr 3 478/3 478, a testy projektu Intel8086 2 820/2 820 w historycznym raporcie tego podetapu. Metadata wyłącza OF, a dla operacji przesunięcia również AF; używane maski to `0xF7FF` i `0xF7EF`. Pełny podzbiór CPU CL: 3 478/3 478; nie rozszerza to pokrycia na formy pamięciowe ani nie dowodzi pełnej zgodności wszystkich cykli dla całego 8086.

### 2026-10-23 — pamięciowe `FE /0,/1` bez prefiksów

Zaimplementowano adresowanie pamięciowe, wybór segmentu domyślnego, displacement, bajtowy odczyt i zapis RMW oraz prefetch. Próbki 40/40 i pełny filtrowany podzbiór źródła: **1 454/1 454**, bez skipów i mismatchy w historycznym raporcie podetapu; testy projektu Intel8086 wtedy **2 862/2 862**. Pełny filtr obejmuje `mod=00/01/10` bez prefiksów; segment override pozostaje poza tym wynikiem.

### 2026-10-23 — prefiksy segmentowe FE

Wdrożono prefiksy segment override dla rejestrowych i pamięciowych `FE /0,/1`, w tym profil prefetch, displacement i segment magistrali. W historycznym raporcie podetapu aktywne próbki przechodziły 120/120, a pełne pliki źródłowe `FE.0.json` i `FE.1.json` 4 000/4 000; wynik ten trzeba potwierdzić na rebazowanym branchu. Weryfikacja porównuje stan, RAM, kolejkę i każdy T-state. Pozostałe pamięciowe formy A6, w tym `FF`, są otwarte.

### 2026-10-23 — nieprefiksowane pamięciowe FF /0,/1

Dodano word RMW dla trybów pamięciowych. Dla adresu parzystego słowo przechodzi jednym transferem magistrali, dla nieparzystego osobnymi cyklami bajtów. Historyczny raport etapu: próbki 40/40; pełny filtr 1 511/1 511 (768 `/0`, 743 `/1`), stan, RAM, flags, kolejka i T-states zgodne. Prefiksowane FF i pamięciowe NOT/NEG/rotacje pozostają otwarte.

### 2026-10-23 — nieprefiksowane pamięciowe F6/F7 /2,/3

Dodano bajtowe i słowne NOT/NEG dla operandów pamięciowych. `NOT` zachowuje flags, `NEG` wylicza flags; słowa pod nieparzystym adresem używają dwóch transferów odczytu i dwóch zapisu. Historyczny raport: próbki 80/80, pełny filtr 2 996/2 996 (F6 `/2` 748, `/3` 766; F7 `/2` 743, `/3` 739), bez skipów i mismatchy. Prefiksowane formy pozostają do etapu A10.

### 2026-10-23 — nieprefiksowane pamięciowe D0–D3 /0–/5,/7

Dodano pamięciowe rotacje i przesunięcia bajtowe/słowne dla count=1 i CL. Zapis operandu uwzględnia pętlę CL (4 T-state na krok i 5 T-state narzutu); słowa wyrównane oraz nieparzyste używają właściwego podziału cykli magistrali. Historyczny raport: próbki 560/560; pełny filtrowany zbiór 21 067/21 067; porównano stan, RAM, kolejkę, flagi według masek metadata i każdy T-state. Prefiksy pozostają do A10.

### 2026-10-23 — pamięciowy word-ALU ModR/M

Dodano 17 kodowań word-ALU z operandem pamięciowym. Kierunek `r/m→reg` oraz `CMP/TEST` wykonują read-only operand; pozostałe operacje wykonują RMW. Profile prefetch i długości instrukcji porównano dla `mod=00/01/10`, obu parzystości i kolejek początkowych o długości 5 i 6. Historyczny raport: próbki 340/340; pełny filtr 12 937/12 937; porównano rejestry, flagi, RAM, kolejkę i każdy T-state.
