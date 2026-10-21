---
applyTo: "docs/**/*.md,docs/**/*.csv"
description: "Dokumentacja i tabele w docs/: po polsku, fakt tylko ze źródłem lub z oznaczeniem niezweryfikowane, bez duplikowania stanu z planu, działające linki, CSV tylko po powtórzeniu weryfikacji."
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
- Tabele CSV w `docs/cpu/opcodes/` rządzą się osobnymi regułami (zob. sekcja 4).

## 3. Forma

- Po polsku; nazwy symboli i poleceń bez tłumaczenia, w znacznikach kodu. Tabele zamiast długich list tam, gdzie porównujesz.
- Linki względne muszą wskazywać istniejące pliki — sprawdź je po zmianie.

## 4. Tabele CSV (`docs/cpu/opcodes/`)

- CSV to **dane**, nie dokument narracyjny: 256 wierszy danych + nagłówek. Opkody dwucyfrowe, hex **wielkimi literami**
  (`0A`, `FF`); kolumny i ich znaczenie opisane w `docs/cpu/opcodes.md`. Nie zmieniaj nagłówka ani kolejności kolumn bez
  aktualizacji tego opisu. Uwaga: pliki wektorów używają hex małymi literami (`0a.json`) — to dwie różne konwencje.
- Edycja ręczna jest dozwolona tylko razem z **powtórzeniem weryfikacji** opisanym w `docs/cpu/opcodes.md`; wynik
  (liczba niezgodności, źródła, data) wpisz do tabeli „Weryfikacja” w tym pliku.
- Liczby w CSV muszą zgadzać się z `docs/cpu/opcodes.md` i z próbką wektorów; sprzeczność oznacz, nie „wygładzaj”.
- Brak danych wypełniaj pustym polem, nie `?`, `0` ani `n/a`.
