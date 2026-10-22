---
applyTo: "src/Core/C64/**/*.cs,src/Mos6502/CycleExact/**/*.cs"
description: "Reguły wierności cyklowej emulacji C64 i rdzenia StateMachineCpuCore: VIC-II jako master, cykle z dostępów, dummy ready zależne od wariantu, open bus, przerwania, zakaz alokacji w pętli."
---

# Emulacja C64 — reguły wierności cyklowej

Cel: **PAL 6569R5, 6510, dokładność cyklowa** (poziom 3 z `docs/plan-c64-cycle-exact.md`).
Każda zmiana w `src/Core/C64` i w `src/Mos6502/CycleExact/` oceniana jest przede wszystkim pod kątem zgodności cyklowej,
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
- Dummy read i zapis starej wartości w RMW są obowiązkowe — urządzenia je widzą. Reguły poniżej
  dotyczą **NMOS 6502/6510**; wariant 65C02 różni się (patrz niżej) i to wariant (`CpuVariant`)
  decyduje, nie `if` w fazach:
  - `abs,X` / `abs,Y` / `(zp),Y`: dummy read spod adresu **bez** korekty strony (NMOS); 65C02 czyta
    zamiast tego ostatni bajt operandu (`PC-1`),
  - `INC`/`DEC`/`ASL`/`LSR`/`ROL`/`ROR`: `read → write(stara) → write(nowa)` (NMOS);
    65C02: `read → read → write(nowa)`,
  - instrukcje jednobajtowe (implied): w 2. cyklu dummy read spod `PC` — w obu wariantach,
  - `zp,X` / `zp,Y` / `(zp,X)`: dummy read spod bazy zerostronicowej przed dodaniem indeksu — w obu wariantach.
- Ghost byte (NMOS): przy przekroczeniu strony `abs,X` czyta spod `(base & 0xFF00) | ((base + X) & 0xFF)`.
- Zapisy zawsze płacą dummy read, odczyty tylko przy przekroczeniu strony.
- Źródłem prawdy dla tych zachowań są wektory SingleStepTests/65x02 (`tests/VibeEmulator.Mos6502.Tests/Vectors`),
  nie pamięć ani dokumentacja z drugiej ręki; zmianę zachowania rdzenia sprawdzaj pełnymi wektorami.

## 3. Open bus

- Odczyt spod niezmapowanego adresu zwraca **ostatnią wartość na magistrali**, nie zero.
- `C64Bus.OpenBus` aktualizuj przy **każdym** odczycie, przed zwróceniem wartości.
- Niestabilne opkody (`ANE`, `LXA`, `SHA`, `SHX`, `SHY`, `TAS`) korzystają z open bus.
  Ich testy muszą jawnie ustawiać open bus, inaczej są niedeterministyczne.

## 4. Processor port 6510

- `$01` read = `(data & ddr) | (0x17 & ~ddr)`.
- Przełączenie banku działa z opóźnieniem **3 cykli** — to nie uproszczenie, to sprzęt.
- Zapis do ROM jest ignorowany, ale nie może rzucać wyjątkiem.
- **Niezweryfikowane:** wzór odczytu `$01` i opóźnienie 3 cykli pochodzą z dokumentacji repo; źródeł o porcie 6510 w `docs/cpu/`
  nie przeczytano. Port nie jest jeszcze podpięty pod adresy `$00`/`$01` w `C64Bus` (faza 4 planu).

## 5. Brak alokacji w pętli maszyny

- `Tick()` i wszystko pod nim **nie może alokować**. Zero LINQ, zero `List`, zero stringów.
- Stan trzymaj w polach `struct`/`class`; rejestry przez `ref`, nie przez kopię.
- `IBusObserver` jest opcjonalny (`null` w normalnej pracy) — jedno sprawdzenie, zero alokacji.

## 6. Czytelność niedokładności

- Każde świadome uproszczenie oznacz komentarzem `// uproszczenie:` i wypisz je w README.
- Nie udawaj dokładności: jeśli czegoś nie modelujesz (sprite DMA, TOD w CIA, rewizje VIC),
  napisz to wprost w dokumentacji typu, a nie tylko w głowie.

## 7. Zanim zmienisz tabelę

- Wartości cykli i dostępów **weryfikuj ze źródłem**, nie z pamięci: dla CPU wektory SingleStepTests i `docs/cpu/`
  (opkody: `docs/cpu/opcodes.md` i tabele CSV), dla VIC-II dokumentacja i VICE.
- Po zmianie uruchom testy w `tests/VibeEmulator.Mos6502.Tests` (rdzeń, wektory) i `tests/VibeEmulator.Core.Tests/C64/`
  (maszyna, magistrala) — muszą przechodzić bez modyfikacji oczekiwanych wartości. Jeśli trzeba zmienić asercję,
  uzasadnij to w opisie zmiany.
- Docelowa walidacja i szkice harnessów (Dormann, nestest, Blargg, SingleStepTests): `docs/cpu/tests.md`.

## 8. Przerwania i reset (etap 2d planu)

Rdzeń ma sekwencje BRK/IRQ/NMI (siedem cykli, `docs/cpu/interrupts-and-reset.md`): `B = 1` w odłożonym `P`
tylko dla `BRK`/`PHP`; IRQ próbkowane na granicy instrukcji, NMI na zboczu; `CLI`/`SEI`/`PLP` opóźniają przerwanie
o jedną instrukcję, `RTI` nie. Piny to `IInterruptLines` (`Cpu.Abstractions`) — poziomy ustawia `C64Machine.Tick()`
przed `Tick` CPU; 65C02 czyści `D` po odłożeniu `P` (dana wariantu `InterruptClearsDecimal`). **Brakuje** sekwencji
RESET (odczyty zamiast zapisów, `S −= 3`) i hijackingu wektorów (NMI w trakcie BRK). Rejestry flag (`B` nie istnieje
fizycznie w `P`) opisuje `docs/cpu/registers-and-flags.md`.
