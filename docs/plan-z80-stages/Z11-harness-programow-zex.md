# Etap Z11 — harness programów (CP/M, zexdoc/zexall)

Poziom: **D** · Pliki wektorów: **0** · Nowe narzędzie: 1

Odpowiednik etapów L–O serii 6502: uruchomienie **programów testowych** na rdzeniu. Jest to druga, niezależna
od Ares wyrocznia (R1 w [README.md](README.md)) i jedyna dla zachowań, których wektory nie pokrywają.

## Zakres

- Nowe narzędzie `tools/Z80Harness` (konsola, `Release`), wzorowane na `tools/CpuHarness`; **osobne**, bo
  `CpuHarness` jest związany z `StateMachineCpuCore` (6502), a jego testy nie powinny się zmieniać.
- Wejście: plik `.com` ładowany pod `0100h`; limit T-state; format `text`/`json`; kody wyjścia `0/2/3/4` jak w etapie L.
- Emulacja minimalnego środowiska CP/M: wywołanie `CALL 5` obsługiwane w harnessie (funkcje wypisywania znaku i
  łańcucha), `JP 0` = koniec programu. **Szczegóły funkcji BDOS i adresów — z dokumentacji zexdoc/zexall, nieprzeczytane
  (niezweryfikowane).**
- Programy: `zexdoc` (flagi udokumentowane) i `zexall` (także `X`/`Y`); oczekiwane sumy kontrolne z samych programów.
- Kandydaci uzupełniający: zestawy `z80test`, Fuse — **obecność i licencje do sprawdzenia** przed użyciem.

## Ograniczenia (zapisane wprost)

- **Binaria testów nie trafiają do repo**, dopóki nie sprawdzono licencji każdego z nich (jak w etapie L dla Dormanna).
  Ścieżkę podaje użytkownik; testy jednostkowe harnessu działają na **syntetycznych obrazach** zbudowanych w pamięci.
- Przebieg zexall to rząd miliardów T-state — czas i limit podaj w dzienniku, nie w CI.
- Harness tylko dla Z80; nie dołączamy go do pipeline'u CI, dopóki czas przebiegu nie jest znany.

## Kryteria akceptacji

- [ ] Testy harnessu na syntetycznych obrazach: wykrycie `CALL 5`, wypisanie łańcucha, `JP 0`, limit T-state, błąd wejścia.
- [ ] `zexdoc`: wynik, liczba T-state i czas wpisane w dzienniku (po pobraniu binarium poza repo).
- [ ] `zexall`: wynik jw.; każda rozbieżność zapisana wprost i przeanalizowana względem wektorów (R1).
- [ ] Definicja ukończenia.

## Pułapki

1. **Wolny test = brak wyniku, nie błąd.** Bez limitu T-state harness wisi; limit jest obowiązkowy (R4).
2. **Wektory zielone, zexall czerwony** oznacza błąd poza pokryciem wektorów (przerwania, łańcuchy prefiksów, niezdefiniowane `ED`) albo błąd w wektorach.
3. **Nie zmieniaj rdzenia pod binarium.** Rozbieżność = analiza źródła, test jednostkowy, dopiero potem poprawka.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
