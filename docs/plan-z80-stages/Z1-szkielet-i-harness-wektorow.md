# Etap Z1 — szkielet rdzenia i harness wektorów

Poziom: **fundament** · Pliki wektorów: **1** (`00`, NOP) · Nowe projekty: 2 · Nowe interfejsy: 1

Zanim powstanie pierwsza instrukcja, trzeba mieć: projekt rdzenia, stan rejestrów, magistralę portów i harness
porównujący `cycles` T-state po T-state. Etap kończy się jednym opkodem (`NOP`), ale **rozstrzyga decyzje D1–D7**
z [README.md](README.md), od których zależy reszta.

## Zakres

- Projekt `src/Z80` (`VibeEmulator.Z80`), zależny od `Memory` i `Cpu.Abstractions`; **nie** od `Mos6502` ani `Core`.
- `Z80State` (struct) z polami z wektorów: `pc sp a b c d e f h l i r ix iy`, cienie `af_ bc_ de_ hl_`, `wz`,
  `im`, `iff1`, `iff2`, `ei`, `p`, `q`. Nazwy pól wektora → pola stanu w jednym miejscu (loader).
- `Z80Core : ICpuCore<Z80State>`; `Tick()` = **jeden T-state**.
- `IPortBus` w `src/Memory` (`byte In(ushort port)`, `void Out(ushort port, byte value)`) wg D3.
- Rejestr pinów ostatniego T-state (adres, dana, piny) odczytywalny po `Tick()` — wg D1.
- Projekt testów `tests/VibeEmulator.Z80.Tests` (xunit jak w pozostałych), dodany do `VibeEmulator.slnx`.
- Loader wektorów: nazwy plików **ze spacjami** (`cb 00.json`, `dd cb __ 06.json`), pole `ports` opcjonalne,
  `null` w `cycles` dla adresu i dany.
- Pliki: `tests/VibeEmulator.Z80.Tests/Vectors/` — `LICENSE-SingleStepTests.txt`, `README.md` (commit źródła,
  liczba testów na plik wg D4, skrypt/komenda przycięcia), pliki `.json` wg `Content` + `CopyToOutputDirectory`.
- Opkod: tylko `00` (NOP, 4 T-state).

## Nowe elementy

- `src/Z80/VibeEmulator.Z80.csproj`, `Z80State.cs`, `Z80Core.cs` (+ pliki `partial` wg faz).
- `src/Memory/IPortBus.cs` (+ `FlatPorts` albo odpowiednik do testów).
- `tests/VibeEmulator.Z80.Tests/Z80VectorTests.cs`, `Z80VectorTestRunner.cs`, `Z80VectorManifest.cs`.
- `tests/VibeEmulator.Z80.Tests/Vectors/README.md` i `LICENSE-SingleStepTests.txt` (MIT).
- Wpis w `docs/plan-c64-cycle-exact.md` („Elementy spoza planu”) wskazujący na ten katalog.
- Sekcja o Z80 dodana do `.github/instructions/single-step-vectors.instructions.md` na wyraźną prośbę użytkownika;
  instrukcja obejmuje teraz format, próbkę i offline testing Z80.

## Rozstrzygnięcia przed implementacją

README źródła został przeczytany w całości z przypiętego commita. Opcje generatora: CMOS wyłączony, uproszczone impulsy
MRQ/RD i MRQ/WR przez 1 T-state, refresh na adresie podczas pobrania opkodu, adres magistrali nieoznaczony jako `null`
podczas wait/internal states. Wektory wygenerowano z Ares; autor źródła zaznacza, że nie są potwierdzone na krzemie.

