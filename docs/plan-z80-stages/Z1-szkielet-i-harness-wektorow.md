# Etap Z1 — szkielet rdzenia i harness wektorów

Poziom: **fundament** · Pliki wektorów: **1** (`00`, NOP) · Nowe projekty: 2 · Nowe interfejsy: 1

Zanim powstanie pierwsza instrukcja, trzeba mieć: projekt rdzenia, stan rejestrów, magistralę portów i harness
porównujący `cycles` T-state po T-state. Etap kończy się jednym opkodem (`NOP`), ale **rozstrzyga decyzje D1–D7**
z [README.md](README.md), od których zależy reszta.

## Zakres

- Projekt `src/Z80` (`VibeEmulator.Z80`), zależny od `Memory` i `Cpu.Abstractions`; **nie** od `Mos6502` ani `Core`.
- `Z80State` (struct) z polami z wektorów: `pc sp a b c d e f h l i r ix iy`, cienie `af_ bc_ de_ hl_`, `wz`,
  `im`, `iff1`, `iff2`, `ei`, `p`, `q`. Nazwy pól wektora → pola stanu w jednym miejscu (loader).
- `Z80Core : ICpuCore<Z80State>`; `Tick()` = **jeden T-state**.
- `IPortBus` w `src/Memory` (`byte In(ushort port)`, `void Out(ushort port, byte value)`) wg D3.
- Rejestr pinów ostatniego T-state (adres, dana, piny) odczytywalny po `Tick()` — wg D1.
- Projekt testów `tests/VibeEmulator.Z80.Tests` (xunit jak w pozostałych), dodany do `VibeEmulator.slnx`.
- Loader wektorów: nazwy plików **ze spacjami** (`cb 00.json`, `dd cb __ 06.json`), pole `ports` opcjonalne,
  `null` w `cycles` dla adresu i dany.
- Pliki: `tests/VibeEmulator.Z80.Tests/Vectors/` — `LICENSE-SingleStepTests.txt`, `README.md` (commit źródła,
  liczba testów na plik wg D4, skrypt/komenda przycięcia), pliki `.json` wg `Content` + `CopyToOutputDirectory`.
- Opkod: tylko `00` (NOP, 4 T-state).

## Nowe elementy

- `src/Z80/VibeEmulator.Z80.csproj`, `Z80State.cs`, `Z80Core.cs` (+ pliki `partial` wg faz).
- `src/Memory/IPortBus.cs` (+ `FlatPorts` albo odpowiednik do testów).
- `tests/VibeEmulator.Z80.Tests/Z80Vectors.cs`, `Z80VectorTests.cs`, `Z80VectorManifest.cs`.
- Wpis w `docs/plan-c64-cycle-exact.md` („Elementy spoza planu”) wskazujący na ten katalog.
- Sekcja o wektorach Z80 w `.github/instructions/single-step-vectors.instructions.md` (obecny plik opisuje tylko
  65x02) — **tylko na prośbę użytkownika**, bo to konfiguracja Copilota.

## Do ustalenia przed pisaniem kodu

1. Przeczytać README repo `z80` w całości (opcje generatora: CMOS, uproszczone T-state, odświeżanie, `null` przy WAIT).
2. Sprawdzić znaczenie `p`, `q`, `ei`, `wz` na kilku testach (nie z opisu) i zapisać w komentarzu loadera.
3. Wywnioskować kolejność znaków w pinach (`r w m i`) na próbkach z zapisem (`LD (HL),r`) i I/O (`OUT`).
4. Sprawdzić, co robi `ICpuCore.Pending` w `StateMachineCpuCore`, i zdecydować semantykę dla Z80 (R2).

## Kryteria akceptacji

- [x] D1–D7 rozstrzygnięte i wpisane w dzienniku. (D1 pełny zapis cycles; D2 algorytmiczne fazy; D3 IPortBus w src/Memory; D4 20 testów/plik; D5 Tick=1 T-state z Pending; D6 Stall=WAIT; D7 tylko NMOS.)
- [x] `dotnet build VibeEmulator.slnx` bez ostrzeżeń (`TreatWarningsAsErrors`), `dotnet format --verify-no-changes` czysto. (Zweryfikowano 2026-10-22.)
- [x] Wektory `00`: próbka w repo przechodzi (stan końcowy, `ram`, pełne `cycles`). (20 testów z `00.json`.)
- [x] Test jednostkowy loadera: plik z `ports`, plik z `null` w `cycles`, nazwa ze spacjami. (Loader_HandlesNullAndPorts, Loader_HandlesFileNameWithSpaces.)
- [x] Pełne wektory `00` (1000 testów) poza repo: wynik wpisany albo „nie uruchomiono”. (Nie uruchomiono.)
- [x] `Tick()` bez alokacji (reguła z `CLAUDE.md`). (Weryfikacja inspekcją: brak new/LINQ/stringów w Tick; LastPins to literał internowany.)

## Pułapki

1. **T-state ≠ cykl 6502.** Nie przenoś `Cycles`/`Pending` z rdzenia 6502 bez sprawdzenia semantyki.
2. **Pola wektora są płaskie** (`af_`, nie `af'`; brak pary `af`) — mapowanie 8-bitowych `a`/`f` na pary zrób raz, w loaderze.
3. **`ram` zawiera tylko dotknięte adresy.** Pozostała pamięć ma być zerowana, nie losowa, żeby test był powtarzalny.
4. **Nazwy ze spacjami** psują ścieżki w skryptach shella bez cudzysłowów — przy przycinaniu użyj tablicy/`find -print0`.
5. **Nie edytuj treści wektorów** (zasada z `single-step-vectors.instructions.md`): przycinanie = wybór pierwszych N
   testów, bez zmiany formatowania.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | szkielet + NOP | 1/1 plików wektorów (20/1000 testów) | build Release 0 ostrzeżeń, format czysto, 1/1 test Z80 | D1: pełny zapis cycles; D5: Tick = 1 T-state; D7: tylko NMOS. Decyzje D2/D3/D4/D6 do udokumentowania. IPortBus.Input/Output (nie In/Out — CA1716). |
