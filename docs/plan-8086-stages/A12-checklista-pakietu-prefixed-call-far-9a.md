# A12 — checklista segment-prefiksowanego `CALL FAR 9A`

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Zaznaczaj punkt i odświeżaj procent oraz dowód po każdym ukończonym punkcie.

## Stan początkowy

- Pełny korpus: **644 989/646 000 (99,8%)**, 1 011 skipów prefiksowanego `CALL FAR 9A`, bez mismatchy/timeoutów/parse errors.
- Źródło `SingleStepTests/8086` pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, `v1/9A.json.gz`, 2 000 rekordów.
- Segment-prefiksowane rekordy: ES 259, CS 252, SS 246, DS 254; razem 1 011. Nieprefiksowane 989.
- Plan przed kodem: [A12-prefixed-call-far-9a-plan.md](A12-prefixed-call-far-9a-plan.md).
- Pakiet: **8/9 (89%)**. Plan `c365fac`, implementacja `3198647`; pozostaje checkpoint dokumentacji.

## Testy i kryteria

- [x] Inwentarz źródła, profile wstępne, zakres i plan zapisane przed kodem; plan commit `DO_UZUPELNIENIA`.
- [x] Dodać 80 źródłowych fixture’ów (20 × 4 prefiksy); lokalny test porównuje pełny stan, RAM, kolejkę i każdy T-state. Wynik 80/80.
- [x] Odtworzyć profile kolejki 5/6, długości 31/33/39/41, fetch i zapis stosu z pełnego źródła; pełne porównanie 1 011 śladów zielone.
- [x] Zaimplementować dekodowanie i cycle-exact segment-prefiksowany far CALL; pełny plik 9A pozostaje zielony.
- [x] Pełny filtr 1 011/1 011 i pełny plik 9A 2 000/2 000; zero skipów, rozbieżności, timeoutów i błędów parsowania.
- [x] Pełny korpus: **646 000/646 000 (100%)**, zero skipów i rozbieżności.
- [x] Bramki jakości i synchronizacja dokumentów zielone: Debug/Release 0/0; Release 23 250/23 250; format verify; OpcodeInfo 512/512; manifest 2 050/36 549; README, macierz ISA i checklisty A12 zaktualizowane.
- [x] Osobny commit implementacji `3198647`; plan `c365fac` zapisany oddzielnie.
- [ ] Osobny commit dokumentacji i zamknięcie checklisty; hashe zapisane.

## Historia aktualizacji

| data | punkt | dowód | passed / 646 000 | korpus | pakiet |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Inwentarz i plan przed kodem | 1 011 prefiksowanych rekordów, per-prefix 259/252/246/254; plan `c365fac` | 644 989 | 99,8% | 11% (1/9) |
| 2026-10-25 | Fixture’y, profile i implementacja | 80/80 fixture’ów; zachowanie kolejki 5/6, stos i transfer far odtworzone z SST | 644 989 | 99,8% | 44% (4/9) |
| 2026-10-25 | Pełny filtr i pełny plik 9A | prefiksowany 1 011/1 011; cały 9A 2 000/2 000; bez błędów | 644 989 | 99,8% | 56% (5/9) |
| 2026-10-25 | Pełny korpus | 646 000/646 000; 0 skipów/mismatchy/timeoutów/parse errors; raport `A12-prefixed-call-far-9a-report.json` | 646 000 | 100% | 67% (6/9) |
| 2026-10-25 | Bramki jakości i dokumentacja | Debug/Release 0/0; Release 23 250/23 250; format, OpcodeInfo 512/512, manifest 2 050/36 549; macierz bez known-gap | 646 000 | 100% | 78% (7/9) |
| 2026-10-25 | Osobny commit implementacji | `3198647`; plan `c365fac` osobno | 646 000 | 100% | 89% (8/9) |

Po każdym punkcie dopisz dowód i zachowaj ostatni kompletny wynik pełnego korpusu, dopóki nie powstanie nowy pełny raport.
