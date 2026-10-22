# Etapy dojścia do pełnego pokrycia Z80 (wektory SingleStepTests/z80)

Plan rdzenia **Zilog Z80** w modelu cykl po cyklu, wzorowany na [../plan-c64-stages/README.md](../plan-c64-stages/README.md).
Etapy `Z1`–`Z11` (litery `A`–`Q` są zajęte przez serię 6502). Plan mówi **co i w jakiej kolejności**;
**jak** (T-state, adresy, wartości na magistrali, flagi `X`/`Y`, `WZ`) mówią wektory.

Stan na 2026-10-22: **wszystkie etapy planowane, kod nie istnieje.**

## Poziomy docelowe

| poziom | zakres | etapy, które go zamykają |
|---|---|---|
| **A. Z80 bez prefiksów** | 252 opkody `00`–`FF` bez bajtów prefiksów | Z1–Z5, część Z6 i Z7 |
| **B. Z80 z `CB` i `ED`** | + 256 plików `CB`, 80 plików `ED` | Z6, Z7 |
| **C. Z80 z `IX`/`IY`** | + 504 pliki `DD`/`FD`, 512 plików `DD CB`/`FD CB` | Z8, Z9 |
| **D. zachowanie poza wektorami** | przerwania, RESET, HALT w czasie, programy testowe | Z10, Z11 |

Poziomy A–C = **1604 pliki wektorów** (patrz „Zestaw wektorów”). Poziom D nie ma wektorów.

## Pliki etapów

| etap | stan | plik | zakres | pliki wektorów |
|---|---|---|---|---|
| Z1 | planowany | [Z1-szkielet-i-harness-wektorow.md](Z1-szkielet-i-harness-wektorow.md) | projekt `src/Z80`, stan, magistrala portów, harness wektorów, `NOP` | 1 |
| Z2 | planowany | [Z2-ladowanie-8-bit.md](Z2-ladowanie-8-bit.md) | `LD r,r'`, `LD r,n`, `LD (BC/DE/nn),A`, `LD A,(BC/DE/nn)` | 77 |
| Z3 | planowany | [Z3-alu-8-bit-i-flagi.md](Z3-alu-8-bit-i-flagi.md) | ADD/ADC/SUB/SBC/AND/XOR/OR/CP, INC/DEC r, DAA, CPL, SCF, CCF, rejestr `Q` | 92 |
| Z4 | planowany | [Z4-16-bit-stos-wymiany.md](Z4-16-bit-stos-wymiany.md) | `LD rr,nn`, ADD HL, INC/DEC rr, PUSH/POP, EX/EXX | 31 |
| Z5 | planowany | [Z5-sterowanie.md](Z5-sterowanie.md) | JP/JR/DJNZ/CALL/RET/RST, HALT, DI/EI | 45 |
| Z6 | planowany | [Z6-cb-rotacje-i-bity.md](Z6-cb-rotacje-i-bity.md) | prefiks `CB` + `RLCA`/`RRCA`/`RLA`/`RRA` | 260 |
| Z7 | planowany | [Z7-ed-porty-i-bloki.md](Z7-ed-porty-i-bloki.md) | prefiks `ED`, `IN`/`OUT`, instrukcje blokowe | 82 |
| Z8 | planowany | [Z8-ix-iy.md](Z8-ix-iy.md) | prefiksy `DD`/`FD` | 504 |
| Z9 | planowany | [Z9-ix-iy-cb.md](Z9-ix-iy-cb.md) | `DD CB d op` i `FD CB d op` | 512 |
| Z10 | planowany | [Z10-przerwania-i-reset.md](Z10-przerwania-i-reset.md) | IRQ (IM 0/1/2), NMI, RESET, `EI` w czasie | 0 |
| Z11 | planowany | [Z11-harness-programow-zex.md](Z11-harness-programow-zex.md) | runner programów CP/M, zexdoc/zexall | 0 |

Suma plików wektorów: 1 + 77 + 92 + 31 + 45 + 260 + 82 + 504 + 512 = **1604** (sprawdzone z drzewem repo źródłowego).

## Zestaw wektorów — fakty i ich źródło

