# Etap A2 — szkielet i harness wektorów

Poziom: **I. Rdzeń uruchamialny** · Wektory: próbka NOP/aktywowane przypadki bazowe

A2 tworzy najmniejszy pionowy wycinek: projekt, jawny stan, krok CPU, kolejkę, ruch magistrali, loader i porównanie przypadku. Żadna dalsza grupa instrukcji nie zaczyna się przed stabilnym runnerem.

## Zakres

- `src/Intel8086/VibeEmulator.Intel8086.csproj`, `Intel8086State`, `Intel8086Core` oraz `partial` dla cyklu/fetch.
- `tests/VibeEmulator.Intel8086.Tests` z xUnit, fixture i `Intel8086VectorManifest`.
- Kontrakt rdzenia wybrany w A0; bez zmian w kontraktach wspólnych o ile nie są konieczne.
- Magistrala 1 MiB z kontraktem szerszych adresów wybranym w A0; obecnego `IMemoryBus` z `ushort` nie da się rozszerzyć samym adapterem. Dla portów ocenić istniejący `IPortBus` przed tworzeniem nowego API.
- Narzędzie `tools/Intel8086VectorRunner` oraz współdzielony loader/comparator używany przez xUnit i pełne zestawy; streaming gzip, raport i limit T-state bez zależności narzędzia od assembly testowego.
- Parser prefiksów segmentowych przed pierwszym aktywnym wektorem, który je zawiera; nie pomijać takich rekordów.
- Test NOP i bezpieczny subset rekordów w pliku źródłowym; pełny przypadek porównuje stan, RAM, kolejkę i cykle.
- Loader pełnego `initial`, częściowego `final`, tablic RAM/queue/cycles; diagnostyka wskazuje plik, `test_num`, `test_hash`, bajty i pierwsze pole/cykl różnicy.

## Decyzje implementacyjne

1. Stan publiczny dla testów zawiera wszystkie 16-bit rejestry ogólne, segmenty, `IP`, flagi oraz stan prefetch wymagany przez źródło. Wewnętrzne pola faz nie muszą być częścią stanu architektonicznego.
2. Rejestry bajtowe AH/AL itd. mapuje jedna warstwa do rejestrów słownych; brak duplikowanych źródeł prawdy.
3. RAM testowy startuje deterministycznie: załadować dokładnie inicjalne zakresy wskazane przez rekord i model wartości pozostałej pamięci uzgodniony w A0/A1.
4. Kolejka jest jawna, kolejność bajtów oraz wskaźniki head/count wynikają z przykładowych fixture i dokumentacji upstream.
5. Cykl przechowuje surowe pola w formacie upstream; comparator może maskować wyłącznie pola jawnie nieistotne dla modelu.
6. Przy NOP odseparować fetch z kolejki od pobrania magistralą; asercjonować wyjścia magistrali/kolejki po każdym Tick oraz końcowy stan na granicy zdefiniowanej przez upstream, z rozdzieleniem architektonicznego IP i wskaźnika prefetch.

## Granica wektora i porównywanie cykli

Źródło: [README upstream, Using the Tests / Cycle Format](https://github.com/SingleStepTests/8086/blob/e71c68d215a6bb8c356bd4cb3842de3bef345ca9/README.md), odczytane 2026-10-23.

- Ślad kończy się przy pobraniu pierwszego bajtu następnej instrukcji z kolejki; samo `Pending == 0` lub długość bieżącej instrukcji nie wyznaczają tej granicy. Zdefiniować mapowanie końcowego IP/kolejki i przetestować brak przesunięcia o jeden Tick.
- Status kolejki opisuje operację z poprzedniego cyklu. Oddzielić aktualizację wewnętrznej kolejki od chwili jej obserwacji w QS.
- Odczytane wektory startują z możliwie pełną kolejką; inicjalizacja musi ustawić też wskaźnik przyszłego fetch. Pusta kolejka i wykonanie ciągłe wymagają osobnych testów, jeśli brak ich w SST.
- Porównywać aktywne połówki 16-bitowych danych według A0/BHE i fazy transferu. Nieaktywne banki ignorować na podstawie źródła, nie przez globalne wyłączenie porównania danych.
- Wyjścia 8288 oddzielić od pinów CPU. README niejednoznacznie opisuje drugą kolumnę jako zatrzaśnięty adres i multipleksowaną magistralę; rozstrzygnąć to danymi oraz dokumentacją przed maskowaniem wartości. T-state jest rekonstruowany przez generator, nie odczytywany z osobnego pinu.

## Nowe testy

- Parse/serialize próbki: częściowe final regs, RAM, kolejka pusta i pełna.
- Każdy rodzaj rekordu cyklu rozpoznany w A1: piny, adres latch/multiplexed bus, status, T-state, kolejka.
- NOP od stanu z seedowaną kolejką; NOP z wymuszonym fetch, jeżeli upstream ma taki przypadek.
- Błąd porównania raportuje pierwszy mismatch bez obcięcia kontekstu.
- Test limitu kroków zabezpiecza przypadki, które nie docierają do granicy instrukcji.

## Kryteria akceptacji

- [x] Projekty rdzenia, testów i runnera dodane do `.slnx`; build całej solucji bez ostrzeżeń.
- [x] Pełny inwentarz zachowuje wszystkie źródłowe pliki; lista aktywnych próbek wskazuje zaimplementowane wektory i test porównuje ją z dyskiem.
- [x] NOP próbki zgodny w pełnym stanie, pamięci, kolejce i porównywalnych cyklach.
- [x] Testy nie używają sieci, cwd, zegara ani nieustalonej losowości.
- [ ] Hot path nie alokuje po rozgrzaniu; nie mierzono, bo A2 jest szkieletem i nie ma stabilnego wzorca benchmarku.
- [x] Pełny zestaw poza repo oznaczony jako „nie uruchomiono”.

## Pułapki

1. Próbka 20 rekordów to kontrola harnessu i wczesne przypadki, nie pokrycie wszystkich stanów.
2. W 8086 `IP` może odnosić się do następnego bajtu, a prefetch zawiera bajty przyszłych instrukcji; odczyt `bytes` nie może niejawnie zmieniać RAM.
3. Częściowe `final` to delta; brak pola oznacza brak zmiany zgodnie ze schematem, nie wartość domyślną.
4. Porównanie wyłącznie „adres+read/write” gubi statusy i operacje kolejki. Zachować cykl po cyklu wszystkie dostępne sygnały.

## Dziennik postępu

| data | zakres | sample | testy/build | wynik pełnego zestawu |
|---|---|---|---|---|
| 2026-10-23 | szkielet, NOP, loader/runner | 20 rekordów `90` | Intel8086: 21 testów; Memory: 17; cała solucja Debug/Release build bez ostrzeżeń; pełny Release suite: 2821 testów przed końcowym dopięciem runnera | próbka 20/20; pełne 645 998 nie uruchomiono |
