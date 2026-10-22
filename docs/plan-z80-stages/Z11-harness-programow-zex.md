# Etap Z11 — harness programów (CP/M, zexdoc/zexall)

Poziom: **D** · Pliki wektorów: **0** · Nowe narzędzie: 1

Odpowiednik etapów L–O serii 6502: uruchomienie **programów testowych** na rdzeniu. Jest to druga, niezależna
od Ares wyrocznia (R1 w [README.md](README.md)) i jedyna dla zachowań, których wektory nie pokrywają.

## Zakres

- Nowe narzędzie `tools/Z80Harness` (konsola, `Release`), wzorowane na `tools/CpuHarness`; **osobne**, bo
  `CpuHarness` jest związany z `StateMachineCpuCore` (6502), a jego testy nie powinny się zmieniać.
- Wejście: plik `.com` ładowany pod `0100h`; limit T-state; format `text`/`json`; kody wyjścia `0/2/3/4` jak w etapie L.
- Minimalne środowisko CP/M ze źródła `zexdoc.z80`: funkcja BDOS 9 wypisuje łańcuch spod `DE` do `$`, stos
  inicjuje się z worda spod adresu 6, a `JP 0` kończy test. Harness obsługuje też funkcję BDOS 2 (znak z `E`),
  zgodnie z zakresem tego etapu. Adres stosu `F000h` i wektor BDOS pod `0005h` są syntetycznym środowiskiem,
  nie pełną emulacją CP/M.
- Programy: `zexdoc` (flagi udokumentowane) i `zexall` (także `X`/`Y`); oczekiwane sumy kontrolne z samych programów.
- Wynik zawiera surowy tekst programu, T-state i czas; `ERROR` zgłoszone przez program daje kod 2, limit kod 3.
- `zexdoc.com` i `zexall.com` są GPL-2.0. Pobrane do ignorowanego `.test-data/ZEXALL/` z repozytorium
  `agn453/ZEXALL`, commit `8f71d418bae69a476a5a0e5c6e122c8801b8d9f4`; binaria i surowe wyniki pozostają poza repo.
- Kandydaci uzupełniający: zestawy `z80test`, Fuse — **obecność i licencje do sprawdzenia** przed użyciem.

## Ograniczenia (zapisane wprost)

- **Binaria testów nie trafiają do repo.** ZEX jest GPL-2.0; lokalny katalog `.test-data/` jest ignorowany przez Git.
  Testy jednostkowe harnessu działają na **syntetycznych obrazach** zbudowanych w pamięci.
- Przebieg zexall to rząd miliardów T-state — czas i limit podaj w dzienniku, nie w CI.
- Harness tylko dla Z80; nie dołączamy go do pipeline'u CI, dopóki czas przebiegu nie jest znany.

## Kryteria akceptacji

- [x] Testy harnessu na syntetycznych obrazach: wykrycie `CALL 5`, wypisanie znaku i łańcucha, `JP 0`, limit T-state,
  nieobsługiwana funkcja BDOS oraz walidacja danych wejściowych — `ZexHarnessTests`, 13/13.
- [x] `zexdoc`: PASS, 46 734 975 782 T-state, 17:19.627 czasu harnessu (17:20.73 wall); 67 grup `OK`, 0 `ERROR`.
- [x] `zexall`: PASS, 46 734 975 782 T-state, 18:47.655 czasu harnessu (18:48.23 wall); 67 grup `OK`, 0 `ERROR`.
  Nie wystąpiły rozbieżności do analizy względem wektorów.
- [x] Definicja ukończenia dla kodu: build Debug/Release, pełne testy Release i formatowanie.

## Pułapki

1. **Wolny test = brak wyniku, nie błąd.** Bez limitu T-state harness wisi; limit jest obowiązkowy (R4).
2. **Wektory zielone, zexall czerwony** oznacza błąd poza pokryciem wektorów (przerwania, łańcuchy prefiksów, niezdefiniowane `ED`) albo błąd w wektorach.
3. **Nie zmieniaj rdzenia pod binarium.** Rozbieżność = analiza źródła, test jednostkowy, dopiero potem poprawka.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | narzędzie i syntetyczne testy CP/M | 1/1 harness | `ZexHarnessTests` 13/13; pełne repo: 2794/2794 testów, build Debug/Release i format czyste | Wstępna weryfikacja implementacji przed pobraniem binariów; wyniki pełnych przebiegów zapisano niżej. |
| 2026-10-22 | ZEXDOC | PASS | 46 734 975 782 T-state; harness 00:17:19.627; wall 1040.73 s; 67/67 grup `OK` | brak `ERROR`; JSON: `.test-data/results/zexdoc.json`; SHA-256 COM: `34923a7ed82285d3038b2d54bd64899e12173eebb61f9d07b4fc72e78af2ae8f`. |
| 2026-10-22 | ZEXALL | PASS | 46 734 975 782 T-state; harness 00:18:47.655; wall 1128.23 s; 67/67 grup `OK` | brak `ERROR`; brak rozbieżności; JSON: `.test-data/results/zexall.json`; SHA-256 COM: `6e2da55147a04f28d303d5da6a1e6b771557ac244653590a0f24a2d39c8537e8`. Maszyna: Intel Core i5-10400F 2.90 GHz, .NET 10 Release. |
