# Vibe Emulator — wskazówki dla GitHub Copilot

## Struktura repozytorium

- `src/Cli` — aplikacja konsolowa (`vibe-emulator`), logika w `CliApplication`.
- `tests/VibeEmulator.Cli.Tests` — testy xUnit.
- `VibeEmulator.slnx` — solucja; `Directory.Build.props` — wspólne ustawienia kompilacji.

## Zasady ogólne

- Target: **.NET 10** (`net10.0`), `Nullable` i `ImplicitUsings` włączone, `LangVersion=latest`.
- Każda nowa funkcja musi mieć test w `tests/`. Testy nie mogą wymagać uruchamiania procesu — logikę CLI przyjmuj przez `TextWriter`.
- Kod produkcyjny nie może pisać bezpośrednio do `Console`; przekazuj `TextWriter`.
- Nazwy testów: `Metoda_Scenariusz_OczekiwanyWynik`.
- Komendy: `dotnet build`, `dotnet test`, `dotnet format`.
- Nie dodawaj zależności NuGet bez uzasadnienia w opisie zmiany.

## Wydajność

Obowiązują reguły z `.github/instructions/csharp.instructions.md`. Najważniejsze:

- Najpierw algorytm i alokacje, dopiero potem mikrooptymalizacje.
- Nie wyłączaj tieringu ani PGO (`TieredCompilation`, `TieredPGO`).
- Nie dodawaj `[MethodImpl(AggressiveOptimization)]` bez pomiaru — na metodach rekurencyjnych pogarsza wynik.
- Wyniki wydajności potwierdzaj pomiarem (BenchmarkDotNet lub minimum z wielu przebiegów), nie intuicją.
