# Etap Z8 — prefiksy `DD` i `FD` (IX, IY)

Poziom: **C** · Pliki wektorów: **504** (252 `dd NN` + 252 `fd NN`) · Nowe: rejestry indeksowe, adresowanie `(IX+d)`

## Zakres

W zestawie jest po **252** pliki dla `DD` i `FD` — wszystkie bajty poza `cb`, `dd`, `ed`, `fd`. Obejmuje to
dwie klasy:

| klasa | opis | uwagi |
|---|---|---|
| instrukcje z `HL`/`H`/`L`/`(HL)` | zamiana na `IX`/`IY`/`IXH`/`IXL`/`(IX+d)` | m.in. `LD IX,nn`, `ADD IX,rr`, `INC IX`, `PUSH IX`, `JP (IX)`, `EX (SP),IX`, `LD (IX+d),n` |
| pozostałe opkody | prefiks działa jak dodatkowe pobranie (4 T-state) przed zwykłą instrukcją | pliki istnieją, więc są weryfikowane |

## Nowe elementy

- Prefiks `DD`/`FD` zwiększa `r` i wydłuża instrukcję o pobranie M1; wskaźnik indeksu (`IX`/`IY`) wybierany raz,
  bez duplikowania logiki `HL` (D2: algorytmiczne dekodowanie).
- Adresowanie `(IX+d)`: odczyt `d`, 5 T-state wewnętrznych dla operacji na pamięci; `LD (IX/IY+d),n` rozdziela dwa stany wewnętrzne przed i po odczycie immediate (z wektorów); `WZ = IX/IY+d`.
- Operacje na połówkach `IXH`/`IXL`/`IYH`/`IYL` — nieudokumentowane, ale mają pliki wektorów.

## Kryteria akceptacji

- [x] 504 pliki wektorów w repo i w manifeście, testy zielone.
- [x] Test jednostkowy: `LD H,(IX+d)` ładuje do `H`, **nie** do `IXH` (instrukcje z `(IX+d)` nie podmieniają `H`/`L`).
- [x] Pełne wektory poza repo: nie uruchomiono; wynik odnotowano w dzienniku.
- [x] Definicja ukończenia: build Debug/Release bez ostrzeżeń, pełne testy Release i formatowanie zielone.

## Pułapki

1. **Łańcuchy prefiksów** (`DD DD`, `DD FD`, `DD ED`) nie mają plików wektorów — zachowanie poza pokryciem; zapisz
   decyzję w dzienniku, nie zgaduj.
2. **`(IX+d)` z przesunięciem ze znakiem** (`-128`…`+127`), zawijanie adresu 16-bitowego.
3. **`DD`/`FD` przed instrukcją bez `HL`** nie zmienia jej semantyki, tylko czas (+4 T-state) i `r`.
4. **Dwie kopie logiki dla `IX` i `IY`** to najczęstsze źródło rozjazdu; użyj jednej ścieżki z wyborem rejestru.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | `DD`/`FD`: rejestry indeksowe, podstawienia `HL`/`H`/`L`, `(IX/IY+d)` i prefiks ignorowany | 504/504 plików, 10 080/10 080 testów próbki | `Z80Z8VectorTests`: 504/504; test `LD H,(IX+d)`; pełny projekt Z80: 1178/1178 | 20 pierwszych rekordów z przypiętego commita `ebe1875…`; pełnych 1000/opkod poza repo nie uruchomiono. Łańcuchy `DD DD`, `DD FD`, `DD ED` bez wektorów: zachowanie poza pokryciem, niezaimplementowane i niezweryfikowane. |
