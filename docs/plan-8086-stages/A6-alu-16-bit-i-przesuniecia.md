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
- [x] Próbki A3–A6 zielone: projekt 8086 1 440/1 440, 0 skipów; nieobjęte formy i pełne źródło opisane jako nieuruchomione.
- [x] Częściowe kryterium 1: osiem word AX-immediate form przechodzi próbki CPU 160/160 i pełny filtrowany zbiór 7 986/7 986.
- [x] Częściowe kryterium 2: siedemnaście word ALU register-direct form przechodzi próbki CPU 340/340 i pełny filtrowany zbiór `mod=11` 4 180/4 180; formy pamięciowe pozostają poza zakresem.

## Następny podetap przed implementacją

1. Dodać register-direct word ALU ModR/M dla `01/03`, `09/0B`, `11/13`, `19/1B`, `21/23`, `29/2B`, `31/33`, `39/3B`, `85`, wyłącznie `mod=11`.
2. Wybrać po 20 pierwszych rekordów źródłowych spełniających dokładne opcode i `mod=11`; porównać cały stan, kolejkę i każdy T-state.
3. Potwierdzić w przypiętym źródle liczbę 4 180 rekordów i trzy T-states dla wszystkich wybranych form.
4. Nie wnioskować o operandach pamięciowych ani grupach immediate do r/m.

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

Zaimplementować słowne `INC/DEC` register-direct (`40`–`4F`), porównując zachowanie CF i pozostałe flagi z fixture. Najpierw sprawdzić dokładne profile źródłowe i liczbę rekordów; nie rozszerzać na pamięciowe grupy unary bez osobnych próbek i cykli.


### 2026-10-23 — word ALU register-direct ModR/M (`mod=11`)

Dodano 17 kodowań: `01/03`, `09/0B`, `11/13`, `19/1B`, `21/23`, `29/2B`, `31/33`, `39/3B`, `85`. Próbki 20/opcode porównują stan końcowy, kolejkę i trzy T-states: **340/340**. Pełny filtrowany upstream: **4 180/4 180**, 0 skipów i mismatchy.
