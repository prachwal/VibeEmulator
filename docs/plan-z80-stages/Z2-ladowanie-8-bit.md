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
- Rejestr `WZ` aktualizowany przez te instrukcje (wartość bierz z pola `wz` wektora, nie z pamięci).
- Brak zmian w loaderze; manifest `Z80VectorManifest` zyskuje 77 opkodów.

## Kryteria akceptacji

- [ ] 77 plików wektorów w repo (próbka wg D4) i w manifeście.
- [ ] Testy wektorowe zielone: stan, `ram`, pełne `cycles`.
- [ ] Pełne wektory (1000 na plik) poza repo: wynik wpisany albo „nie uruchomiono”.
- [ ] Definicja ukończenia (build Debug/Release, testy, `dotnet format`).

## Pułapki

1. **`LD (HL),(HL)` nie istnieje** — to bajt `76` (HALT). Nie dekoduj `40`–`7F` jednym wzorem bez wyjątku.
2. **`WZ` po `LD A,(nn)`/`LD (nn),A`** — zachowanie rejestru bywa opisywane różnie w źródłach (niezweryfikowane);
   rozstrzyga pole `wz` w wektorach. Przy rozbieżności opisz ją, nie dobieraj formuły pod testy.
3. **Rejestr `r` (refresh)** rośnie przy pobraniu opkodu, nie przy odczycie/zapisie danych; sprawdź w polu `r` wektora.
4. **Kolejność rejestrów w polu 3-bitowym** (`B C D E H L (HL) A`) — jedno miejsce w kodzie, bez kopii.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
