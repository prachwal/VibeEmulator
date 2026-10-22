# Audyt wydajności — Vibe Emulator (branch `perf-audit`)

Data pomiaru: 2026-10-22 · .NET 10.0.12 · 8 rdzeni · Release.

## 1. Metodologia

- Narzędzia: `tools/EmuBench`, `tools/C64Trace` (scenariusz `count`), `tools/PerfAudit` (nowy, na branchu).
- Pomiar klatki: minimum z 5 rund po 200 klatek (jak w `C64Trace`).
- Pomiar mikro: pętla 0,2–10 mln iteracji, po rozgrzewce.
- Brak alokacji na gorącej ścieżce (`Tick`/`RunFrame`).

## 2. Wyniki

### 2.1 Klatka C64 (Cycle-exact)

| metryka | wartość |
|---|---|
| klatka (min z 5 rund) | **0,454 ms** |
| tick (średnio) | **23,1 ns** |
| budżet 50 Hz | 20 ms → udział **2,3 %** |
| FPS teoretyczny | ~2 200 |

Porównanie rdzeni:

| rdzeń | ms/klatka | ns/tick |
|---|---|---|
| Cycle-exact (`StateMachineCpuCore`) | 0,454 | 23,1 |
| Atomic (`AtomicCpuCore`) | 0,291 | 14,8 |

Cycle-exact jest ~1,56× wolniejszy od atomowego — koszt maszyny stanów i cyklowości.

### 2.2 Mikro-pomiary (ns/operacja)

| element | ns/op | uwagi |
|---|---|---|
| `StateMachineCpuCore.Tick` | 41,9 | gdy CPU ma dostęp do magistrali |
| `VicII.Tick` | 3,1 | |
| `C64Bus.Read` | 3,6 | w tym dekoder banków + open bus |
| `C64Bus.Write` | 8,4 | |
| `AtomicCpuCore.Tick` | 123,7 | adapter, amortyzowany inaczej |

### 2.3 EmuBench (aspekty emulacji)

| sekcja | kluczowy wynik |
|---|---|
| Dispatch (10 M) | `switch` 8,9 ns vs tablica delegatów 12,5 ns vs fnptr 9,3 ns vs wywołanie bezpośrednie 1,6 ns |
| Pamięć (10 M) | odczyt 8-bit 1,6 ns; bankowany 3,1 ns; stronicowany+TLB 2,9 ns |
| ALU (20 M) | flagi eager 6,8 ns vs lazy 0,5 ns (13×); dzielenie 7,1 ns; BCD 3,2 ns |
| Bloki (10 M) | interpreter (switch) 1,6 ns vs prekompilowany 0,22 ns (7×) |
| CPU (10 M) | `Step` LDA #imm 11,9 ns |

## 3. Gdzie jest wąskie gardło i dlaczego

1. **`StateMachineCpuCore` (CPU) — główny koszt.** Każdy cykl to `switch (_phase)` z ~60 gałęziami + wywołania `Execute*`. Fazy to osobne metody, więc koszt wywołania i powrotu. Dodatkowo `_bus.Read`/`Write` są wywołaniami interfejsu (`IMemoryBus`), które JIT nie zawsze dewirtualizuje, bo pole ma typ interfejsu.
2. **`C64Bus.Read`/`Write` — drugi koszt.** Dekoder adresów to kaskada `if` z odczytami `Port.BasicRomEnabled` itd. (właściwości z obliczeniami). Każdy odczyt CPU przechodzi przez ten dekoder; przy 10–20 M operacji/s to dominuje.
3. **`C64Machine.Tick()` — orkiestracja.** Co cykl: `if (_cpu is IInterruptLines lines)` (test typu), `Sid6581.Tick()` (pusty, ale wywołanie), `_bus.Port.Tick()` (zawsze, nawet bez opóźnienia), `Observer?.OnAccess` (null-check).
4. **Eager flags w ALU** — 6,8 ns vs 0,5 ns (lazy): potencjalnie 13× tańsze, ale wymaga zmiany semantyki i testów.
5. **Brak specjalizacji szablonu** — rdzeń jest generyczny przez `IMemoryBus`, więc JIT nie monomorfizuje `_bus.Read` dla `C64Bus`.

## 4. Propozycje usprawnień (ficzery/hacki .NET)

### A. Dewirtualizacja magistrali (największy zysk szac. 20–40 %)
- Zmienić `StateMachineCpuCore` na generyk `StateMachineCpuCore<TBus> where TBus : IMemoryBus`. JIT monomorfizuje `_bus.Read`/`Write` dla `C64Bus` i eliminuje wywołanie interfejsu (devirtualizacja + inlining).
- Alternatywa (mniejsza zmiana): cache'ować `_bus` jako `C64Bus` w ścieżce C64 (podklasa lub pole konkretnego typu) — narusza warstwy, ale mierzalne.

