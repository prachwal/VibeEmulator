# Vibe Emulator — wskazówki dla GitHub Copilot

## Struktura repozytorium

| Ścieżka | Zawartość |
|---|---|
| `src/Cli` | aplikacja konsolowa (`vibe-emulator`), logika w `CliApplication` |
| `src/Core/Fibonacci.cs` | implementacje odniesienia (rekurencja, iteracja, memoizacja, fast doubling) |
| `src/Core/Cpu` | generyczny model 6502: `CpuState`, `IMemoryBus`, `OpcodeTable`, kompozycja grup |
| `src/Core/C64` | architektura C64: `C64Machine`, `VicII`, `C64Bus`, `ProcessorPort`, `Cia6526`, `Sid6581` |
| `tests/VibeEmulator.Cli.Tests` | testy CLI |
| `tests/VibeEmulator.Core.Tests` | testy Core, w tym `C64/` (maszyna, magistrala, rdzenie CPU) |
| `tools/Bench` | benchmark Fibonacciego (4 implementacje) |
| `tools/EmuBench` | mikrobenchmarki: dispatch, pamięć, cache, ALU, bloki, prawdziwy CPU |
| `tools/CycleModel` | porównanie modeli czasu: atomowy vs cykl po cyklu |
| `tools/C64Trace` | tracer C64: ślad cykl po cyklu, `--from/--to`, `--dump` |
| `tools/obsidian-mcp` | serwer MCP nad vaultem (Node, bez zależności npm) |
| `docs/` | dokumentacja projektowa (`plan-c64-cycle-exact.md`) |

`VibeEmulator.slnx` — solucja; `Directory.Build.props` — wspólne ustawienia kompilacji
(`TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`);
`Directory.Packages.props` — centralne wersje pakietów NuGet; `coverlet.runsettings` — pokrycie kodu;
`.github/workflows/ci.yml` — CI.

## Zasady ogólne

- Target: **.NET 10** (`net10.0`), `Nullable` i `ImplicitUsings` włączone, `LangVersion=latest` —
  wszystko ustawione w `Directory.Build.props`; nie powtarzaj tego w `.csproj`.
- `TreatWarningsAsErrors=true` — każde ostrzeżenie łamie build. Napraw przyczynę; wyciszenie tylko
  punktowe (konkretny kod/reguła) z komentarzem uzasadniającym, nigdy globalnie.
- Każda nowa funkcja musi mieć test w `tests/`. Testy nie mogą wymagać uruchamiania procesu —
  logikę CLI przyjmuj przez `TextWriter`.
- Kod produkcyjny nie może pisać bezpośrednio do `Console`; przekazuj `TextWriter`.
  Wyjątkiem są projekty w `tools/` — to narzędzia konsolowe.
- Nazwy testów: `Metoda_Scenariusz_OczekiwanyWynik`.
- Komendy: `dotnet build`, `dotnet test`, `dotnet format`.
- Formatowanie i styl pilnuje `.editorconfig`; `dotnet format --verify-no-changes --severity warn`
  musi przechodzić czysto.
- Nie dodawaj zależności NuGet bez uzasadnienia w opisie zmiany. Wersje pakietów są **tylko**
  w `Directory.Packages.props` (Central Package Management) — w `.csproj` pisz
  `<PackageReference Include="..." />` bez `Version`.
- Pokrycie kodu: `dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
  (wyniki w `artifacts/coverage/`, poza gitem).
- CI (`.github/workflows/ci.yml`): restore, build Release, testy, `dotnet format --verify-no-changes`,
  testy hooków (`test-*.sh`) i `node tools/obsidian-mcp/test.js`. Przed pushem uruchom lokalnie to samo.

## Pliki instructions (stosują się automatycznie według `applyTo`)

| Plik | Zakres |
|---|---|
| `csharp.instructions.md` | `src/**/*.cs` — optymalizacja i styl |
| `c64-emulation.instructions.md` | `src/Core/C64/**/*.cs` — wierność cyklowa emulacji |
| `csharp-tests.instructions.md` | `tests/**/*.cs` — konwencje xUnit |
| `tools-benchmarks.instructions.md` | `tools/**/*.cs` — pomiary i narzędzia |
| `hooks-shell.instructions.md` | `.github/hooks/**/*.sh` — skrypty hooków |

## Model czasu — rzecz najważniejsza w tym repo

Emulator ma dwa rdzenie CPU za jednym kontraktem `ICpuCore`. Wybór ma konsekwencje:

- `StateMachineCpuCore` — **jedyny zgodny cyklowo**. `Tick()` = 1 cykl = 1 dostęp do magistrali.
  Używaj go do pracy nad emulacją.
- `AtomicCpuCore` — adapter nad `Cpu<TBus>`, wygodny do testów logiki, ale **nie jest cycle-exact**
  (wszystkie dostępy instrukcji widzą ten sam moment).

Nie dodawaj `Step()` do ścieżki emulacji C64 — VIC-II musi móc zatrzymać CPU w środku instrukcji
przez `AEC`. Szczegóły: `docs/plan-c64-cycle-exact.md`.

## Wydajność

Obowiązują reguły z `.github/instructions/csharp.instructions.md`. Najważniejsze:

- Najpierw algorytm i alokacje, dopiero potem mikrooptymalizacje.
- Nie wyłączaj tieringu ani PGO (`TieredCompilation`, `TieredPGO`).
- Nie dodawaj `[MethodImpl(AggressiveOptimization)]` bez pomiaru — na metodach rekurencyjnych
  zmierzono pogorszenie ~20%.
- Wyniki wydajności potwierdzaj pomiarem (BenchmarkDotNet lub minimum z wielu przebiegów),
  nie intuicją.
- Dla emulacji punkt odniesienia: klatka PAL = **19 656 cykli**, zmierzone ~**0,35 ms** (~1,7%
  budżetu 20 ms). Dokładność cyklowa jest darmowa — nie optymalizuj jej kosztem poprawności.

## Hooki

`.github/hooks/guard-dangerous-commands.sh` (`PreToolUse`) blokuje niebezpieczne polecenia;
`format-csharp.sh` (`PostToolUse`) formatuje zmienione pliki `.cs`. Testy hooków uruchamiaj
skryptami `test-*.sh`. Nie modyfikuj hooków bez wyraźnej prośby użytkownika.

