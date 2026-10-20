---
applyTo: "tests/**/*.cs"
description: "Konwencje testów xUnit w repozytorium."
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
