# Wektory Z80 — SingleStepTests/z80

Źródło: <https://github.com/SingleStepTests/z80>, commit
`ebe1875d48f374bcfd4b505d8eb8ee751568b5f7`; licencja MIT w `LICENSE-SingleStepTests.txt`.
Wektory wygenerowano z rdzenia Ares; autor źródła zaznacza, że mogą zawierać błędy i nie są potwierdzone na krzemie.

## Zawartość próbki

| katalog źródłowy | plik w repo | opkod | testów z 1000 |
|---|---|---:|---:|
| `v1/00.json` | `00.json` | `00` (NOP) | pierwsze 20 |
| `v1/NN.json` | `NN.json` | `40`–`7F` bez `76` (63), `06 0E 16 1E 26 2E 36 3E` (8), `0A 1A 3A`, `02 12 32` (6) | pierwsze 20 z każdego pliku |
| `v1/NN.json` | `NN.json` | Z3: `80`–`BF` (64), `C6 CE D6 DE E6 EE F6 FE` (8), INC/DEC (16), `27 2F 37 3F` (4) | pierwsze 20 z każdego pliku |
| `v1/NN.json` | `NN.json` | Z4: `01 11 21 31 22 2A F9`, INC/DEC rr (8), ADD HL,rr (4), PUSH/POP (8), `E3 EB 08 D9` | pierwsze 20 z każdego pliku |
| `v1/NN.json` | `NN.json` | Z5: JP (10), JR/DJNZ (6), CALL (9), RET (9), RST (8), `76 F3 FB` | pierwsze 20 z każdego pliku |
| `v1/cb NN.json` | `cb NN.json` | Z6: `CB` `00`–`FF` (256) | pierwsze 20 z każdego pliku |
| `v1/NN.json` | `NN.json` | Z6: `07 0F 17 1F` (`RLCA`/`RRCA`/`RLA`/`RRA`) | pierwsze 20 z każdego pliku |
| `v1/ed NN.json` | `ed NN.json` | Z7: `40`–`7F` (64), `A0`–`A3`, `A8`–`AB`, `B0`–`B3`, `B8`–`BB` (16) | pierwsze 20 z każdego pliku |
| `v1/dd NN.json`, `v1/fd NN.json` | `dd NN.json`, `fd NN.json` | Z8: wszystkie bajty poza `CB`, `DD`, `ED`, `FD` (252 na prefiks) | pierwsze 20 z każdego pliku |
| `v1/NN.json` | `NN.json` | Z7: `DB D3` (`IN A,(n)` / `OUT (n),A`) | pierwsze 20 z każdego pliku |

Zawartość etapu Z2 to 77 opkodów, 77 plików i 1540 przypadków; wraz z `00` manifest obejmuje 78 plików i 1560 przypadków.
Z3 dodaje 92 pliki / 1840 przypadków; Z4 dodaje 31 plików / 620 przypadków;
Z5 dodaje 45 plików / 900 przypadków; Z6 dodaje 260 plików / 5200 przypadków; Z7 dodaje 82 pliki / 1640 przypadków; Z8 dodaje 504 pliki / 10 080 przypadków;
razem manifest obejmuje 1092 pliki / 21 840 przypadków. Katalog zajmuje 18 MB (`du -sh`, 2026-10-22).
HALT obejmuje jedną instrukcję; dalsze fetch/refresh i przerwania pozostają w Z10. Pełny zestaw źródłowy ma po
1000 przypadków na plik. Pełne Z3–Z7 poza repo: **510 000 przeszło / 0 pominięto / 0 niezgodnych**
(stan, RAM, cycles i accessy pamięci); pełnych Z1–Z2 nie uruchomiono.

Przycięcie jest prefiksem źródłowego JSON bez modyfikowania treści rekordów:

```python
import json
source = json.load(open("00.json", encoding="utf-8"))
json.dump(source[:20], open("00.sample.json", "w", encoding="utf-8"), separators=(",", ":"))
```

Testy nie pobierają danych z sieci. `Z80VectorManifest` jest listą opkodów objętych implementacją; test kontroluje,
że każdy plik z manifestu istnieje i zawiera 20 przypadków. Dla Z1–Z8 próbki stanowią pierwsze 20 rekordów z pliku
źródłowego, w kolejności bez zmian.

## Uwagi do formatu

- `cycles` ma jeden wpis na T-state: `[adres|null, data|null, "piny"]`; test porównuje piny i wartości cykl po cyklu,
  rzeczywiste odczyty/zapisy pamięci, cały stan końcowy i RAM po nałożeniu `initial.ram` oraz `final.ram`.
- Wektor próbki ma pola `initial`, `final`, `ram`, `ports` oraz piny `r/w/m/i`.
- Odczytywane fakty o opcjach generatora pochodzą z README źródłowego w przypiętym commicie. Generator używa
  uproszczonych impulsów MRQ/RD i MRQ/WR przez 1 T-state i wyświetla refresh na adresie; CMOS wyłączony.
- Pełne 1000 testów/opkod uruchamia się poza repo; zakres próbek nie jest pełną walidacją.
