---
applyTo: "tests/**/*.cs"
description: "Konwencje testów xUnit w repozytorium, w tym testy emulacji (cykle, magistrala, determinizm) i testy wektorowe SingleStepTests (próbka w repo, Skip tylko z opisem)."
---

# Konwencje testów

- Framework: **xUnit** (`[Fact]`, `[Theory]`, `[InlineData]`, `[MemberData]`).
- Nazwy: `Metoda_Scenariusz_OczekiwanyWynik` (podkreślenia dozwolone w testach).
- Jeden test = jedno zachowanie; asercje przez `Assert.*`. Arrange/Act/Assert rozdzielaj pustą linią, bez komentarzy-sekcji.
- Bez `Thread.Sleep`; zależności czasowe przez `TimeProvider`.
- Testy nie zależą od bieżącego katalogu, zmiennych środowiskowych, sieci ani **stanu maszyny poza repo** (np. prawdziwych
  katalogów użytkownika). Potrzebne dane buduj w katalogu tymczasowym i sprzątaj w `finally`.
- Logikę CLI testuj przez `CliApplication.Run(args, stdout, stderr)` z `StringWriter`.
- Projekty: `<Projekt>.Tests` w `tests/`. Testy typów z `src/Mos6502` należą do `VibeEmulator.Mos6502.Tests`, z `src/Memory`
  do `Memory.Tests`, z `src/Core` do `Core.Tests` (testy C64 w `C64/`).

## Testy emulacji

- Testy CPU uruchamiaj przez **`Tick()`**, nie `Step()` — rdzeń cyklowy nie ma `Step()`, a model atomowy nie jest cycle-exact.
- Liczbę cykli instrukcji licz jawnie:

  ```csharp
  int cycles = 0;
  do
  {
      cpu.Tick();
      cycles++;
  }
  while (cpu.Pending != 0 && cycles < 64);
  ```

- Asercje **liczbowe**, nie opisowe: `Assert.Equal(6, cycles)`, a nie `Assert.True(cycles > 5)`.
- Zapisy do magistrali weryfikuj jako **sekwencję** (RMW NMOS: stara i nowa wartość; 65C02: dwa odczyty i jeden zapis):

  ```csharp
  Assert.Equal([(0x0300, (byte)0x41), (0x0300, (byte)0x42)], bus.Writes);
  ```

- Niezmienniki maszyny: klatka = **19 656 cykli**, badline kradnie **40 cykli** (cykle 15–54).
- Deterministycznie: bez `Random` bez stałego ziarna i bez czasu ściennego. Czasów wykonania nie porównuj w testach
  jednostkowych — od pomiarów są projekty w `tools/`.

## Testy wektorowe (SingleStepTests/65x02)

- W repo leży **próbka**: pierwsze 100 testów z każdego pliku, w `tests/VibeEmulator.Mos6502.Tests/Vectors/{nmos6502,wdc65c02}`
  (opis, licencja i commit źródłowy: `Vectors/README.md`). Pełne pliki (10 000 testów na opkod) **nie trafiają do repo**.
- Pliki ładuj z dysku (`Content` kopiowane do katalogu wyjściowego) przez `[MemberData]`; **nigdy nie pobieraj z sieci w teście**.
- Test porównuje cykl po cyklu dostępy do magistrali (adres, wartość, odczyt/zapis), granicę instrukcji, rejestry i pamięć końcową.
  Przy niezgodności komunikat wskazuje **wariant, opkod, nazwę testu z wektora, numer cyklu oraz oczekiwany i faktyczny dostęp**.
- Nowy opkod dodaj do **obu** wariantów, gdy dotyczy obu, i do próbki wektorów. Zielona próbka to nie to samo co zielony
  pełny zestaw: wynik pełnych wektorów podawaj osobno i zaznacz, czy je uruchomiono.
- `[Fact(Skip = …)]` wolno **tylko** z dokładnym opisem różnicy (opkod, przykład, oczekiwane vs faktyczne) i odnośnikiem do
  etapu, który to naprawi. Cichych pominięć nie ma. Jedyny stały wyjątek: `ADC`/`SBC #` z flagą Decimal do etapu 2e.
- Reguły zachowania bierz z wektorów i `docs/cpu/`, nie z pamięci (zob. `mos6502.instructions.md`).
