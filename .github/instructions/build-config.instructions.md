---
applyTo: "**/*.csproj,Directory.Build.props,Directory.Packages.props,global.json,coverlet.runsettings,.editorconfig,VibeEmulator.slnx"
description: "Konfiguracja budowania: .csproj bez TargetFramework/Nullable/ImplicitUsings, wersje pakietów tylko w Directory.Packages.props (CPM), SDK z global.json, filtry pokrycia w coverlet.runsettings, reguły analizatorów w .editorconfig, spójność VibeEmulator.slnx."
---

# Konfiguracja budowania

## `.csproj`

- **Nie powtarzaj** właściwości z `Directory.Build.props`: `TargetFramework`, `LangVersion`, `Nullable`, `ImplicitUsings`,
  `AnalysisLevel`, `EnforceCodeStyleInBuild`, `Deterministic`, `TreatWarningsAsErrors`.
- `PackageReference` **bez `Version`** — wersja żyje wyłącznie w `Directory.Packages.props` (Central Package Management).
- Projekt w `tests/` ma `IsPackable=false`, `NoWarn` na `NETSDK1188`, cztery referencje testowe (`coverlet.collector`,
  `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`) i `<Using Include="Xunit" />`. Nowy projekt testowy
  kopiuje ten szablon zamiast wymyślać własny.
- Projekt w `tools/` ma `OutputType=Exe`; dopuszczalne ustawienia wydajnościowe (`AllowUnsafeBlocks`,
  `ServerGarbageCollection`, `InvariantGlobalization`) ustawiaj punktowo i uzasadnij w opisie zmiany.
- Kierunek referencji: `Memory` ← `Mos6502` → `Cpu.Abstractions`, `Core` → wszystkie trzy. Nie dodawaj referencji wstecz
  ani z `Memory`/`Cpu.Abstractions` do 6502.
- Nowy projekt .NET dopisz do `VibeEmulator.slnx`; projekt poza solucją zostaje tylko wtedy, gdy nie jest projektem .NET
  (`tools/obsidian-mcp`).

## `VibeEmulator.slnx`

- Lista projektów musi odpowiadać katalogom w `src/`, `tests/` i `tools/`: brak projektu w solucji oznacza, że CI go nie
  zbuduje i nie przetestuje.
- Usunięcie lub przeniesienie projektu aktualizuje też `ProjectReference` w projektach zależnych i wpis w tym pliku.

## `Directory.Packages.props`

- Wyłącznie tutaj wersje pakietów. Nie zostawiaj wpisów nieużywanych i nie dodawaj pakietu bez uzasadnienia.

## `global.json`

- Wersja SDK z `rollForward: latestFeature`. Podniesienie wersji to osobna zmiana — CI bierze SDK z tego pliku
  (`global-json-file`), więc zmiana działa dopiero po przebiegu workflow.

## `coverlet.runsettings`

- Mierzone jest `src/`; `tests/`, `tools/` i `obj/` są wyłączone. Nowy katalog z kodem produkcyjnym poza `src/` wymaga
  świadomej decyzji: albo trafia do pomiaru, albo do `ExcludeByFile`.

## `.editorconfig`

- Reguły analizatorów przypięte do **ścieżek** (`[tests/**/*.cs]`, `[src/Mos6502/CpuState.cs]`, `[src/Mos6502/StatusFlags.cs]`)
  przepinaj przy przenoszeniu plików — inaczej build kończy się błędem CA.
- Nie wyciszaj analizatorów globalnie; wyciszenie tylko punktowo, z uzasadnieniem w komentarzu obok reguły.
- `dotnet format --verify-no-changes --severity warn` musi przechodzić bez zmian; nowa reguła stylu wymaga sformatowania
  całego repo w tym samym commicie.
