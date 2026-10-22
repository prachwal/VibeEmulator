# Vibe Emulator

Emulator CLI w C# / .NET 10.

## Wymagania

- .NET SDK 10.0.401 (wersja przypięta w `global.json`)

## Struktura

```
src/Cli/                        # vibe-emulator (aplikacja konsolowa)
src/Core/                       # logika wielokrotnego użytku (Fibonacci)
src/Cpu.Abstractions/           # kontrakty rdzeni CPU neutralne rodzinowo: ICpuCore, ICpuCore<TState>
src/Memory/                     # generyczna magistrala: IMemoryBus, FlatMemory, MemoryMap, IBusObserver (bez 6502)
src/Mos6502/                    # rodzina 6502/6510: rejestry, opkody, tryby adresowania, Cpu<TBus>, StateMachineCpuCore, Disassembler
src/Core/C64/                   # architektura C64: zegar, VIC-II, CIA, SID, magistrala
docs/                           # dokumentacja projektowa (plan C64 cycle-exact)
docs/cpu/                       # dokumentacja CPU i testów: opkody, przerwania, tryb dziesiętny, Dormann, nestest
tests/VibeEmulator.Cli.Tests/   # testy xUnit (CLI)
tests/VibeEmulator.Core.Tests/  # testy xUnit (Core)
tests/VibeEmulator.Memory.Tests/ # testy xUnit (magistrala: MemoryMap, FlatMemory, obserwatorzy)
tests/VibeEmulator.Mos6502.Tests/ # testy xUnit (rodzina 6502)
tools/Bench/                    # pomiary Fibonacciego
tools/EmuBench/                 # mikrobenchmarki aspektów emulacji
tools/CycleModel/               # porównanie modeli czasu: atomowy vs cykl po cyklu
tools/C64Trace/                 # tracer: wykonanie C64 cykl po cyklu
tools/OpcodeInfo/               # zapytania o opkody z tabel CSV jako JSON
tools/obsidian-mcp/             # serwer MCP nad vaultem Obsidiana
.github/instructions/           # reguły dla GitHub Copilot (C#, C64, testy, tools, hooki)
.github/skills/                 # skille Copilota (np. pomiar wydajności)
.github/hooks/                  # hooki: guard niebezpiecznych poleceń, auto-format .cs
.vscode/                        # taski, debug (launch), ustawienia, MCP, rekomendowane rozszerzenia
```

## Obsidian MCP

`tools/obsidian-mcp` to serwer MCP (stdio, JSON-RPC 2.0) nad vaultem `~/Obsidian/Vault`.
Nie wymaga działającej aplikacji Obsidian ani pluginu Local REST API — vault to katalog z `.md`.
Zero zależności npm.

Konfiguracja: `.vscode/mcp.json` (serwer `obsidian`). Ścieżkę vaulta VS Code pyta przy pierwszym starcie
serwera (input `obsidian-vault`) i przekazuje ją w zmiennej `OBSIDIAN_VAULT`; podaj ścieżkę absolutną.

Claude Code: `.mcp.json` w korzeniu repo (zakres projektu). Launcher `sh -c 'cd "$(git rev-parse --show-toplevel)" && exec node …'`
sprawia, że serwer startuje niezależnie od katalogu, z którego uruchomiono `claude` (zmienna `${CLAUDE_PROJECT_DIR:-.}`
w `args` zawsze rozwija się do `.`, więc nie nadaje się do ścieżki). Bez `env` serwer używa `~/Obsidian/Vault`; zmienna
`OBSIDIAN_VAULT` z powłoki ją nadpisuje. Serwer z `.mcp.json` wymaga jednorazowego zatwierdzenia w interaktywnym `claude`
(najpierw dialog zaufania katalogu).

Obsidian na Windowsie nie otworzy vaulta leżącego w systemie plików WSL (dysk mapowany `\\wsl.localhost\…` używa
protokołu 9P, na którym nie działa `fs.watch`; błąd: `EISDIR: illegal operation on a directory, watch`). Trzymaj vault
na dysku Windows (np. `/mnt/e/Obsidian/vibe-emulator` z poziomu WSL), a `~/Obsidian/Vault` zrób dowiązaniem
symbolicznym do niego (`ln -s /mnt/e/Obsidian/vibe-emulator ~/Obsidian/Vault`) — serwer działa wtedy bez zmiennej
środowiskowej i bez zmian w konfiguracji.

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
cd tools/obsidian-mcp && node test.js   # 42 testy dymne, na fixture w katalogu tymczasowym
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

