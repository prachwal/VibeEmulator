# Etap J — nieoficjalne opkody NMOS

Poziom: **B** · Opkody: **93** · Nowe tryby: 0 · Nowe fazy: 0 · Nowe akcje: ~20

Po etapie: NMOS **242/256** bajtów (zostaje 12 KIL — etap K). Bez tego etapu nie ma zgodności z `nestest`.

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

- [ ] 93 wpisy w katalogu (klasa `Nmos`); 93 próbki w `Vectors/nmos6502/`.
- [ ] `--verify-code`: NMOS `missingInCode` spada o 93 (zostaje 12 KIL).
- [ ] `nestest.log` (harness H2) przechodzi dla 10 rodzin użytych w teście.
- [ ] Wektory zielone; opkody niestabilne mają wpis w `uwagi` z podanym źródłem decyzji.

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
