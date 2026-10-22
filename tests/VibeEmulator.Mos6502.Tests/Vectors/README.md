# Wektory referencyjne SingleStepTests/65x02

- Źródło: <https://github.com/SingleStepTests/65x02> (Thomas Harte et al.), commit
  `2f6980a2d95757486c7bee24355c360e40e2a224`.
- Pliki źródłowe: `wdc65c02/v1/<opkod>.json` i `6502/v1/<opkod>.json` (po 10 000 testów każdy).
- Licencja: MIT — tekst w `LICENSE-SingleStepTests.txt`.
- **Przycięcie:** w repo leżą wyłącznie pierwsze **100 testów** z każdego pliku (kolejność źródłowa, bez zmian
  treści, zapis zwarty JSON: `json.dumps(..., separators=(",", ":"))`). Pełne zestawy nie są w repo; do pełnej
  walidacji pobierz je z podanego commita (`https://raw.githubusercontent.com/SingleStepTests/65x02/<commit>/<wariant>/v1/<opkod>.json`).
- Testy czytają pliki z katalogu wyjściowego (`Content` + `CopyToOutputDirectory`), bez dostępu do sieci.
- Opkod `00` (BRK) ma próbkę od etapu F — sekwencja siedmiu cykli jest walidowana wektorami.
- Opkody `cb` (WAI) i `db` (STP) **nie mają próbek**: pliki źródłowe w SingleStepTests są puste.

Format testu: `{name, initial:{pc,s,a,x,y,p,ram}, final:{...}, cycles:[[adres,wartość,"read"|"write"],…]}`.

## Zawartość

Wszystkie pliki mają 100 testów. Rozmiar katalogu: `du -sh` ≈ 14 MB (wszystkie pliki).

| katalog | liczba plików | opkody (hex, małymi literami) | uwagi |
|---|---|---|---|
| `nmos6502/` | 151 | 00 01 05 06 08 09 0a 0d 0e 10 11 15 16 18 19 1d 1e 20 21 24 25 26 28 29 2a 2c 2d 2e 30 31 35 36 38 39 3d 3e 40 41 45 46 48 49 4a 4c 4d 4e 50 51 55 56 58 59 5d 5e 60 61 65 66 68 69 6a 6c 6d 6e 70 71 75 76 78 79 7d 7e 81 84 85 86 88 8a 8c 8d 8e 90 91 94 95 96 98 99 9a 9d a0 a1 a2 a4 a5 a6 a8 a9 aa ac ad ae b0 b1 b4 b5 b6 b8 b9 ba bc bd be c0 c1 c4 c5 c6 c8 c9 ca cc cd ce d0 d1 d5 d6 d8 d9 dd de e0 e1 e4 e5 e6 e8 e9 ea ec ed ee f0 f1 f5 f6 f8 f9 fd fe | wariant NMOS 6502 (`CpuVariants.Nmos6502`), w tym `00` (BRK) |
| `wdc65c02/` | 254 | 00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f 10 11 12 13 14 15 16 17 18 19 1a 1b 1c 1d 1e 1f 20 21 22 23 24 25 26 27 28 29 2a 2b 2c 2d 2e 2f 30 31 32 33 34 35 36 37 38 39 3a 3b 3c 3d 3e 3f 40 41 42 43 44 45 46 47 48 49 4a 4b 4c 4d 4e 4f 50 51 52 53 54 55 56 57 58 59 5a 5b 5c 5d 5e 5f 60 61 62 63 64 65 66 67 68 69 6a 6b 6c 6d 6e 6f 70 71 72 73 74 75 76 77 78 79 7a 7b 7c 7d 7e 7f 80 81 82 83 84 85 86 87 88 89 8a 8b 8c 8d 8e 8f 90 91 92 93 94 95 96 97 98 99 9a 9b 9c 9d 9e 9f a0 a1 a2 a3 a4 a5 a6 a7 a8 a9 aa ab ac ad ae af b0 b1 b2 b3 b4 b5 b6 b7 b8 b9 ba bb bc bd be bf c0 c1 c2 c3 c4 c5 c6 c7 c8 c9 ca cc cd ce cf d0 d1 d2 d3 d4 d5 d6 d7 d8 d9 da dc dd de df e0 e1 e2 e3 e4 e5 e6 e7 e8 e9 ea eb ec ed ee ef f0 f1 f2 f3 f4 f5 f6 f7 f8 f9 fa fb fc fd fe ff | wariant CMOS 65C02 (`CpuVariants.Cmos65C02`) — **256/256 bajtów** od etapu I; bez `cb`/`db` (WAI/STP — puste pliki w źródle) |

Podział `wdc65c02/`: dodatki 65C02 z etapu 2a (`1a 3a 64 74 9c 9e`) i etapu H (61 opkodów: RMB/SMB/BBR/BBS,
BRA, `(zp)`, BIT zpx/abx/imm, TSB/TRB, PHX/PHY/PLX/PLY, JMP (abs,X)), 44 NOP-y z etapu I; reszta to opkody
dziedziczone z NMOS, sprawdzane też na 65C02.
Etap A (`docs/plan-c64-stages/A-logika-i-porownania.md`) dodał do obu katalogów 54 pliki,
etap B (`docs/plan-c64-stages/B-przesuniecia-i-rotacje.md`) — 24 pliki,
etap C (`docs/plan-c64-stages/C-rozgalezienia.md`) — 8 plików (`10 30 50 70 90 b0 d0 f0`),
etap D (`docs/plan-c64-stages/D-stos.md`) — 4 pliki (`08 28 48 68`),
etap E (`docs/plan-c64-stages/E-sterowanie.md`) — 4 pliki (`20 40 60 6c`),
etap F (`docs/plan-c64-stages/F-przerwania.md`) — 1 plik (`00`, BRK),
etap H (`docs/plan-c64-stages/H-nowe-opkody-65c02.md`) — 53 pliki (nowe opkody 65C02; bez `cb`/`db`),
etap I (`docs/plan-c64-stages/I-nop-65c02.md`) — 44 pliki (niezdefiniowane NOP-y 65C02; 65C02 ma 256/256 bajtów):
logikę (`01 05 09 0d 11 15 19 1d 21 25 29 2d 31 35 39 3d 41 45 49 4d 51 55 59 5d`),
ADC/SBC w pozostałych trybach (`61 65 6d 71 75 79 7d e1 e5 ed f1 f5 f9 fd`),
CMP/CPX/CPY (`c1 c5 c9 cd d1 d5 d9 dd e0 e4 ec c0 c4 cc`) i BIT (`24 2c`).

Plik `LICENSE-SingleStepTests.txt` zawiera pełną treść licencji MIT z repozytorium źródłowego.
