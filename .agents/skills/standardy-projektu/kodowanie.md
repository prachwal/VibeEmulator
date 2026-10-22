# Standardy kodowania

Źródła: `.editorconfig`, `Directory.Build.props`, `.github/copilot-instructions.md`, `csharp.instructions.md`,
`csharp-tests.instructions.md`, `mos6502.instructions.md`, `c64-emulation.instructions.md`, `tools-benchmarks.instructions.md`
(wszystko w `.github/instructions/`).

## 1. Build i analizatory

- `net10.0`, `LangVersion latest`, `Nullable`, `ImplicitUsings`, `AnalysisLevel latest-recommended`,
  `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors` ustawia **tylko** `Directory.Build.props`. Nie powtarzaj ich w `.csproj`.
- Wersje pakietów wyłącznie w `Directory.Packages.props`; w `.csproj` `PackageReference` **bez** `Version`.
  Nowa zależność wymaga uzasadnienia w opisie zmiany.
- Każde ostrzeżenie łamie build. Napraw przyczynę; wyciszaj tylko punktowo i z uzasadnieniem.
- Reguły o poziomie `warning` w `.editorconfig` (łamią build): `CA1852` (typ bez dziedziczenia ma być `sealed`),
  `CA1861`, `CA1863`, `CA2016` oraz nazwa interfejsu zaczynająca się od `I`. Pozostałe reguły `CA` z `.editorconfig`
  mają poziom `suggestion` (nie łamią buildu), a w `tests/**` `CA1707` jest wyłączona (nazwy z podkreśleniami).
- Reguły analizatorów przypięte do ścieżek (np. `src/Mos6502/State/CpuState.cs`) **przepnij przy przenoszeniu pliku**,
  inaczej build ma błędy `CA`.

## 2. Styl (`.editorconfig`)

| zasada | wartość |
|---|---|
| kodowanie, końce linii | UTF-8, LF, pusta linia na końcu pliku, bez spacji na końcu linii |
| wcięcia | 4 spacje w `.cs`; 2 spacje w `csproj/props/targets/json/yml/md` |
| namespace | file-scoped (`namespace X;`) |
| klamry | zawsze, także przy jednej instrukcji |
| `var` | **nie** dla typów wbudowanych (`int`, `byte`); tak, gdy typ wynika z prawej strony |
| kwalifikatory `this.` | nie używaj |
| `using System.*` | na początku |
| składnia | preferuj dopasowanie wzorców, `switch` expression, konstruktory główne, `readonly struct` |

Formatowanie sprawdza `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn` — to samo robi CI.

## 3. Pliki i podział na `partial`

- Miękki limit **300 linii** na plik `.cs`; **powyżej 400 podział jest obowiązkowy**. Limit nie dotyczy danych
  i plików generowanych (`.json`, `.csv`, `Vectors/`).
