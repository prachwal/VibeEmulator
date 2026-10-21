# Etap A — logika, porównania, ADC/SBC w pozostałych trybach

Poziom: **A** (6502 funkcjonalny) · Opkody: **54** · Nowe tryby: 0 · Nowe fazy: 0 · Nowe akcje: 7

## Cel

Dodać wszystkie oficjalne opkody, których jedynym brakującym elementem jest **akcja** — tryby adresowania już
istnieją i są zweryfikowane wektorami. Po etapie: **111/151** oficjalnych opkodów NMOS (73,5%).

## Zakres

| grupa | opkody | tryby | akcja |
|---|---|---|---|
| ADC | `61 65 6D 71 75 79 7D` | izx, zp, abs, izy, zpx, aby, abx | `Adc` (istnieje) |
| SBC | `E1 E5 ED F1 F5 F9 FD` | jw. | `Sbc` (istnieje) |
| AND | `21 25 29 2D 31 35 39 3D` | izx, zp, imm, abs, izy, zpx, aby, abx | **`And`** |
| ORA | `01 05 09 0D 11 15 19 1D` | jw. | **`Ora`** |
| EOR | `41 45 49 4D 51 55 59 5D` | jw. | **`Eor`** |
| CMP | `C1 C5 C9 CD D1 D5 D9 DD` | jw. | **`Cmp`** |
| CPX | `E0 E4 EC` | imm, zp, abs | **`Cpx`** |
| CPY | `C0 C4 CC` | imm, zp, abs | **`Cpy`** |
| BIT | `24 2C` | zp, abs | **`Bit`** |

`ADC`/`SBC` w trybie `imm` już są (`69`, `E9`); ten etap dokłada pozostałe 7 trybów każdemu.

## Nowe elementy

- `CpuAction.And`, `Ora`, `Eor`, `Cmp`, `Cpx`, `Cpy`, `Bit`.
- Obsługa w `ApplyReadToRegister` (`StateMachineCpuCore`): wszystkie są `OpKind.Read`, więc nie dotykają faz.
- Brak zmian w `Addressing`, `CpuVariant` i `Disassembler` (deasembler czyta tabelę).
- Brak zmian w formacie cykli — `StartIndexedAccess` już liczy dummy read i page-cross dla odczytu.

## Semantyka (do potwierdzenia wektorami, nie z pamięci)

- `AND`/`ORA`/`EOR`: wynik do `A`, `N`/`Z` z wyniku, `C` i `V` bez zmian.
- `CMP`/`CPX`/`CPY`: odejmowanie bez zapisu (`A−M`, `X−M`, `Y−M`), `C`/`Z`/`N` z różnicy, `V` bez zmian.
- `BIT`: `Z` z `A & M`, `N` i `V` **z operandu** (bity 7 i 6), `C` bez zmian; `A` i pamięć bez zmian.

Wartości i przypadki brzegowe bierz z wektorów `6502/v1/<hex>.json`; przy rozbieżności pierwszeństwo ma wektor.

## Kryteria akceptacji

- [x] 54 wpisy w `OpcodeCatalog` (jedno źródło: adnotacja + tablica katalogu).
- [x] Próbki wektorów: 54 pliki w `Vectors/nmos6502/` i 54 w `Vectors/wdc65c02/`, po 100 testów.
- [x] `SingleStepVectorTests`: opkody dopisane do `NmosOpcodes` i `CmosOpcodes`.
- [x] `--verify-code`: `mismatched: 0`, `missingInCode` dla NMOS **94 → 40**.
- [x] Testy wektorowe zielone dla 54 opkodów w obu wariantach.
- [x] `DisassemblerTests` (iteruje po całej tabeli) bez zmian oczekiwań.
- [ ] Pełne wektory (10 000 na opkod) — **nieuruchomione** (zapisane wprost, nie obiecane jako zweryfikowane).

## Pułapki

1. **Page cross tylko przy odczycie.** `ADC/SBC/CMP` w `abx`/`aby`/`izy` płacą +1 cykl wyłącznie przy przekroczeniu
   strony; `STA` (zapis) płaci zawsze. Kod już to rozdziela po `OpKind`, ale trzeba to potwierdzić wektorami dla
   nowych akcji — brak dummy read tam, gdzie go nie ma.
2. **`CMP` nie ustawia `V`.** Częsty błąd z pamięci; wektory pokażą.
3. **`BIT` w 65C02 z `imm`** (`89`) nie istnieje w NMOS — nie dopisywać go w tym etapie (etap H).
4. **`Cpx`/`Cpy` nie mają trybów indeksowanych** — jeśli pojawi się taki wpis, to błąd danych.
5. **Tryb dziesiętny.** Wektory dla `ADC`/`SBC` z `D=1` są nadal pomijane (etap G). Nie „naprawiać” ich tutaj.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | ADC/SBC (7 trybów) + AND/ORA/EOR + CMP/CPX/CPY + BIT | 54/54 | `--verify-code`: `mismatched: 0`, `missingInCode` NMOS = **40**; testy Mos6502 **354/354** (108 nowych przypadków wektorowych) | wektory: 54 pliki × 2 zestawy po 100 testów; wyjątek dziesiętny rozszerzony z `imm` na wszystkie tryby ADC/SBC (etap G) |

## Wynik etapu

- Opkody: **54/54** (NMOS 111/151 = 73,5% oficjalnych).
- Wektory: **54/54** w obu zestawach, po 100 testów; pełne wektory (10 000 na opkod):
  **nieuruchomione** — próbka w repo przechodzi, pełny przebieg wymaga runnera poza repo.
- `--verify-code`: `mismatched: 0`, `missingInCode` NMOS 94 → **40**, c02 bez zmian względem dziedziczenia.
- Struktura: opkody etapu w `src/Mos6502/OpcodeCatalog.Nmos.Logic.cs`; tablica `Opcodes` jest właściwością
  z leniwą inicjalizacją, bo obejmuje pola z dwóch plików `partial`.