## Architektura C64 (`src/Core/C64`)

Szkielet maszyny z wymiennym rdzeniem CPU. VIC-II jest nadrzędny: to on decyduje,
czy procesor dostanie dostęp do magistrali w danym cyklu.

```csharp
// rdzeń cyklowy — maszyna stanów, zgodny co do cyklu
C64Machine exact = C64Machine.CreateCycleExact();
exact.Reset(0x0200);
exact.Bus.LoadRam(0x0200, [0xA9, 0x10, 0x8D, 0x00, 0x03, 0x00]);
exact.RunFrame();          // 19 656 cykli

// rdzeń atomowy — adapter nad Cpu<TBus>, szybki, nie cycle-exact
C64Machine fast = C64Machine.CreateAtomic();
```

| element | rola |
|---|---|
| `C64Machine` | zegar: VIC → CPU/stall → CIA ×2 → SID → port; `RunFrame` = 19 656 cykli |
| `ICpuCore<CpuState>` (`src/Cpu.Abstractions`) | kontrakt rdzenia: `Tick()` (1 cykl), `Stall()` (cykl ukradziony) |
| `StateMachineCpuCore` (`src/Mos6502`) | rdzeń cyklowy; cykle **wynikają** z liczby faz, nie z tabeli |
| `AtomicCpuCore` | adapter nad `Cpu<TBus>` z `CycleCostTable`; udokumentowane ograniczenie |
| `VicII` | raster 63×312, badline, BA/AEC (kradzież 40 cykli), rejestry `$D000-$D3FF` |
| `C64Bus` | dekoder `$0000-$FFFF`, bankowanie, open bus |
| `ProcessorPort` | `$00`/`$01`, DDR, przełączanie banków z opóźnieniem 3 cykli (jeszcze nie podpięty pod adresy `$00`/`$01` w `C64Bus`) |
| `Cia6526` | mockup: dwa timery, flagi IRQ |
| `Sid6581` | mockup: model rejestrowy |

Sprawdzone testami: klatka = 19 656 cykli, badline kradnie dokładnie 40 cykli w oknie
15–54, `INC abs` = 6 cykli z dwoma zapisami, `LDA abs,X` = 5 cykli przy przekroczeniu strony.

### Opkody z tabel CSV (`tools/OpcodeInfo`)

Narzędzie czyta `docs/cpu/opcodes/*.csv` (albo wskazane `--file`) i wypisuje wybrane opkody jako JSON:

```bash
dotnet run -c Release --project tools/OpcodeInfo -- --opcode A9
dotnet run -c Release --project tools/OpcodeInfo -- --opcode 00-0F --set nmos --pretty
dotnet run -c Release --project tools/OpcodeInfo -- --mnemonic LDA --mode abs,abx
dotnet run -c Release --project tools/OpcodeInfo -- --class 'official*' --set c02
```

| opcja | znaczenie |
|---|---|
| `--opcode <spec>` | opkod lub grupa: `A9`, `A9,EA`, `00-0F`, `A9,00-0F` |
| `--mnemonic <lista>` | mnemonic lub alias (`LDA,ADC`) |
| `--mode <lista>` | tryb adresowania; `impl`/`acc` = tryb niejawny (puste pole w CSV) |
| `--class <lista>` | klasa; `*` na końcu dopasowuje prefiks (`official*`) |
| `--set <lista>` | `nmos`, `c02` |
| `--file`, `--csv-dir` | własne źródło danych |
| `--pretty` | JSON z wcięciami (domyślnie jedna linia) |
| `--verify-code` | porównaj adnotacje `[Opcode]` w kodzie z CSV (wynik: JSON, exit 1 przy niezgodności) |
| `--include-missing` | z `--verify-code`: pełna lista opkodów z CSV nieobecnych w kodzie |

Wynik na stdout, błędy na stderr. Kody wyjścia: `0` = OK (także 0 trafień), `1` = niezgodności kodu z CSV
(tylko `--verify-code`), `2` = błąd użycia lub danych.
Zmiana CSV musi przejść weryfikację z `docs/cpu/opcodes.md` — narzędzie tylko czyta dane.

