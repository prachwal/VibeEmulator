# Vibe Emulator — wskazówki dla GitHub Copilot

Emulator CLI w C# / .NET 10. Cel: emulacja C64 zgodna cyklowo (6510 + VIC-II), z wariantami rodziny 6502 (NMOS, 65C02)
walidowanymi wektorami SingleStepTests i docelowo testami Dormanna oraz NES. Ten plik jest dołączany zawsze — trzymaj go
krótkim. Reguły zależne od ścieżki są w `.github/instructions/` (tabela niżej), powtarzalne zadania w `.github/prompts/`.

## Zanim zaczniesz

1. Przeczytaj plik instructions dla ścieżki, którą zmieniasz (tabela „Pliki instructions”).
2. **Zachowania CPU, VIC-II, przerwań i opkodów nie wpisuj z pamięci.** Źródłem prawdy są wektory SingleStepTests
   (`tests/VibeEmulator.Mos6502.Tests/Vectors`) i dokumentacja `docs/cpu/` (zacznij od `README.md`). Czego tam nie ma,
   oznacz w kodzie, teście lub opisie jako **niezweryfikowane**.
3. Stan prac i kolejne etapy: `docs/plan-c64-cycle-exact.md` (tabela „Stan realizacji”, etapy 2b–2e, harnessy H1–H7).

## Struktura repozytorium

| Ścieżka | Zawartość |
|---|---|
| `src/Cli` | aplikacja konsolowa (`vibe-emulator`), logika w `CliApplication` |
| `src/Memory` | generyczna magistrala (bez 6502): `IMemoryBus`, `FlatMemory`, `MemoryMap`, `IBusObserver`, `CompositeBusObserver`, `IPeekableBus` |
| `src/Cpu.Abstractions` | kontrakty rdzeni CPU neutralne rodzinowo (bez zależności): `ICpuCore`, `ICpuCore<TState>` |
| `src/Mos6502` | rodzina 6502/6510: `CpuState`, `OpcodeTable`, `Cpu<TBus>`, rdzeń cyklowy `StateMachineCpuCore`, `Disassembler`; warianty `CpuVariant` (`Nmos6502` domyślny, `Cmos65C02`) |
| `src/Core` | `Fibonacci.cs` oraz `C64/`: `C64Machine`, `VicII`, `C64Bus`, `ProcessorPort`, `Cia6526`, `Sid6581`, `AtomicCpuCore` |
| `tests/*.Tests` | xUnit: `Cli`, `Core` (w tym `C64/`), `Memory`, `Mos6502` (w tym `Vectors/` z próbką wektorów) |
| `tools/Bench`, `EmuBench`, `CycleModel` | pomiary: Fibonacci, mikrobenchmarki, porównanie modeli czasu |
| `tools/C64Trace` | tracer cykl po cyklu; własne programy (`--hex`/`--load`), warunki stopu (`--until`, `--instr`), formaty `text`/`vice`/`json`; skill `c64-trace-debug` |
| `tools/obsidian-mcp` | serwer MCP nad vaultem (Node, bez zależności npm); konfiguracja `.mcp.json` i `.vscode/mcp.json` |
| `docs/` | `plan-c64-cycle-exact.md` oraz `cpu/` (opkody z tabelami CSV, przerwania, tryb dziesiętny, testy) |

Kierunek zależności projektów: `Memory` ← `Mos6502` → `Cpu.Abstractions`; `Core` → wszystkie trzy. Nigdy odwrotnie.
`VibeEmulator.slnx` — solucja; `Directory.Build.props` — wspólne ustawienia (`TargetFramework`, `Nullable`,
`ImplicitUsings`, `TreatWarningsAsErrors`); `Directory.Packages.props` — wersje pakietów; `coverlet.runsettings` — pokrycie.

## Definicja ukończenia (przed zakończeniem zadania)

Uruchom i potwierdź **czysty** wynik (podaj liczby, nie „powinno działać”):

```bash
dotnet build VibeEmulator.slnx -c Debug --nologo     # 0 ostrzeżeń, 0 błędów
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
```

- Zmiana w `.github/hooks/`: `for t in .github/hooks/test-*.sh; do bash "$t"; done`.
- Zmiana w `tools/obsidian-mcp/`: `node tools/obsidian-mcp/test.js`.
- Zmiana zachowania: test (liczbowy, zob. `csharp-tests.instructions.md`) oraz aktualizacja planu lub `docs/cpu/`.
- Nie obiecuj zakończenia z czerwonym buildem, pominiętym testem ani ostrzeżeniem. Szybka pętla: `/sprawdz-przed-commitem`.

## Zasady ogólne

- Target: **.NET 10**; `Nullable`, `ImplicitUsings`, `LangVersion` i `TreatWarningsAsErrors` ustawia `Directory.Build.props` —
  nie powtarzaj ich w `.csproj`. Każde ostrzeżenie łamie build: napraw przyczynę, wyciszaj tylko punktowo z uzasadnieniem.
- Każda nowa funkcja ma test w `tests/`. Testy nie uruchamiają procesów; logikę CLI przyjmuj przez `TextWriter`.
- Kod produkcyjny (`src/`) nie pisze do `Console`; wyjątek: projekty w `tools/`.
- Wersje pakietów **tylko** w `Directory.Packages.props`; w `.csproj` `PackageReference` bez `Version`. Nowa zależność
  wymaga uzasadnienia w opisie zmiany.
- Formatowanie i styl pilnuje `.editorconfig`. Reguły analizatorów przypięte do ścieżek plików (np. `CpuState.cs`) trzeba
  przepiąć przy przenoszeniu plików, inaczej build ma błędy CA.
