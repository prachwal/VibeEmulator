# Etap C — rozgałęzienia warunkowe

Poziom: **A** · Opkody: **8** · Nowe tryby: **1** (`Relative`) · Nowe fazy: **1** · Nowe akcje: 8

Po etapie: **143/151** (94,7%). Pierwszy etap, który dotyka maszyny stanów.

## Zakres

| opkod | mnemonic | warunek |
|---|---|---|
| `10` | BPL | `N = 0` |
| `30` | BMI | `N = 1` |
| `50` | BVC | `V = 0` |
| `70` | BVS | `V = 1` |
| `90` | BCC | `C = 0` |
| `B0` | BCS | `C = 1` |
| `D0` | BNE | `Z = 0` |
| `F0` | BEQ | `Z = 1` |

## Nowe elementy

- `AddressingMode.Relative` (nowa wartość na końcu enuma).
- Nowa faza: pobranie offsetu (1 bajt), **dummy read**, dodanie offsetu ze znakiem, ewentualna kara cyklu.
- `OpKind.Branch` (albo rozszerzenie `Jump` — decyzję zapisz w kodzie) i akcje warunków.

## Cykle

- Warunek fałszywy: 2 cykle (fetch + operand).
- Warunek prawdziwy, ta sama strona: 3 cykle.
- Warunek prawdziwy, inna strona: 4 cykle (+1 kara).
- Dummy read występuje przy przejściu strony — **adres i kolejność z wektorów**, nie z pamięci.

## Kryteria akceptacji

- [x] 8 wpisów; 8 próbek wektorów w obu katalogach.
- [x] `--verify-code`: NMOS `missingInCode` **16 → 8**.
- [x] Cztery przypadki (przez wektory, nie osobne testy jednostkowe): brak skoku, skok w tej samej stronie, skok na inną stronę, skok w tył (ujemny offset).
- [x] Wektory zielone dla wszystkich 8 opkodów.

## Pułapki

1. **Kara tylko przy przejściu strony** — nie za sam skok i nie za każdy warunek prawdziwy.
2. **Offset ze znakiem** (`sbyte`), zakres −128…+127; dodawanie do `PC` po pobraniu operandu.
3. **Dummy read przy przejściu strony** ma adres bez korekty strony — sprawdzić w wektorach.
4. `PC` w wektorach jest liczone po instrukcji; nie „poprawiać” go o długość instrukcji drugi raz.

## Odkrycie w trakcie implementacji (do zapisania, nie do zgubienia)

Rozgałęzienie z prawdziwym warunkiem **zawsze** robi dummy read spod PC (cykl 3), niezależnie od page cross.
Bez page cross to koniec (3 cykle); z page cross dokłada BranchCrossed spod adresu bez korekty (4 cykle).
Harness wymaga dokładnie jednego dostępu na Tick, więc faza musiała zostać rozbita na trzy:
BranchOperand (odczyt offsetu + test warunku), BranchTaken (dummy read + skok) i BranchCrossed (kara).
Pierwsza wersja z dwoma dostępami w jednym Tick padała z „otrzymano read X, read Y”.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | BPL/BMI/BVC/BVS/BCC/BCS/BNE/BEQ | 8/8 | `--verify-code`: `mismatched: 0`, `missingInCode` NMOS = **8**; testy Mos6502 **418/418** (16 nowych przypadków wektorowych) | wektory: 8 plików × 2 zestawy po 100 testów |
| 2026-10-21 | pełne wektory (10 000 na opkod) | 8/8 × 2 zestawy | NMOS: przeszło **80 000**, pominięto 0, 0 niezgodnych; 65C02: przeszło **80 000**, pominięto 0, 0 niezgodnych | runner poza repo (`/tmp`, nie commitowany), pliki z przypiętego commita, usuwane po przebiegu; uwaga: skrypt etapu C miał błąd parsowania pary `nmos:6502` (dwukropek w URL) — naprawiony przed przebiegiem |

- [x] Pełne wektory (10 000 na opkod) — uruchomione poza repo: **8/8 opkodów × 2 zestawy bez niezgodności**
  (80 000 + 80 000 testów przeszło, 0 pominięto, 0 niezgodnych).
