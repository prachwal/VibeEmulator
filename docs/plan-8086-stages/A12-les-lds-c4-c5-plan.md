# A12 — `LES`/`LDS` (`C4`/`C5`)

## Cel i zakres

Zaimplementować pamięciowe instrukcje ładowania wskaźnika dalekiego w Intel 8086: `C4` ładuje `r16` oraz `ES`, a `C5` ładuje `r16` oraz `DS`. Zakres obejmuje wszystkie kombinacje pamięciowego ModR/M, displacement 0/8/16 bit, wszystkie segmenty domyślne, cztery prefiksy segmentowe, wyrównanie parzyste/nieparzyste, odczyty czterech kolejnych bajtów, kolejkę prefetch i T-states magistrali. Rejestrowego `mod=11` nie zaimplementowywać: nie występuje w przypiętym źródle.

Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Zachowania i cykli nie wyprowadzać z tabel czasowych. Wektory są referencją modelu SST; różnice z dokumentacją krzemu zapisać, jeśli zostaną wykryte.

## Inwentaryzacja

- `C4.json.gz`: 2 000 rekordów; przed zmianą 0 passed, 2 000 skipped.
- `C5.json.gz`: 2 000 rekordów; przed zmianą 0 passed, 2 000 skipped.
- Baseline pełnego filtra: 4 000 rekordów, 0 passed, 4 000 skipped, 0 mismatchy, timeoutów i błędów parsowania.
- Wszystkie rekordy mają pamięciowy ModR/M `mod=00/01/10`; nie ma `mod=11`.
- Rozkład nieprefiksowanych `mod=00/01/10`: `C4` = `318/332/345`; `C5` = `337/364/329`.
- Suma: 2 025 nieprefiksowanych i 1 975 segment-prefiksowanych (`C4`: 995/1 005; `C5`: 1 030/970). Prefiksy występują jako ES/CS/SS/DS i mają używać wskazanego segmentu do obliczenia adresu źródłowego.
- Wszystkie 4 000 rekordów są klasyfikowane jako skip w runnerze; `C4/C5` nie mają jeszcze aktywnych wpisów lokalnego manifestu. Dodać deterministyczne lokalne fixture’y na podstawie pierwszych 20 rekordów każdego pliku, bez modyfikowania treści wektorów.

## Wymagania implementacyjne

1. Dla każdego poprawnego ModR/M obliczyć adres efektywny i segment źródłowy przez istniejący dekoder, z domyślnym SS dla adresowań używających BP oraz override ES/CS/SS/DS.
2. Odczytać wskaźnik jako cztery bajty spod kolejnych adresów fizycznych, z 20-bitowym zawinięciem. Bajty 0–1 trafiają do rejestru ogólnego wskazanego przez `reg`; bajty 2–3 do ES (`C4`) albo DS (`C5`). Ustalić na wszystkich wektorach kolejność widocznych odczytów oraz chwilę aktualizacji rejestrów.
3. Zachować FLAGS. Nie zmieniać pozostałych rejestrów ani pamięci. Segment-prefiksy nie mogą przełączać segmentu docelowego: `C4` nadal zapisuje ES, a `C5` DS.
4. Odtworzyć start, fazy i rozdzielenie cykli odczytu dla adresu parzystego/nieparzystego, linie BHE/status segmentu, idle, kodowanie/prefetch kolejki i zakończenie instrukcji na podstawie pełnych filtrów.
5. Poprawnie konsumować prefiks, opcode, ModR/M i displacement z kolejki; nie pobierać operandów poza kolejką w czasie dekodowania.
6. Nie zaliczać form rejestrowych, jeśli nie występują w źródle. Zachować istniejące zachowanie innych opkodów i prefiksów.

## Kolejność prac

1. Zatwierdzić ten plan osobnym commitem przed zmianami kodu.
2. Dodać pierwsze 20 rekordów `C4`/`C5` do fixture’ów, aktywować je w manifeście i uruchomić testy projektu Intel8086.
3. Implementować wspólną ścieżkę czterobajtowego odczytu dla C4/C5, korzystając z istniejącego odczytu słowa tam, gdzie nie naruszy to zgodności cyklowej. Iterować na pierwszym mismatchu (hash i numer cyklu), nie zmieniać oczekiwań.
4. Po ustabilizowaniu uruchomić pełne filtry `C4` oraz `C5` po 2 000 rekordów każdy.
5. Uruchomić pełny korpus 646 000 rekordów raz po stabilizacji, przeanalizować także regresje/zmianę kategorii skipów i obliczyć nowy procent `passed / 646 000`.
6. Zaktualizować manifest, macierz ISA, README wektorów, raport, checklistę i stan wznowienia. Zatwierdzić implementację osobnym commitem po zielonych bramkach.

## Weryfikacja końcowa

- [x] Lokalne fixture’y po 20 rekordów: C4 i C5 (40/40); testy Intel8086 Release 18 271/18 271.
- [x] Pełne filtry: C4 2 000/2 000 i C5 2 000/2 000, zero skipów/mismatchy/timeoutów/błędów parsowania.
- [x] Pełny korpus 646 000: 562 450 passed / 83 550 skipped (87,1%), +4 000 passed / −4 000 skipów względem 558 450/87 550; 0 mismatchy/timeoutów/błędów parsowania.
- [x] Debug i Release bez ostrzeżeń/błędów; pełne testy Release 21 071/21 071; formatter verify, walidator manifestu i OpcodeInfo zielone.
- [x] Checklista A12, README, macierz, raport oraz stan wznowienia zaktualizowane po końcowej bramce.
- [x] Plan `5158550` i implementacja zapisane osobnymi commitami na branchu A12; nie wypychać.

Przykładowy filtr pełnego źródła:

```bash
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 \
  --manifest /tmp/a12-les-lds-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a12-les-lds-full.json
```