Źródło pierwotne: <https://github.com/SingleStepTests/z80>, licencja MIT. Zmierzone **2026-10-22** na commicie
`ebe1875d48f374bcfd4b505d8eb8ee751568b5f7` (HEAD `main`, data commita 2026-04-10) przez `gh api` (drzewo plików)
i pobranie próbek `00`, `76`, `db`, `ed b0`.

| fakt | wartość | skąd |
|---|---|---|
| liczba plików w `v1/` | **1604** | drzewo git commita |
| układ nazw | `NN.json` (252), `cb NN.json` (256), `dd NN.json` (252), `fd NN.json` (252), `ed NN.json` (80), `dd cb __ NN.json` (256), `fd cb __ NN.json` (256) | drzewo git; **nazwy zawierają spacje** |
| brakujące bajty bez prefiksu | tylko `cb`, `dd`, `ed`, `fd` (to same prefiksy) | drzewo git |
| pliki `ed` | `40`–`7F` (64) + `A0`–`A3`, `A8`–`AB`, `B0`–`B3`, `B8`–`BB` (16) | drzewo git; **nie ma** plików dla niezdefiniowanych `ED xx` |
| testów w pliku | **1000** | 4 próbki |
| rozmiar pliku | 606–1018 KB na 1000 testów (~0,6–1 KB/test) | 4 próbki |
| pola `initial`/`final` | `pc sp a b c d e f h l i r ix iy af_ bc_ de_ hl_ wz im iff1 iff2 ei p q ram` | próbki |
| `ram` | tylko dotknięte adresy: `[[adres, wartość], …]` | próbki |
| `ports` | tylko instrukcje I/O: `[[adres16, wartość, "r"|"w"]]` | próbka `db` |
| `cycles` | **jeden wpis na T-state**: `[adres|null, dana|null, "piny"]` | próbki |
| piny | 4 znaki; widziane: `----`, `r-m-` (odczyt pamięci), `r--i` (odczyt I/O); kolejność `r w m i` **wnioskowana z przykładów** | próbki |
| długości cykli | `00`: 4, `76`: 4, `db`: 11, `ed b0`: 21 (wszystkie 1000 testów) | próbki |
| odświeżanie | T3–T4 `NOP`: adres `I<<8 | R` (166·256+16 = 42512) | próbka `00`, test 0 |
| adres portu | `IN A,(n)`: `A<<8 | n` (227·256+249 = 58361) | próbka `db`, test 0 |
| opcje generatora (CMOS wyłączony, uproszczone T-state pamięci, odświeżanie w adresie, `null` w WAIT wyłączony) | j.w. | **README repo przez WebFetch (streszczenie) — przeczytać w całości przed Z1** |
| wektory wygenerowane z rdzenia **Ares**, autor: „prawdopodobnie nie idealne” | j.w. | **README repo przez WebFetch (streszczenie)** |

Wniosek: `Tick()` rdzenia to **jeden T-state**, nie cykl maszynowy. To inny kontrakt czasowy niż w serii 6502
(tam cykl = jeden dostęp do magistrali).

## Decyzje do podjęcia w Z1 (z rekomendacją)

| # | decyzja | rekomendacja | uzasadnienie |
|---|---|---|---|
| D1 | co porównywać w `cycles` | **pełny zapis** (adres, dana, piny, każdy T-state) | format jest tak zapisany; zdarzenia `Read`/`Write` nie pokazują odświeżania ani T-state bez dostępu |
| D2 | dekodowanie opkodów | **algorytmiczne** (pola bitowe opkodu + faza), bez katalogu 256×N wpisów | Z80 jest regularny; brak CSV-źródła do `--verify-code`, więc wektory są jedynym wyrocznią |
| D3 | gdzie leży magistrala portów | **`src/Memory`** (`IPortBus`, obok `IMemoryBus`) | `Memory` jest „magistralą bez wiedzy o CPU”; przestrzeń I/O to ten sam poziom abstrakcji |
| D4 | ile testów na plik w repo | **20** (≈ 26 MB); 100 testów ≈ 128 MB | szacunek 1604 × testów × ~0,8 KB (średnia z 4 próbek); seria 6502 ma 27 MB |
| D5 | granulacja stanu rdzenia | **maszyna stanów cykli maszynowych** (M1, odczyt, zapis, I/O, wewnętrzny) z licznikiem T-state | analogia do faz `StateMachineCpuCore`; bez alokacji i LINQ w `Tick()` |
| D6 | `ICpuCore.Stall()` | mapować na **WAIT** (zegar biegnie, stan stoi) | kontrakt wymaga `Stall()`; w C64 rdzeń Z80 nie jest podłączany |
| D7 | wariant | tylko **NMOS** (zestaw: CMOS wyłączony), bez `Z80Variant` na razie | jeden zestaw wektorów; wariant dodać, gdy pojawi się źródło dla CMOS |

