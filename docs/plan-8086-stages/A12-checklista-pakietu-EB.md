# A12 — checklista pakietu segment-prefiksowanego `JMP EB`

Stan: 2026-10-25, branch `A12-8086-pelna-weryfikacja`. Ta osobna lista jest źródłem bieżącego postępu pakietu. Po każdym zamkniętym punkcie należy od razu zaznaczyć `[x]`, zapisać dowód/commit i zaktualizować procent.

## Wskaźniki

- **Zgodność z przypiętym korpusem:** 92,0% — 594 203/646 000 `passed`; 51 797 jawnych `skipped`; 0 rozbieżności, timeoutów i błędów parsowania. Pełny raport: `/tmp/a12-eb-final-corpus-report.json`.
- **Ukończenie pakietu EB:** 8/8 (100%). Ten licznik opisuje zadania pakietu, nie zgodność całego CPU.
- Pin źródła: `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; plan: `37ae6a6`.

## Punkty do zamknięcia

- [x] Zapisać plan oraz przypiąć cztery grupy źródłowe (1 018 przypadków). Wynik korpusu: 91,8% (593 185/646 000).
- [x] Dodać cztery grupy fixture’ów i test; 80/80 przechodzi. Korpus: 91,8%.
- [x] Zaimplementować zachowanie EB z prefiksem segmentowym na podstawie śladów. Korpus przed pełnym przebiegiem pozostaje 91,8%.
- [x] Zweryfikować pełne filtry: prefiks EB 1 018/1 018, plik EB 2 000/2 000 oraz regresję E8–EA+EB 4 034/4 034 i 8 000/8 000. Korpus: 91,8%.
- [x] Uruchomić pełny korpus: 594 203/646 000 (92,0%), 51 797 skipów, 0 błędów; +1 018 `passed`.
- [x] Zaliczyć zbiorcze bramki: Debug/Release 0 ostrzeżeń i błędów, Release 22 887/22 887, `dotnet format`, format verify, OpcodeInfo 512/512, manifest 2 026/35 829, `git diff --check`. Korpus: 92,0%.
- [x] Uzgodnić raport, macierz ISA, README i checklisty; utrzymać stan 594 203/646 000 (92,0%).
- [x] Zapisać implementację osobnym commitem `d3b57a9`. Korpus: 92,0%.
- [x] Zapisać dokumentację osobnym commitem `e1663ff`; hash jest w raporcie, checklista zamknięta. Korpus: 92,0%.

## Historia aktualizacji

| data | ukończony punkt | dowód | korpus | pakiet |
|---|---|---|---:|---:|
| 2026-10-25 | Plan i inwentarz | `37ae6a6`; 1 018 przypadków / 4 grupy | 91,8% | 12,5% (1/8) |
| 2026-10-25 | Fixture’y | 80/80; cztery źródłowe grupy | 91,8% | 25,0% (2/8) |
| 2026-10-25 | Implementacja | dekoder i wykonanie EB | 91,8% | 37,5% (3/8) |
| 2026-10-25 | Pełne filtry i kontrola regresji | EB 1 018/1 018, pliki 2 000/2 000, E8–EA+EB 4 034/4 034 oraz 8 000/8 000 | 91,8% | 50,0% (4/8) |
| 2026-10-25 | Pełny korpus | `/tmp/a12-eb-final-corpus-report.json`; 594 203 passed, 51 797 skipped, zero błędów | 92,0% | 62,5% (5/8) |
| 2026-10-25 | Bramki jakości | 22 887 testów; buildy, format, OpcodeInfo i manifest zielone | 92,0% | 75,0% (6/8) |
| 2026-10-25 | Synchronizacja dokumentacji pakietu | raport, macierz ISA, README, checklisty i stan wznowienia uzgodnione | 92,0% | 87,5% (7/8) |
| 2026-10-25 | Osobny commit implementacji | `d3b57a9`; plan `37ae6a6` pozostaje osobny | 92,0% | 87,5% (7/8) |
| 2026-10-25 | Osobny commit checkpointu dokumentacji i zamknięcie EB | `e1663ff`; raport zawiera hashe planu, implementacji i dokumentacji | 92,0% | 100% (8/8) |

## Polecenia odtworzenia

Szczegółowe polecenia źródłowe i bramki znajdują się w [planie EB](A12-prefixed-short-jump-eb-plan.md). Pełny raport pakietu: [A12-prefixed-short-jump-eb-report.json](A12-prefixed-short-jump-eb-report.json).
