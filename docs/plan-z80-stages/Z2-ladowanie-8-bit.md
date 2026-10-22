# Etap Z2 — ładowanie 8-bitowe

Poziom: **A (Z80 bez prefiksów)** · Pliki wektorów: **77** · Nowe M-cykle: odczyt, zapis · Nowe akcje: 0 flag

Pierwszy etap z dostępem do pamięci poza pobraniem opkodu: dodaje M-cykle odczytu i zapisu oraz adresowanie
przez `HL`, `BC`, `DE` i `nn`. Nie dotyka flag, więc błędy czasu i `WZ` widać bez szumu ALU.

## Zakres

| grupa | opkody | uwagi |
|---|---|---|
| `LD r,r'` i `LD r,(HL)`, `LD (HL),r` | `40`–`7F` **bez** `76` (63 opkody) | `76` to HALT, etap Z5 |
| `LD r,n`, `LD (HL),n` | `06 0E 16 1E 26 2E 36 3E` (8) | |
| `LD A,(BC)`, `LD A,(DE)`, `LD A,(nn)` | `0A 1A 3A` (3) | |
| `LD (BC),A`, `LD (DE),A`, `LD (nn),A` | `02 12 32` (3) | |

Razem 77 plików. Po etapie: 78/252 opkodów bez prefiksów (z `00`).

## Nowe elementy

- M-cykle `MemRead` i `MemWrite` (po 3 T-state, wg wektorów) w maszynie stanów rdzenia.
- Rejestr `WZ` aktualizowany zgodnie z polem `wz` w próbkach przypiętego commita; szczegóły wynikające z próbki są w Pułapkach.
- Brak zmian w loaderze; manifest `Z80VectorManifest` zyskuje 77 opkodów.

## Kryteria akceptacji

- [x] 77 plików wektorów w repo (próbka wg D4) i w manifeście. (77/77 nowych opkodów, 20 rekordów/plik; wraz z bazowym `00`: 78 plików/1560 rekordów. Manifest wymienia wszystkie 78.)
- [x] Testy wektorowe zielone: pełny stan, `ram`, pełne `cycles`. (1540/1540 rekordów Z2; każdy T-state porównuje piny/dane i rzeczywiste accessy RAM, asercjonowane są wszystkie pola stanu oraz 64-KB RAM. `WZ` jest sprawdzany.)
- [x] Pełne wektory (1000 na plik) poza repo: wynik wpisany albo „nie uruchomiono”. (Nie uruchomiono; w repo próbki 20/opkod.)
- [x] Definicja ukończenia (build Debug/Release, testy, `dotnet format`). (2026-10-22: build Debug/Release, 0 ostrzeżeń; testy solucji 1164 passed; `dotnet format --verify-no-changes --severity warn` czysto.)

## Pułapki

1. **`LD (HL),(HL)` nie istnieje** — to bajt `76` (HALT). Nie dekoduj `40`–`7F` jednym wzorem bez wyjątku.
2. **`WZ`** — w pierwszych 20 wektorach/opkodzie `LD A,(BC/DE)` ustawia się na para+1; `LD (BC/DE),A` ustawia
   `(A<<8)|((para+1)&$FF)`; `LD A,(nn)` ustawia się na `nn+1`; `LD (nn),A` na `(A<<8)|((nn+1)&$FF)`.
   Pozostałe instrukcje Z2 zostawiają `WZ` bez zmian w próbce. To ustalenia z wektorów `SingleStepTests/z80`
   z przypiętego commita, nie potwierdzenie zachowania na krzemie.
3. **Rejestr `r` (refresh)** rośnie przy pobraniu opkodu, nie przy odczycie/zapisie danych; sprawdź w polu `r` wektora.
4. **Kolejność rejestrów w polu 3-bitowym** (`B C D E H L (HL) A`) — jedno miejsce w kodzie, bez kopii.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | LD 8-bit, wszystkie rodziny Z2 | 77/77 opkodów; próbka 20/20 z każdego pliku | testy Z80 83/83 (77 teorii × 20 wektorów + 6 testów Z1); build Debug/Release 0 ostrzeżeń; format czysto | Dodano manifest i próbki. Naprawiono M1 (fetch w T1), pojedyncze accessy na T-state, `LD (HL),n` = 10 T-state i aktualizację WZ zgodną z próbkami. Pełnych zestawów 1000/opkod nie uruchomiono. |
