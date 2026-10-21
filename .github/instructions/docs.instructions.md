---
applyTo: "docs/**/*.md"
description: "Dokumentacja w docs/: po polsku, fakt tylko ze źródłem lub z oznaczeniem niezweryfikowane, bez duplikowania stanu z planu, działające linki."
---

# Dokumentacja (`docs/`)

## 1. Dyscyplina faktów

- Fakt trafia do dokumentu **tylko po przeczytaniu źródła pierwotnego** albo sprawdzeniu programowym. Podaj źródło (odnośnik,
  wersja/rewizja, plik) w tym samym akapicie lub tabeli.
- Czego nie sprawdzono, oznacz **(niezweryfikowane)**. Czego nie przeczytano, wpisz do listy „Nie przeczytano” w pliku
  `docs/cpu/sources.md`. Nie przepisuj twierdzeń innego dokumentu (także z repo) jako własnych ustaleń — sprawdź je.
- Rozbieżność źródeł opisz wprost (kto co twierdzi, które jest zmierzone); nie wybieraj po cichu.
- Liczby podawaj z zakresem ich ważności: np. „próbka 100 testów w repo, pełne 10 000 poza repo”, a nie „zwalidowane”.
- Sprzeczne lub poprawione twierdzenie z historii dokumentu zapisz w „Historia poprawek” (`docs/cpu/sources.md`), nie w indeksie.

## 2. Podział treści

- `docs/plan-c64-cycle-exact.md` utrzymuje **stan implementacji** (Stan realizacji, etapy, wyniki). `docs/cpu/` opisuje
  **sprzęt i testy**. Nie kopiuj liczb stanu do `docs/cpu/`: daj odnośnik do planu.
- Zmiana stanu implementacji (nowy opkod, etap, wynik wektorów) → zaktualizuj plan w tym samym commicie.
- Tabele CSV w `docs/cpu/opcodes/` są wygenerowane ze źródeł i zweryfikowane wektorami; nie edytuj ich ręcznie bez powtórzenia
  weryfikacji (metoda: `docs/cpu/opcodes.md`).

## 3. Forma

- Po polsku; nazwy symboli i poleceń bez tłumaczenia, w znacznikach kodu. Tabele zamiast długich list tam, gdzie porównujesz.
- Linki względne muszą wskazywać istniejące pliki — sprawdź je po zmianie.