### B. Cache pól na gorącej ścieżce
- `_cpu is IInterruptLines` → pole `_interruptLines` ustawiane w konstruktorze (raz).
- `_bus.Port` → pole `_port` w `C64Machine` i `C64Bus` (zamiast właściwości).
- `Observer?.OnAccess` → sprawdzać `if (_observer is not null)` przed wywołaniem (już podobne, ale auto-property dodaje pośrednictwo).
- `Sid6581.Tick()` to statyczna pusta metoda — usunąć wywołanie z `Tick` lub oznaczyć `[MethodImpl(AggressiveInlining)]` (JIT i tak usunie).

### C. Atrybuty kompilatora na gorących metodach
- `[MethodImpl(MethodImplOptions.AggressiveOptimization)]` na `Tick`, `Execute`, `Fetch`, `Access` — wymusza zoptymalizowany tier wcześniej (TieredCompilation i tak to robi, ale PGO potrzebuje rozgrzewki).
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` na małych metodach (`ReadDecoded`, `SelectIndex`, `ApplyImplied`) — jeśli JIT nie inlinuje sam.
- `[SkipLocalsInit]` na `Tick`/`Execute` — pomija zerowanie lokalnych (bezpieczne, bo wszystkie zmienne są nadpisywane).
- `unsafe` + `Unsafe.SkipInit<T>` dla buforów faz.

### D. Tabele dekodera (C64Bus)
- Zamiast kaskady `if` w `ReadDecoded`/`WriteDecoded`: tablica `byte[256]` regionów (high byte adresu → region) + switch na region. Albo `FrozenDictionary<ushort, Func>` — wolniejsze. Tablica bajtów jest najszybsza.
- Cache `BasicRomEnabled`/`KernalRomEnabled`/`IoVisible` w polach aktualizowanych w `ProcessorPort.Tick` (zamiast obliczać z `Effective` przy każdym odczycie).
- Open bus: przypisanie `OpenBus = value` tylko gdy potrzebne (zawsze potrzebne dla nieoficjalnych opkodów — zostawić, ale jako pole, nie property).

### E. Tabele opkodów i dispatch
- `_table` z `CpuOpcode?[]` (nullable) → `CpuOpcode[]` (wszystkie 256 wariantów zdefiniowane). Eliminuje `HasValue` i null-check w Fetch.
- `switch (_phase)` jest już tablicą skoków; można rozważyć `switch` expression z `goto`? Nie poprawia.
- Prekompilowane bloki (EmuBench: 7× szybciej) — dla emulatora JIT CPU nie stosujemy, ale dla testów/benchmarków tak.

### F. ALU i flagi
- Przejście z eager flags na lazy (13×) — wymaga refaktoru `StatusFlags` i testów wektorowych; ryzykowne. Zalecane dopiero po pełnej weryfikacji.
- BCD adjust (3,2 ns) — akceptowalny, rzadko na gorącej ścieżce.

### G. Pamięć i cache
- `_ram` to `byte[65536]` — mieści się w L2; losowe dostępy 8,8 ns dla 12 MB (miss L3). Dla RAM 64 KB brak problemu.
- ROM-y (`_basicRom` itd.) — opcjonalnie `ReadOnlyMemory<byte>` z `MemoryMarshal.GetArrayDataReference` dla odczytu bez sprawdzeń granic (micro-optymalizacja).
- `LoadRam` przez `CopyTo` — OK.

### H. Infrastruktura .NET
- Włączyć `DOTNET_TieredPGO=1` (już w `Directory.Build.props`).
- `ReadyToRun` dla narzędzia CLI (szybszy start, nie runtime).
- `PublishAot` dla CLI (opcjonalnie) — ale refleksja/`--verify-code` może to utrudnić.
- Użyć `dotnet-counters`/`dotnet-trace` w CI do regresji wydajności (już mamy `PerfAudit`).

## 5. Proponowana kolejność (niskie ryzyko → wysokie)

1. Cache `IInterruptLines` + `Port` + usunięcie `Sid6581.Tick()` (A/B) — ryzyko niskie, zysk ~5 %.
2. `_table` nullable → non-nullable (E) — ryzyko niskie.
3. Tabela dekodera w `C64Bus` (D) — ryzyko średnie, wymaga testów mirrorów.
4. Atrybuty `AggressiveOptimization`/`SkipLocalsInit` (C) — ryzyko niskie, pomiar przed/po.
5. Generyk `StateMachineCpuCore<TBus>` (A) — ryzyko wysokie (API, testy), największy zysk.
6. Lazy flags (F) — ryzyko bardzo wysokie, na końcu.

## 6. Pliki zmienione na branchu

- `tools/PerfAudit/` (nowe narzędzie pomiarowe).
- `docs/perf-audit.md` (ten raport).

Zmiany architektoniczne **nie** zostały wprowadzone — branch zawiera tylko pomiary i dokumentację. Implementacja propozycji to osobne zadania.
EOF
echo done