# Etap A10 — prefiksy i domknięcie dekodera

Poziom: **audyt poziomów II/III; domknięcie kodowań bazowych** · Zależność: A3–A9

## Zakres

Prefiksy segment override, `LOCK`, `REP/REPE/REPNE` i ich kombinacje tylko w formach z przypiętego źródła oraz dokumentacji 8086. Parser segment override powstaje w A2/A3, REP potrzebny przez IDIV w A8, a REP łańcuchów w A9; A10 domyka dekodowanie, kolejność prefiksów, queue semantics i pozostałe udokumentowane kombinacje.

## Plan realizacji

1. Z A1 wygenerować tabelę wszystkich bajtów/form, w tym instrukcji, pól ModR/M i prefiksów; zestawić z referencyjnym manualem.
2. Klasyfikować rekordy: udokumentowany 8086, nieudokumentowany ale obecny w źródle, brak danych, 80186+ poza zakresem.
3. Domknąć parser prefiksów bez limitu arbitralnego, lecz z granicą obronną runnera dla niekończących się sekwencji.
4. Weryfikować segment override dla form pamięciowych i ignorowanie prefiksów tam, gdzie źródło to wymaga.
5. `LOCK` zweryfikować dedykowanymi testami z dokumentacją producenta. README upstream wyklucza testowanie F0/F1, a format cyklu nie ma osobnego pola LOCK, więc nie deklarować dowodu tego pinu na podstawie SST. Nie narzucać reguł legalności z nowszych x86; zachowanie pozostałych kombinacji pozostaje niezweryfikowane.
6. Wykonać testy prefiksów redundantnych, różnych kolejności i wielokrotnych prefiksów, które występują w zestawie.
7. Zamknąć manifest i macierz ISA: każdy plik upstream i każda forma instrukcji mają status implemented, out-of-scope z powodem albo known-gap.

## Testy i kryteria

- [ ] Walidator inwentarza porównuje SHA, każdy wpis `metadata.json`, plik źródłowy i próbki aktywne. Testy syntetyczne wykrywają brak pliku, brak próbki, złą liczność, duplikat wpisu i niespójny status; wyjście podaje opcode/form i oczekiwaną wartość.
- [ ] Pełna inwentaryzacja przypiętego źródła jest zgodna: 323 pliki JSON, 646 000 rekordów, 256 kluczy metadata oraz jawne statusy normal/alias/prefix/fpu/undocumented/group.
- [ ] Każdy opcode i każda forma ModR/M zdefiniowana dla 8086 ma status implemented, out-of-scope z uzasadnieniem albo known-gap; aliasy i grupy bez statusu metadata nie są mylone z brakującymi danymi.
- [ ] Wszystkie aktywne próbki z głównego i etapowych manifestów istnieją, mają właściwy SHA i zadeklarowaną liczbę rekordów; rozbieżność 1 820 aktywnych rekordów w `source-inventory.json` wobec 5 720 wpisów manifestu głównego oraz osobnych manifestów A8/A9 została wyjaśniona i naprawiona albo opisana jako odrębne próbki pochodne.
- [ ] Kolejność prefiksów segment/REP, powtórzone prefiksy, redundancja oraz kolejka fetch są testowane na pełnych wektorach i lokalnych scenariuszach; wszystkie dostępne kombinacje z pinned SHA przechodzą pełny stan, RAM, queue i każdy T-state.
- [ ] `LOCK` ma jawne źródło producenta i osobne testy semantyki; brak bitu LOCK w formacie SST pozostaje oznaczony jako luka dowodowa i nie jest przedstawiany jako zgodność wektorowa.
- [ ] Formy nieudokumentowane, aliasy 8086 i kodowania 80186+ są zestawione z manualem i README pinned upstream; każda różnica ma decyzję oraz miejsce dalszej implementacji.

## Podstawa audytu

- Branch: `A10-8086-prefixes-decoder`, oparty na commicie A9.
- Inwentarz lokalny deklaruje SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, 323 pliki JSON i 646 000 rekordów.
- `metadata.json` ma 256 opcode statusów (204 normal, 20 alias, 8 prefix, 8 FPU, 1 undocumented, 15 grup bez statusu); testy A10 mają zachować te rozróżnienia.
- Wstępny audyt znalazł 286 pozycji w głównym manifeście (5 720 rekordów) oraz 1 820 w liczniku `active_sample_records` inwentarza. Ten licznik nie obejmuje całego aktualnego zestawu A8/A9 i wymaga rekonsyliacji przed zamknięciem etapu.
- Zakres `LOCK` pozostaje osobnym kryterium dokumentacyjnym: README źródła wyklucza F0/F1 z testowania, a cykle SST nie udostępniają pinu LOCK.

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
