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

- [x] 55 wpisów w katalogu (klasa `Cmos`), 53 próbki w `Vectors/wdc65c02/` (WAI/STP bez plików — puste w źródle).
- [x] `--verify-code`: c02 `missingInCode` **55 → 0** (klasa `nowy-65C02` zniknęła; zostały etapy I/J/K).
- [x] Test 65C02 Dormanna (harness H3) — **wykonany w etapie N**: `pass`, `PC = $24F1`, 66 907 072 cykli
      (szczegóły w [N-dormann-65c02.md](N-dormann-65c02.md)).
- [x] Wektory zielone; cykle zgodne z CSV (próbka 53/53 × 100 i pełne 53 × 10 000 = 530 000, 0 pominięć).

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/55 | c02 `missingInCode` = 55 (nowe) | etap otwarty |
| 2026-10-21 | cały etap | 55/55 | `--verify-code` c02 `nowy-65C02` 55 → 0; próbka 53/53 × 100 i pełne 53 × 10 000 = 530 000 (0 pominięć); testy 527/527 | reguły z wektorów: RMB/SMB bez zmian flag; BIT #imm tylko Z; BBR/BBS przy cross — dwa dummy pod PC; JMP (abs,X) dummy pod adresem low operandu; BRA ghost przy cross; WAI/STP bez wektorów (puste pliki w źródle; zachowanie z CSV: 3 cykle); numer bitu w `CpuOpcode.BitNumber`; H3 Dormann nie uruchomiony |
