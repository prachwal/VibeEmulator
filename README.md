# Vibe Emulator

Emulator CLI w C# / .NET 10.

## Wymagania

- .NET SDK 10.0.401 (wersja przypięta w `global.json`)

## Struktura

```
src/Cli/                        # vibe-emulator (aplikacja konsolowa)
src/Core/                       # logika wielokrotnego użytku (Fibonacci)
src/Core/Cpu/                   # generyczny model procesora (rejestry, opkody, magistrala)
tests/VibeEmulator.Cli.Tests/   # testy xUnit (CLI)
tests/VibeEmulator.Core.Tests/  # testy xUnit (Core)
tools/Bench/                    # pomiary Fibonacciego
tools/EmuBench/                 # mikrobenchmarki aspektów emulacji
tools/CycleModel/               # porównanie modeli czasu: atomowy vs cykl po cyklu
tools/obsidian-mcp/             # serwer MCP nad vaultem Obsidiana
.github/instructions/           # reguły dla GitHub Copilot (C#, testy)
.github/skills/                 # skille Copilota (np. pomiar wydajności)
.github/hooks/                  # hooki: guard niebezpiecznych poleceń, auto-format .cs
.vscode/                        # taski, debug, ustawienia
```

## Obsidian MCP

`tools/obsidian-mcp` to serwer MCP (stdio, JSON-RPC 2.0) nad vaultem `~/Obsidian/Vault`.
Nie wymaga działającej aplikacji Obsidian ani pluginu Local REST API — vault to katalog z `.md`.
Zero zależności npm.

Konfiguracja: `.vscode/mcp.json` (serwer `obsidian`, zmienna `OBSIDIAN_VAULT`).

| narzędzie | opis |
|---|---|
| `vault_info` | ścieżka, liczba notatek, liczba tagów |
| `list_notes` | lista notatek z filtrem ścieżki |
| `read_note` | treść notatki |
| `create_note` | utworzenie (domyślnie bez nadpisywania) |
| `append_note` | dopisanie na końcu |
| `search_notes` | podciąg lub regex, numery linii |
| `list_tags` | tagi z frontmattera YAML i inline |

Bezpieczeństwo: blokowane są ścieżki absolutne, wyjście poza vault (`../`) i dostęp do `.obsidian`.

```bash
cd tools/obsidian-mcp && node test.js   # 15 testów dymnych
```

## Modele czasu (`tools/CycleModel`)

Demonstracja, czym różni się instrukcja atomowa od wykonania cykl po cyklu — na tym samym programie:

```bash
dotnet run -c Release --project tools/CycleModel
```

| model | `STA $2007` widziany w dotach | zapisy RMW |
|---|---|---|
| A: atomowy | 6, 24 | 1 (tylko nowa wartość) |
| B: cykl po cyklu | 15, 33 | 2 (stara i nowa) |

Stałe przesunięcie **9 dotów = 3 piksele** bierze się z tego, że model A wykonuje wszystkie
dostępy instrukcji w jednym momencie, a model B rozkłada je na kolejne cykle.

## Model procesora (`src/Core/Cpu`)

Generyczny rdzeń emulacji: ten sam kod działa dla płaskiej pamięci, mapy regionów
i dowolnej magistrali implementującej `IMemoryBus`.

```csharp
var rom = new FlatMemory(0x2000);
rom.Load(0, [0xA9, 0x10, 0x8D, 0x00, 0x02, 0x00]); // LDA #$10; STA $0200; BRK

var ram = new FlatMemory(0x2000);
MemoryMap map = MemoryMap.CreateBuilder()
    .Map(0x0000, 0x1FFF, ram)
    .Map(0x8000, 0x9FFF, rom)
    .Build();

Cpu<MemoryMap> cpu = Cpu6502.Create(map);
cpu.Reset(0x8000);

cpu.State.A = 0x7F;   // rejestry są dostępne przez ref — bez kopiowania
cpu.Step();           // fetch → decode → execute, zwraca Instruction
int executed = cpu.Run();
```

