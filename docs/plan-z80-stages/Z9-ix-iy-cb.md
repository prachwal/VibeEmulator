# Etap Z9 — `DD CB d op` i `FD CB d op`

Poziom: **C** · Pliki wektorów: **512** (256 `dd cb __ NN` + 256 `fd cb __ NN`) · Nowe: sekwencja z przesunięciem przed opkodem

## Zakres

Wszystkie 256 wartości `op` dla obu prefiksów. Nazwa pliku to `dd cb __ NN.json`; znaczenie `__` (miejsce na
przesunięcie `d`) jest **wnioskowane z nazwy — potwierdź w polu `name` testu** przed napisaniem loadera.

| grupa | `op` | uwagi |
|---|---|---|
| rotacje/przesunięcia na `(IX+d)` | `00`–`3F` | wynik także kopiowany do rejestru dla `op` ≠ `x6` (nieudokumentowane) |
| `BIT n,(IX+d)` | `40`–`7F` | bez zapisu |
| `RES n,(IX+d)` | `80`–`BF` | |
| `SET n,(IX+d)` | `C0`–`FF` | |

## Nowe elementy

- Kolejność bajtów: `DD CB d op` — **przesunięcie przed opkodem**; opkod ostatni jest odczytem pamięci, nie M1
  i nie zwiększa `r` w badanych wektorach. `DD` i `CB` zwiększają `r` łącznie o 2.
- Wspólna ścieżka z Z6 (rotacje, `BIT`/`RES`/`SET`) po obliczeniu adresu `IX/IY+d` — nie kopiuj logiki bitowej.
- Kopiowanie wyniku do rejestru (`op` ≠ `x6`): `LD r` równolegle do zapisu pamięci.

## Kryteria akceptacji

- [x] 512 plików wektorów w repo i w manifeście, testy zielone.
- [x] Wzorzec `__` w nazwach potwierdzony na polu `name`, wynik w dzienniku.
- [x] Pełne wektory poza repo: wynik wpisany albo „nie uruchomiono”.
- [x] Definicja ukończenia dla projektu: build Debug/Release, testy i formatowanie.

## Pułapki

1. **`BIT n,(IX+d)`:** `X`/`Y` z górnego bajtu adresu efektywnego, zgodnie z badaną próbką wektorów.
2. **Wyniki zapisywane do rejestrów** to zachowanie nieudokumentowane; wektory je zawierają, nie „czyść” ich.
3. **Wszystkie 512 plików przechodzą przez jedną ścieżkę** — błąd w obliczeniu adresu psuje całą grupę naraz,
   więc zacznij od `op = 06` i `46`.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | `DD CB d op` / `FD CB d op` | 512/512 plików; 10 240/10 240 przypadków próbki | testy Z9: 512/512; pełnych wektorów (1000/opkod) nie uruchomiono | `__` w nazwach odpowiada bajtowi `d`, potwierdzone w polu `name`; końcowy `op` nie jest M1 i nie zwiększa `R`; fetch prefiksów `DD` i `CB` zwiększa `R` łącznie o 2. `BIT` trwa 20 T-state, pozostałe operacje 23; wynik jest kopiowany do rejestru dla kodu różnego od 6, z wyjątkiem `BIT`. |
