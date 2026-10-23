# Etap A10 — prefiksy i domknięcie dekodera

Poziom: **audyt poziomów II/III; domknięcie kodowań bazowych** · Zależność: A3–A9

## Zakres

Prefiksy segment override, `LOCK`, `REP/REPE/REPNE` i ich kombinacje tylko w formach z przypiętego źródła oraz dokumentacji 8086. Parser segment override powstaje w A2/A3, REP potrzebny przez IDIV w A8, a REP łańcuchów w A9; A10 domyka dekodowanie, kolejność prefiksów, queue semantics i pozostałe udokumentowane kombinacje.

## Kolejność

1. Z A1 wygenerować tabelę wszystkich bajtów/form, w tym instrukcji, pól ModR/M i prefiksów; zestawić z referencyjnym manualem.
2. Klasyfikować rekordy: udokumentowany 8086, nieudokumentowany ale obecny w źródle, brak danych, 80186+ poza zakresem.
3. Domknąć parser prefiksów bez limitu arbitralnego, lecz z granicą obronną runnera dla niekończących się sekwencji.
4. Weryfikować segment override dla form pamięciowych i ignorowanie prefiksów tam, gdzie źródło to wymaga.
5. `LOCK` zweryfikować dedykowanymi testami z dokumentacją producenta. README upstream wyklucza testowanie F0/F1, a format cyklu nie ma osobnego pola LOCK, więc nie deklarować dowodu tego pinu na podstawie SST. Nie narzucać reguł legalności z nowszych x86; zachowanie pozostałych kombinacji pozostaje niezweryfikowane.
6. Wykonać testy prefiksów redundantnych, różnych kolejności i wielokrotnych prefiksów, które występują w zestawie.
7. Zamknąć manifest i macierz ISA: każdy plik upstream i każda forma instrukcji mają status implemented, out-of-scope z powodem albo known-gap.

## Kryteria akceptacji

- [ ] Weryfikacja kompletności wykrywa brak wpisu inwentarza, a dla aktywnych wpisów również brak próbki lub niezgodną liczbę rekordów.
- [ ] Każdy opcode/form zdefiniowany dla zakresu 8086 jest implemented lub ma jawnie zarejestrowany blocker.
- [ ] Przedrostki zachowują prawidłowy order fetch/queue i widoczne cykle.
- [ ] Zachowanie `LOCK` ma jawne źródło i oddzielne testy; brak dowodu oznacza lukę, nie zgodność SST.
- [ ] Porównano nieudokumentowane zachowania z referencją i rozpisano różnicę źródłową.

## Pułapki

1. Prefiksy zmieniają kontekst następnej instrukcji; nie wolno ich traktować jako samodzielnego opcode wykonującego cykl „NOP”.
2. README zestawu opisuje różne generacje testów; tylko commit zapisany w `Vectors/README.md` jest źródłem danych.
3. Obecność bajtu w źródle nie dowodzi, że to oficjalna instrukcja 8086.
4. Dekoder zamknięty na same opkody bez kombinacji ModR/M/prefixów nie jest kompletny.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

| data | grupa | manifest complete | wyniki próbki | rozbieżności |
|---|---|---|---|---|
| — | — | nieuruchomiono | nieuruchomiono | wykonać po A9 |
