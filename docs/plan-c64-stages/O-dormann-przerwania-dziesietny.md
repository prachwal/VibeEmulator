# Etap O — Dormann: test przerwań i test dziesiętny

Poziom: **przerwania + tryb dziesiętny** · Opkody: 0 · Nowe fazy: 0 (tylko naprawy)

Dwa pozostałe testy Dormanna. Oba **nie mają gotowych binariów** w repozytorium źródłowym — trzeba je
zasemblować. To pierwszy etap, w którym potrzebny jest asembler, więc sposób budowania jest częścią wyniku.

## Zakres

### O.1 Test przerwań (`6502_interrupt_test.a65`)

- Sprawdza IRQ, NMI, `RTI`, opóźnienia `CLI`/`SEI`/`PLP`.
- Wymaga **rejestru zwrotnego** — zapisy do `$BFFC` sterują liniami IRQ (bit 0) i NMI (bit 1); harness musi je
  odczytywać i podawać na `IInterruptLines` rdzenia.
- Sukces: pętla `jmp *` pod adresem z listingu (do ustalenia po pierwszym przebiegu i zapisania w tym pliku).
- Asemblacja: `as65` z `as65_142.zip` (Windows/DOS) albo port. **Decyzja do sprawdzenia w etapie:** jeśli
  `as65` nie działa, przepisać tylko potrzebne dyrektywy do składni `ca65` (jak zrobił to `amb5l` dla trzech
  innych testów) i trzymać **patch poza repo** — licencja GPL-3.0.

### O.2 Test dziesiętny (`6502_decimal_test.a65`, kod Bruce'a Clarka)

- Sukces: bajt `ERROR` = 0 (adres w listingu).
- Konfiguracja: `cputype` (NMOS/65C02/65816), `vld_bcd`, `chk_*` — ustawiamy wariant NMOS z poprawnymi BCD
  i pełną kontrolą flag, potem powtarzamy dla 65C02.
- Asemblacja: port `ca65` z `amb5l/6502_65C02_functional_tests` (`ca65/6502_decimal_test.ca65` + `example.cfg`;
  `ca65`/`ld65` są w systemie).
- Wymaga 17 bajtów RAM; ok. minuty przy 1 MHz — limit cykli wysoki.

## Nowe elementy

- Rozszerzenie `CpuHarness` (etap L) o dwa tryby:
  - `--watch-register 0xBFFC` — zapisy do adresu sterują `IrqLevel`/`NmiLevel` (bity 0 i 1) w następnym cyklu;
  - `--report-byte 0x0000 --report-address <adres>` — po pętli `PC` odczyt bajtu wyniku i porównanie z 0.
- Skrypty asemblacji **poza repo** (`/tmp`), z zapisanym w `docs/cpu/tests.md` poleceniem i wersją źródeł.
- Wpis w `docs/cpu/tests.md` i `interrupts-and-reset.md`/`decimal-mode.md`, jeśli przebieg ujawni rozbieżność
  z dotychczasowym opisem.

## Kryteria akceptacji

- [x] Test przerwań: `outcome = pass` pod `$06F5` (NMOS, 3 016 cykli, 1 050 instrukcji, 24 zapisy rejestru)
      i `$0719` (65C02 z `D_clear = 1`, 3 058 cykli, 1 068 instrukcji).
- [x] Test dziesiętny: `ERROR = 0` dla NMOS (`cputype = 0`, 53 953 828 cykli) i 65C02 (`cputype = 1`,
      56 640 804 cykli).
- [x] Poprawki rdzenia **nie były potrzebne**.
- [x] Definicja ukończenia bez zmian.

### Przyjęta ścieżka asemblacji (fakt, nie plan)

`as65` z repozytorium źródłowego to **32-bitowy ELF** (`ld-linux.so.2`), niedostępny na tej maszynie; `ca65`/`ld65` są. Dlatego powstał `tools/dormann-asm/as65_to_ca65.py` — konwerter **wyłącznie składni**
(org/segmenty, if/else/endif, macro/endm, parametry `\1`, etykiety `NAME\?`, equ/=/dw/db/ds). W repo nie ma
ani jednej linii kodu testów (GPL-3.0). Konwerter zweryfikowano porównaniem wyniku: konwersja z wersji w repo
daje binarnie identyczny obraz (`cmp` = brak różnic).

Adresy sukcesu pochodzą z listingu `ca65 -l`, nie z pamięci; listing podaje offsety segmentu `CODE`, więc
adres bezwzględny to offset + `$0400`.

## Pułapki

1. **Polaryzacja rejestru zwrotnego nie jest zweryfikowana** — czytać komentarze w `6502_interrupt_test.a65`,
   nie zgadywać; wynik zapisać w `docs/cpu/interrupts-and-reset.md`.
2. **Licencja GPL-3.0**: binaria i patche zostają poza repo; w repo tylko polecenie i wynik.
3. Test przerwań wymaga **zapisywalnych wektorów** i zmienialnej flagi `I` — jeśli harness tego nie daje,
   poprawić harness, nie test.
4. Test dziesiętny celowo testuje **niepoprawne BCD** w niektórych konfiguracjach — najpierw tryb
   `vld_bcd = 1`, dopiero potem ewentualne rozszerzenie.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | przerwania (NMOS) | 1/2 | pass, `PC = $06F5`, 3 016 cykli, 1 050 instrukcji, 24 zapisy `$BFFC` | rejestr zwrotny: bit 0 = IRQ, bit 1 = NMI (polaryzacja potwierdzona przebiegiem, `interrupts-and-reset.md`) |
| 2026-10-22 | przerwania (65C02) | 2/2 | pass, `PC = $0719`, 3 058 cykli | przebieg z `D_clear = 1` na wariancie c02 |
| 2026-10-22 | dziesiętny (NMOS) | 1/2 | pass, `ERROR = 0`, `PC = $044B`, 53 953 828 cykli, 17 609 916 instrukcji | port ca65 z `amb5l` (`ca65/6502_decimal_test.ca65`), `cputype = 0` |
| 2026-10-22 | dziesiętny (65C02) | 2/2 | pass, `ERROR = 0`, 56 640 804 cykli | `cputype = 1` |
