---
applyTo: "tests/**/Vectors/**"
description: "Wektory SingleStepTests/65x02 i Z80: przypięte źródło/licencja, próbka bez edycji danych, testy offline, format cykli, różnica próbki i pełnego zestawu."
---

# Wektory referencyjne (`tests/**/Vectors/`)

Pliki w `tests/VibeEmulator.Mos6502.Tests/Vectors/` to **dane referencyjne**, nie kod: obowiązuje je zakaz ręcznej edycji treści.

## Pochodzenie i przycięcie

- Źródło: <https://github.com/SingleStepTests/65x02>, commit z `Vectors/README.md`; licencja MIT w `LICENSE-SingleStepTests.txt`.
  Nie usuwaj pliku licencji ani informacji o commicie i wariancie (`6502/v1`, `wdc65c02/v1`).
- W repo leży wyłącznie **pierwsze 100 testów** z każdego pliku, w kolejności źródłowej i zwartym JSON. Nie „uzupełniaj”
  plików do 10 000 testów i nie zmieniaj formatowania — to zmiana danych, nie poprawka.
- Pełne zestawy pobiera się **poza repo** z podanego commita. Testy czytają pliki z katalogu wyjściowego
  (`Content` + `CopyToOutputDirectory`) i nigdy nie sięgają do sieci.
- Brak próbki dla opkodu (np. `00`/BRK) to świadoma decyzja etapu, nie brak pliku do dorobienia po cichu.

## Zmiany w wektorach

- Dodanie lub zmiana próbki wymaga: źródła (commit + plik), powodu (np. nowy opkod), wpisu w `Vectors/README.md`
  (tabela katalogów, plików i opkodów) oraz testu, który tę próbkę czyta.
- Nie „naprawiaj” wektora, gdy test nie przechodzi. Wektor jest źródłem prawdy — różnicę opisuje `Skip` w teście
  (zasady w `csharp-tests.instructions.md`) albo pozycja w `docs/plan-c64-cycle-exact.md`.
- Nazwy plików: `<opkod>.json` małymi literami hex; katalogi `nmos6502/` i `wdc65c02/` odpowiadają wariantom `CpuVariant`.
- Format bez zmian: `{name, initial:{pc,s,a,x,y,p,ram}, final:{…}, cycles:[[adres,wartość,"read"|"write"],…]}`.
  Nie dodawaj pól ani komentarzy do JSON.
- Wyniki podawaj z rozróżnieniem próbki i pełnego zestawu: „próbka 100 testów w repo” to nie „pełna walidacja”.

## Wektory Z80 (`SingleStepTests/z80`)

- Źródło i pin: `tests/VibeEmulator.Z80.Tests/Vectors/README.md`; MIT w `LICENSE-SingleStepTests.txt`.
  Pliki źródłowe mają 1000 testów/opkod i pochodzą z Ares; nie są potwierdzeniem zachowania na krzemie.
- W repo dla Z1–Z2 leży próbka pierwszych **20 rekordów** na plik. Nie zmieniaj treści rekordów ani ich kolejności;
  zachowuj zwarty JSON. Manifest `Z80VectorManifest` określa opkody i wymagane pliki próbki.
- Format jest odmienny od 65x02: `cycles` ma jeden wpis na **T-state** `[adres|null, dana|null, "piny"]`; `ports`
  jest opcjonalne. Kolejność pinów to `r w m i`. `null` oznacza elektrycznie nieistotną/odłączoną magistralę i nie
  porównuje się tej wartości. Nazwy przyszłych prefiksowych plików zawierają spacje — cytuj ścieżki.
- Runner porównuje pełny stan, RAM, piny i dane T-state po T-state oraz rzeczywiste accessy pamięci w taktowanych
  fazach. Testy nigdy nie pobierają wektorów z sieci.
- Pełne 1000/opkod uruchamiaj poza repo. Raportuj „nie uruchomiono”, jeśli pełnego zestawu nie wykonano; zielona próbka
  20/opkod nie jest pełną walidacją.
