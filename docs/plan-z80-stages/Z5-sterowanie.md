# Etap Z5 — skoki, wywołania, powroty, HALT, DI/EI

Poziom: **A** · Pliki wektorów: **45** · Nowe elementy: warunki, `ei`, HALT

## Zakres

| grupa | opkody | liczba |
|---|---|---|
| `JP nn`, `JP cc,nn` | `C3`; `C2 CA D2 DA E2 EA F2 FA` | 9 |
| `JP (HL)` | `E9` | 1 |
| `JR e`, `JR cc,e` | `18`; `20 28 30 38` | 5 |
| `DJNZ e` | `10` | 1 |
| `CALL nn`, `CALL cc,nn` | `CD`; `C4 CC D4 DC E4 EC F4 FC` | 9 |
| `RET`, `RET cc` | `C9`; `C0 C8 D0 D8 E0 E8 F0 F8` | 9 |
| `RST p` | `C7 CF D7 DF E7 EF F7 FF` | 8 |
| `HALT` | `76` | 1 |
| `DI`, `EI` | `F3 FB` | 2 |

## Nowe elementy

- Warunki `NZ Z NC C PO PE P M` — jedna funkcja na podstawie pola 3-bitowego i `F`.
- Zachowanie „warunek niespełniony” skraca instrukcję (liczba T-state z wektorów, dwa warianty na opkod).
- EI ustawia IFF1/IFF2 i pozostawia `ei=1`; następna instrukcja zeruje `ei`. Ponowne EI odnawia opóźnienie,
  DI zeruje oba IFF i `ei`. Stan potwierdzony pełnymi wektorami; przyjmowanie IRQ należy do Z10.
- `HALT`: z próbki `76` — 4 T-state, `pc` po instrukcji = `pc`+1. Zachowanie **w czasie** (powtarzanie pobrań NOP do
  przerwania) wektory jednej instrukcji nie pokrywają — patrz Z10.

## Kryteria akceptacji

- [x] 45 plików wektorów w repo i w manifeście, testy zielone.
- [x] Ręczne testy dla gałęzi, które losowe wektory mogą rzadko trafiać (`DJNZ` z `B=1`, `JR` przez granicę strony).
- [x] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [x] Definicja ukończenia.

## Pułapki

1. **`WZ`**: JP nn/cc,nn i CALL nn/cc,nn ustawiają cel także przy niespełnionym warunku.
   RET/JR/DJNZ ustawiają cel tylko przy wykonanym skoku; przy niespełnionym warunku zachowują stare WZ.
   RST ustawia WZ na swój cel, JP (HL) zachowuje WZ. Potwierdzone pełnymi wektorami, źródło poniżej.
2. **`JP (HL)`** to w istocie `LD PC,HL` — nie czyta pamięci pod `HL`.
3. **`RST`** używa starego `PC`+1 jako adresu powrotu; adres docelowy to `p`·8.
4. **Nie dodawaj tu przyjmowania przerwań.** `EI`/`DI` zmieniają flagi `iff1`/`iff2`/`ei`, ale akceptacja IRQ to Z10.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | JP nn/cc,nn i JP (HL) | 10/45 | 200/200 próbki; 10 000/10 000 pełne | 10 / 4 T-state |
| 2026-10-22 | JR i DJNZ | 16/45 | 120/120 próbki; 6000/6000 pełne | JR: 7/12; DJNZ: 8/13 T-state |
| 2026-10-22 | CALL nn/cc,nn | 25/45 | 180/180 próbki; 9000/9000 pełne | 10/17 T-state; starszy bajt powrotu pierwszy |
| 2026-10-22 | RET/RET cc | 34/45 | 180/180 próbki; 9000/9000 pełne | 10 / 5/11 T-state |
| 2026-10-22 | RST | 42/45 | 160/160 próbki; 8000/8000 pełne | 11 T-state; PC+1 na stosie |
| 2026-10-22 | HALT, DI/EI | 45/45 | 60/60 próbki; 3000/3000 pełne | 4 T-state; HALT w czasie w Z10 |


Źródło: [SingleStepTests/z80](https://github.com/SingleStepTests/z80/tree/ebe1875d48f374bcfd4b505d8eb8ee751568b5f7/v1),
commit `ebe1875d48f374bcfd4b505d8eb8ee751568b5f7`. W repo **45 plików × 20 = 900 przypadków**.
Pełne wektory poza repo: **45 000 przeszło / 0 pominięto / 0 niezgodnych**. Runner w `/tmp` używa tego samego
harnessu co próbki: porównuje cały stan, RAM, każdy T-state (adres/dana/piny), Pending i rzeczywiste accessy pamięci.
Wektory pochodzą z modelu Ares, nie z pomiaru krzemu.

`Z80Core.Control` dekoduje warunki, czasy i stan DI/EI; `ControlCycles` realizuje odczyty celu i powrotu,
a fazy wewnętrzne i stosu zapewniają dostępy CALL/RET/RST. Liczba T-state jest znana po odczycie opkodu
(T2) na podstawie F lub B — Pending od tej chwili uwzględnia wybraną ścieżkę.

Dodatkowe testy sprawdzają DJNZ dla B=1/0/2, JR przez granice strony i przestrzeni adresowej, WZ i brak dostępów
do stosu przy fałszywym warunku, JP (HL) bez odczytu pamięci pod HL, CALL/RET/RST z zawijaniem PC/SP,
WAIT przed zapisem adresu powrotu, ponowne EI oraz EI→DI.

**Ograniczenie HALT w Z5:** `Halted` jest osobnym zatrzaskiem (nie `Jammed`), PC wskazuje bajt po HALT.
Dalsze Tick zwiększają czas, nie wykonując kolejnych instrukcji ani dostępów pamięci; Reset kasuje zatrzask.
To jawne uproszczenie: powtarzane fetch/refresh i wyjście z HALT przez IRQ/NMI pozostają w Z10.
Test po zakończeniu instrukcji sprawdza ten kontrakt uproszczenia, nie zgodność dalszych cykli z krzemem.

Regresja faz stosu i wewnętrznych T-state: pełne Z4 ponownie **31 000 przeszło / 0 pominięto / 0 niezgodnych**.
Próbki Z1–Z4 również zielone; pełnych Z1–Z2 nie uruchomiono.

Kontrola końcowa: build Debug i Release — po **0 ostrzeżeń i 0 błędów**; testy Release — **1389 zaliczonych,
0 nieudanych, 0 pominiętych** (Z80 308, Mos6502 1014, Core 50, Memory 11, Cli 6).
`dotnet format --verify-no-changes --severity warn` — exit 0; `OpcodeInfo --verify-code` — 512 wierszy CSV,
0 niezgodności (kontrola 6502; Z80 nie ma CSV). Manifest: 246 unikalnych opkodów; próbki Z5 zgodne z pierwszymi
20 rekordami źródła we wszystkich 45 plikach.
