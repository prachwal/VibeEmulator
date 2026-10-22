# Etap Z10 — przerwania, RESET, HALT w czasie

Poziom: **D (poza wektorami)** · Pliki wektorów: **0** · Nowe: linie `INT`/`NMI`/`RESET`, tryby `IM 0/1/2`

Zestaw SingleStepTests/z80 ma pliki wyłącznie dla opkodów (1604, nazwane po bajtach) — **nie ma plików dla akceptacji
przerwania, RESET ani HALT w czasie** (sprawdzone na drzewie repo, 2026-10-22). Ten etap nie ma wyroczni
wektorowej; wyrocznią musi być dokument producenta i testy programowe (Z11).

## Zakres

- Linie: `IrqLevel`, `NmiLevel` przez istniejące `IInterruptLines` (kontrakt neutralny rodzinowo) oraz osobne wejście RESET.
- Tryby: `IM 0` (instrukcja z magistrali — najczęściej `RST`), `IM 1` (skok pod stały adres), `IM 2` (wektor przez tablicę z `I`).
- `NMI`: wykrywanie zbocza, skok pod stały adres, zerowanie `iff1`; `iff2` pozostaje bez zmian.
- `HALT` w czasie: powtarzane pobrania opkodu bez zwiększania `PC`, aż do przerwania.
- `EI`: opóźnienie o jedną instrukcję (w Z5 jest tylko stan `ei`; tu — zachowanie z przerwaniem).
- `RESET`: sygnał trzymany minimum 3 T-state; dokument producenta określa resetowane pola `PC`, `I`, `R`, `IFF1`, `IFF2` i `IM`.

## Źródła użyte do implementacji

| źródło | po co |
|---|---|
| [Zilog Z80 Family CPU User Manual (UM0080, rev. 11, 2016)](https://www.zilog.com/docs/z80/z80cpu_um.pdf) | linie INT/NMI/RESET, tryby przerwań, `IFF`, flaga `P/V` przy `LD A,I`/`LD A,R`, HALT |
| [The Undocumented Z80 Documented, v0.91 (Sean Young)](https://www.ballyalley.com/ml/z80_docs/Undocumented%20Z80%20Documented,%20The%20%28Sean%20Young%29.pdf) | empiryczne zachowanie IM2 dla nieparzystego bajtu wektora, domyślne `FF` na magistrali IM0, timing i `IFF2` po NMI |
| [redcode/Z80 Wiki: Interrupts](https://github.com/redcode/Z80/wiki/Interrupts) (odczytano 2026-10-22) | sekwencje M-cycle/T-state IM0/1/2 i NMI, opóźnienie EI, prefiksy w IM0, zachowanie HALT |
| [redcode/Z80: test-Z80.c](https://github.com/redcode/Z80/blob/master/sources/test-Z80.c) | inwentarz programowych zestawów Z80 (w tym testów Patrika Raka); bez kopiowania kodu testów do repo |

Rozbieżności źródeł: manual Ziloga opisuje parzyste bajty wektora IM2, ale Sean Young raportuje test potwierdzający
odczyt tabeli spod dokładnego adresu `I:byte` również dla nieparzystego bajtu. Rdzeń używa dokładnego adresu.
Tabela Ziloga podaje dla „Accept NMI” `IFF1=0`, `IFF2=*`; zgodnie z tym i testem zagnieżdżonego NMI Seana
Younga obsługa nie kopiuje `IFF1` do `IFF2`.

Dopisz każde przeczytane źródło do listy źródeł w `docs/cpu/sources.md` (reguła z `docs.instructions.md`).

## Kryteria akceptacji

- [x] Testy jednostkowe z jawnymi czasami T-state dla `IM 0/1/2` i `NMI`, w tym `IM 0` z `RST 38h` i opkodem `NOP` z magistrali.
- [x] Test: `EI` bezpośrednio przed przerwaniem — przerwanie dopiero po następnej instrukcji.
- [x] Test: `HALT` wykonuje powtarzane cykle pobrania; po IRQ adres powrotu wskazuje instrukcję za `HALT`.
- [x] Test: `LD A,I` tuż przed przyjęciem przerwania czyści `P/V` zgodnie z manualem Ziloga.
- [x] RESET trwający 3 T-state przywraca udokumentowane pola, zachowując pozostałe rejestry.
- [x] Definicja ukończenia: Debug/Release build, testy Release, format i weryfikacja OpcodeInfo.

## Pułapki

1. **`IM 0` przyjmuje instrukcję z magistrali danych** — maszyna, która nic nie podaje, musi zwrócić zdefiniowaną wartość.
2. **Przerwania nie są przyjmowane tuż po prefiksie** (`DD`, `FD`, `ED`, `CB`) ani w trakcie `EI` (niezweryfikowane).
3. **`NMI` zbocze, `INT` poziom** — nie mieszaj; kontrakt `IInterruptLines` mówi to samo o 6502.
4. **RESET:** źródło nie określa wartości pozostałych rejestrów; sygnał RESET nie powinien ich zerować na podstawie zgadywania.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | źródła i plan | — | UM0080, Sean Young v0.91 i redcode/Z80 Wiki przeczytane | Skorygowano pierwotne założenie o kopiowaniu `IFF1` do `IFF2` przy NMI; dla IM2 zachowujemy nieparzysty bit wektora zgodnie z testem Seana Younga. |
| 2026-10-22 | linie, HALT i obsługa IRQ/NMI | 0 wektorów | testy Z10 (10) oraz pełne kontrole repozytorium | IM0 obsługuje opkody i prefiks `CB` z magistrali; IM1/2 i NMI wykonują sekwencje T-state; RESET po minimum 3 T-state. |
