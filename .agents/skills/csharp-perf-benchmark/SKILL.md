---
name: csharp-perf-benchmark
description: 'Measure and compare C# performance without being misled by noise. Use when optimizing a hot path, comparing implementations, validating a micro-optimization, or when someone claims code is faster without numbers. Covers warmup, tiered JIT/PGO effects, build configuration, and when to reach for BenchmarkDotNet.'
argument-hint: 'Opisz, co chcesz zmierzyć (np. "porównaj wersję rekurencyjną i iteracyjną")'
---

# C# Performance Benchmark

## When to Use

- Chcesz porównać dwie implementacje tej samej funkcji.
- Ktoś twierdzi, że zmiana jest szybsza — trzeba to potwierdzić.
- Podejrzewasz regresję wydajności po refaktorze.
- Zastanawiasz się nad atrybutem JIT (`AggressiveInlining`, `AggressiveOptimization`).

## Zasady, które trzeba znać zanim zmierzysz

1. **Mierz po warmupie.** Pierwsze wywołania idą przez tier-0 JIT; dopiero po rejicie z PGO widać
   docelową wydajność. Jeden przebieg bez warmupu jest niemiarodajny.
2. **Nie wyłączaj tieringu ani PGO.** `TieredPGO=0` i `TieredCompilation=0` pogarszają wynik
   (zmierzone odpowiednio ok. −33% i −43%).
3. **`[MethodImpl(AggressiveOptimization)]` nie jest darmowym przyspieszeniem.** Wyłącza rejit z PGO
   dla metody; na rekurencji zmierzono pogorszenie ~20%. Stosuj tylko po pomiarze.
4. **Build zawsze w `Release`** (`<Optimize>true</Optimize>`).
5. **Minimalna wartość z wielu przebiegów**, nie średnia — obciążenie systemu zawyża wyniki.
   Sprawdź `uptime`/`nproc`; przy wysokim load average przeplataj warianty w rundach.
6. **Nie dopuść do eliminacji kodu.** Kompilator potrafi wyciągnąć niezmiennicze wywołanie z pętli
   lub zwinąć wynik. Użyj zmiennej `sink` i argumentów z `args`, nigdy stałych w pętli.

## Procedura

1. Zbuduj warianty w jednym projekcie, wybierane argumentem (np. `dotnet run -c Release -- 38 2 baseline`).
2. Uruchom warmup, potem pętlę `reps` razy i zmierz `Stopwatch.GetTimestamp()` /
   `Stopwatch.GetElapsedTime(t0)`.
3. Wypisz wynik do porównania:

   ```text
   <czas_w_sekundach> <sink>
   ```

4. Uruchom każdy wariant co najmniej 5–7 razy, przeplatając je między sobą, i wybierz minimum.
5. Przelicz na jednostkę pracy (ns/wywołanie), żeby porównać różne rozmiary danych.
6. Dopiero potem wyciągaj wnioski i zapisz liczby w opisie zmiany.

## Kiedy przejść na BenchmarkDotNet

Ręczny `Stopwatch` wystarcza do szybkiego porównania. Do wniosków publikowanych lub do regresji
w CI użyj BenchmarkDotNet — daje warmup, statystykę, izolację procesu i wykrywanie eliminacji kodu:

```bash
dotnet add package BenchmarkDotNet
dotnet run -c Release
```

## Czego nie robić

- Nie mierz w `Debug`.
- Nie porównuj czasów z różnych maszyn i różnych stanów obciążenia.
- Nie optymalizuj bez profilu (`dotnet-counters`, `dotnet-trace`) — najpierw znajdź wąskie gardło.
- Nie zmieniaj kilku rzeczy naraz.
