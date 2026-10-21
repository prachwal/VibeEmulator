---
applyTo: "tests/**/Vectors/**"
description: "Wektory referencyjne SingleStepTests/65x02 w repo: nie edytuj treści próbki, przycięcie do 100 testów, pochodzenie i commit, licencja MIT, brak sieci w testach, format JSON i zasady dodawania próbek."
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
