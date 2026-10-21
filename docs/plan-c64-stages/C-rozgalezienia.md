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

- [ ] 8 wpisów; 8 próbek wektorów w obu katalogach.
- [ ] `--verify-code`: NMOS `missingInCode` **16 → 8**.
- [ ] Cztery testy jednostkowe: brak skoku, skok w tej samej stronie, skok na inną stronę, skok w tył (ujemny offset).
- [ ] Wektory zielone dla wszystkich 8 opkodów.

## Pułapki

1. **Kara tylko przy przejściu strony** — nie za sam skok i nie za każdy warunek prawdziwy.
2. **Offset ze znakiem** (`sbyte`), zakres −128…+127; dodawanie do `PC` po pobraniu operandu.
3. **Dummy read przy przejściu strony** ma adres bez korekty strony — sprawdzić w wektorach.
4. `PC` w wektorach jest liczone po instrukcji; nie „poprawiać” go o długość instrukcji drugi raz.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/8 | `missingInCode` NMOS = 16 | etap otwarty |
