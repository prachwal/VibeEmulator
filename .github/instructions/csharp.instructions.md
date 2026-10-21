---
applyTo: "src/**/*.cs"
description: "Zasady wydajności i stylu kodu C# w src/: algorytm przed mikrooptymalizacją, brak alokacji w gorącej ścieżce, nie wyłączaj tieringu/PGO, pomiar zamiast intuicji."
---

# Wydajność i styl kodu C# (`src/`)

Reguły uporządkowane od największego do najmniejszego zysku. Mikrooptymalizacji bez pomiaru nie stosuj, a kod ma
pozostać idiomatyczny. W tym repo **dokładność cyklowa emulacji jest ważniejsza niż szybkość** (zob.
`c64-emulation.instructions.md`).

## 1. Algorytm przed wszystkim

- Najpierw złożoność: O(n²) → O(n log n) daje zysk nieosiągalny dla JIT.
- Memoizacja, `HashSet`/`Dictionary` zamiast skanowania liniowego; powtarzalne obliczenia wynoś przed pętlę.

## 2. Alokacje i GC

- `Span<T>`/`ReadOnlySpan<T>` zamiast `Substring`, `ToArray`, `ToList`; `stackalloc` dla małych buforów (< ~1 KB),
  `ArrayPool<T>.Shared` dla większych (zwracaj w `finally`).
- Bez LINQ i bez boxingu w gorących ścieżkach (alokują enumeratory, delegaty, obiekty).
- Stringi buduj przez `StringBuilder` z pojemnością lub `string.Create`, nie w pętli przez `+`.

## 3. JIT i typy

- Klasy `sealed`, gdy nie są dziedziczone (devirtualizacja); `readonly struct` dla niemutowalnych typów wartościowych;
  `in`/`ref` dla dużych struktur.
- `AggressiveInlining` tylko na małych, gorących metodach.
- **Nie dodawaj `[MethodImpl(AggressiveOptimization)]` bez pomiaru.** Wyłącza rejit z PGO; na metodach rekurencyjnych
  zmierzono pogorszenie ~20%.
- **Nie wyłączaj `TieredCompilation` ani `TieredPGO`.** Zmierzono: `TieredPGO=0` ≈ −33%, `TieredCompilation=0` ≈ −43%.
- Build produkcyjny i pomiary zawsze w `Release`.

## 4. Pomiar

- Optymalizację zaczynaj od profilu lub pomiaru; podaj liczby (czas, alokacje) w opisie zmiany.
- Standard tego repo: harness w `tools/` z **minimum z wielu przebiegów** po warmupie, warianty przeplatane
  (zob. `tools-benchmarks.instructions.md`, skill `csharp-perf-benchmark`). BenchmarkDotNet dodawaj tylko z uzasadnieniem
  nowej zależności (repo go dziś nie ma).
- Jeden przebieg `Stopwatch` jest niemiarodajny — szum potrafi odwrócić wniosek.

## 5. Bezpieczeństwo typów

Nie poświęcaj `Nullable` ani sprawdzania zakresów dla hipotetycznego zysku. Nie blokuj wątków (`Task.Wait()`/`.Result`
w kodzie async); `ConfigureAwait(false)` w bibliotekach.

## 6. Model maszyny (`src/Mos6502`, `src/Core/C64`)

- Stan maszyny w `struct` z publicznymi polami, przekazywany przez `ref` (wzorzec: `CpuState`). Rejestrów nie opakowuj we
  właściwości z logiką w gorącej pętli.
- Hot path (`Tick`, operacje ALU) nie alokuje, nie używa LINQ ani stringów.
- Warianty procesora i ustawienia czytaj **raz** w konstruktorze do pól `readonly`; w `Tick` tylko odczyt pola.
- Szczegóły: `c64-emulation.instructions.md`, `mos6502.instructions.md`.
## 7. Pliki: dziel na `partial`

**Miękki limit 300 linii** na plik `.cs`; **powyżej 400 linii podział jest obowiązkowy**. Limit nie dotyczy danych
i plików generowanych (`.json`, `.csv`, `Vectors/`).

- Dziel **typ**, nie logikę: `public static partial class OpcodeCatalog` w `OpcodeCatalog.cs`, `OpcodeCatalog.Nmos.cs`,
  `OpcodeCatalog.Cmos.cs`. `partial` nie zmienia wydajności, ABI ani widoczności — JIT widzi jedną klasę.
- Nazwa pliku: `Typ.cs` dla części głównej (pola, konstruktor, API), `Typ.Grupa.cs` dla części tematycznych
  (`StateMachineCpuCore.Operations.cs`, `QueryOptions.Parse.cs`). Grupa = odpowiedzialność, nie „część 1/2/3”.
- **Klasy zagnieżdżone też mogą być `partial`** — dzięki temu katalog opkodów da się rozbić na warianty w osobnych
  plikach bez zmiany API (`OpcodeCatalog.Nmos` zostaje tą samą nazwą).
- Nie przenoś do osobnego pliku jednej metody dla „zmniejszenia liczby linii”; podział ma sens, gdy części mają
  osobną odpowiedzialność (parsowanie vs wykonanie, wariant vs rdzeń, dane vs logika).
- Po podziale plik nie może przekraczać 300 linii — inaczej podziel go dalej.
- Testy: `KlasaTests.cs` można rozbić na `KlasaTests.Grupa.cs`, jeśli rośnie poza limit; nazwy testów i ich liczba się
  nie zmieniają.
- Przy przenoszeniu plików przepnij reguły `.editorconfig` przypięte do ścieżek (np. `src/Mos6502/CpuState.cs`) i wpisy
  `applyTo` — inaczej build ma błędy CA albo reguły przestają działać.
