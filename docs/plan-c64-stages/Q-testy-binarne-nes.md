# Etap Q — testy binarne NES (nestest, Blargg)

Poziom: **walidacja programowa** · Opkody: 0 · Nowe narzędzie: 1

Ostatni etap serii: uruchomienie **prawdziwych ROM-ów testowych NES** na rdzeniu cyklowym. To pierwszy przypadek,
w którym emulujemy nie pojedynczą instrukcję, lecz **program** z własnymi wektorami, RAM-em i mapowaniem NROM.

## Zakres

### Q.1 `nestest` (Kevin Horton)

- Plik `nestest.nes` (24 592 B, iNES, mapper 0/NROM); log wzorcowy `nestest.log` (8991 linii).
- Uruchomienie: `PC = $C000`, stan początkowy `A = X = Y = 0`, `P = $24`, `SP = $FD`, `CYC = 7`.
- Porównanie po każdej instrukcji: `PC A X Y P SP CYC` z kolejną linią logu (kolumna `PPU` wynika z `CYC`,
  więc jej nie porównujemy).
- Wynik: bajty `$02` i `$03` w RAM = `$00` (sukces); kody błędów w `nestest.txt`.

### Q.2 Blargg `instr_test-v5`

- `all_instrs.nes` (albo `official_only.nes`); protokół `$6000`: `$80` = trwa, `$00`–`$7F` = kod wyniku,
  tekst od `$6004` z terminatorem.
- Wymaga NROM + **RAM pod `$6000`** (8 KB) — bez PPU/APU.

### Q.3 Harness NES

- Nowy tryb w `tools/CpuHarness` (albo osobny `tools/NesHarness`, jeśli wariantów będzie więcej):
  `--ines <plik>` (mapowanie NROM-128/256, mirroring PRG), `--ram-at 0x6000 --ram-size 8192`,
  `--log <nestest.log>` (komparator), `--until-pc`, `--until-status 0x6000`.
- Progi: limit cykli, kod wyjścia 0/2/3 jak w etapie L.

## Nowe elementy

- `tools/NesHarness/` (lub rozszerzenie `tools/CpuHarness`) + testy logiki **na syntetycznym „iNES”** zbudowanym
  w pamięci (nagłówek + 1 × 16 KB PRG) — bez prawdziwych ROM-ów w repo.
- `docs/cpu/tests.md`: wynik `nestest` (linie, cykle, `CYC` ostatniej linii) i `instr_test-v5` (kod wyniku),
  z commitem źródła i datą.

## Kryteria akceptacji

- [x] `nestest`: **8991/8991** linii logu zgodnych (26 553 cykli), a po przebiegu `$02 = $03 = $00`.
- [x] Blargg `rom_singles`: **15 / 16 pass**; jedyna porażka to `03-immediate` z powodu niestabilnego `ATX`
      (`$AB`) — opisane wprost niżej i w `docs/cpu/tests.md`.
- [x] `all_instrs.nes` / `official_only.nes`: **nie uruchomione** — to mapper 1 (MMC1), a harness obsługuje
      wyłącznie NROM; zamiast nich użyto `rom_singles` (mapper 0, ten sam zestaw testów).
- [x] Testy harnessu w repo przechodzą bez sieci i bez ROM-ów (`NesHarnessTests`, 10 testów).
- [x] ROM-y i log wzorcowy **nie trafiają do repo**; w repo tylko polecenie i tabela wyników.
- [x] Definicja ukończenia bez zmian.

### Rozbieżność: `ATX` (`$AB`)

Blargg oczekuje `A = (A | 0xEE) & imm`, rdzeń realizuje wariant z wektorów SingleStepTests
(`imm & (0xEE | (A & 0x11))`). Opkod jest niestabilny, a reguła repozytorium mówi: wektory są źródłem prawdy.
Zmiana implementacji pod Blargga złamałaby 10 000 testów wektorowych, więc rozbieżność jest udokumentowana,
nie wyciszona. Pozostałe ROM-y (w tym `16-special` i `02-implied`) przechodzą.

## Pułapki

1. **`nestest` nie używa** `ANC`, `ALR`, `ARR`, `XAA`, `AXS`, `AHX`, `TAS`, `SHY`, `SHX`, `LAS` — zielony
   `nestest` nie znaczy „wszystkie nieoficjalne zweryfikowane”.
2. **Tryb dziesiętny nie jest potrzebny** (2A03 go nie ma) — użyć wariantu z etapu P.
3. Odbicie PRG NROM-128 (`$C000` = kopia `$8000`) — **niezweryfikowane w źródle**; sprawdzić przy pierwszym
   przebiegu i zapisać wynik w `docs/cpu/tests.md`.
4. Zapisy do `$4000`–`$401F` (APU/IO) muszą być **przyjmowane i ignorowane**, inaczej program testowy się
   rozsypie.
5. `instr_test-v5` nie testuje 12 KIL ani `$8B $93 $9B $9F $BB` — nie traktować jego wyniku jako pokrycia
   całości.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | nestest | 1/2 | **8991/8991 linii**, 26 553 cykli, 8991 instrukcji; `$02 = $03 = $00` | start `PC = $C000`; komparator `PC A X Y P SP CYC` z offsetem cykli 7; kolumny PPU nie porównywane (wynikają z CYC) |
| 2026-10-22 | Blargg rom_singles | 2/2 | **15 / 16 pass** (03-immediate: kod 1) | `--blargg` czeka **najpierw** na status `$80`, potem na kod końcowy — bez tego pamięć wyzerowana dawała fałszywy „pass” po 1 instrukcji |
| 2026-10-22 | rozbieżność ATX | — | `AB ATX #n` z raportu ROM-u | wektory (wariant stabilny) > Blargg; decyzja zapisana w `docs/cpu/tests.md` |
