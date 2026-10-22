# Etap J — nieoficjalne opkody NMOS

Poziom: **B** · Opkody: **93** · Nowe tryby: 0 · Nowe fazy: 0 · Nowe akcje: ~20

Po etapie: NMOS **244/256** bajtów (zostaje 12 KIL — etap K). Bez tego etapu nie ma zgodności z `nestest`.

## Zakres (20 rodzin)

| rodzina | liczba | typ | uwaga |
|---|---|---|---|
| SLO, RLA, SRE, RRA | 28 | RMW + ALU | 4 akcje łączące; tryby jak w oryginałach |
| DCP, ISC | 14 | RMW + porównanie/arytmetyka | jw. |
| LAX, SAX | 11 | dwa rejestry | `LAX #imm` (`AB`) niestabilny |
| ANC, ALR, ARR, AXS, SBC `$EB` | 6 | arytmetyka z flagami | `ARR` ma nietypowe `V`/`C` |
| NOP (nieoficjalne) | 27 | 1–3 bajtów | różne tryby, w tym `zpx`, `abx` |
| AHX, SHX, SHY, TAS, LAS, XAA | 7 | **niestabilne** | wynik zależy od układu |

Konkretne opkody: `tools/OpcodeInfo --set nmos --class unofficial`.

## Semantyka

- Rodziny łączą istniejące akcje (np. `SLO` = `ASL` + `ORA`, `RRA` = `ROR` + `ADC`, `DCP` = `DEC` + `CMP`).
  Implementacja przez złożenie akcji, nie przez kopiowanie kodu.
- **Niestabilne** (`LAX #imm`, `XAA`, `SBC $EB`, `AHX`, `SHX`, `SHY`, `TAS`, `LAS`) — wektory przyjmują **jeden**
  wybór; wynik bierz z wektorów i oznacz źródło. `docs/cpu/opcodes.md` odsyła do „No More Secrets” (nieotwarte).

## Kryteria akceptacji

- [x] 93 wpisy w katalogu (klasa `Nmos`, plik `OpcodeCatalog.Nmos.Unofficial.cs`); 93 próbki w `Vectors/nmos6502/`.
- [x] `--verify-code`: NMOS `missingInCode` spadło o 93 — z 105 do **12** (zostają tylko KIL).
- [ ] `nestest.log` (harness H2) — **nieuruchomiony**; brak wpisu potwierdzającego.
- [x] Wektory zielone; opkody niestabilne mają wpis w `uwagi` z podanym źródłem decyzji.

## Pułapki

1. **RMW indeksowane** w rodzinach łączących — dwa zapisy (NMOS) i dummy read; bierz z wektorów.
2. **`LAX`** zapisuje do `A` i `X`; `SAX` zapisuje `A & X` do pamięci **bez** zmiany flag.
3. **`ANC`/`ALR`/`ARR`/`AXS`** ustawiają `C` inaczej niż „naturalne” — wektory rozstrzygają.
4. **Opkody NOP nieoficjalne** mają inne tryby niż oficjalne NOP-y; długość i cykle z CSV.
5. Po tym etapie `02 12 22 …` nadal są nieznane (KIL) — nie wpisywać ich tutaj.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/93 | `missingInCode` NMOS = 93 (nieoficjalne) | etap otwarty |
| 2026-10-21 | katalog + rdzeń | 93/93 | `--verify-code`: `consistent: true`, `mismatched: 0`, `missingInCode: 12` (KIL) | `OpcodeCatalog.Nmos.Unofficial.cs`; akcje `Slo…Xaa` + `NopDummy`; `WriteUnstable()` dla SHX/SHY/AHX/TAS |
| 2026-10-21 | wektory | 93/93 | próbki: 498/498 testów (244 NMOS + 254 65C02); pełne wektory: **930 000 / 930 000**, 0 pominięć | `CpuVariants.Cmos65C02` scala bazę NMOS z nadpisaniem przez dodatki 65C02 (te same bajty mają inne znaczenie) |
| 2026-10-21 | testy jednostkowe | 93/93 | `VibeEmulator.Mos6502.Tests`: 698/698 | `StateMachineCpuCoreTests.Unofficial.cs` |

### Semantyka wyprowadzona z wektorów (nie z pamięci)

- `SLO`/`RLA`/`SRE`/`RRA`/`DCP`/`ISC`: modyfikacja pamięci **następuje przed** operacją ALU na akumulatorze,
  a ALU dostaje **nową** wartość. `RRA` używa `C` **z rotacji** (nie sprzed niej) w `ADC`; `ISC` wykonuje `SBC`
  (także w trybie dziesiętnym).
- `ARR` w trybie dziesiętnym: `and = A & imm`, `rotated = ROR(and, C)`, korekta obu cyfr niezależnie
  (niska: gdy `(and & 0x0F) >= 5`, wysoka: gdy `(and & 0xF0) >= 0x50`), `C = and[6] ^ res[5]`, `N = C wejściowe`,
  `Z` z `rotated`, `V = and[6] ^ and[7]`. W trybie binarnym `C = res[6]`, `V = res[6] ^ res[5]`.
  Reguła uzgodniona na 5017 przypadkach `D=1` i 1504 `D=0`.
- `LAX #imm` (`AB`): `A = X = imm & (0xEE | (A & 0x11))` — stabilna wersja „magicznej maski”; naiwne `A & imm`
  daje ~900 niezgodności na 10 000.
- `XAA` (`8B`): `A = (A | 0xEE) & X & imm`.
- `LAS`: `A = X = SP = M & SP` (wykorzystuje ostatni odczyt, nie bajt duchowy).
- `SHX`/`SHY`/`AHX`/`TAS`: `value = rejestr & (high+1)`; **tylko przy przekroczeniu strony** wartość ta staje się
  bajtem wysokim adresu zapisu; `TAS` dodatkowo `SP = A & X`.
- Nieoficjalne `NOP`-y NMOS **czytają adres efektywny** (np. `0C` czyta `$3C35`), inaczej niż `NOP abs` 65C02
  (czyta `PC-1`) — stąd osobna akcja `CpuAction.NopDummy`.
