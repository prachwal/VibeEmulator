# Etap I — niezdefiniowane NOP-y 65C02

Poziom: **C** · Opkody: **44** · Nowe tryby: 0 · Nowe fazy: 0 · Nowe akcje: 0

Po etapie: 65C02 ma **256/256** bajtów zdefiniowanych.

## Zakres

```
02 03 0B 13 1B 22 23 2B 33 3B 42 43 44 4B 53 54 5B 5C 62 63 6B 73 7B 82 83 8B
93 9B A3 AB B3 BB C2 C3 D3 D4 DC E2 E3 EB F3 F4 FB FC
```

Są to NOP-y o różnych długościach (1–4 bajtów) i cyklach (1–8). Dziś nieznany bajt → JAM, więc zachowanie jest
przybliżone i niezweryfikowane.

## Kryteria akceptacji

- [ ] 44 wpisy w klasie `Cmos` z akcją `Nop` i trybami zgodnymi z CSV (długość i cykle biorą się z trybu).
- [ ] 44 próbki w `Vectors/wdc65c02/`.
- [ ] `--verify-code`: c02 `missingInCode` **44 → 0**.
- [ ] Wektory zielone.

## Pułapki

1. **`$5C`**: datasheet WDC i Oxyron 8 cykli, wektory 4. Decyzja: **wektory są źródłem prawdy**, różnica zostaje
   w kolumnie `uwagi` w CSV i w komentarzu testu. Nie „poprawiać” CSV pod datasheet.
2. **Tryb decyduje o długości**: NOP-y 1-bajtowe (`impl`), 2-bajtowe (`imm`, `zp`, `zpx`), 3-bajtowe (`abs`, `abx`).
   Zły tryb = zła długość i złe cykle, mimo poprawnego mnemonika.
3. **Nie mylić z KIL** (etap K): te same bajty w NMOS mają inne znaczenie.
4. NOP-y nie mogą zmieniać rejestrów ani flag — wektory to sprawdzają.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/44 | c02 `missingInCode` = 44 | etap otwarty; decyzja `$5C` do zapisania |
