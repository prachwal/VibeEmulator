# Etap K — KIL (JAM)

Poziom: **B** (domknięcie) · Opkody: **12** · Nowe tryby: 0 · Nowe fazy: 2 · Nowe akcje: 1

Po etapie: NMOS **256/256** bajtów.

## Zakres

`02 12 22 32 42 52 62 72 92 B2 D2 F2`

Przed etapem nieznany bajt kończył się `Jammed` bez wpisu w tabeli: `--verify-code` zgłaszał brak, a testy
wektorowe nie miały czego porównać. Po etapie każdy bajt NMOS ma wpis, a sekwencja dostępów jest zgodna
z wektorami (patrz niżej).

## Nowe elementy

Stan po realizacji (opis faktów, nie planu):

- **Akcja `CpuAction.Kil`** i **rodzaj `OpKind.Kil`** w tabeli wariantu NMOS; 12 pól w
  `src/Mos6502/OpcodeCatalog/OpcodeCatalog.Nmos.Kil.cs`. `InternalCycles = 9` to liczba dostępów po fetchu i dummy read.
- Rdzeń: `Phase.KilDummy` (dummy read spod PC) i `Phase.KilRead` (9 dostępów: `$FFFF`, `$FFFE`, `$FFFE`,
  `$FFFF` ×6), po ostatnim cyklu `Jammed = true`. Zatrzymanie nie jest wyjątkiem, a `Tick` po jamie nadal
  liczy cykle (bez dostępów do magistrali) — VIC-II nie może się zatrzymać.
- CSV nie ma dla KIL kolumny `cykle_bazowe`, więc bramka `--verify-code` nie porównuje cykli; wartość 11
  pochodzi z kolumny `wektory_cykle` i z wektorów. Formuła w `CodeAnnotations` (`OpKind.Kil` → `1 + 1 + 9`)
  służy tylko diagnostyce.
- Sekwencja dostępów **nie jest** wzięta z datasheetu ani z pamięci: 12 × 10 000 wektorów pokazuje ten sam
  wzorzec (`$FFFF`, `$FFFE`, `$FFFE`, potem `$FFFF`), więc rdzeń go odtwarza.

## Kryteria akceptacji

- [x] 12 wpisów w klasie `Nmos`; `--verify-code`: `missingInCode` = **0** (`consistent: true`, 512/512 wierszy CSV).
- [x] Próbki wektorów dla 12 opkodów — pliki istnieją w źródle (`6502/v1/02.json` … `f2.json`), po 100 testów
  w `Vectors/nmos6502/`.
- [x] Test: po `Kil` rdzeń nie zmienia stanu i nie zgłasza wyjątku; `Jammed == true`
  (`StateMachineCpuCoreTests.Kil.cs`, 12 opkodów + wzorzec odczytów).

## Pułapki

1. **Te same bajty w 65C02 nie są KIL** — `92 B2 D2 F2` to prawdziwe instrukcje (etap H). Wpis tylko w wariancie NMOS.
2. KIL w 65C02 istnieje jako `STP` (`DB`) — nie mylić.
3. Nie „naprawiać” zachowania KIL przez wyjątek — emulator ma się zatrzymać, nie wywrócić.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/12 | `missingInCode` NMOS = 12 | etap otwarty |
| 2026-10-22 | katalog + rdzeń | 12/12 | `--verify-code`: `consistent: true`, `attributes: 361`, `matched: 512`, `mismatched: 0`, `missingInCode: 0` | `OpcodeCatalog.Nmos.Kil.cs`, `OpKind.Kil`, fazy `KilDummy`/`KilRead` |
| 2026-10-22 | wektory | 12/12 | próbki: 510/510 (256 NMOS + 254 65C02); pełne wektory: **120 000 / 120 000**, 0 pominięć | wzorzec: fetch, dummy spod PC, `$FFFF`, `$FFFE`, `$FFFE`, `$FFFF`×6; `PC + 1`, reszta stanu bez zmian |
| 2026-10-22 | testy jednostkowe | 12/12 | `VibeEmulator.Mos6502.Tests`: 727/727; cała solucja (Release): **788/788** | `StateMachineCpuCoreTests.Kil.cs`; testy „nieznany bajt” używają już wariantu z niepełną tabelą |

Po etapie: NMOS **256/256 bajtów** — poziom **B** zamknięty.
