# Etap L — harness testów Dormanna (wspólny runner)

Poziom: **brak nowych opkodów** · Nowe narzędzie: 1 · Nowe testy: harness + syntetyczne obrazy

Ten etap **nie dodaje instrukcji**. Buduje narzędzie, bez którego etapy M–O byłyby ręcznym klikaniem:
wspólny runner obrazów 64 KB z wykrywaniem pętli `PC`, raportem JSON i kontrolą limitu cykli.

## Zakres

- Nowy projekt `tools/CpuHarness` (konsola, `Release`, pisze do `Console` — dozwolone w `tools/`).
- Wejście: plik binarny 64 KB (albo krótszy, ładowany od `$0000`), opcjonalny `--origin`, `--pc`, `--variant`,
  `--limit-cycles`, `--format json|text`.
- Wyjście: `pc`, `cycles`, `instructions`, `outcome` (`pass`/`fail`/`limit`/`jam`), `writes` (opcjonalnie).
- Zatrzymanie: **pętla `PC`** — po ukończonej instrukcji `PC` równa się `PC` sprzed instrukcji (Klaus Dormann
  używa `jmp *` jako „koniec testu”).
- Sukces: `PC` równa się adresowi ze `--success-pc` (albo brak tej opcji → tylko raport).
- Limit cykli: domyślnie z opcji; przekroczenie → `outcome = limit` i kod wyjścia 3, bez zawieszania.
- Kod wyjścia: `0` = sukces, `2` = porażka (pętla pod innym adresem), `3` = limit cykli, `4` = błąd wejścia.

## Nowe elementy

- `tools/CpuHarness/Program.cs` — parsowanie argumentów i wypisanie wyniku.
- `tools/CpuHarness/HarnessRunner.cs` — pętla `Tick` + detekcja pętli `PC` + limit cykli.
- `tools/CpuHarness/ImageLoader.cs` — wczytanie obrazu do `FlatMemory` z kontrolą rozmiaru.
- `tests/VibeEmulator.Mos6502.Tests/CpuHarnessTests.cs` — testy logiki runnera **na syntetycznych obrazach**
  (żadnych binariów Dormanna w repo): wykrycie pętli, limit cykli, jam, adres sukcesu, ładowanie obrazu.

## Ograniczenia (zapisane wprost)

- **Binaria Dormanna nie trafiają do repo** (GPL-3.0, a repozytorium nie trzyma ROM-ów). Runner przyjmuje
  ścieżkę; etapy M–O podają commit źródła i wynik przebiegu wykonanego poza repo.
- Testy w repo nie wołają sieci i nie uruchamiają procesów — sprawdzają wyłącznie logikę harnessu.

## Kryteria akceptacji

- [x] `tools/CpuHarness` buduje się w Release i ma `--help` z pełną listą opcji.
- [x] Testy logiki runnera na syntetycznym obrazie przechodzą — `CpuHarnessTests`, **14/14** (pętla sukcesu,
      pętla porażki, brak adresu sukcesu, limit cykli, jam/KIL 11 cykli, rejestr zwrotny, bajt raportu,
      walidacja obrazu, parsowanie opcji).
- [x] Definicja ukończenia: build Debug/Release 0 ostrzeżeń, testy **802/802**, `dotnet format` exit 0.

## Pułapki

1. **Pętla `PC` musi być sprawdzana po instrukcji**, nie po `Tick` — `Tick` to jeden cykl, a instrukcja ma ich
   2–11. Inaczej harness zgłosi fałszywy koniec na każdym `JMP`.
2. **`Pending`** w `ICpuCore` mówi o gotowości magistrali, nie o końcu instrukcji — nie używać jako warunku.
3. Limit cykli liczony w cyklach φ2 (`cpu.Cycles`), nie w instrukcjach; test Dormanna to dziesiątki milionów
   cykli, więc limit domyślny musi być duży (rzędu 200 000 000) i nadpisywalny.
4. Runner nie może alokować na cykl (żadnych stringów w pętli); raport budowany po zakończeniu.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | harness + testy | 1/1 | `CpuHarnessTests` 14/14; cała solucja (Release) 802/802; build 0 ostrzeżeń | `tools/CpuHarness` (`HarnessOptions`, `ImageLoader`, `HarnessBus`, `HarnessRunner`, `Program`); pętla `PC` sprawdzana po instrukcji (`Pending == 0` w fazie Fetch) |
