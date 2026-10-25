# A12 — audyt łańcucha branchy i commitów A0–A12

Audyt wykonany 2026-10-25 w checkoutcie `A12-8086-pelna-weryfikacja`. Dla każdego sąsiedniego etapu polecenie `git merge-base --is-ancestor A(n-1) A(n)` zwróciło sukces. Każdy branch jest więc oparty na poprzednim, a wszystkie etapy są osiągalne z A12.

| etap | branch | tip w chwili audytu | commity wprowadzone po poprzednim branchu |
|---|---|---|---:|
| A0 | `A0-8086-architektura` | `19ca357` | punkt bazowy |
| A1 | `A1-8086-wektory` | `16d8cfb` | 2 |
| A2 | `A2-8086-szkielet` | `9132b5f` | 1 |
| A3 | `A3-8086-transfery-8-bit` | `d095df0` | 8 |
| A4 | `A4-8086-alu-8-bit` | `1f98664` | 4 |
| A5 | `A5-8086-transfery-16-bit-i-stos` | `44e7c73` | 3 |
| A6 | `A6-8086-alu-16-bit-shifts` | `fef18d4` | 35 |
| A7 | `A7-8086-sterowanie-przeplywem` | `1e334c7` | 17 |
| A8 | `A8-8086-multiply-divide-bcd` | `3d09d01` | 21 |
| A9 | `A9-8086-string-io` | `a7a8768` | 33 |
| A10 | `A10-8086-prefixes-decoder` | `c5de77a` | 3 |
| A11 | `A11-8086-reset-przerwania-stany` | `750e51b` | 1 |
| A12 | `A12-8086-pelna-weryfikacja` | `2ed7eec` | 120 |

## Plany i rozdział zmian

- Pełna kaskada planów A0–A12 została zapisana przed implementacją w osobnym commicie `ffc9557` (`docs(plan): rozpisz kaskadę rdzenia Intel 8086`); zawiera 13 dokumentów etapów i indeks README.
- Branch A1–A12 wnosi osobne commity implementacyjne danego etapu. Katalog `docs/plan-8086-stages/` przechowuje plany, checklisty i wyniki etapów; wspólny commit `ffc9557` jest bazą planistyczną dla A0–A11, a nie osobnym commitem planu każdego późniejszego branchu.
- Dodatkowe pakiety A12, dla których połączono nowy kod po pełnym korpusie, mają osobne commity planu, implementacji i dokumentacji. Dla bieżącego programu integracyjnego: plan `79bb04c`, test/program `2dc4968`; dokumentacja audytu jest w osobnym commicie po końcowych bramkach.
- Niepozostawione wcześniej kryterium alokacji A2 ma osobny testowy commit `2ed7eec`; pomiar Release: 1 000 iteracji warmup, 10 000 iteracji mierzonych, 0 bajtów alokacji.
- Historia branchy nie jest przepisywana. Tabela podaje tipy istniejące przed końcowym commitem checklist/raportu A12.

## Zakres i ograniczenia audytu

To potwierdza istnienie i kolejność branchy oraz osiągalność ich commitów, nie certyfikuje pełnej zgodności wszystkich instrukcji/maszyn. Postęp wektorów wynosi 646 000/646 000 (100%) dla pinu P80C86A-2; ograniczenia sprzętowe są jawnie wymienione w [rejestrze A10/A11](A12-audyt-ograniczen-a10-a11.md).
