---
applyTo: "src/Core/C64/**/*.cs,src/Mos6502/StateMachineCpuCore.cs"
description: "Reguły wierności cyklowej emulacji C64: VIC-II jako master, maszyna stanów CPU, open bus, zakaz alokacji w pętli."
---

# Emulacja C64 — reguły wierności cyklowej

Cel: **PAL 6569R5, 6510, dokładność cyklowa** (poziom 3 z `docs/plan-c64-cycle-exact.md`).
Każda zmiana w `src/Core/C64` i w `src/Mos6502/StateMachineCpuCore.cs` oceniana jest przede wszystkim pod kątem zgodności cyklowej,
dopiero potem wydajności.

## 1. VIC-II jest masterem, nie CPU

- `VicII.Tick()` rozstrzyga, czy CPU dostanie magistralę w danym cyklu (`AEC`).
- CPU **musi** dać się zatrzymać w środku instrukcji → tylko `Tick()`/`Stall()`, nigdy `Step()`.
- Nie przenoś sterowania do CPU „bo tak wygodniej". Badline i sprite DMA kradną cykle bez zgody CPU.
- Okno kradzieży na linii badline: `BA` od cyklu 12, `AEC` od 15, koniec po 54 → **40 cykli**.

## 2. Cykle wynikają z dostępów

- Nie dodawaj tabel cykli do rdzenia cyklowego. Liczba cykli = liczba faz = liczba dostępów
  do magistrali (`StateMachineCpuCore`).
- Każdy nowy tryb adresowania dodawaj jako **fazy**, nie jako stałą w tabeli.
- Dummy read i zapis starej wartości w RMW są obowiązkowe — urządzenia je widzą:
  - `abs,X` / `abs,Y`: dummy read spod adresu **bez** korekty strony,
  - `INC`/`DEC`/`ASL`/`LSR`/`ROL`/`ROR`: `read → write(stara) → write(nowa)`.
- Ghost byte: przy przekroczeniu strony `abs,X` czyta spod `(base & 0xFF00) | ((base + X) & 0xFF)`.
- Zapisy zawsze płacą dummy read, odczyty tylko przy przekroczeniu strony.

## 3. Open bus

- Odczyt spod niezmapowanego adresu zwraca **ostatnią wartość na magistrali**, nie zero.
- `C64Bus.OpenBus` aktualizuj przy **każdym** odczycie, przed zwróceniem wartości.
- Niestabilne opkody (`ANE`, `LXA`, `SHA`, `SHX`, `SHY`, `TAS`) korzystają z open bus.
  Ich testy muszą jawnie ustawiać open bus, inaczej są niedeterministyczne.

## 4. Processor port 6510

- `$01` read = `(data & ddr) | (0x17 & ~ddr)`.
- Przełączenie banku działa z opóźnieniem **3 cykli** — to nie uproszczenie, to sprzęt.
- Zapis do ROM jest ignorowany, ale nie może rzucać wyjątkiem.

## 5. Brak alokacji w pętli maszyny

- `Tick()` i wszystko pod nim **nie może alokować**. Zero LINQ, zero `List`, zero stringów.
- Stan trzymaj w polach `struct`/`class`; rejestry przez `ref`, nie przez kopię.
- `IBusObserver` jest opcjonalny (`null` w normalnej pracy) — jedno sprawdzenie, zero alokacji.

## 6. Czytelność niedokładności

- Każde świadome uproszczenie oznacz komentarzem `// uproszczenie:` i wypisz je w README.
- Nie udawaj dokładności: jeśli czegoś nie modelujesz (sprite DMA, TOD w CIA, rewizje VIC),
  napisz to wprost w dokumentacji typu, a nie tylko w głowie.

## 7. Zanim zmienisz tabelę

- Wartości cykli **weryfikuj z dokumentacją i emulatorem referencyjnym (VICE)**, nie z pamięci.
- Po zmianie uruchom testy w `tests/VibeEmulator.Core.Tests/C64/` — muszą przechodzić bez modyfikacji
  oczekiwanych wartości. Jeśli trzeba zmienić asercję, uzasadnij to w opisie zmiany.
- Docelowa walidacja: Tom Harte `65x02`, VICE `testprogs`, `perfect6502`, nestest.
