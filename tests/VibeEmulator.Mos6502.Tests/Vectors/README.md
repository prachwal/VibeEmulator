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

Wszystkie pliki mają 100 testów. Rozmiar katalogu: `du -sh` ≈ 3,9 MB (wszystkie pliki).

| katalog | liczba plików | opkody (hex, małymi literami) | uwagi |
|---|---|---|---|
| `nmos6502/` | 56 | a9 a5 b5 ad bd a2 a6 a0 a4 85 8d 9d 86 84 69 e9 e8 ca c8 88 aa a8 8a 98 ea 4c ee e6 ce c6 b9 a1 b1 b6 ae be b4 ac bc 95 99 81 91 96 8e 94 8c 18 38 58 78 b8 d8 f8 ba 9a | wariant NMOS 6502 (`CpuVariants.Nmos6502`), bez `00` (BRK) |
| `wdc65c02/` | 62 | 1a 3a 64 74 9c 9e c6 ce e6 ee a9 a5 b5 ad bd a2 a6 a0 a4 85 8d 9d 86 84 69 e9 e8 ca c8 88 aa a8 8a 98 ea 4c b9 a1 b1 b6 ae be b4 ac bc 95 99 81 91 96 8e 94 8c 18 38 58 78 b8 d8 f8 ba 9a | wariant CMOS 65C02 (`CpuVariants.Cmos65C02`), bez `00` (BRK); w tym opkody dziedziczone z NMOS |

Podział `wdc65c02/`: dodatki 65C02 `1a 3a 64 74 9c 9e c6 ce e6 ee`; dziedziczone z NMOS (zgodne z wektorami NMOS, sprawdzane też
na 65C02) `a9 a5 b5 ad bd a2 a6 a0 a4 85 8d 9d 86 84 69 e9 e8 ca c8 88 aa a8 8a 98 ea 4c`; nowe opkody etapu 2a `b9 a1 b1 b6 ae be b4 ac bc 95 99 81 91 96 8e 94 8c 18 38 58 78 b8 d8 f8 ba 9a`.

Plik `LICENSE-SingleStepTests.txt` zawiera pełną treść licencji MIT z repozytorium źródłowego.
