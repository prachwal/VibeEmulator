---
name: "nowy-opkod"
description: "Dodaj opkody do rdzenia cycle-exact według wektorów SingleStepTests i docs/cpu: wpis w tabeli wariantu, fazy dla nowego trybu, próbka wektorów, testy, deasembler, aktualizacja planu. Use when: implementacja opkodów etapów 2b-2e i 3, nowy tryb adresowania, nowy wariant CPU."
argument-hint: "opkody lub grupa, np. AND ORA EOR we wszystkich trybach; albo $24 $2C (BIT zp, abs)"
agent: "agent"
---

# Nowe opkody w rdzeniu cycle-exact

Zasady i procedura są w `.github/instructions/mos6502.instructions.md` (sekcje 2–4) oraz `c64-emulation.instructions.md`.
Ten prompt to kolejność pracy. **Zachowania opkodu nie wpisuj z pamięci** — bierz je z wektorów i `docs/cpu/`.

## 1. Zbierz dane o opkodach z argumentu

- Wiersze z `docs/cpu/opcodes/nmos6502.csv` i `wdc65c02.csv`: mnemonik, tryb, bajty, cykle bazowe, kolumna `uwagi`.
- `docs/cpu/opcodes.md` (rozbieżności, np. `$5C`) i `docs/plan-c64-cycle-exact.md` (do którego etapu opkod należy,
  czy dotyczy NMOS, 65C02 czy obu).
- Podaj w odpowiedzi tabelę „opkod → mnemonik, tryb, cykle, wariant”, zanim zaczniesz kodować.

## 2. Wektory

Pobierz pliki wektorów z **przypiętego commita** wskazanego w `tests/VibeEmulator.Mos6502.Tests/Vectors/README.md`
(`SingleStepTests/65x02`, zestawy `6502` i `wdc65c02`, ścieżka `<zestaw>/v1/<hex małymi>.json`) do katalogu **poza repo**
(np. `$TMPDIR`). Do repo zapisz **tylko pierwsze 100 testów** każdego pliku jako zwarty JSON w
`tests/VibeEmulator.Mos6502.Tests/Vectors/<nmos6502|wdc65c02>/<hex>.json` i zaktualizuj `Vectors/README.md` (lista, rozmiar).
Pełnych plików nie commituj.

## 3. Implementacja

1. Dodaj wpisy `CpuOpcode` do tabeli właściwego wariantu (`src/Mos6502/CpuVariant.cs`). Nowy tryb adresowania to **nowe
   fazy** rdzenia i nowa wartość `AddressingMode` na końcu enuma; różnicę wariantu wyrażaj właściwością `CpuVariant`, nie `if`.
2. Dopasuj `Disassembler` (format zgodny z istniejącym, np. `LDA abs,Y $1234`).
3. Dodaj opkody do listy w `SingleStepVectorTests` dla każdego wariantu, w którym istnieją.

## 4. Weryfikacja

Uruchom `/sprawdz-przed-commitem` (lub komendy z „Definicji ukończenia”). Jeśli test wektorowy nie przechodzi, **nie zmieniaj
oczekiwań** — popraw rdzeń; `Skip` tylko z dokładnym opisem różnicy i odnośnikiem do etapu naprawy.

## 5. Dokumentacja

Zaktualizuj w tym samym zakresie zmian `docs/plan-c64-cycle-exact.md` (Stan realizacji, liczba opkodów, wyniki wektorów).
Jeśli wiedza o sprzęcie się zmieniła, popraw `docs/cpu/`.

## 6. Raport

Tabela: opkod, wariant, wynik próbki wektorów (zaliczone/wszystkie), uwagi. Napisz wprost, czy uruchomiono **pełne**
wektory (10 000 na opkod, poza repo) — jeśli nie, to opkody są zweryfikowane tylko na próbce. Bez pytań i ofert na końcu.
