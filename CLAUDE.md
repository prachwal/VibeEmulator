# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Emulator C64 (6510 + VIC-II, cel: PAL 6569R5 cycle-exact) w C# / .NET 10. Repo jest prowadzone głównie w GitHub Copilot;
reguły są w `.github/` i **obowiązują także tutaj** — nie powielam ich, tylko wskazuję:

- `.github/copilot-instructions.md` — struktura, zasady ogólne, model czasu, wydajność
- `.github/instructions/*.instructions.md` — reguły zależne od ścieżki (`applyTo` w nagłówku): przed zmianą
  w `src/Core/C64`, `tests/`, `tools/` lub hookach przeczytaj odpowiedni plik
- `docs/plan-c64-cycle-exact.md` — plan faz 0–10 i tabela „Stan realizacji" (co jest zrobione, czego brakuje)

## Polecenia

```bash
dotnet build VibeEmulator.slnx                       # Debug; TreatWarningsAsErrors=true, więc ostrzeżenie = błąd
dotnet test VibeEmulator.slnx                        # wszystkie testy
dotnet test tests/VibeEmulator.Core.Tests --filter "FullyQualifiedName~C64MachineTests"                 # klasa
dotnet test tests/VibeEmulator.Core.Tests --filter "FullyQualifiedName~Frame_HasExactly19656Cycles"      # jeden test
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn   # musi przejść czysto (to samo robi CI)
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"   # pokrycie → artifacts/coverage/

for t in .github/hooks/test-*.sh; do bash "$t"; done   # testy hooków
node tools/obsidian-mcp/test.js                        # testy serwera MCP
```

Narzędzia w `tools/` uruchamiaj **zawsze z `-c Release`** (Debug daje fikcyjne wyniki pomiarów), np.
`dotnet run -c Release --project tools/C64Trace -- badline --from 3030 --to 3060` (`--help` pokazuje scenariusze).

Tracer C64 jest zaprojektowany pod agenta: własny program (`--hex "A9 7F …"`, `--load`), warunki stopu
(`--until pc=|write=|read=|raster=L:C|jam`, `--instr`, `--cycles`), wynik `--format json` (NDJSON), kody wyjścia
0/2/3. Z agenta uruchamiaj zbudowaną bibliotekę (`dotnet build tools/C64Trace -c Release`, potem
`dotnet tools/C64Trace/bin/Release/net10.0/VibeEmulator.C64Trace.dll …`) — ta forma jest w auto-approve, `dotnet run` nie.
Przepisy i ograniczenia modelu: `.github/skills/c64-trace-debug/SKILL.md`.
CI (`.github/workflows/ci.yml`) robi: build Release, test, format-verify, testy hooków, testy MCP — przed pushem
uruchom to lokalnie.

## Architektura

Dwie warstwy w `src/Core`, które łatwo pomylić:

- **`Core/Cpu`** — generyczny, *atomowy* model 6502: `Cpu<TBus>` z `Step()` wykonującym całą instrukcję naraz,
  `OpcodeTable` składana z grup opkodów (`OpcodeGroups`, `Operations`, `Addressing`). Ma zweryfikowane ALU, ale
  **nie nadaje się** do emulacji C64, bo nie da się go zatrzymać w środku instrukcji.
- **`Core/C64`** — właściwa emulacja. `C64Machine.Tick()` = jeden cykl φ2 i ma stałą kolejność: `VicII.Tick()` →
  (AEC? `cpu.Tick()` : `cpu.Stall()`) → CIA ×2 → SID → `ProcessorPort.Tick()`. **VIC-II jest masterem** i decyduje,
  czy CPU dostaje magistralę (badline kradnie 40 cykli). Dlatego CPU jest maszyną stanów, a `Step()` nie istnieje
  w ścieżce C64.

Dwa rdzenie CPU za kontraktem `ICpuCore`, wybierane fabryką `C64Machine.CreateCycleExact()` / `CreateAtomic()`:
`StateMachineCpuCore` (jedyny cycle-exact; cykle wynikają z faz = dostępów do magistrali, bez tabeli kosztów) oraz
`AtomicCpuCore` (adapter nad `Cpu<TBus>` + `CycleCostTable`, tylko do testów logiki). Szczegółowe pułapki
(dummy read, RMW z dwoma zapisami, open bus, opóźnienie portu 6510) są w `c64-emulation.instructions.md`.

`C64Bus` dekoduje pamięć/banking i woła opcjonalny `IBusObserver` przy każdym dostępie — na tym opiera się
`tools/C64Trace`. Slot `Bus.Observer` jest jeden, więc wielu obserwatorów składaj przez `CompositeBusObserver`.
Do podglądu pamięci służy `C64Bus.Peek` (bez skutków ubocznych: nie rusza `OpenBus`, nie woła obserwatora), a do
deasemblacji `Disassembler` w Core — generowany z tablicy `StateMachineCpuCore`, nie z osobnej listy opkodów.

`tools/` to osobne programy konsolowe (nie testowane jednostkowo, mogą pisać do `Console`): `Bench`/`EmuBench`/
`CycleModel` to pomiary, `C64Trace` to debugger cykl po cyklu, `obsidian-mcp` to niezwiązany z emulacją serwer MCP
(Node, bez zależności npm, konfiguracja w `.vscode/mcp.json`). `src/Core/Fibonacci.cs` to pozostałość scaffoldu,
używana przez `tools/Bench` i `FibonacciTests`.

## Konwencje, które łatwo złamać

- Kod w `src/` nie pisze do `Console` — przyjmuj `TextWriter` (CLI testuje się bez uruchamiania procesu).
- `Nullable`, `ImplicitUsings`, `TargetFramework`, `TreatWarningsAsErrors` są w `Directory.Build.props` — nie
  powtarzaj ich w `.csproj`. Wersje pakietów tylko w `Directory.Packages.props` (`PackageReference` bez `Version`).
- `Tick()` i wszystko pod nim nie alokuje (bez LINQ, `List`, stringów). Nie wyłączaj `TieredCompilation`/`TieredPGO`
  i nie dodawaj `AggressiveOptimization` bez pomiaru.
- Wartości cykli weryfikuj z dokumentacją/VICE, nie z pamięci; zmiana oczekiwanej asercji w `tests/.../C64/`
  wymaga uzasadnienia.

## Środowisko Copilota w repo

Hooki w `.github/hooks/` mają format GitHub Copilota (`guard-dangerous-commands.sh` jako `PreToolUse`,
`format-csharp.sh` jako `PostToolUse`) — Claude Code ich **nie uruchamia**, więc sam pilnuj
`dotnet format --verify-no-changes` po edycjach `.cs`. Nie modyfikuj hooków bez wyraźnej prośby użytkownika.
W `.vscode/settings.json` auto-approve celowo nie zawiera `dotnet run` ani `dotnet add`.
