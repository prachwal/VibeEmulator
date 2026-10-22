# Etap N — Dormann: test 65C02 (extended opcodes)

Poziom: **C w praktyce** · Opkody: 0 · Nowe fazy: 0 (tylko naprawy, jeśli test je wykaże)

Drugi przebieg zewnętrzny — na wariancie CMOS. Test 65C02 zakłada, że test funkcjonalny NMOS już przechodzi
(etap M), bo wywołuje te same procedury.

## Zakres

- Obraz: `65C02_extended_opcodes_test.bin` (65 536 B) z tego samego repozytorium, katalog `bin_files/`.
- Uruchomienie: `CpuHarness --image <plik> --variant c02 --pc 0x0400 --success-pc 0x24F1
  --limit-cycles 200000000`.
- Sukces: pętla `jmp *` pod `$24F1`.
- Wariant: `CpuVariants.Cmos65C02` (domyślne opcje źródła: `wdc_op`; `skip_nop` **nie** ustawiamy, bo mamy
  44 NOP-y z etapu I).

## Nowe elementy

- Wpis w `docs/cpu/tests.md` z wynikiem (cykle, instrukcje, commit źródła).
- Poprawki rdzenia tylko po wskazaniu adresu porażki; każda z uzasadnieniem i testem regresyjnym w repo.

## Kryteria akceptacji

- [ ] Przebieg kończy się `outcome = pass`, `pc = 0x24F1`.
- [ ] Wynik zapisany z liczbami.
- [ ] Definicja ukończenia bez zmian.

## Pułapki

1. Test pokrywa **dodatkowe** opkody 65C02 (RMB/SMB/BBR/BBS, STZ, TSB/TRB, BIT #/zp,X/abs,X, INC A/DEC A,
   `(zp)`, JMP (abs,X)) — czyli materiał etapów H i I.
2. `WAI`/`STP` test pokrywa w teście przerwań (etap O), nie tutaj.
3. Ten sam bajt ma w 65C02 inne znaczenie niż w NMOS (np. `$64`) — harness musi dostać `--variant c02`,
   inaczej przejdzie tabelę NMOS i pętla `PC` wypadnie w losowym miejscu.
4. Test **wymaga** wcześniejszego zaliczenia etapu M; porażka tutaj przy zielonym M wskazuje na różnice
   wariantu, nie na podstawowe instrukcje.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | — | 0/1 | — | etap otwarty |