Każdy zarejestrowany opkod ma adnotację `[Opcode(opkod, zestaw, mnemonic, tryb, rodzaj, akcja, cykle)]`
w plikach `src/Mos6502/OpcodeCatalog/OpcodeCatalog*.cs` (podział na warianty i etapy planu). `--verify-code` czyta ją refleksją
i porównuje z CSV: mnemonic, tryb adresowania,
długość instrukcji i **cykle bazowe** (wyprowadzone z faz rdzenia: tryb + rodzaj + cykle wewnętrzne, a nie skopiowane
z CSV), a także wartość pola z adnotacją. Zestaw `c02` dziedziczy tabelę NMOS, tak jak `CpuVariants.Cmos65C02`.
Krok jest bramką CI (`Opcode catalog (verify)`).

```bash
dotnet run -c Release --project tools/OpcodeInfo -- --verify-code --pretty
```

### Śledzenie wykonania (`tools/C64Trace`)

Tracer wykonuje program cykl po cyklu i pokazuje stan rejestrów, instrukcję oraz każdy
dostęp do magistrali:

```bash
dotnet run -c Release --project tools/C64Trace -- demo     # LDA/STA/INC/JMP
dotnet run -c Release --project tools/C64Trace -- rmw      # RMW: dwa zapisy
dotnet run -c Release --project tools/C64Trace -- badline  # BA/AEC i kradzież cykli
dotnet run -c Release --project tools/C64Trace -- count    # koszt klatki
```

Opcje:

| opcja | znaczenie |
|---|---|
| `--from <cykl>` / `-f` | pierwszy wyświetlany cykl (włącznie) |
| `--to <cykl>` / `-t` | ostatni wyświetlany cykl (włącznie); poza zakresem = ostrzeżenie na stderr |
| `--dump <plik>` / `-d` | zapisuje ślad do pliku (w tym samym formacie co stdout) |
| `--format text\|vice\|json` | `text` = czytelna tabela, `vice` = format zbliżony do logu VICE (zgodności z VICE nie potwierdzono), `json` = NDJSON: wiersz na cykl + podsumowanie |
| `--hex "A9 7F …"` / `--load <plik>` | własny program zamiast scenariusza (`.prg`: 2 pierwsze bajty = adres ładowania) |
| `--org <adres>` | adres ładowania i start PC (domyślnie `0200`) |
| `--reg A=7F` / `--poke ADRES=WART` | rejestry CPU (A, X, Y, SP) i zapisy do pamięci/rejestrów VIC przed startem; powtarzalne |
| `--cycles <N>` | limit cykli maszyny (1..1 000 000); własny program bez limitu i warunków kończy się po 1000 |
| `--instr <N>` | zatrzymaj po N ukończonych instrukcjach |
| `--until <warunek>` | zatrzymaj na `pc=ADR`, `write=ADR`, `read=ADR`, `raster=LINIA:CYKL` lub `jam`; powtarzalne |
| `--help` / `-h` | pomoc, scenariusze i kody wyjścia |

Kody wyjścia: `0` OK, `2` błąd użycia (m.in. nieznany scenariusz, zły argument, błąd zapisu `--dump`),
`3` CPU zawiesił się (JAM) bez `--until jam`. Ostrzeżenia i nagłówki w trybach `vice`/`json` idą na stderr.

Przykłady:

```bash
dotnet run -c Release --project tools/C64Trace -- badline --from 3030 --to 3060
dotnet run -c Release --project tools/C64Trace -- demo --dump artifacts/trace.txt --format vice
dotnet run -c Release --project tools/C64Trace -- --hex "A9 7F 8D 00 03 4C 00 02" --until write=0300
dotnet run -c Release --project tools/C64Trace -- --hex "EA 4C 00 02" --poke D011=18 --cycles 3100 --until raster=48:14 --format json
```

Do pracy z agentem (Copilot, Claude Code) szybciej uruchamiać zbudowaną bibliotekę
(`dotnet build tools/C64Trace -c Release`, potem `dotnet tools/C64Trace/bin/Release/net10.0/VibeEmulator.C64Trace.dll …`) —
to polecenie jest dopuszczone w auto-approve (`.vscode/settings.json`). Przepisy (przeliczanie rastra na cykl,
badline/AEC/BA, analiza JSON, ograniczenia modelu) opisuje skill `.github/skills/c64-trace-debug`.

