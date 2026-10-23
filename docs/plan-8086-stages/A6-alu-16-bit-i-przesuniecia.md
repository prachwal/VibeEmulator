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
- [x] Aktywne próbki CPU A3–A6 zielone: 1 820/1 820, 0 skipów; nieobjęte formy i pełne źródło opisane jako nieuruchomione.
- [x] Częściowe kryterium 1: osiem word AX-immediate form przechodzi próbki CPU 160/160 i pełny filtrowany zbiór 7 986/7 986.
- [x] Częściowe kryterium 2: siedemnaście word ALU register-direct form przechodzi próbki CPU 340/340 i pełny filtrowany zbiór `mod=11` 4 180/4 180; formy pamięciowe pozostają poza zakresem.
- [x] Częściowe kryterium 3: rejestrowe `INC/DEC r16` przechodzi 320 próbek i pełne 32 000 rekordów; wartości CF zgodne we wszystkich fixture.
- [x] Częściowe kryterium 4: grupy `FF /0` i `FF /1` z rejestrowym operandem word przechodzą próbki 40/40 i pełny filtr `mod=11` 506/506.
- [x] Częściowe kryterium 5: `FE /0,/1 mod=11` przechodzi próbki 40/40 i pełny filtr 561/561; oba źródłowe podzbiory mają trzy T-states.

## Następny podetap przed implementacją

1. Rozpoznać formy `FE /0,/1` z operandem pamięciowym; ująć odczyt, zapis, flagi i dokładną sekwencję cykli RMW przed implementacją.
2. Powtórzyć analizę dla `FF /0,/1` word-memory i sprawdzić dodatkowe formy `FF` poza zakresem A6.
3. Do czasu potwierdzenia każdego dostępu magistrali formy pamięciowe pozostają niezaimplementowane.

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

## Następny podetap

Zweryfikować implementację rejestrowego `FE /0,/1` na próbkach i pełnym filtrze, gdy SDK umożliwi build. Następnie rozważyć pamięciowe formy `FE`/`FF` z uwzględnieniem logu RMW.


### 2026-10-23 — word ALU register-direct ModR/M (`mod=11`)

Dodano 17 kodowań: `01/03`, `09/0B`, `11/13`, `19/1B`, `21/23`, `29/2B`, `31/33`, `39/3B`, `85`. Próbki 20/opcode porównują stan końcowy, kolejkę i trzy T-states: **340/340**. Pełny filtrowany upstream: **4 180/4 180**, 0 skipów i mismatchy.


### 2026-10-23 — rejestrowe `INC/DEC r16` (`40`–`4F`)

Przypięte fixture ma 2 T-states i zużywa dwa bajty kolejki (opkod i kolejny bajt); każdy z 16 plików zawiera 2 000 rekordów. Zaimplementowano zachowanie słów i zachowanie CF przez helper. Próbka 320/320 i pełne 32 000/32 000 przechodzą bez skipów ani mismatchy.


### 2026-10-23 — grupy `FF /0` i `/1` rejestrowe

Przypięte źródło zawiera 251 rekordów `FF /0 mod=11` i 255 rekordów `FF /1 mod=11`, po trzy T-states. Próbki 40/40 i pełny filtr 506/506 przechodzą bez skipów ani mismatchy.


### 2026-10-23 — rejestrowe `INC/DEC r/m8` (`FE /0,/1`)

Przypięte źródło zawiera 258 przypadków `FE /0 mod=11` i 303 `FE /1 mod=11`, wszystkie po trzy T-states. Próbki 40/40 i pełny filtr 561/561 przechodzą bez skipów ani mismatchy. Testy projektu Intel8086: 4 403/4 403. Do odtworzenia buildu w tym środowisku potrzebne było `-m:1`; runner xUnit wymagał uruchomienia poza ograniczeniem lokalnych socketów.
