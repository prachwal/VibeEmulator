---
applyTo: "src/**/*.cs"
description: "Zasady optymalizacji wydajności kodu C# w projektach produkcyjnych (src/)."
---

# Zasady optymalizacji C# (kod produkcyjny)

Reguły uporządkowane od największego do najmniejszego zysku. Nie stosuj mikrooptymalizacji,
dopóki nie masz pomiaru wskazującego wąskie gardło.

## 1. Algorytm > wszystko

- Najpierw sprawdź złożoność. Zmiana O(n²) → O(n log n) daje zyski nieosiągalne dla JIT.
- Używaj memoizacji, wyszukiwania binarnego, `HashSet`/`Dictionary` zamiast liniowego skanowania.
- Powtarzalne obliczenia dla tych samych argumentów wynoś przed pętlę lub do cache.

## 2. Alokacje i GC

- Preferuj `Span<T>`/`ReadOnlySpan<T>` zamiast `Substring`, `ToArray`, `ToList`.
- Używaj `stackalloc` dla małych buforów tymczasowych (< ~1 KB) i `ArrayPool<T>.Shared` dla większych
  — zawsze zwracaj w `finally`.
- Unikaj LINQ w gorących ścieżkach (`Where`/`Select`/`Any` alokują enumeratory i delegaty).
- Unikaj boxingu: nie przekazuj typów wartościowych jako `object`/interfejs bez potrzeby.
- Buduj stringi przez `StringBuilder` z zadaną pojemnością lub `string.Create`; nie łącz w pętli.
- Stosuj `CollectionsMarshal.AsSpan(list)` i `FrozenDictionary` przy odczytach read-only.

## 3. Decyzje JIT i typy

- Oznaczaj klasy `sealed`, gdy nie są dziedziczone — umożliwia devirtualizację.
- `readonly struct` dla niemutowalnych typów wartościowych; `in`/`ref` dla dużych struktur.
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` tylko na małych, gorących metodach.
- **Nie używaj `[MethodImpl(AggressiveOptimization)]` domyślnie.** Wyłącza rejit z PGO; na metodach
  rekurencyjnych zmierzono pogorszenie ~20% względem kodu bez atrybutu. Dodawaj wyłącznie po pomiarze.
- W gorących ścieżkach async używaj `ValueTask` zamiast `Task`; w bibliotekach dodawaj
  `ConfigureAwait(false)`.
- Serializacja: `System.Text.Json` z source generatorami zamiast refleksji.

## 4. Konfiguracja kompilacji i runtime

- Build produkcyjny zawsze w `Release` (`<Optimize>true</Optimize>`).
- **Nie wyłączaj `TieredCompilation` ani `TieredPGO`.** Zmierzono: `TieredPGO=0` ≈ −33% wydajności,
  `TieredCompilation=0` ≈ −43% w gorącej pętli.
- `PublishReadyToRun` — skraca start, nie zmienia wydajności pętli.
- `NativeAOT` — dla szybkiego startu i małego binarium; nie przyspiesza obliczeń. Wymaga `clang`.
- `ServerGarbageCollection` — tylko dla usług serwerowych o dużej przepustowości.
- `InvariantGlobalization` — gdy aplikacja nie potrzebuje danych kulturowych.

## 5. I/O i współbieżność

- Nie blokuj wątków: `Task.Wait()`/`.Result` w kodzie async jest zabronione.
- Strumienie buforuj (`BufferedStream`, `StreamReader` z buforem) i czytaj partiami.
- `Parallel.ForEachAsync` / `Parallel.For` dla CPU-bound; `SemaphoreSlim` do limitowania I/O.

## 6. Dyscyplina pomiarowa

- Optymalizację zaczynaj od profilu: `dotnet-counters`, `dotnet-trace`, benchmark.
- Do porównań używaj BenchmarkDotNet (warmup, statystyka, izolacja). Ręczny `Stopwatch` na jednym
  przebiegu jest niemiarodajny — szum potrafi odwrócić wniosek.
- Zawsze mierz stan ustalony (po warmupie) i porównuj z wariantem bazowym.
- W komentarzu lub opisie zmiany podaj liczby: czas, alokacje, rozmiar danych.

## 7. Czytelność

- Mikrooptymalizacja bez pomiaru jest zabroniona. Kod ma pozostać idiomatyczny.
- Nie poświęcaj bezpieczeństwa typów (`Nullable`, sprawdzanie zakresów) dla hipotetycznego zysku.
