# Etap H — nowe opkody 65C02

Poziom: **C** · Opkody: **55** · Nowe tryby: 3 (`izp`, `zpr`, `iax`) · Nowe fazy: ~3 · Nowe akcje: 13

Po etapie: **212/212** udokumentowanych opkodów 65C02 (151 dziedziczonych + 61 nowych).

## Zakres

| grupa | opkody | tryb | nowy element |
|---|---|---|---|
| `(zp)` bez indeksu | `12 32 52 72 92 B2 D2 F2` | `izp` | tryb + faza wskaźnika |
| BIT | `34 3C 89` | zpx, abx, imm | akcje istnieją |
| BRA | `80` | rel | rozgałęzienie bezwarunkowe (3 cykle) |
| RMB0–7 | `07 17 27 37 47 57 67 77` | zp | akcja `Rmb` (bit w operandzie opkodu) |
| SMB0–7 | `87 97 A7 B7 C7 D7 E7 F7` | zp | akcja `Smb` |
| BBR0–7 | `0F 1F 2F 3F 4F 5F 6F 7F` | `zpr` | akcja `Bbr` + tryb `zp,rel` |
| BBS0–7 | `8F 9F AF BF CF DF EF FF` | `zpr` | akcja `Bbs` |
| PHX/PHY/PLX/PLY | `DA 5A FA 7A` | impl | 4 akcje, fazy stosu z etapu D |
| TSB/TRB | `04 0C 14 1C` | zp, abs | 2 akcje (RMW + `A`) |
| JMP (abs,X) | `7C` | `iax` | tryb + faza |
| WAI / STP | `CB` / `DB` | impl | 2 akcje; `STP` zatrzymuje rdzeń jak KIL |

## Pułapki

1. **`92 B2 D2 F2` to w NMOS KIL**, a w 65C02 `STA/LDA/CMP/SBC (zp)`. Ten sam bajt, dwa zachowania — tabela
   wariantu musi to rozdzielić. Nie kopiować wpisów NMOS do c02 dla tych czterech.
2. **`BBR`/`BBS`** mają 5 cykli i tryb `zp,rel` (dwa bajty operandu + gałąź). Kara cyklu jak w etapie C.
3. **`RMB`/`SMB`/`BBR`/`BBS` numer bitu** jest zaszyty w opkodzie, nie w operandzie — akcja potrzebuje numeru bitu
   (pole w `CpuOpcode` albo osobne akcje; decyzję zapisz w kodzie).
4. **`WAI`/`STP`** zmieniają stan rdzenia poza tabelą — potrzebny stan `Waiting`/`Stopped`.
5. **`TSB`/`TRB`** to RMW z dodatkowym odczytem `A`; styl RMW wariantu (dwa odczyty) obowiązuje.

## Kryteria akceptacji

- [ ] 55 wpisów w katalogu (klasa `Cmos`), 55 próbek w `Vectors/wdc65c02/`.
- [ ] `--verify-code`: c02 `missingInCode` **55 → 0** (po uwzględnieniu etapu I: 44 → 0).
- [ ] Test 65C02 Dormanna (harness H3).
- [ ] Wektory zielone; cykle zgodne z CSV (bez `$5C`, który jest w etapie I).

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/55 | c02 `missingInCode` = 55 (nowe) | etap otwarty |
