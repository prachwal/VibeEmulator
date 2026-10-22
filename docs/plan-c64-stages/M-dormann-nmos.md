# Etap M — Dormann: test funkcjonalny NMOS

Poziom: **A/B w praktyce** · Opkody: 0 · Nowe fazy: 0 (tylko naprawy, jeśli test je wykaże)

Pierwszy przebieg **zewnętrznego** zestawu testów na rdzeniu cyklowym. Do tej pory jedynym źródłem prawdy
były wektory pojedynczych opkodów; ten etap sprawdza **programy** — samomodyfikujący się kod, pętle, flagi
i kolejność faz w długim przebiegu.

## Zakres

- Obraz: `6502_functional_test.bin` (65 536 B) z repozytorium
  `Klaus2m5/6502_65C02_functional_tests`, katalog `bin_files/`.
- Uruchomienie: `CpuHarness --image <plik> --variant nmos --pc 0x0400 --success-pc 0x3469
  --limit-cycles 200000000`.
- Sukces: pętla `jmp *` pod `$3469` („test passed, no errors”). Każdy inny adres pętli = porażka, a adres
  wskazuje test w listingu `.lst`.
- Wariant: `CpuVariants.Nmos6502`, `disable_decimal = 0` (domyślna konfiguracja źródła), `I_flag = 3`.

## Nowe elementy

- Wpis w `docs/cpu/tests.md` z **wynikiem** (liczba cykli, liczba instrukcji, commit źródła, data).
- Ewentualne poprawki rdzenia — **tylko** jeśli test wskaże adres porażki; każda poprawka z uzasadnieniem
  w `docs/cpu/` albo w `docs/plan-c64-stages/J-nieoficjalne-nmos.md` (jeśli dotyczy nieoficjalnych opkodów).
- Skrypt pomocniczy w `/tmp` (poza repo) do pobrania obrazu — repo nie trzyma binariów.

## Kryteria akceptacji

- [x] Przebieg kończy się `outcome = pass`, `pc = 0x3469`.
- [x] Wynik zapisany z liczbami: **96 241 367 cykli**, **30 646 177 instrukcji** (pełny przebieg, bez limitu).
- [x] Poprawki rdzenia **nie były potrzebne** — test przeszedł za pierwszym przebiegiem.
- [x] Definicja ukończenia bez zmian (build, testy, format, `--verify-code`).

## Pułapki

1. Test **nie używa** sekwencji resetu — wektor `RESET` w obrazie wskazuje pułapkę. `PC` ustawiamy ręcznie.
2. Test jest **samomodyfikujący** — obraz musi leżeć w RAM, nie w pamięci tylko do odczytu.
3. Test sprawdza **tylko udokumentowane** zachowanie NMOS; nie używa opkodów nieoficjalnych (więc etap M nie
   waliduje etapu J).
4. Flag `N`/`V`/`Z` w trybie dziesiętnym test **nie sprawdza** — to robi etap O (Clark).
5. Limit cykli: liczony pomiarem, nie zgadywany; przy dziesiątkach milionów cykli Release jest obowiązkowy.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | przebieg NMOS | 1/1 | `outcome = pass`, `pc = $3469`, **96 241 367 cykli**, 30 646 177 instrukcji | obraz `6502_functional_test.bin` z `bin_files/` (pobrany do `/tmp`, nie do repo); `--limit-cycles 100000000` (= pomiar + ~4% marginesu) |