| Element | Rola |
|---|---|
| `CpuState` | `A/X/Y/PC/SP/P` — struktura, pola publiczne, zero alokacji |
| `IMemoryBus` | magistrala; `FlatMemory`, `MemoryMap`, własne implementacje |
| `MemoryMapBuilder` | dekoder adresów — regiony RAM/ROM/IO z wykrywaniem nakładania |
| `AddressingMode` + `Addressing.Resolve` | tryby adresowania jako strategia (implied, immediate, zero page, absolute, indirect) |
| `OpcodeTable` / `OpcodeTableBuilder` | niemutowalna tablica 256 opkodów + kompozycja grup |
| `OpcodeGroups` | `LoadStore`, `Arithmetic`, `ControlFlow`, `Standard` |
| `Cpu<TBus>` | generyczny procesor; `State` zwracane przez `ref` |
| `CpuBuilder<TBus>` | składanie CPU z magistrali + grup + pojedynczych instrukcji |

Dostępne opkody: 29 (10 load/store, 12 arytmetycznych, 7 sterowania), m.in.
`LDA/LDX/LDY/STA/STX/STY`, `ADC/SBC/INC/DEC/INX/DEX/INY/DEY`, `JMP/NOP/BRK/TAX/TAY/TXA/TYA`.

## Benchmarki emulacji

`tools/EmuBench` mierzy pięć aspektów istotnych dla emulatora:

```bash
dotnet run -c Release --project tools/EmuBench -- [dispatch|memory|cache|alu|blocks|cpu|all] [rounds]
```

| Sekcja | Co mierzy |
|---|---|
| `dispatch` | koszt pobrania instrukcji: switch, tablica delegatów, wskaźniki funkcji, wywołanie bezpośrednie |
| `memory` | odczyt/zapis 8–64 bit, wyrównanie, `BinaryPrimitives` vs `BitConverter` vs `Unsafe`, pamięć bankowana i stronicowana z TLB |
| `cache` | przepustowość przy working secie 4 KB–64 MB (sekwencyjnie i losowo) — kolana wskazują granice L1/L2/L3 |
| `alu` | szerokość rejestru, flagi eager vs lazy, mnożenie, dzielenie, rotacje, BCD |
| `blocks` | interpreter instrukcja-po-instrukcji vs uprzednio rozwiązana sekwencja |
| `cpu` | prawdziwy `Cpu<TBus>` z `src/Core/Cpu`: `Step()` i `Run()` — fetch + decode + execute |

## Hooki
| Plik | Event | Działanie |
|---|---|---|
| `guard-dangerous-commands.json` | `PreToolUse` | blokuje (`deny`) lub wymaga potwierdzenia (`ask`) dla destrukcyjnych poleceń |
| `format-code.json` | `PostToolUse` | formatuje zmienione pliki `.cs` (`dotnet format`) |

Testy hooków:

```bash
./.github/hooks/test-guard-dangerous-commands.sh
./.github/hooks/test-format-csharp.sh
```

Polityka guarda:

- **deny** — `rm -rf` na `/`, `~`, `$HOME`, `.`, `*`, katalogu repo lub `src/`/`tests/`/`.github/`/`tools/`; `git push --force`/`-f`/`+branch`/`--delete`/`--mirror`; `git reset --hard`; `git clean -fdx`; `git filter-*`; `sudo` bez `-n`; `mkfs`/`dd`/`shred`; zapis do `/dev/sd*` i `/etc/`; `chmod -R 777`; `curl|bash`; edycja własnych hooków.
- **ask** — `rm -rf` poza artefaktami budowania; `git clean -fd`; `git branch -D`; `git config --global`; `chmod 777`; `git push --force-with-lease`; `dotnet nuget push` / `npm publish`; benchmark bez `-c Release`.
- **allow** — wszystko inne, w tym `rm -rf bin obj artifacts`.


## Komendy

```bash
dotnet build                      # Debug
dotnet build -c Release           # Release (zoptymalizowany JIT)
dotnet test                       # wszystkie testy
dotnet run --project src/Cli      # uruchomienie CLI
dotnet format                     # formatowanie wg .editorconfig
```

## Użycie

```bash
dotnet run --project src/Cli -- version
dotnet run --project src/Cli -- help
```

## Zasady

Reguły optymalizacji i konwencje kodu: `.github/instructions/csharp.instructions.md`.
