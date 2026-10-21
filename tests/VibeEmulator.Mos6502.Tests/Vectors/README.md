# Wektory referencyjne SingleStepTests/65x02

- Źródło: <https://github.com/SingleStepTests/65x02> (Thomas Harte et al.), commit
  `2f6980a2d95757486c7bee24355c360e40e2a224`.
- Pliki źródłowe: `wdc65c02/v1/<opkod>.json` i `6502/v1/<opkod>.json` (po 10 000 testów każdy).
- Licencja: MIT — tekst w `LICENSE-SingleStepTests.txt`.
- **Przycięcie:** w repo leżą wyłącznie pierwsze **100 testów** z każdego pliku (kolejność źródłowa, bez zmian
  treści, zapis zwarty JSON). Pełne zestawy nie są w repo; do pełnej walidacji pobierz je z podanego commita.
- Testy czytają pliki z katalogu wyjściowego (`Content` + `CopyToOutputDirectory`), bez dostępu do sieci.

Format testu: `{name, initial:{pc,s,a,x,y,p,ram}, final:{...}, cycles:[[adres,wartość,"read"|"write"],…]}`.

## Zawartość

| katalog | opkody | uwagi |
|---|---|---|
| `wdc65c02/` | `1a 3a 64 74 9c 9e c6 ce e6 ee` | wariant CMOS 65C02 |
| `nmos6502/` | `a9 a5 b5 ad bd a2 a6 a0 a4 85 8d 9d 86 84 69 e9 e8 ca c8 88 aa a8 8a 98 ea 4c c6 ce e6 ee` | wariant NMOS 6502, bez `00` (BRK) |

Plik `LICENSE-SingleStepTests.txt` zawiera pełną treść licencji MIT z repozytorium źródłowego.