Rozstrzygnięcie każdej decyzji wpisz w dzienniku Z1.

## Zasada: plan mówi „co", wektory mówią „jak"

Zachowanie Z80 bierz z wektorów i dokumentu producenta, nie z pamięci ani z tego planu. Wektory są **wyjściem
cudzej implementacji (Ares)**, a nie zapisem krzemu — przy rozbieżności wektora z drugim źródłem (dokumentacja
Zilog, zestawy programowe z Z11) opisz ją wprost w teście i w dzienniku, nie wybieraj po cichu. Rozbieżności
zgłasza się też do repozytorium źródłowego (autor prosi o zgłoszenia w Issues).

Wymóg `.github/instructions/docs.instructions.md`: fakt bez źródła oznaczaj **(niezweryfikowane)**. W planach
etapów tak oznaczone są wszystkie twierdzenia o sprzęcie wzięte z pamięci.

## Obowiązek oznaczania postępu

Jak w serii 6502: po każdej grupie (~10 opkodów albo jedna rodzina mnemoniczna) jeden wpis w „Dzienniku postępu”
pliku etapu: `data | grupa | postęp (pliki wektorów dodane/wszystkie w etapie) | weryfikacja | uwagi`.

**Miara postępu** (nie ma odpowiednika `--verify-code`, bo brak CSV): lista opkodów w manifeście testu
(`Z80VectorManifest`) policzona komendą z Z1; liczba plików wektorów, które przechodzą, i liczba w manifeście
muszą się zgadzać z tabelą powyżej. Po każdym etapie zaktualizuj:

1. tabelę stanu w tym README (kolumna „stan”),
2. `tests/VibeEmulator.Z80.Tests/Vectors/README.md` (commit źródła, katalog, liczba testów na plik, rozmiar),
3. `docs/plan-c64-cycle-exact.md` — **jedno zdanie** w „Elementy spoza planu” wskazujące na ten katalog (w Z1).

## Wspólny rytuał zakończenia etapu

1. Opkody etapu zaimplementowane w `src/Z80`; manifest testu zawiera ich pliki wektorów.
2. Próbka wektorów (pierwsze N testów z pliku, zwarty JSON, N z D4) + wpis w `Vectors/README.md`.
3. Testy wektorowe zielone: stan końcowy, `ram`, `ports` i **pełny** `cycles` (D1).
4. Pełne wektory (1000 na plik) **poza repo**; jeśli ich nie uruchomiono, napisz to wprost.
5. Wpis w dzienniku etapu i w tabeli powyżej.
6. Definicja ukończenia z `.github/copilot-instructions.md` (build Debug/Release, testy, `dotnet format --verify-no-changes`).

## Poza zakresem

- Podłączenie Z80 do maszyny (ZX Spectrum, MSX, karta CP/M do C64) — osobny plan; rdzeń ma być użyteczny bez niego.
- Warianty CMOS, Z180, eZ80, Game Boy SM83 (osobny zestaw wektorów `sm83`).
- Dźwięk, wideo, taśma.

## Ryzyka

| # | ryzyko | postępowanie |
|---|---|---|
| R1 | wektory pochodzą z rdzenia Ares i mogą zawierać błędy | rozbieżność = drugie źródło (Z11), nie „naprawa” rdzenia pod wektor |
| R2 | kontrakt `ICpuCore.Pending` zakłada znaną liczbę cykli do końca instrukcji; przy Z80 zależy od warunków | w Z1 sprawdzić, jak `Pending` liczy rdzeń 6502, i zapisać semantykę dla Z80 |
| R3 | rozmiar zestawu (1604 × 1000 testów, rząd GB) | pełne wektory tylko poza repo; w repo próbka z D4 |
| R4 | pętle bez końca w instrukcjach powtarzanych (`LDIR` itp.) w teście | limit T-state na test w harnessie |
