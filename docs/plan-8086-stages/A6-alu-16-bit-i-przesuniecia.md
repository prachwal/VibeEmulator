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

## Kryteria akceptacji

- [ ] Każda instrukcja/form ma wektor próbki albo oznaczoną lukę źródłową.
- [ ] Testy flag obejmują boundary wartości i kontrolują flagi zachowywane.
- [ ] Operandy pamięciowe mają kompletny log read/write/cycles, łącznie z RMW.
- [ ] Rozbieżności wynikające z nieokreślonych flag są maskowane wyłącznie na podstawie wiarygodnego źródła.
- [ ] Próbki A3–A6 zielone; brak pełnego runu opisany jawnie.

## Pułapki

1. Nie używać reguł count-maskowania z 80186 lub 80286; wariantem jest 8086 i sprawdza się go wektorami/manualem.
2. Count 0 i count większy od szerokości mogą mieć różne skutki dla flag; nie ekstrapolować z jednego przypadku.
3. Carry flag rotacji i overflow flag zależą od count; testować bity osobno.
4. `INC/DEC` nie mogą przypadkowo nadpisywać flagi, którą instrukcja zachowuje.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

| data | grupa | pass/manifest | wyniki próbki | uwagi |
|---|---|---|---|---|
| — | — | — | nie uruchomiono | szczegóły count potwierdzić ze źródłem |
