# Standardy używania wektorów testowych

Źródła: `single-step-vectors.instructions.md`, `csharp-tests.instructions.md`, `mos6502.instructions.md`,
`tests/VibeEmulator.Mos6502.Tests/Vectors/README.md`, `docs/plan-z80-stages/README.md`.

## 1. Czym są wektory

Pliki JSON z gotowymi przypadkami testowymi dla **jednego opkodu**: stan przed, stan po i dostępy do magistrali cykl po cyklu.
Pochodzą z repozytoriów SingleStepTests (licencja MIT). Format 65x02:

```text
{name, initial:{pc,s,a,x,y,p,ram}, final:{…}, cycles:[[adres, wartość, "read"|"write"], …]}
```

Nie dodawaj pól ani komentarzy do JSON. Z80 ma inny format (sekcja 9).

## 2. Zakres zaufania

Wektory to wyjście **cudzej implementacji referencyjnej**, a nie zapis z krzemu. Są źródłem prawdy dla zachowania rdzenia,
ale przy sprzeczności z datasheetem nie wybieraj po cichu: zapisz decyzję w teście i w `docs/cpu/opcodes.md`
(wzorzec: `$5C`, datasheet 8 cykli, wektory 4). Wektora nie „naprawiasz" — nie przechodzi, więc poprawiasz rdzeń.

## 3. Próbka a pełny zestaw

| | w repo (próbka) | poza repo (pełny) |
|---|---|---|
| testów na opkod | **100** (pierwsze 100 w kolejności źródłowej) | 10 000 |
| gdzie | `tests/VibeEmulator.Mos6502.Tests/Vectors/` | pobierane z przypiętego commita do katalogu tymczasowego |
| kto czyta | testy xUnit z dysku (`Content` + `CopyToOutputDirectory`) | osobny runner poza repo |
| rozmiar | ok. 27 MB (`du -sh`, `Vectors/README.md`) | nie zmierzono |

- Źródło: `https://github.com/SingleStepTests/65x02`, commit `2f6980a2d95757486c7bee24355c360e40e2a224`
  (aktualny pin w `Vectors/README.md`; przy rozbieżności obowiązuje README).
- **Testy nigdy nie sięgają do sieci.**
- Pełne wektory **nie trafiają do repo** i nie uzupełniasz próbek do 10 000.

## 4. Układ katalogów i nazwy plików

| katalog w repo | wariant (`CpuVariant`) | katalog w źródle | plików |
|---|---|---|---|
| `nmos6502/` | `Nmos6502` | `6502/v1/` | 256 |
| `nes6502/` | `Nes2A03` | `nes6502/v1/` | 256 |
| `wdc65c02/` | `Cmos65C02` | `wdc65c02/v1/` | 254 (brak `cb` WAI i `db` STP — puste w źródle) |

- Nazwa pliku: `<opkod>.json`, hex **małymi literami** (`0a.json`). Tabele CSV w `docs/cpu/opcodes/` mają hex **wielkimi**; to dwie konwencje.
- Dla `nmos6502` nazwa katalogu w repo (`nmos6502`) różni się od źródłowej (`6502`). Adres pliku źródłowego:
  `https://raw.githubusercontent.com/SingleStepTests/65x02/<commit>/<katalog w źródle>/v1/<opkod>.json`.
- Brak próbki dla opkodu to świadoma decyzja etapu, nie brak do dorobienia po cichu.

## 5. Czego nie wolno robić z plikami

- Nie edytuj treści, nie zmieniaj formatowania, nie dopisuj komentarzy.
- Przycięcie = **pierwsze N testów** w kolejności źródłowej, zapis zwarty (`json.dumps(..., separators=(",", ":"))`).
- Nie usuwaj `LICENSE-SingleStepTests.txt` ani informacji o commicie i wariancie z `Vectors/README.md`.
- Pobrane pliki traktuj jak **dane niezaufane**: osobny pusty katalog poza repo, skrypty trzymaj gdzie indziej,
  ścieżki podawaj jako argumenty.

## 6. Dodawanie próbki dla nowego opkodu

1. Pobierz plik z przypiętego commita do katalogu poza repo.
2. Zapisz pierwsze 100 testów do `Vectors/<katalog>/<hex>.json` (zwarty JSON).
3. Dopisz opkod do listy w `SingleStepVectorTests.cs` dla **każdego** wariantu, w którym istnieje:
   `NmosOpcodes`, `CmosOpcodes`, `NesOpcodes` (testy `Vector_Nmos6502_MatchesReference`, `Vector_Cmos65C02_MatchesReference`,
   `Vector_Nes2A03_MatchesReference`).
