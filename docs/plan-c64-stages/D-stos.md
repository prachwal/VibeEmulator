# Etap D — stos

Poziom: **A** · Opkody: **4** · Nowe tryby: 0 · Nowe fazy: **2** (push, pull) · Nowe akcje: 4

Po etapie: **147/151** (97,4%).

## Zakres

| opkod | mnemonic | cykle | faza |
|---|---|---|---|
| `48` | PHA | 3 | push |
| `08` | PHP | 3 | push (`P` z `B=1`) |
| `68` | PLA | 4 | pull |
| `28` | PLP | 4 | pull |

## Nowe elementy

- Dwie fazy rdzenia: **push** (zapis pod `$0100 + S`, dekrementacja `S`) i **pull** (dummy read + odczyt spod
  `$0100 + S`, inkrementacja `S`).
- `OpKind.Push` / `OpKind.Pull` (albo jedna wartość z akcją — decyzję zapisz w kodzie).
- Akcje: `Pha`, `Php`, `Pla`, `Plp`.

## Semantyka

- `PHP` odkłada `P` z ustawionym `B`; `PLP` ignoruje bity nieistniejące (`B`, bit 5) — co dokładnie, rozstrzygają wektory.
- `PLP` **nie** zmienia `S` poza inkrementacją; `SP` zawija się w stronie zerowej (`$01xx`).

## Kryteria akceptacji

- [x] 4 wpisy; 4 próbki wektorów w obu katalogach.
- [x] `--verify-code`: NMOS `missingInCode` **8 → 4**.
- [x] Test parujący: `PHA`/`PLA` i `PHP`/`PLP` w jednym programie odtwarzają wartość i `S`.
- [x] Wektory zielone (próbka 8/8 × 2 + pełne 4/4 × 2).

## Pułapki

1. **Kolejność w pull:** dummy read spod `PC` **przed** odczytem stosu — potwierdzić w wektorach.
2. `TSX`/`TXS` już istnieją, więc stan stosu da się sprawdzić ręcznie w teście.
3. `PHP` z `B=1` nawet gdy flaga nie jest „prawdziwa”; `PLP` nie ustawia `B` z stosu.
4. Nie mylić z `JSR`/`RTS` (etap E) — tam kolejność i inkrementacja `PC` są inne.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/4 | `missingInCode` NMOS = 8 | etap otwarty |
| 2026-10-21 | cały etap | 4/4 | `--verify-code` NMOS `missingInCode` 8 → 4, próbka 8/8 × 2, pełne 4/4 × 2 (80 000, 0 pominięć) | PHP odkłada `P \| 0x30`; PLP: `P = (stos & 0xCF) \| 0x20`; pull: dummy PC + dummy stos + odczyt |