| decyzja | rozstrzygnięcie | dowód / ograniczenie |
|---|---|---|
| D1 — porównanie `cycles` | adres (gdy nie `null`), dane i piny na każdym T-state; dodatkowo rzeczywiste wywołania pamięci muszą wystąpić w T-state z `r-m-`/`-wm-` | runner sprawdza próbkę `00`; `null` adres oznacza odłączone piny, więc adresu wtedy się nie porównuje |
| D2 — dekodowanie | dekoder algorytmiczny/fazowy, bez tablicy 256×N | decyzja architektoniczna; Z1 implementuje tylko `00`, pełnego dekodera jeszcze nie ma |
| D3 — przestrzeń portów | `IPortBus` w `src/Memory`; API `Input`/`Output` | `In`/`Out` powodują CA1716; `Memory` nie zależy od Z80 |
| D4 — rozmiar próbki | pierwsze 20 rekordów każdego pliku | `00.json` ma pierwsze 20 z 1000 rekordów źródłowych |
| D5 — `Pending` | liczba T-state do granicy instrukcji, 0 na granicy | NOP: runner asercjonuje wartości 3, 2, 1, 0 |
| D6 — `Stall()` | odpowiednik WAIT: zwiększa licznik czasu, nie zmienia stanu ani `Pending` | test `Stall_AdvancesClockWithoutChangingCpuState` |
| D7 — wariant | tylko NMOS, bez `Z80Variant` | Z1 używa przypiętego zestawu Z80 bez wariantu CMOS |

Na pierwszych 20 wektorach `00` NOP: `ei` może przejść 1→0, `p` i `q` są czyszczone, `wz` pozostaje bez zmian,
`r` inkrementuje się przy M1. To obserwacje dla NOP/próbki, nie uogólnienie na inne instrukcje.

## Kryteria akceptacji

- [x] D1–D7 rozstrzygnięte i wpisane w dzienniku oraz sekcji powyżej.
- [x] `dotnet build VibeEmulator.slnx` bez ostrzeżeń (`TreatWarningsAsErrors`), `dotnet format --verify-no-changes --severity warn` czysto. (Zweryfikowano 2026-10-22: Debug/Release, 0 ostrzeżeń; format bez zmian.)
- [x] Wektory `00`: próbka w repo przechodzi (pełny stan, `ram`, pełne `cycles`). (20/20; porównywane są wszystkie pola stanu, cała 64-KB pamięć, piny każdego T-state i rzeczywiste odczyty/zapisy pamięci.)
- [x] Testy loadera: `ports`, `null` w `cycles`, nazwa pliku ze spacjami. (3 testy Z80 zielone; loader obsługuje te kształty.)
- [x] Pełne wektory `00` (1000 testów) poza repo: wynik wpisany albo „nie uruchomiono”. (Nie uruchomiono; pełny zestaw nie został wykonany.)
- [x] `Tick()` bez alokacji w gorącej ścieżce. (Test po rozgrzewce: 10 000 T-state, 0 bajtów zaalokowanych przez bieżący wątek.)

## Pułapki

1. **T-state ≠ cykl 6502.** Nie przenoś `Cycles`/`Pending` z rdzenia 6502 bez sprawdzenia semantyki.
2. **Pola wektora są płaskie** (`af_`, nie `af'`; brak pary `af`) — mapowanie 8-bitowych `a`/`f` na pary zrób raz, w loaderze.
3. **`ram` zawiera tylko dotknięte adresy.** Pozostała pamięć ma być zerowana, nie losowa, żeby test był powtarzalny.
4. **Nazwy ze spacjami** psują ścieżki w skryptach shella bez cudzysłowów — przy przycinaniu użyj tablicy/`find -print0`.
5. **Nie edytuj treści wektorów** (zasada z `single-step-vectors.instructions.md`): przycinanie = wybór pierwszych N
   testów, bez zmiany formatowania.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-22 | szkielet + NOP | 1/1 opkodów etapu; próbka 20/1000 | build Debug/Release 0 ostrzeżeń; format czysto; testy solucji 1087 passed, w tym Z80 6/6 | D1–D7 zapisane; pełny stan/RAM/cykle i zdarzenia pamięci asercjonowane; pomiar Tick: 10 000 T-state, 0 B. Manifest i licencja MIT dodane. Pełne 1000 wektorów nie uruchomiono. |
