# Wektory Z80 — SingleStepTests/z80

Źródło: <https://github.com/SingleStepTests/z80>, commit
`ebe1875d48f374bcfd4b505d8eb8ee751568b5f7`; licencja MIT w `LICENSE-SingleStepTests.txt`.
Wektory wygenerowano z rdzenia Ares; autor źródła zaznacza, że mogą zawierać błędy i nie są potwierdzone na krzemie.

## Zawartość próbki

| katalog źródłowy | plik w repo | opkod | testów z 1000 |
|---|---|---:|---:|
| `v1/00.json` | `00.json` | `00` (NOP) | pierwsze 20 |
| `v1/NN.json` | `NN.json` | `40`–`7F` bez `76` (63), `06 0E 16 1E 26 2E 36 3E` (8), `0A 1A 3A`, `02 12 32` (6) | pierwsze 20 z każdego pliku |

Zawartość etapu Z2 to 77 opkodów, 77 plików i 1540 przypadków; wraz z `00` manifest obejmuje 78 plików i 1560 przypadków.
Cały katalog zajmuje 1,1 MB (`du -sh`). Opkod `76` (`HALT`) jest celowo wyłączony do Z5. Pełny zestaw źródłowy ma po
1000 przypadków na plik i nie został uruchomiony poza repo.

Przycięcie jest prefiksem źródłowego JSON bez modyfikowania treści rekordów:

```python
import json
source = json.load(open("00.json", encoding="utf-8"))
json.dump(source[:20], open("00.sample.json", "w", encoding="utf-8"), separators=(",", ":"))
```

Testy nie pobierają danych z sieci. `Z80VectorManifest` jest listą opkodów objętych implementacją; test kontroluje,
że każdy plik z manifestu istnieje i zawiera 20 przypadków. Dla Z1 i Z2 próbki stanowią pierwsze 20 rekordów z pliku
źródłowego, w kolejności bez zmian.

## Uwagi do formatu

- `cycles` ma jeden wpis na T-state: `[adres|null, data|null, "piny"]`; test porównuje piny i wartości cykl po cyklu,
  rzeczywiste odczyty/zapisy pamięci, cały stan końcowy i RAM po nałożeniu `initial.ram` oraz `final.ram`.
- Wektor próbki ma pola `initial`, `final`, `ram`, `ports` oraz piny `r/w/m/i`.
- Odczytywane fakty o opcjach generatora pochodzą z README źródłowego w przypiętym commicie. Generator używa
  uproszczonych impulsów MRQ/RD i MRQ/WR przez 1 T-state i wyświetla refresh na adresie; CMOS wyłączony.
- Pełne 1000 testów/opkod uruchamia się poza repo; zakres próbek nie jest pełną walidacją.
