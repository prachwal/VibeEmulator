---
applyTo: "tests/**/*.cs"
description: "Konwencje testów xUnit w repozytorium, w tym testy emulacji (cykle, magistrala, determinizm)."
---

# Konwencje testów

- Framework: **xUnit** (`[Fact]`, `[Theory]`, `[InlineData]`).
- Nazewnictwo: `Metoda_Scenariusz_OczekiwanyWynik` (podkreślenia są dozwolone w testach).
- Jeden test = jedno zachowanie; asercje przez `Assert.*`.
- Arrange/Act/Assert rozdzielaj pustą linią, bez komentarzy-sekcji.
- Nie używaj `Thread.Sleep`; do zależności czasowych wstrzykuj `TimeProvider`.
- Testy nie mogą zależeć od bieżącego katalogu, zmiennych środowiskowych ani sieci.
- Logikę CLI testuj przez `CliApplication.Run(args, stdout, stderr)` z `StringWriter`.
- Nazwy projektów: `<Projekt>.Tests` w `tests/`.

## Testy emulacji (`tests/**/C64/`)

- Testy CPU uruchamiaj przez **`Tick()`**, nie przez `Step()` — `Step()` nie istnieje w rdzeniu
  cyklowym, a model atomowy nie jest cycle-exact.
- Liczbę cykli instrukcji licz w teście jawnie:

  ```csharp
  int cycles = 0;
  do
  {
      cpu.Tick();
      cycles++;
  }
  while (cpu.Pending != 0 && cycles < 64);
  ```

- Asercje mają być **liczbowe**, nie opisowe: `Assert.Equal(6, cycles)`, a nie
  `Assert.True(cycles > 5)`.
- Zapis do magistrali weryfikuj jako **sekwencję** (`RMW` zapisuje starą i nową wartość):

  ```csharp
  Assert.Equal([(0x0300, (byte)0x41), (0x0300, (byte)0x42)], bus.Writes);
  ```

- Testy maszyny używaj do sprawdzania niezmienników: klatka = **19 656 cykli**,
  badline kradnie **40 cykli** (cykle 15–54).
- Testy muszą być deterministyczne: żadnego `Random` bez stałego ziarna, żadnego czasu ściennego.
- Nie porównuj czasów wykonania w testach jednostkowych — od pomiarów są projekty w `tools/`.

## Testy danych (wektory referencyjne)

- Zestawy wektorów (Tom Harte, nestest) trzymaj jako pliki w `tests/` i ładuj przez `[MemberData]`.
- Pliki nie mogą być pobierane z sieci w trakcie testu.
- Przy niezgodności test ma wskazać **konkretny opkod, adres i oczekiwany cykl**, a nie tylko
  „test failed".

