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

- [x] SHA zapisany i potwierdzony przez `git rev-parse HEAD` po checkout.
- [x] Licencja MIT i oba numery wersji zachowane; `metadata.json` skopiowane bez zmian.
- [x] Inwentarz obejmuje 323/323 pliki JSON i 323/323 pliki MOO; rekordy i bajty policzone dla wszystkich plików.
- [x] 323 próbki kontrolne po 20 rekordów wyeksportowano do `/tmp`, porównano semantycznie z prefiksem źródła; aktywnych próbek w repo 0.
- [x] Każdy plik źródłowy ma pozycję w `source-inventory.json`; braki upstream i statusy grupowe są osobne od statusu implementacji.
- [x] Ograniczenia modelu CPU, trybu, wait states i niećwiczonych wejść zapisano w `Vectors/README.md` ze źródłem upstream.

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
| 2026-10-23 | 323 JSON gzip + metadata + 323 MOO gzip | 646 000 rekordów; 323×2000; 2 442 107 061 bajtów po dekompresji | `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; MIT | 323/323 próbki ×20 zgodne z prefiksem; testów wykonania CPU nie uruchomiono |

Audyt A12 wykrył błąd granicy bufora w zliczaniu tokenu `"test_num"`: dwa klucze przecinały granicę 1 MiB i były pominięte. Zliczanie zachowuje teraz nakładający się sufiks; test regresyjny pokrywa taki podział. Ponowne zliczenie rekordów JSON daje 646 000.
