# Etap P — wariant NES (2A03)

Poziom: **nowy wariant** · Opkody: 0 nowych · Nowe właściwości wariantu: 1–2

Osobny `CpuVariant` dla **Ricoh 2A03** (NES): ta sama tabela co NMOS 6502 (łącznie z 93 nieoficjalnymi
i 12 KIL), ale **bez trybu dziesiętnego**. Dziś różnicę da się wyrazić tylko flagą wariantu — rdzeń nie ma
pojęcia „brak D”.

## Zakres

- `CpuVariants.Nes2A03` (`src/Mos6502/OpcodeCatalog/CpuVariant.cs`): nazwa, tabela = `OpcodeCatalog.Nmos.Opcodes`,
  `RmwStyle.DoubleWrite`, `IndexedDummyRead.GhostByte`, **`decimalMode = false`**.
- Nowa właściwość `CpuVariant.HasDecimalMode` (domyślnie `true`), czytana raz do pola `readonly` w rdzeniu.
  W `Adc`/`Sbc` gałąź dziesiętna jest wybierana tylko gdy `HasDecimalMode && D = 1`; przy `HasDecimalMode ==
  false`
  instrukcje `CLD`/`SED` ustawiają flagę (tak robi 2A03 — flaga istnieje, tylko nic nie zmienia), a `ADC`/`SBC`
  liczą binarnie.
- Wektory: zestaw `nes6502` z SingleStepTests/65x02 (`6502/v1` → katalog `nes6502/v1`, ten sam commit
  `2f6980a2d95757486c7bee24355c360e40e2a224`) — próbki 100 testów na opkod dla wszystkich 256 bajtów.
- Testy: `SingleStepVectorTests.Vector_Nes2A03_MatchesReference` + lista `Nes2A03Opcodes` (256 pozycji).
- `--verify-code` **nie** dotyczy 2A03 (CSV nie ma osobnego zestawu) — zapisać to wprost w komentarzu narzędzia.

## Nowe elementy

- `CpuVariant.HasDecimalMode` + `CpuVariants.Nes2A03`.
- 256 plików próbek w `tests/VibeEmulator.Mos6502.Tests/Vectors/nes6502/`.
- Test jednostkowy: `ADC` z `D = 1` na 2A03 daje wynik **binarny**, a na NMOS dziesiętny (ten sam stan
  początkowy, dwa warianty).

## Kryteria akceptacji

- [x] `CpuVariants.Nes2A03` istnieje, ma 256 wpisów, a `Describe(0xEA)` zwraca `NOP`.
- [x] `ADC #$01` z `A = $09`, `D = 1`: NMOS → `$10`, 2A03 → `$0A` (test porównujący oba warianty).
- [x] Wektory `nes6502`: 256/256 opkodów, próbki w repo (256 plików), pełne wektory **2 560 000 / 2 560 000**,
      0 pominięć.
- [x] Definicja ukończenia bez zmian.

## Pułapki

1. **2A03 nie ma trybu dziesiętnego, ale ma flagę D** — `SED`/`CLD` nie mogą być ignorowane, tylko flaga nie
   wpływa na `ADC`/`SBC`. Nie „naprawiać” tego przez wyzerowanie flagi.
2. Zestaw `nes6502` w źródle **nie ma** plików dla opkodów, które są inne na NES — jeśli któryś plik jest
   pusty albo go brak, zapisać to wprost (jak `WAI`/`STP` dla 65C02), nie zgadywać.
3. Tabela 2A03 = tabela NMOS: **nie kopiować** wpisów, użyć tej samej tablicy katalogu (jedno źródło prawdy).
4. `--verify-code` nie obejmie 2A03 — brak wierszy CSV; nie dopisywać zestawu do CSV bez źródła.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | wariant + wektory | 256/256 | próbki 256/256; pełne wektory **2 560 000 / 2 560 000**, 0 pominięć; `VibeEmulator.Mos6502.Tests` 1002/1002 | `CpuVariant.HasDecimalMode` (domyślnie `true`), pole `_decimalMode` w rdzeniu; ADC/SBC/ARR i dodatkowy cykl dziesiętny 65C02 respektują flagę; `HarnessRunner` przyjmuje `--variant 2a03` |
