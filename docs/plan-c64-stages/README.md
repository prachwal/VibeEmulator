# Etapy dojścia do 100% pokrycia opkodów

Ten katalog rozwija sekcję 9 planu ([../plan-c64-cycle-exact.md](../plan-c64-cycle-exact.md)) na **osobny plik
na etap**: od `A` do `K`. Plan trzyma stan i kolejność; tutaj jest zakres, akceptacja i dziennik postępu.

## Poziomy docelowe

| poziom | zakres | etapy, które go zamykają |
|---|---|---|
| **A. 6502 funkcjonalny** | 151 oficjalnych opkodów NMOS | A–E (opkody), F (przerwania), G (tryb dziesiętny) |
| **B. 6502 kompletny** | 256/256 bajtów NMOS | J (nieoficjalne), K (KIL) |
| **C. 65C02 kompletny** | 256/256 bajtów 65C02 | H (nowe), I (NOP-y) |

## Pliki etapów

| etap | stan | plik | zakres | opkody |
|---|---|---|---|---|
| A | **zamknięty** (2026-10-21, pełne wektory 54/54 × 2) | [A-logika-i-porownania.md](A-logika-i-porownania.md) | logika, porównania, ADC/SBC w pozostałych trybach | 54 |
| B | **zamknięty** (2026-10-21, pełne wektory 24/24 × 2) | [B-przesuniecia-i-rotacje.md](B-przesuniecia-i-rotacje.md) | ASL/LSR/ROL/ROR, INC/DEC `zp,X`/`abs,X` | 24 |
| C | **zamknięty** (2026-10-21, pełne wektory 8/8 × 2) | [C-rozgalezienia.md](C-rozgalezienia.md) | 8 rozgałęzień warunkowych (nowy tryb `Relative`) | 8 |
| D | **zamknięty** (2026-10-21, pełne wektory 4/4 × 2) | [D-stos.md](D-stos.md) | PHA/PHP/PLA/PLP | 4 |
| E | **zamknięty** (2026-10-21, pełne wektory 4/4 × 2) | [E-sterowanie.md](E-sterowanie.md) | JSR/RTS/RTI/JMP (ind.) — domyka poziom A | 4 |
| F | **zamknięty** (2026-10-21, BRK pełne wektory 2 × 10 000; H4 Dormann nie uruchomiony — patrz dziennik) | [F-przerwania.md](F-przerwania.md) | pełny BRK, IRQ, NMI, RTI | 0 |
| G | **zamknięty** (2026-10-21, pełne wektory 320 000 bez pominięć; H5 Clark nie uruchomiony — patrz dziennik) | [G-tryb-dziesietny.md](G-tryb-dziesietny.md) | ADC/SBC z `D=1` | 0 |
| H | **zamknięty** (2026-10-21, pełne wektory 53 × 10 000; H3 Dormann nie uruchomiony — patrz dziennik) | [H-nowe-opkody-65c02.md](H-nowe-opkody-65c02.md) | RMB/SMB/BBR/BBS, BRA, PHX…, TSB/TRB, `(zp)`, WAI/STP | 55 |
| I | **zamknięty** (2026-10-21, pełne wektory 44 × 10 000; 65C02 256/256) | [I-nop-65c02.md](I-nop-65c02.md) | niezdefiniowane NOP-y 65C02 | 44 |
| J | **zamknięty** (2026-10-21, pełne wektory 93 × 10 000 = 930 000; NMOS 244/256, `nestest` nieuruchomiony) | [J-nieoficjalne-nmos.md](J-nieoficjalne-nmos.md) | nieoficjalne opkody NMOS | 93 |
| K | **zamknięty** (2026-10-22, pełne wektory 12 × 10 000 = 120 000; NMOS 256/256 = poziom B) | [K-kil.md](K-kil.md) | KIL/JAM | 12 |

## Zasada: plan mówi „co", wektory mówią „jak"

Zakres, kolejność i akceptacja są tutaj. **Kolejność faz, dummy ready, adresy i wartości na magistrali** bierz
z wektorów SingleStepTests (`tests/VibeEmulator.Mos6502.Tests/Vectors/README.md`) i `docs/cpu/` — nie z pamięci
i nie z tego planu. Rozbieżność źródeł opisuj wprost (jak `$5C` w `docs/cpu/opcodes.md`).

## Obowiązek oznaczania postępu

To nie jest sugestia — bez wpisu etap jest **nierozliczony**, nawet jeśli kod działa.

**Kiedy wpisywać:** po każdej ukończonej grupie (jedna rodzina mnemoniczna albo ~10 opkodów), nie dopiero na końcu
etapu; oraz zawsze po zdjęciu `Skip` albo zmianie liczby opkodów.

**Co wpisać** (jedna linia w tabeli „Dziennik postępu” w pliku etapu):

| kolumna | zawartość |
|---|---|
| data | `RRRR-MM-DD` |
| grupa | np. `AND/ORA/EOR`, `CMP/CPX/CPY`, `BIT`, `cały etap` |
| postęp | `dodane/wszystkie` w obrębie etapu, np. `54/54` |
| weryfikacja | wynik `--verify-code` (`missingInCode` dla danego zestawu) i liczba testów wektorowych, np. `wektory 54/54 po 100 testów, testy 361/361` |
| uwagi | odchylenia, `Skip`, brak pełnych wektorów, decyzje |

**Dodatkowo, po każdym etapie** zaktualizuj:

1. tabelę „Stan realizacji” w [../plan-c64-cycle-exact.md](../plan-c64-cycle-exact.md) — wiersz fazy 1 i status etapu,
2. `tests/VibeEmulator.Mos6502.Tests/Vectors/README.md` — lista plików, opkody, rozmiar katalogu,
3. `docs/cpu/README.md` tylko wtedy, gdy zmieniła się wiedza o sprzęcie (liczby opkodów należą do planu).

**Czym mierzyć postęp:** jedną komendą, nie „na oko”:

```bash
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code --include-missing
```

`counts.missingInCode` maleje po każdym etapie; dla poziomu A: 94 → 40 → 16 → 8 → 4 → 0 (A → E); dla poziomu B: 105 → 12 (J) → 0 (K).
Dla 65C02: 94 → 0 po A–E, potem 55 → 0 (H) i 44 → 0 (I).

## Wspólny rytuał zakończenia etapu

1. Wpisy w pliku katalogu: `src/Mos6502/OpcodeCatalog.Nmos.cs` (baza) / `OpcodeCatalog.Nmos.<Etap>.cs`
   (pole z `[Opcode]` + pozycja w tablicy katalogu) lub `OpcodeCatalog.Cmos.cs`.
2. Próbka wektorów (pierwsze 100 testów, zwarty JSON) + wpis w `Vectors/README.md`.
3. Opkod w liście `SingleStepVectorTests` dla **każdego** wariantu, w którym istnieje.
4. `--verify-code` → exit 0.
5. Pełne wektory (10 000 na opkod) **poza repo**; jeśli ich nie uruchomiono, napisz to wprost.
6. Wpis w dzienniku postępu etapu i w „Stanie realizacji”.
7. Definicja ukończenia z `.github/copilot-instructions.md` (build Debug/Release, testy, format).
