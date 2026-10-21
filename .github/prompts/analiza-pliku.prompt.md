---
name: "analiza-pliku"
description: "Recenzja wskazanego pliku: struktura, spójność z resztą repo, problemy wg wagi, rekomendacje. Tylko analiza: nie zmienia plików i nie pyta o poprawki. Use when: analiza pliku, recenzja dokumentu lub kodu, przegląd przed zmianą."
argument-hint: "ścieżka pliku do analizy, np. docs/cpu/README.md"
agent: "ask"
tools: [read/readFile, search/codebase]
---

# Analiza pliku (tylko raport)

Przeanalizuj wskazany plik i zwróć raport. To zadanie jest **tylko do odczytu**.

## Zasady, których nie wolno łamać

- **Nie modyfikuj żadnego pliku** i nie proponuj, że to zrobisz. Poprawki nanosi się na osobne, wyraźne polecenie.
- **Nie kończ odpowiedzi pytaniem ani ofertą.** Nie pisz: „Chcesz, żebym naniósł te poprawki?", „Czy mam to
  poprawić?", „Daj znać, jeśli…", „Mogę przygotować…". Ostatnim elementem odpowiedzi jest ostatni punkt rekomendacji.
- **Zarzuty sprawdzaj na plikach.** Zanim napiszesz, że coś jest nieprawdziwe, przeczytaj pliki, do których tekst się
  odwołuje (linki, nazwy klas, liczby, adresy), i sąsiednie dokumenty. Fakt, którego nie sprawdzono, oznacz jako
  *niezweryfikowany*. Nie przepisuj twierdzeń z analizowanego pliku jako własnych ustaleń.
- Podawaj numery linii lub cytaty przy każdym zarzucie.

## Co zrobić

1. Wczytaj plik wskazany w argumencie (jeśli go nie podano, użyj pliku otwartego w edytorze).
2. Ustal rolę pliku w repo i sprawdź, czy wskazane w nim pliki i liczby istnieją i się zgadzają.
3. Porównaj z sąsiednimi plikami (dokumenty tej samej rodziny, plan, instrukcje, kod, którego dotyczy).

## Format raportu

```text
## Analiza `<plik>`

### Rola pliku
<1–3 zdania>

### Spójność faktów
<co sprawdzono i z czym; tabela „twierdzenie → werdykt"; zgodne wymień zwięźle>

### Problemy (wg wagi)
1. **<tytuł> (wysokie/średnie/niskie).** <opis, dowód z numerem linii, skutek>

### Rekomendacja
1. <co zmienić, gdzie i dlaczego>
```

- Problemy uporządkuj od najpoważniejszych: najpierw **prawdziwość** (dokument twierdzi coś nieprawdziwego lub niezweryfikowanego),
  potem **koszt utrzymania** (duplikacja, rozjazd z innym plikiem), potem precyzja pojęć, na końcu kosmetyka.
- Jeśli nie znalazłeś problemów, napisz to wprost i wymień, co sprawdziłeś.
- Rekomendacje są **stwierdzeniami**, nie pytaniami. Raport kończy się ostatnią rekomendacją.