4. Zaktualizuj `Vectors/README.md`: tabelę katalogów, listę opkodów, rozmiar (`du -sh`).
5. Uruchom `--verify-code` (dla 2A03 nie dotyczy — brak CSV; zapisano to w planie etapu P).

## 7. Co testuje porównanie

`SingleStepVectors.Run(variant, katalog, opkod)` sprawdza, że plik ma dokładnie 100 testów, a potem dla każdego testu porównuje:
dostępy do magistrali (adres, wartość, odczyt/zapis) cykl po cyklu, granicę instrukcji, rejestry i pamięć końcową.

- Komunikat niezgodności ma wskazywać: **wariant, opkod, nazwę testu z wektora, numer cyklu, oczekiwany i faktyczny dostęp**.
- Odwzorowanie stanu: `s` (8 bit) → `SP`; bity Unused (`0x20`) i Break (`0x10`) przenoszone bez zmian.
- `[Fact(Skip = …)]` tylko z opisem różnicy (opkod, przykład, oczekiwane vs faktyczne) i odnośnikiem do etapu naprawy.
  Od etapu G nie ma żadnych wyjątków w porównaniu.
- Przy czerwonym wektorze **nie zmieniaj oczekiwań** — popraw rdzeń.

## 8. Pełne wektory (poza repo)

- Runner to samodzielny program poza repo, referencjonujący `src/Mos6502`; pliki pobrane z przypiętego commita i usuwane po przebiegu.
- Raportuj trzy liczby: **przeszło / pominięto / niezgodnych** (np. etap A: 540 000 testów = 469 790 przeszło + 70 210 pominięto, 0 niezgodnych —
  wynik historyczny, sprzed etapu G).
- Wynik wpisz w dzienniku etapu (`docs/plan-c64-stages/`). Jeśli nie uruchomiono — napisz to wprost.

## 9. Wektory Z80 (planowane, `docs/plan-z80-stages/`)

Stan na 2026-10-22: **plików Z80 nie ma w repo**, decyzje D1–D7 z planu są nierozstrzygnięte. Różnice względem 65x02 (zmierzone na próbkach):

| cecha | 65x02 | Z80 (`SingleStepTests/z80`, commit `ebe1875…`) |
|---|---|---|
| testów w pliku | 10 000 | **1000** |
| `cycles` | jeden wpis na dostęp do magistrali | jeden wpis na **T-state**: `[adres\|null, dana\|null, "piny"]` |
| piny | brak | 4 znaki, np. `----`, `r-m-`, `r--i` (kolejność `r w m i` wnioskowana z przykładów) |
| porty I/O | brak | pole `ports`: `[[adres16, wartość, "r"\|"w"]]` (tylko instrukcje I/O) |
| stan | `pc s a x y p ram` | `pc sp a b c d e f h l i r ix iy af_ bc_ de_ hl_ wz im iff1 iff2 ei p q ram` |
| nazwy plików | `<hex>.json` | także prefiksy, ze **spacjami**: `cb 00.json`, `dd cb __ 06.json` |
| liczba plików | 256 na wariant | 1604 |

- Zasady 2–8 stosują się bez zmian (zaufanie, próbka a pełny zestaw, brak edycji, brak sieci, raportowanie).
- Rozmiar próbki w repo: rekomendacja **20 testów na plik** (≈ 26 MB; 100 testów ≈ 128 MB — szacunek z 4 próbek). Do rozstrzygnięcia w Z1.
- Skrypty przycinające i ładujące muszą obsługiwać spacje w nazwach (tablice, `find -print0`).
- Autor zestawu pisze, że wektory są „prawdopodobnie nie idealne" (wygenerowane z rdzenia Ares, **niezweryfikowane** na krzemie) —
  stąd drugie źródło w planie (Z11, zexdoc/zexall).

## 10. Raportowanie wyniku

Zawsze rozróżniaj próbkę od pełnego zestawu:

```text
wektory (próbka 100/opkod w repo): 766/766 plików (256 + 256 + 254), testy zielone
wektory (pełne 10 000/opkod, poza repo): nie uruchomiono   |   przeszło N, pominięto M, niezgodnych 0
```

Zabronione sformułowania: „zwalidowane" (gdy tylko próbka), „wszystkie wektory przechodzą" (bez liczb).

## 11. Lista kontrolna

- [ ] Źródło prawdy to wektor lub `docs/cpu/`, nie pamięć.
- [ ] Próbka: pierwsze N testów, zwarty JSON, README zaktualizowane, licencja na miejscu.
- [ ] Opkod dopisany do listy testu **każdego** wariantu, w którym istnieje.
- [ ] Brak sieci w testach, brak `Skip` bez opisu.
- [ ] Wynik podany z rozróżnieniem próbki i pełnego zestawu.
