# Etap Z10 — przerwania, RESET, HALT w czasie

Poziom: **D (poza wektorami)** · Pliki wektorów: **0** · Nowe: linie `INT`/`NMI`/`RESET`, tryby `IM 0/1/2`

Zestaw SingleStepTests/z80 ma pliki wyłącznie dla opkodów (1604, nazwane po bajtach) — **nie ma plików dla akceptacji
przerwania, RESET ani HALT w czasie** (sprawdzone na drzewie repo, 2026-10-22). Ten etap nie ma wyroczni
wektorowej; wyrocznią musi być dokument producenta i testy programowe (Z11).

## Zakres

- Linie: `IrqLevel`, `NmiLevel` przez istniejące `IInterruptLines` (kontrakt neutralny rodzinowo) oraz osobne wejście RESET.
- Tryby: `IM 0` (instrukcja z magistrali — najczęściej `RST`), `IM 1` (skok pod stały adres), `IM 2` (wektor przez tablicę z `I`).
- `NMI`: wykrywanie zbocza, skok pod stały adres, kopiowanie `iff1` do `iff2` i zerowanie `iff1`.
- `HALT` w czasie: powtarzane pobrania opkodu bez zwiększania `PC`, aż do przerwania.
- `EI`: opóźnienie o jedną instrukcję (w Z5 jest tylko stan `ei`; tu — zachowanie z przerwaniem).
- `RESET`: stan początkowy rejestrów (wartości **z dokumentu producenta**, niezweryfikowane).

## Źródła do przeczytania przed implementacją (nieprzeczytane — niezweryfikowane)

| źródło | po co |
|---|---|
| Zilog Z80 CPU User Manual (UM0080) | czasy akceptacji przerwań, stan po RESET, HALT |
| „The Undocumented Z80 Documented” (Sean Young) | `LD A,I` a przerwanie, `X`/`Y`, `MEMPTR` |
| zestawy Fuse / z80test (Patrik Rak) | zachowanie rejestrów i flag po przerwaniu |

Dopisz każde przeczytane źródło do listy źródeł w `docs/cpu/sources.md` (reguła z `docs.instructions.md`).

## Kryteria akceptacji

- [ ] Testy jednostkowe z jawnie zapisanymi sekwencjami T-state dla `IM 0/1/2` i `NMI` — z liczbami z dokumentu, nie z pamięci.
- [ ] Test: `EI` bezpośrednio przed przerwaniem — przerwanie dopiero po następnej instrukcji.
- [ ] Test: `HALT` kończy się przerwaniem; adres powrotu wskazuje instrukcję po `HALT`.
- [ ] Test: `LD A,I` tuż przed przyjęciem przerwania daje `PV` zgodnie z udokumentowanym zachowaniem NMOS (pole `p`).
- [ ] Definicja ukończenia.

## Pułapki

1. **`IM 0` przyjmuje instrukcję z magistrali danych** — maszyna, która nic nie podaje, musi zwrócić zdefiniowaną wartość.
2. **Przerwania nie są przyjmowane tuż po prefiksie** (`DD`, `FD`, `ED`, `CB`) ani w trakcie `EI` (niezweryfikowane).
3. **`NMI` zbocze, `INT` poziom** — nie mieszaj; kontrakt `IInterruptLines` mówi to samo o 6502.
4. **Nie pisz ujemnych oczekiwań z pamięci** — brak wektorów oznacza brak automatycznej wyroczni.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
