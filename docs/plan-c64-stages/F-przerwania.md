# Etap F — przerwania

Poziom: **A** (poprawność funkcjonalna) · Opkody: **0** · Nowe fazy: 1 · Nowe akcje: 0

Bez tego etapu poziom A jest **niepełny**: `BRK` ustawia tylko flagę `B`, nie odkłada `PC` ani nie skacze przez
wektor. Procent opkodów się nie zmienia — zmienia się poprawność.

## Zakres

- Pełny `BRK`: odłożenie `PC` (już po inkrementacji?), `P`, wektor `$FFFE`/`$FFFF`, ustawienie `I`.
- Wejścia `IRQ` i `NMI` w rdzeniu: **piny** wywoływane z `C64Machine.Tick()` (jak `AEC`/`Stall`), nie `if` w fazie.
- `NMI` ma wektor `$FFFA`/`$FFFB` i jest nieblokowalne; `IRQ` respektuje `I` i przybywa z `C64Bus`/VIC/CIA.
- `RTI` (etap E) musi zdejmować stan w kolejności zgodnej z tym, co odkłada przerwanie.

## Dane do potwierdzenia

Kolejność zapisów na stosie, moment próbkowania linii `IRQ`, zachowanie przy `BRK` z `I=1`, znaczenie bitu `B`
na stosie — **z testu przerwń Dormanna** (harness H4 w planie) i wektorów, nie z pamięci.

## Kryteria akceptacji

- [ ] Test przerwań Dormanna: rejestr zwrotny `$BFFC`, bity IRQ = 0, NMI = 1. **Harness H4 nie uruchomiony**
      (binarium GPL trzeba zasemblować; poza zakresem tego etapu — zob. dziennik).
- [x] Test: `BRK` → wektor `$FFFE` → `RTI` wraca pod adres za `BRK` + 1 (test `Brk_Rti_RoundTrip`).
- [x] `--verify-code` bez zmian: 157 adnotacji, `mismatched` 0, `missingInCode` 204 (żadnych nowych wpisów).
- [x] Przerwanie **nie** zmienia cykli instrukcji już zaimplementowanych (wszystkie testy wektorowe zielone;
      BRK dodany do próbek 00.json i pełnych wektorów 2 × 10 000).

## Pułapki

1. **Kiedy próbkować `IRQ`:** na końcu instrukcji, nie w środku; inaczej psuje cykle wektorowe.
2. **`BRK` a `PHP`:** na stosie `B=1`; `IRQ`/`NMI` odkładają `B=0`.
3. **`I` ustawiane przy wejściu**, `NMI` go nie respektuje.
4. **`RTI` po `BRK`** wraca pod `PC+2` (bo `BRK` ma bajt sygnatury) — sprawdzić w wektorach/testach.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/1 | `--verify-code` zielone, ale `BRK` niepełny | etap otwarty |
| 2026-10-21 | cały etap | 1/1 | BRK: próbka 2/2 × 100 i pełne 2 × 10 000 zielone; 7 testów jednostkowych (BRK, IRQ, NMI, CLI/PLP deferral, RTI); testy 515/515; `--verify-code` bez zmian | reguły BRK z wektorów: pushy PCH/PCL/P\|0x30, PC+2, P\|I (65C02 czyści D — dana `InterruptClearsDecimal`); IRQ/NMI: 7 cykli, P\|0x20, wektory $FFFE/$FFFA; deferral po CLI/SEI/PLP jak w docs; test Dormanna H4 zostaje do zasemblowania (GPL) |
