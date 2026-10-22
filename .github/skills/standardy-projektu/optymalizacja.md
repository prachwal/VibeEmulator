# Standardy optymalizacji

Źródła: `csharp.instructions.md`, `tools-benchmarks.instructions.md`, `c64-emulation.instructions.md`, `Directory.Build.props`,
skille `csharp-perf-benchmark` i `perf-check`, `tools/PerfAudit/README.md`, `docs/perf-audit.md`.

## 1. Zanim zaczniesz

- **Dokładność cyklowa jest ważniejsza niż szybkość.** Optymalizacja nie może zmienić semantyki: testy i wektory mają
  przejść bez modyfikacji oczekiwań.
- Klatka PAL to 19 656 cykli i zajmuje około 2% budżetu 20 ms (`docs/perf-audit.md`: 0,42–0,48 ms na `PerfAudit`).
  Podaj w opisie zmiany **konkretny powód** optymalizacji (cel, budżet, brakujący zapas) — sam „da się szybciej" nim nie jest.
- Zacznij od pomiaru lub profilu (`dotnet-counters`, `dotnet-trace`), nie od domysłu.

## 2. Kolejność działań (od największego zysku)

1. **Algorytm i struktura danych** — O(n²) → O(n log n), `HashSet`/`Dictionary` zamiast skanowania, obliczenia przed pętlę.
2. **Alokacje i GC** — `Span<T>`/`ReadOnlySpan<T>` zamiast `Substring`/`ToArray`/`ToList`; `stackalloc` do ~1 KB;
   `ArrayPool<T>.Shared` dla większych (zwrot w `finally`); `StringBuilder` z pojemnością albo `string.Create`.
3. **Typy i JIT** — `sealed`, `readonly struct`, `in`/`ref` dla dużych struktur, `AggressiveInlining` tylko na małych gorących metodach.
4. **Mikrooptymalizacje** — tylko z pomiarem i tylko gdy 1–3 nie wystarczą.

## 3. Hot path: `Tick()` i wszystko pod nim

- **Zero alokacji**, zero LINQ, zero `List`, zero stringów, zero boxingu.
- Stan maszyny w `struct` z publicznymi polami przekazywanym przez `ref` (wzorzec `CpuState`); nie opakowuj rejestrów
  we właściwości z logiką.
- Warianty i ustawienia czytaj **raz** w konstruktorze do pól `readonly`.
- Wartości wywiedzione na gorącej ścieżce cache'uj w polach (`C64Machine._interruptLines`, `_port`; flagi bankowania w `ProcessorPort`).
  Zmierzony efekt (`PerfAudit`, mediana z 9 rund): `Frame/nop` −4,5%, `CpuTick/nop` −4,0%, `Bus/mixed + banking` −6,2%,
  `Frame/full + banking` ~0%. Wniosek: efekt jest rzędu kilku procent i zależy od ścieżki.
- `IBusObserver` jest opcjonalny (`null`): jedno sprawdzenie, zero alokacji.

## 4. Czego nie robić (zmierzone)

| zakaz | dowód |
|---|---|
| wyłączać `TieredPGO` | −33% (`csharp.instructions.md`) |
| wyłączać `TieredCompilation` | −43% (j.w.) |
| dodawać `[MethodImpl(AggressiveOptimization)]` bez pomiaru | rekurencja ok. −20%; na `Tick`/`Execute`/`Fetch`/`Read`/`Write` klatka 0,44 → 0,56 ms (`docs/perf-audit.md`, sekcja 6) — atrybut wyłącza rejit z PGO |
| mierzyć w `Debug` | JIT nie optymalizuje, wynik jest fikcją |
| porównywać średnie | obciążenie systemu tylko zawyża czas; raportuj minimum |
| zmieniać kilka rzeczy naraz w jednym pomiarze | nie wiadomo, co zadziałało |
| porównywać czasy z różnych maszyn lub obciążeń | brak wspólnej bazy |
| dodawać BenchmarkDotNet bez uzasadnienia | to nowa zależność; repo go dziś nie ma |

## 5. Metodyka pomiaru

- Zawsze `-c Release`. Polecenia pomiarowe w dokumentacji i kodzie zawierają `-c Release`; hook wymaga potwierdzenia dla `tools/Bench` bez niego.
- **Rozgrzewka** przed pomiarem (tier-0 JIT). `PerfAudit` rozgrzewa każdy przypadek co najmniej 300 ms.
- **Minimum i mediana z ≥ 9 rund**, warianty **naprzemiennie** (baza, nowa, baza, nowa). `PerfAudit` wymusza `--rounds` ≥ 9.
- **Próg istotności 3%** dla klatki C64. Różnica powyżej progu wymaga wyjaśnionej przyczyny w opisie zmiany; poniżej progu to szum.
- **Nie dopuść do eliminacji kodu:** wynik do zmiennej `sink`, argumenty z `args`, nigdy stałe w pętli.
- Podaj `nproc` i `load average`. Gdy load jest porównywalny z liczbą rdzeni, wynik jest obarczony błędem — napisz to.
- Gdy nowy pomiar odbiega od punktu odniesienia o > 20%, najpierw sprawdź load i czy build był w `Release`.
- Pojedynczy przebieg `Stopwatch` jest niemiarodajny. Wartość „−72%" z jednorazowego pomiaru bez rozgrzewki okazała się artefaktem
  (poprawka w `docs/perf-audit.md`, commit `62c0dbb`).

Narzędzia: `tools/PerfAudit` (klatka, `Tick`, magistrala, weryfikacja przed pomiarem: `--verify-only`),
`tools/C64Trace` (scenariusz `count`), `tools/EmuBench` (aspekty emulacji), `tools/Bench` (Fibonacci).

```bash
dotnet build tools/PerfAudit -c Release
dotnet tools/PerfAudit/bin/Release/net10.0/VibeEmulator.PerfAudit.dll --rounds 9
```

## 6. Raport z optymalizacji

Format (liczby z narzędzia, nie z pamięci):

```text
przypadek      baza (min / med)   nowa (min / med)   zmiana
<przypadek>    <min> / <med>      <min> / <med>      <±%>
rundy: <N ≥ 9>, build: Release, load average: <x> / <rdzenie>, maszyna: <CPU>, .NET <wersja>
testy: <liczba> zielonych; semantyka bez zmian (wektory, Dormann)
```

Do raportu dopisz: co zmieniono, czy wynik przekracza próg 3%, czy ścieżka z bankowaniem zyskała (może nie).

## 7. Kandydaci, których nie wdrażaj bez osobnego zadania

Z `docs/perf-audit.md`: generyczny `StateMachineCpuCore<TBus>` (dewirtualizacja magistrali; zmiana API i testów),
tabela dekodera w `C64Bus` (wymaga testów mirrorów), lazy flags w ALU (wymaga pełnych wektorów). Mają wyższe ryzyko niż
cache pól i wymagają pełnych wektorów oraz testów Dormanna po zmianie.
