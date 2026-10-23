# Etap A1 — źródło i inwentaryzacja wektorów

Poziom: **fundament danych** · Opkody: **0** · Pliki próbek: do ustalenia

Źródło startowe to [SingleStepTests/8086](https://github.com/SingleStepTests/8086). README repo opisuje wersję 1.0.1, testy z P80C86A-2 w Maximum Mode, format JSON/MOO i 2000 testów na opkod. Te informacje nie zastępują pomiaru przypiętego drzewa. Recenzja wskazuje roboczy SHA w [README planu](README.md); A1 potwierdza go i mierzy rzeczywisty zakres.

## Procedura

1. Wykonać polecenia inwentaryzacji z [README planu](README.md), zapisać `git rev-parse HEAD`, tag/wydanie i status drzewa.
2. Przeczytać README, changelog, licencję i dokumentację formatu; zachować źródłowe pliki licencji w katalogu testów.
3. Zinwentaryzować `v1/*.json.gz`, `v1/metadata.json` i `v1_binary/*.MOO.gz`: nazwy plików, liczbę rekordów, rozmiary, duplikaty, puste/niepełne zestawy i relację JSON↔MOO.
4. Zparsować reprezentatywne przypadki dostępnych klas: rejestr, ModR/M, pamięć, I/O, branch, string/REP, wyjątek, przerwanie programowe, prefix, kolejka. Brakujące klasy, w tym puste kolejki lub wejścia asynchroniczne, odnotować jako luki i zaplanować testy dedykowane.
5. Spisać JSON schema faktycznie zaobserwowane: `bytes`, `initial/final.regs`, `ram`, `queue`, `cycles`, `test_hash`, `test_num` oraz ewentualne pola opcjonalne. Przykład README ma `hash`/`idx`, ale trzy odczytane pliki na roboczym SHA mają `test_hash`/`test_num`; adapter ma wynikać z plików, a nie przykładu. Nie zakładać pól niewidzianych w źródle.
6. Odczytać `metadata.json` z tego samego SHA (w odczytanych metadanych wersja 1.0.0, mimo README/changelogu 1.0.1) i ustalić semantykę masek flag na poziomie opkodu oraz rozszerzenia ModR/M. Jeśli źródło nie podaje maski, nie wymyślać jej: oznaczyć pytanie i znaleźć dokumentację producenta/drugi niezależny test.
7. Wybrać próbkę na plik na podstawie pomiaru całkowitego rozmiaru. Domyślnie pierwsze 20 rekordów, o ile zmieści się rozsądnie; próbka to nie pełna walidacja.
8. Zapisać commit, datę, wersję, licencję, komendy pobrania, wielkości, manifest i ograniczenia w `Vectors/README.md`.

## Wyniki wymagane

- Pełny maszynowo czytelny inwentarz: nazwa źródłowego pliku gzip, opkod/rozszerzenie ModR/M, liczba źródłowa, liczba próbki, status implementacji, etap. Osobno aktywne próbki w repo; nie wymagać próbki dla niezaimplementowanych pozycji.
- Osobna macierz ISA z manuala: instrukcje/formy bez wektorów pozostają widoczne. Brak pliku nie dowodzi braku instrukcji; prefiksy nie muszą mieć samodzielnych plików.
- Test integralności offline, który sprawdza aktywne pozycje manifestu, pliki i liczbę rekordów próbki wobec zapisanego inwentarza. Osobna kontrola A1/pełnego runnera porównuje inwentarz z rzeczywistym przypiętym drzewem; testy xUnit nie pobierają tego drzewa z sieci.
- Skrypt/komenda reprodukowalnego wycięcia pierwszych N rekordów; oryginalna kolejność i obiekty pozostają niezmienione, JSON zwarty.
- Rozstrzygnięte użycie `v1` JSON jako kanonicznego formatu. Nie przechowywać jednocześnie duplikatów JSON i MOO.

## Kryteria akceptacji

- [ ] SHA zapisany i powtórnie weryfikowany po checkout.
- [ ] Licencja MIT i historia wersji zachowane.
- [ ] Raport obejmuje pełne drzewo i rozmiar na dysku.
- [ ] Próbki parsują się i ich rekordy są źródłowymi prefiksami bez zmian danych.
- [ ] Każdy plik źródłowy ma status w manifeście; braki odróżniają brak źródła od niezaimplementowanego opkodu.
- [ ] Ograniczenia zestawu (model CPU, Maximum Mode, wait states, przerwania i flags) zapisane z odnośnikiem do źródła.

## Pułapki

1. Upstream może dodać/zmienić dane — po A1 pracować tylko na SHA, nigdy na `main`.
2. JSON i MOO mogą opisywać te same przypadki; nie sumować ich jako osobnych wektorów.
3. Adresy w wektorze są wartościami magistrali/układu testowego. Nie normalizować ich bez zachowania surowej reprezentacji.
4. `final.regs` i `final.ram` są deltą według README upstream; `final.queue` opisuje końcową kolejkę. Stan `final` może być częściowy i zawierać tylko zmienione pola; runner musi z tego powodu odtworzyć stan oczekiwany, a nie uznać brak pola za zero.
5. Treści upstream są danymi referencyjnymi: żadnego ręcznego „naprawiania” JSON.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

| data | pliki/format | rezultat inwentaryzacji | SHA/licencja | uwagi |
|---|---|---|---|---|
| — | — | plan; inwentaryzacja nieuruchomiona | do ustalenia | uzupełnić po pobraniu źródła |
