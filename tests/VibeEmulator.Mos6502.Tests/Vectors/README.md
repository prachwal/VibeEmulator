# Wektory referencyjne SingleStepTests/65x02

- Źródło: <https://github.com/SingleStepTests/65x02> (Thomas Harte et al.), commit
  `2f6980a2d95757486c7bee24355c360e40e2a224`.
- Pliki źródłowe: `wdc65c02/v1/<opkod>.json` i `6502/v1/<opkod>.json` (po 10 000 testów każdy).
- Licencja: MIT — tekst w `LICENSE-SingleStepTests.txt`.
- **Przycięcie:** w repo leżą wyłącznie pierwsze **100 testów** z każdego pliku (kolejność źródłowa, bez zmian
  treści, zapis zwarty JSON: `json.dumps(..., separators=(",", ":"))`). Pełne zestawy nie są w repo; do pełnej
  walidacji pobierz je z podanego commita (`https://raw.githubusercontent.com/SingleStepTests/65x02/<commit>/<wariant>/v1/<opkod>.json`).
- Testy czytają pliki z katalogu wyjściowego (`Content` + `CopyToOutputDirectory`), bez dostępu do sieci.
- Opkod `00` (BRK) nie ma próbki: BRK nie jest walidowany wektorami w tym etapie.

Format testu: `{name, initial:{pc,s,a,x,y,p,ram}, final:{...}, cycles:[[adres,wartość,"read"|"write"],…]}`.

## Zawartość

Wszystkie pliki mają 100 testów. Rozmiar katalogu: `du -sh` ≈ 7,7 MB (wszystkie pliki).

| katalog | liczba plików | opkody (hex, małymi literami) | uwagi |
|---|---|---|---|
| `nmos6502/` | 110 | 01 05 09 0d 11 15 18 19 1d 21 24 25 29 2c 2d 31 35 38 39 3d 41 45 49 4c 4d 51 55 58 59 5d 61 65 69 6d 71 75 78 79 7d 81 84 85 86 88 8a 8c 8d 8e 91 94 95 96 98 99 9a 9d a0 a1 a2 a4 a5 a6 a8 a9 aa ac ad ae b1 b4 b5 b6 b8 b9 ba bc bd be c0 c1 c4 c5 c6 c8 c9 ca cc cd ce d1 d5 d8 d9 dd e0 e1 e4 e5 e6 e8 e9 ea ec ed ee f1 f5 f8 f9 fd | wariant NMOS 6502 (`CpuVariants.Nmos6502`), bez `00` (BRK) |
| `wdc65c02/` | 116 | 01 05 09 0d 11 15 18 19 1a 1d 21 24 25 29 2c 2d 31 35 38 39 3a 3d 41 45 49 4c 4d 51 55 58 59 5d 61 64 65 69 6d 71 74 75 78 79 7d 81 84 85 86 88 8a 8c 8d 8e 91 94 95 96 98 99 9a 9c 9d 9e a0 a1 a2 a4 a5 a6 a8 a9 aa ac ad ae b1 b4 b5 b6 b8 b9 ba bc bd be c0 c1 c4 c5 c6 c8 c9 ca cc cd ce d1 d5 d8 d9 dd e0 e1 e4 e5 e6 e8 e9 ea ec ed ee f1 f5 f8 f9 fd | wariant CMOS 65C02 (`CpuVariants.Cmos65C02`), bez `00` (BRK); w tym opkody dziedziczone z NMOS |

Podział `wdc65c02/`: dodatki 65C02 `1a 3a 64 74 9c 9e c6 ce e6 ee`; reszta to opkody dziedziczone z NMOS,
sprawdzane też na 65C02. Etap A (`docs/plan-c64-stages/A-logika-i-porownania.md`) dodał do obu katalogów 54 pliki:
logikę (`01 05 09 0d 11 15 19 1d 21 25 29 2d 31 35 39 3d 41 45 49 4d 51 55 59 5d`),
ADC/SBC w pozostałych trybach (`61 65 6d 71 75 79 7d e1 e5 ed f1 f5 f9 fd`),
CMP/CPX/CPY (`c1 c5 c9 cd d1 d5 d9 dd e0 e4 ec c0 c4 cc`) i BIT (`24 2c`).

Plik `LICENSE-SingleStepTests.txt` zawiera pełną treść licencji MIT z repozytorium źródłowego.