- Dziel **typ**, nie logikę: `Typ.cs` (pola, konstruktor, API), `Typ.Grupa.cs` (odpowiedzialność, nie „część 2").
  Przykłady: `StateMachineCpuCore.Operations.cs`, `OpcodeCatalog.Nmos.Logic.cs`.
- Nie wynoś jednej metody do osobnego pliku dla samej liczby linii. Po podziale plik nadal ≤ 300 linii.
- Testy: `KlasaTests.cs` dziel na `KlasaTests.Grupa.cs`; nazwy i liczba testów się nie zmieniają.

## 4. Warstwy i zależności

| projekt | zawartość | może zależeć od |
|---|---|---|
| `Cpu.Abstractions` | `ICpuCore`, `ICpuCore<TState>`, `IInterruptLines` | nic |
| `Memory` | `IMemoryBus`, `FlatMemory`, `MemoryMap`, `IBusObserver`, `IPeekableBus` | nic |
| `Mos6502` | `CpuState`, `Cpu<TBus>` (atomowy), `StateMachineCpuCore` (cycle-exact), `CpuVariant`, `Disassembler` | `Memory`, `Cpu.Abstractions` |
| `Core` | maszyna C64, `VicII`, `C64Bus`, CIA, SID | wszystkie powyższe |

- Nigdy odwrotnie. Kod specyficzny dla C64 (VIC-II, port 6510 na magistrali) należy do `Core`.
- Nie uogólniaj `Cpu<TBus>` do silnika dla dowolnego CPU. Inne rodziny (Z80) mają **własne rdzenie** za
  `ICpuCore<TState>`; wspólne są kontrakty, magistrala i narzędzia diagnostyczne.
- Kod w `src/` **nie pisze do `Console`** — przyjmuj `TextWriter`. Wyjątek: programy w `tools/`.

## 5. Warianty: dane zamiast `if`

- Różnicę wariantu wyrażaj wpisem tabeli `CpuVariant` albo właściwością czytaną **raz** do pola `readonly` w konstruktorze.
  W `Tick` tylko odczyt pola; bez wyjątków per instrukcja w fazach.
- Domyślny wariant w konstruktorach (`new StateMachineCpuCore(bus)`) zostaje NMOS — jego zmiana psuje C64.
- Nowy wariant = nowy `CpuVariant` plus testy wektorowe jego zestawu. Wzorzec: `HasDecimalMode` w `Nes2A03`.
- Konstruktor `CpuVariant` odrzuca zduplikowany opkod. Tabelę 65C02 buduje `BuildCmosOpcodes()` (baza NMOS nadpisana
  dodatkami) — to jedyne miejsce, gdzie wolno nadpisać wpis bazy.

## 6. Wierność cyklowa (`src/Core/C64`, `src/Mos6502/CycleExact`)

Dokładność cyklowa jest ważniejsza niż szybkość.

- `Tick()` = jeden cykl. Do CPU w C64 służy `Tick()`/`Stall()`, **nigdy `Step()`** — VIC-II musi móc zatrzymać CPU w środku instrukcji.
- **VIC-II jest masterem:** rozstrzyga o `AEC`. Badline kradnie **40 cykli** (`BA` od 12, `AEC` od 15 do 54).
- Cykle wynikają z faz = dostępów do magistrali. **Nie dodawaj tabel cykli** do rdzenia cyklowego. Nowy tryb adresowania
  to nowe fazy i nowa wartość `AddressingMode` na końcu enuma.
- Dummy read i zapis starej wartości w RMW są obowiązkowe; różnią się per wariant (NMOS: `read → write(stara) → write(nowa)`,
  65C02: `read → read → write(nowa)`). Rozstrzyga wariant, nie `if`.
- Odczyt spod niezmapowanego adresu zwraca **ostatnią wartość na magistrali** (`C64Bus.OpenBus`), nie zero.
- Zapis do ROM jest ignorowany bez wyjątku. Przełączenie banku portu 6510 trwa 3 cykle **(niezweryfikowane: wzór odczytu `$01`
  i opóźnienie pochodzą z dokumentacji repo, źródeł o porcie w `docs/cpu/` nie przeczytano — `c64-emulation.instructions.md` pkt 4)**.
- Świadome uproszczenie oznacz komentarzem `// uproszczenie:` i opisz w dokumentacji typu. Nie udawaj dokładności.
- Zmiana wartości w tabeli lub oczekiwanej asercji w `tests/.../C64/` wymaga uzasadnienia w opisie zmiany i weryfikacji
  ze źródłem (wektory, `docs/cpu/`, dla VIC-II dokumentacja i VICE).
- `Tick()` i wszystko pod nim **nie alokuje** (szczegóły w [optymalizacja.md](optymalizacja.md)).

## 7. Dodawanie opkodu

Pełna procedura: `.agents/skills/nowy-opkod/SKILL.md` oraz `mos6502.instructions.md` sekcja 3. Skrót:

1. Dane opkodu z `docs/cpu/opcodes/nmos6502.csv` / `wdc65c02.csv` i `docs/cpu/opcodes.md`, nie z pamięci.
2. Pole `CpuOpcode` **z adnotacją `[Opcode]`** w `src/Mos6502/OpcodeCatalog/OpcodeCatalog.Nmos.<Etap>.cs` (albo `Cmos.cs`);
   dopisz do tablicy `Opcodes`/`Additions`. Nie dopisuj wpisów w `CpuVariant.cs`.
3. `Disassembler` jest generowany z tablicy rdzenia, ale sprawdź test po całej tablicy wariantu: wymaga tekstu ≠ `???` i właściwej długości.
4. Próbka wektorów i opkod w liście testu — zob. [wektory.md](wektory.md).
5. `dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code` → exit 0.

## 8. Testy (xUnit)

- Nazwa: `Metoda_Scenariusz_OczekiwanyWynik`. Jeden test = jedno zachowanie. Arrange/Act/Assert rozdzielone pustą linią.
- Testy CPU przez **`Tick()`**. Liczbę cykli licz jawnie i asercją liczbową (`Assert.Equal(6, cycles)`), nie `> 5`.
  Pętla: `do { cpu.Tick(); cycles++; } while (cpu.Pending != 0 && cycles < 64);`.
- Zapisy do magistrali sprawdzaj jako **sekwencję** (RMW NMOS: stara i nowa wartość).
- Niezmienniki: klatka = **19 656 cykli**, badline = **40 cykli**.
- Deterministycznie: bez `Random` bez ziarna, bez czasu ściennego, bez `Thread.Sleep` (czas przez `TimeProvider`).
  Czasów wykonania nie porównuj w testach jednostkowych — od tego są `tools/`.
- Testy nie zależą od bieżącego katalogu, zmiennych środowiskowych, sieci ani stanu maszyny poza repo.
- Logikę CLI testuj przez `CliApplication.Run(args, stdout, stderr)` z `StringWriter`.
- Każda nowa funkcja ma test. Testy typów z `Mos6502` → `Mos6502.Tests`, z `Memory` → `Memory.Tests`, z `Core` → `Core.Tests` (C64 w `C64/`).
- `[Fact(Skip = …)]` tylko z dokładnym opisem różnicy (opkod, przykład, oczekiwane vs faktyczne) i odnośnikiem do etapu naprawy.

## 9. Dokumentacja i komentarze

- Po polsku; nazwy symboli i poleceń bez tłumaczenia, w znacznikach kodu.
- Fakt trafia do dokumentu tylko ze źródłem albo po sprawdzeniu programowym; reszta jest **(niezweryfikowana)**.
  Liczby podawaj z zakresem ważności („próbka 100 testów w repo, pełne poza repo").
- Stan implementacji (opkody, etapy, wyniki) utrzymuje `docs/plan-c64-cycle-exact.md`; `docs/cpu/` opisuje sprzęt i testy.
  Nie kopiuj liczb stanu do `docs/cpu/`. Linki względne muszą wskazywać istniejące pliki.

## 10. Definicja ukończenia

Uruchom i podaj **liczby**:

```bash
dotnet build VibeEmulator.slnx -c Debug --nologo     # 0 ostrzeżeń, 0 błędów
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

Debug nie jest powtarzany w CI, więc uruchom go lokalnie. Zmiana w `.github/hooks/`: `for t in .github/hooks/test-*.sh; do bash "$t"; done`.
Zmiana w `tools/obsidian-mcp/`: `node tools/obsidian-mcp/test.js`. Nie modyfikuj hooków bez wyraźnej prośby.

## 11. Commity (tylko na prośbę)

Format `typ(zakres): opis` po polsku. Typy: `feat fix refactor test docs chore ci build`. Zakresy: `core mos6502 memory tools mcp vscode copilot plan`.
Treść: **dlaczego** i jak zweryfikowano (liczby). Jedna rzecz to jeden commit i każdy commit musi się budować.
