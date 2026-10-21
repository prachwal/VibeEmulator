# Etap K — KIL (JAM)

Poziom: **B** (domknięcie) · Opkody: **12** · Nowe akcje: 1

Po etapie: NMOS **256/256** bajtów.

## Zakres

`02 12 22 32 42 52 62 72 92 B2 D2 F2`

Dziś nieznany bajt → `Jammed`, więc zachowanie jest przybliżone, ale **niezweryfikowane** (nie ma wpisu w tabeli,
`--verify-code` zgłasza brak, a testy wektorowe nie mają czego porównać).

## Nowe elementy

- Akcja `Kil` w tabeli wariantu NMOS.
- Wpis `OpKind.Implied` z `InternalCycles` zgodnym z `wektory_cykle` w CSV (KIL nie ma `cykle_bazowe`).
- Zachowanie rdzenia: zatrzymanie bez wyjątku (już jest), ale przez jawny wpis, nie „nieznany bajt”.

## Kryteria akceptacji

- [ ] 12 wpisów w klasie `Nmos`; `--verify-code`: `missingInCode` dla NMOS = **0**.
- [ ] Próbki wektorów dla 12 opkodów (jeśli pliki istnieją w źródle — sprawdzić; jeśli nie, zaznaczyć wprost).
- [ ] Test: po `Kil` rdzeń nie zmienia stanu i nie zgłasza wyjątku; `Jammed == true`.

## Pułapki

1. **Te same bajty w 65C02 nie są KIL** — `92 B2 D2 F2` to prawdziwe instrukcje (etap H). Wpis tylko w wariancie NMOS.
2. KIL w 65C02 istnieje jako `STP` (`DB`) — nie mylić.
3. Nie „naprawiać” zachowania KIL przez wyjątek — emulator ma się zatrzymać, nie wywrócić.

## Dziennik postępu

| data | grupa | postęp | weryfikacja | uwagi |
|---|---|---|---|---|
| 2026-10-21 | — | 0/12 | `missingInCode` NMOS = 12 | etap otwarty |