- Pokrycie: `dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"` (wyniki w `artifacts/`).
- CI (`.github/workflows/ci.yml`) robi to samo co „Definicja ukończenia” plus testy hooków i MCP.

## Commity (tylko gdy użytkownik o to poprosi)

Nie commituj ani nie wypychaj bez wyraźnej prośby. Format: `typ(zakres): opis` po polsku, np. `feat(mos6502): …`.
Typy: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `build`. Zakres: `core`, `mos6502`, `memory`, `tools`,
`mcp`, `vscode`, `copilot`, `plan`. Treść: **dlaczego** i jak zweryfikowano (liczby). Jedna rzecz to jeden commit i każdy commit
musi się budować.

## Analiza i recenzja plików — tylko raport, bez pytania o poprawki

Gdy użytkownik zleca „analiza pliku", „recenzja", „przejrzyj" lub „oceń" (zwykle ze wskazanym plikiem) albo uruchamia
`/analiza-pliku`, zadaniem jest **wyłącznie analiza**:

1. **Nie modyfikuj żadnych plików.** Poprawki nanoś dopiero na wyraźne polecenie w osobnej wiadomości
   („popraw", „nanieś", „zastosuj").
2. **Nie kończ odpowiedzi pytaniem ani ofertą.** Zabronione: „Chcesz, żebym naniósł te poprawki?", „Czy mam to poprawić?",
   „Daj znać, jeśli mam…", „Mogę przygotować…". Odpowiedź kończy się ostatnim punktem rekomendacji.
3. **Proponowane poprawki podawaj jako rekomendacje w raporcie** (co, gdzie, dlaczego), nie jako pytanie.
4. **Zarzuty sprawdzaj na plikach.** Przeczytaj sąsiednie pliki i kod, do którego dokument się odwołuje; czego nie sprawdzono,
   oznacz jako niezweryfikowane.

Format raportu: `.github/prompts/analiza-pliku.prompt.md`.

## Model czasu — rzecz najważniejsza w tym repo

Dwa rdzenie CPU za jednym kontraktem `ICpuCore<CpuState>` (`src/Cpu.Abstractions`):

- `StateMachineCpuCore` (`src/Mos6502`) — **jedyny zgodny cyklowo**. `Tick()` = 1 cykl = 1 dostęp do magistrali.
  Używaj go do pracy nad emulacją.
- `AtomicCpuCore` (`src/Core/C64`) — adapter nad `Cpu<TBus>`; wygodny do testów logiki, ale **nie cycle-exact**.

Nie dodawaj `Step()` do ścieżki emulacji C64: VIC-II musi móc zatrzymać CPU w środku instrukcji (`AEC`).
Szczegółowe reguły: `c64-emulation.instructions.md`, `mos6502.instructions.md`.

## Pliki instructions (dołączane automatycznie według `applyTo`)

| Plik | Zakres |
|---|---|
| `csharp.instructions.md` | `src/**/*.cs` — wydajność i styl |
| `c64-emulation.instructions.md` | `src/Core/C64/**/*.cs`, `src/Mos6502/StateMachineCpuCore.cs` — wierność cyklowa |
| `mos6502.instructions.md` | `src/Mos6502`, `src/Memory`, `src/Cpu.Abstractions` — warstwy, warianty, wektory |
| `csharp-tests.instructions.md` | `tests/**/*.cs` — konwencje xUnit i testów wektorowych |
| `tools-benchmarks.instructions.md` | `tools/**/*.cs` — pomiary i narzędzia konsolowe |
| `obsidian-mcp.instructions.md` | `tools/obsidian-mcp/**/*.js` — serwer MCP i jego testy |
| `docs.instructions.md` | `docs/**/*.md` — dyscyplina faktów w dokumentacji |
| `hooks-shell.instructions.md` | `.github/hooks/**` — kontrakt i testy hooków |
| `github-customization.instructions.md` | `.github/instructions`, `prompts`, `skills` — poprawny frontmatter |

Prompty (`/nazwa` w czacie lokalnym): `/analiza-pliku`, `/sprawdz-przed-commitem`, `/nowy-opkod`, `/perf-check`.
Skille: `c64-trace-debug` (debugowanie tracerem), `csharp-perf-benchmark` (rzetelny pomiar).

## Wydajność

Szczegóły w `csharp.instructions.md`. Najważniejsze: najpierw algorytm i alokacje; nie wyłączaj tieringu ani PGO;
`AggressiveOptimization` tylko po pomiarze; wyniki potwierdzaj minimum z wielu przebiegów, nie intuicją. Punkt odniesienia:
klatka PAL = **19 656 cykli**, ~**0,31–0,35 ms** (~1,6% budżetu 20 ms). Dokładność cyklowa jest ważniejsza niż szybkość.

## Hooki

`.github/hooks/` (format `version: 1`, `timeoutSec`; szczegóły w `hooks-shell.instructions.md`):

| hook | zdarzenie | działanie |
|---|---|---|
| `guard-dangerous-commands.sh` | `PreToolUse` | blokuje lub pyta o niebezpieczne polecenia terminala |
| `format-csharp.sh` | `PostToolUse` | formatuje zmienione pliki `.cs` |
| `verify-build.sh` | `Stop` | nie pozwala zakończyć tury, gdy zmieniony kod nie buduje się lub nie przechodzi `dotnet format` |

Testy: `test-*.sh`. Hook `Stop` odpowiada „zielony build przed końcem pracy”; nie obchodź go, tylko napraw błąd (`VERIFY_BUILD_SKIP=1`
jest dla awarii, nie dla wygody). Nie modyfikuj hooków bez wyraźnej prośby użytkownika.
