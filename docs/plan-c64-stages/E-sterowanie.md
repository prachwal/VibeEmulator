# Etap E — sterowanie: JSR/RTS/RTI/JMP (ind.)

Poziom: **A** — ten etap **domyka** 151/151 oficjalnych opkodów NMOS (100%).

## Zakres

| opkod | mnemonic | tryb | uwaga |
|---|---|---|---|
| `20` | JSR | abs | odkłada adres powrotu na stos |
| `60` | RTS | impl | dummy read, inkrementacja `PC` po zdjęciu |
| `40` | RTI | impl | zdejmuje `P`, potem `PC`; bez inkrementacji |
| `6C` | JMP | ind | **błąd strony tylko w NMOS** |

## Nowe elementy

- Fazy dla `JSR` (push adresu powrotu) i `RTS`/`RTI` (pull).
- Tryb `Indirect` dla `JMP` — pierwszy opkod z tym trybem w tabeli.
- Nowa właściwość `CpuVariant`: zachowanie `JMP (ind.)` przy wskaźniku na końcu strony
  (`MemoryWrap`/`PageWrap`). **Różnica wariantu jako dana, nie `if` w fazie.**

## Semantyka

- `JMP (ind.)` NMOS: `$xxFF` → wysokie bajto z `$xx00` (błąd strony). 65C02 czyta poprawnie `$xxFF`/`$(xx+1)00`.
  To pierwszy opkod, gdzie wariant różni się **zachowaniem**, nie tylko tabelą.
- `JSR`: co dokładnie trafia na stos i kiedy `PC` się zmienia — z wektorów.
- `RTI` nie inkrementuje `PC`; `RTS` inkrementuje.

## Kryteria akceptacji

- [x] 4 wpisy; 4 próbki wektorów w obu katalogach.
- [x] `--verify-code`: NMOS `missingInCode` **4 → 0** (poziom A: 151/151).
- [x] Test `JMP ($10FF)`: NMOS czyta `$1000`, 65C02 czyta `$1100`.
- [x] Wektory zielone dla obu wariantów (próbka 8/8 × 2 + pełne 4/4 × 2).

## Pułapki

1. **Błąd strony `JMP (ind.)`** — jeśli wektory 65C02 pokażą inne zachowanie niż NMOS, to jest dowód na potrzebę
   właściwości wariantu. Nie dodawać `if (variant == ...)`.
2. **`RTS` a `RTI`**: różna liczba zdjęć ze stosu i inna inkrementacja `PC`.
3. `JSR` ma 6 cykli; ostatni cykl to odczyt, nie zapis — sprawdzić w wektorach.
4. Po tym etapie `BRK` nadal jest niepełny (etap F), więc `--verify-code` będzie zielone, ale funkcjonalnie
   poziom A **nie jest** zamknięty.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/4 | `missingInCode` NMOS = 4 | etap otwarty |
| 2026-10-21 | cały etap | 4/4 | `--verify-code` NMOS `missingInCode` 4 → 0, próbka 8/8 × 2, pełne 4/4 × 2 (80 000, 0 pominięć) | JSR: low/dummy/pushy/pushy/fetch; RTS: dummy/dummy/PCL/PCH/fetch+1; RTI bez inkrementacji; JMP-ind: błąd strony NMOS, fixup 65C02 (właściwość `JmpWrap`, nie `if`) |
