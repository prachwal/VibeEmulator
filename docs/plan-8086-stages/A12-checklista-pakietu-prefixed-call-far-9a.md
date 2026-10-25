# A12 — checklista segment-prefiksowanego `CALL FAR 9A`

Stan na 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Zaznaczaj punkt i odświeżaj procent oraz dowód po każdym ukończonym punkcie.

## Stan początkowy

- Pełny korpus: **644 989/646 000 (99,8%)**, 1 011 skipów prefiksowanego `CALL FAR 9A`, bez mismatchy/timeoutów/parse errors.
- Źródło `SingleStepTests/8086` pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, `v1/9A.json.gz`, 2 000 rekordów.
- Segment-prefiksowane rekordy: ES 259, CS 252, SS 246, DS 254; razem 1 011. Nieprefiksowane 989.
- Plan przed kodem: [A12-prefixed-call-far-9a-plan.md](A12-prefixed-call-far-9a-plan.md).
- Pakiet: **1/9 (11%)** — plan i inwentarz.

## Testy i kryteria

- [x] Inwentarz źródła, profile wstępne, zakres i plan zapisane przed kodem; plan commit `DO_UZUPELNIENIA`.
- [ ] Dodać 80 źródłowych fixture’ów (20 × 4 prefiksy), lokalny test porównuje pełny stan, RAM, kolejkę i każdy T-state.
- [ ] Odtworzyć profile kolejki 5/6, długości 31/33/39/41, fetch i zapis stosu z pełnego źródła.
- [ ] Zaimplementować dekodowanie i cycle-exact segment-prefiksowany far CALL, bez zmian nieprefiksowanego `9A`.
- [ ] Pełny filtr: 1 011/1 011; pełny plik: 2 000/2 000; zero błędów.
- [ ] Pełny korpus: 646 000/646 000, zero skipów i rozbieżności.
- [ ] Bramki jakości, walidator manifestu i synchronizacja dokumentów zielone.
- [ ] Osobny commit implementacji; hash zapisany.
- [ ] Osobny commit dokumentacji i zamknięcie checklisty; hashe zapisane.

## Historia aktualizacji

| data | punkt | dowód | passed / 646 000 | korpus | pakiet |
|---|---|---|---:|---:|---:|
| 2026-10-25 | Inwentarz i plan przed kodem | 1 011 prefiksowanych rekordów, per-prefix 259/252/246/254; plan do osobnego commitu | 644 989 | 99,8% | 11% (1/9) |

Po każdym punkcie dopisz dowód i zachowaj ostatni kompletny wynik pełnego korpusu, dopóki nie powstanie nowy pełny raport.