W VS Code dostępne są konfiguracje **C64 Trace: demo**, **C64 Trace: badline** i
**C64 Trace: rmw** (`.vscode/launch.json`) — można ustawiać breakpointy w
`StateMachineCpuCore.Execute()` i `C64Machine.Tick()`.

Kolumny: cykl, linia rastra, cykl w linii, `AEC` (`cpu` = dostęp, `---` = cykl ukradziony),
`BA`, PC instrukcji, opkod, instrukcja, rejestry, flagi, dostępy do magistrali.

Do śledzenia służy `C64Bus.Observer` (`IBusObserver` z `src/Memory`) — w normalnej pracy jest `null`,
więc kosztuje jedno sprawdzenie na dostęp.

## Plan: C64 cycle-exact
Docelowa implementacja 6510 + VIC-II (PAL 6569R5, 19 656 cykli/klatkę) opisana jest w
[`docs/plan-c64-cycle-exact.md`](docs/plan-c64-cycle-exact.md) — 11 faz, kryteria akceptacji
dla każdej i zestawy walidacyjne (Tom Harte, VICE testprogs, perfect6502). Aktualny stan faz opisuje
tabela „Stan realizacji" na początku dokumentu.

## Model procesora (`src/Mos6502`)

Generyczny rdzeń emulacji: ten sam kod działa dla płaskiej pamięci, mapy regionów
i dowolnej magistrali implementującej `IMemoryBus`. Tu leży też rdzeń cyklowy `StateMachineCpuCore`
(kontrakt `ICpuCore<CpuState>` z `src/Cpu.Abstractions`) i deasembler `Disassembler`.

Rdzeń cyklowy działa według wariantu `CpuVariant` (pełna tabela opkodów, styl RMW i adres dummy read przy
indeksowaniu): `CpuVariants.Nmos6502` (domyślny, C64) albo `CpuVariants.Cmos65C02` (NMOS + STZ, INC A,
DEC A; RMW z dwoma odczytami). Wariant 6510 dojdzie w kolejnym etapie. Rdzeń jest walidowany wektorami
SingleStepTests/65x02 (w repo próbka 100 testów na opkod; licencja MIT,
patrz `tests/VibeEmulator.Mos6502.Tests/Vectors/README.md`). Zakres zaimplementowanych i zweryfikowanych
opkodów podaje `tools/OpcodeInfo --verify-code` oraz tabela „Stan realizacji” w `docs/plan-c64-cycle-exact.md`.

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
| `cpu` | prawdziwy `Cpu<TBus>` z `src/Mos6502`: `Step()` i `Run()` — fetch + decode + execute |

## Hooki
| Plik | Event | Działanie |
|---|---|---|
| `guard-dangerous-commands.json` | `PreToolUse` | blokuje (`deny`) lub wymaga potwierdzenia (`ask`) dla destrukcyjnych poleceń |
| `format-code.json` | `PostToolUse` | formatuje zmienione pliki `.cs` (`dotnet format`) |

Testy hooków (w VS Code: zadanie `test:hooks`):

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
dotnet format --verify-no-changes --severity warn   # tylko sprawdzenie, bez zmian w plikach
```

### VS Code: zadania i debugowanie

Zadania (`Terminal → Run Task`, definicje w `.vscode/tasks.json`): `build`, `build:release`, `test`,
`format`, `format:verify`, `test:hooks`, `trace:demo`, `run:cli`, `publish:cli`.

Konfiguracje debugowania (`.vscode/launch.json`): `CLI (Debug)`, `CLI (Release)`, trzy scenariusze
`C64 Trace`, oraz narzędzia pomiarowe w Release: `Bench: Fibonacci`, `EmuBench: cpu`, `CycleModel`.
Pomiary pod debuggerem są zniekształcone — liczby bierz z `Ctrl+F5` lub z poleceń `dotnet run -c Release`.

## Użycie

```bash
dotnet run --project src/Cli -- version
dotnet run --project src/Cli -- help
```

## Zasady

Reguły optymalizacji i konwencje kodu: `.github/instructions/csharp.instructions.md`.
