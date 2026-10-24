# Etap A9 — instrukcje łańcuchowe i wejście/wyjście

Poziom: **II. Zestaw bazowy, część 7** · Zależność: A3/A5/A7

## Zakres

Łańcuchy: `MOVS`, `CMPS`, `STOS`, `LODS`, `SCAS` w wariantach byte/word; `REP`, `REPE/REPZ`, `REPNE/REPNZ`; I/O: `IN` i `OUT` przez immediate port oraz `DX`. `INS`/`OUTS` są poza baseline 8086: manual 8086 oznacza `6C`–`6F` jako nieużywane, a manual rodziny 80186 wymienia `INS` i `OUTS` jako nowe instrukcje. Przypięte wektory 8086 opisują te kodowania jako aliasy `JL/JNL/JLE/JNLE`; inwentarz klasyfikuje je jako `alias`. Semantyka aliasów należy do A10, nie wolno dekodować ich jako INS/OUTS w rdzeniu 8086. Źródła: [manual 8086, tabela 4-13](https://www.ceibo.com/eng/datasheets/Intel_8086_Family_Users_Manual.pdf), [manual 80186, różnice instrukcji](https://www.bitsavers.org/components/intel/80186/270788-001_80186_80188_80C186_80C188_Hardware_Reference_Manual_1990.pdf).

## Kolejność

1. Zaimplementować pojedynczy transfer string byte (`MOVS`) i wskaźniki SI/DI/D direction flag.
2. Dodać warianty word oraz `STOS`/`LODS` z testami segmentów źródła i celu.
3. Dodać `CMPS`/`SCAS`, porównanie flags i warunek powtarzania.
4. Dodać `REP` jako sekwencję iteracji; `REPE/REPNE` sprawdzają flagę po iteracji zgodnie z fixture.
5. Wprowadzić przerwanie między iteracjami tylko w modelu linii A11; nie symulować go w pojedynczej instrukcji bez dowodu.
6. Wykorzystać istniejący [`IPortBus`](../../src/Memory/IPortBus.cs) (`Input/Output`, adres `ushort`, dane bajtowe) lub uzasadnić rozszerzenie dla transferów słownych; zdefiniować deterministyczny adapter zgodny z warstwami repo; `IN`/`OUT` immediate i przez DX.
7. Porównać cykle I/O, kierunek, adres portu, dane i stany queue.

## Testy i kryteria

- [x] Adapter `Intel8086PortBus` izoluje niezmapowaną przestrzeń portów i domyślnie zwraca `0xFF` dla `IN`; 2 testy adaptera przechodzą. To nie zalicza wykonania instrukcji CPU.
- [x] Przygotowano bez modyfikowania rekordów 18 plików po 20 pierwszych przypadków z przypiętego źródła: 10 opcode string oraz 8 opcode I/O; dodatkowa próbka: 20 nieprefiksowanych MOVSB z DF=0; próbki w `tests/VibeEmulator.Intel8086.Tests/Vectors/A9String/` i `A9Io/`.
- [x] Nieprefiksowane `MOVSB` porównuje pełny stan, RAM i wszystkie T-states dla DF=0: 20/20 filtrowanych przypadków, oraz DF=1: 2/2 rekordy dostępne w próbce A4; pokryto początkową kolejkę 5 i 6 bajtów.
- [x] Nieprefiksowane `MOVSW`: 4/4 rekordy A5 zgodne w stanie, RAM, kolejce i wszystkich T-states; przypadki pokrywają źródło wyrównane i nieparzyste (dwa transfery bajtowe), z wyrównanym celem.
- [x] Częściowo: `REP/REPNE MOVSW` (`F2/F3 A5`) przechodzi 6/6 lokalnych fixture (testy 1, 3, 4, 13, 14, 16), pokrywając wyrównane i nieparzyste źródło/cel; dane obu bajtów, SI/DI/CX, RAM, kolejka i każdy T-state zgodne. Pełny filtr A5 przechodzi 2 000/2 000; segment override jest pokryty przez lokalne filtry segmentowe i pełny przebieg 20 000 stringów.
- [x] Częściowo: nieprefiksowane `STOSB/STOSW` przechodzą 9/9 dostępnych wektorów (`AA` testy 0, 3, 4, 13; `AB` testy 0, 11, 15, 16, 19), w tym zapis słowa wyrównany i rozdzielony dla nieparzystego celu; porównano stan, RAM, kolejkę i każdy T-state. Prefiksy są objęte pełnymi filtrami upstream; AA i AB przechodzą po 2 000/2 000.
- [x] Częściowo: nieprefiksowane `LODSB/LODSW` przechodzą 10/10 wybranych rekordów (`AC` testy 6, 10, 13, 15; `AD` testy 1, 3, 12, 14, 16, 18); pełny stan, RAM, kolejka i każdy T-state zgodne, w tym odczyt słowa z nieparzystego adresu. Pełne AC przechodzi 2 000/2 000, a prefiksy i override segmentu są objęte pełnym filtrem stringów.
- [x] Częściowo: nieprefiksowane `SCASB/SCASW` przechodzą 9/9 dostępnych wektorów (`AE` testy 3, 10, 11, 13, 19; `AF` testy 2, 4, 17, 19); pełny stan, FLAGS, RAM, kolejka i każdy T-state zgodne, w tym word przy parzystym i nieparzystym ES:DI. Każda forma ma teraz 20/20 lokalnych próbek oraz pełny filtr upstream.
- [x] Częściowo: nieprefiksowane `CMPSB/CMPSW` przechodzą 8/8 wybranych wektorów (`A6` testy 2, 7, 9, 12, 16, 17, 19; `A7` test 0); pełny stan, FLAGS, RAM, kolejka i każdy T-state zgodne, źródło DS i cel ES czytane osobno. Każda forma ma teraz 20/20 lokalnych próbek oraz pełny filtr upstream.
- [x] Częściowo: `IN AL,imm8` (`E4`) i `IN AL,DX` (`EC`) przechodzą wszystkie lokalne próbki: 40/40, pełny stan, kolejka i każdy T-state, w tym IOR i dane `0xFF` z niepodłączonej magistrali. Wstrzyknięty dziennik potwierdza dokładny port; wszystkie formy byte/word IN/OUT przechodzą też pełne filtry upstream.
- [x] Częściowo: `OUT imm8,AL` (`E6`) i `OUT DX,AL` (`EE`) przechodzą wszystkie lokalne próbki: 40/40, pełna magistrala IOW i każdy T-state; wstrzyknięty dziennik potwierdza port i wartość AL.
- [x] Częściowo: `IN AX,imm8` (`E5`) i `IN AX,DX` (`ED`) przechodzą wszystkie lokalne próbki: 40/40, w tym nieparzyste porty z dwiema fazami i BHE; wstrzyknięty dziennik potwierdza kolejność portów n, n+1. Pełne filtry E5 i ED przechodzą 4 000/4 000.
- [x] Częściowo: `OUT AX,imm8` (`E7`) i `OUT AX,DX` (`EF`) przechodzą wszystkie lokalne próbki: 40/40, pełne IOW i dane na obu bankach; dla portu nieparzystego dziennik potwierdza bajty AX na portach n, n+1.
- [x] Częściowo: `REP MOVSB` z `CX=0` (A4 testy 13, 17) i `REPNE SCASB` z `CX=0` (AE test 15) przechodzą 3/3 wektory; brak transferu/porównania, SI/DI/CX/FLAGS zachowane, IP, kolejka i wszystkie 9 T-states zgodne.
- [x] Częściowo: `REPNE SCASB` z `CX=4` przechodzi pełny wektor 71/71 T-states, cztery odczyty ES:DI, aktualizacje DI/CX/FLAGS i końcowe queue. Pełny filtr REP dla wszystkich opkodów przechodzi; wielokrotne przebiegi warunkowe CMPS/SCAS są objęte pełnym źródłem.
- [x] Częściowo: `REP/REPNE MOVSB` (`F2/F3 A4`) przechodzi 5/5 źródłowych wielokrotnych przypadków z próbki (CX 36, 48, 54, 78, 124), wszystkie z DF=1; pokryto kolejkę początkową długości 5 i 6 bajtów, stan CPU, pamięć, kolejkę i każdy T-state. Pełny filtr A4 przechodzi 2 000/2 000, a bezpośredni test CX=1 obejmuje wszystkie opcode.
- [x] Częściowo: `REP/REPNE STOSB` (`F2/F3 AA`) przechodzi 4/4 źródłowe wielokrotne przypadki z próbki (CX 14, 78, 90, 112), DF=0/1, kolejki długości 5/6, w tym cel nieparzysty; SI/DI/CX, RAM, kolejka i wszystkie T-states zgodne. `STOSW` ma 3/3 lokalne przypadki REP; CX=1 przechodzi test dla wszystkich opcode, a pełny AA 2 000/2 000.
- [x] Częściowo: `REP/REPNE STOSW` (`F2/F3 AB`) przechodzi 3/3 źródłowe wielokrotne przypadki (CX 38, 70, 112), DF=0/1, cele wyrównane i nieparzyste, kolejki 5/6; oba bajty celu, DI/CX, RAM, kolejka i każdy T-state zgodne. CX=1 przechodzi test dla wszystkich opcode, a pełny AB 2 000/2 000.
- [x] Częściowo: `REPE/REPNE CMPSB` (`F3/F2 A6`) przechodzi 3/3 lokalne fixture (testy 6, 8, 14): dwa przebiegi kończą się po pierwszym porównaniu, a jeden wykonuje 92 iteracje; SI/DI/CX/FLAGS, RAM, kolejka i każdy T-state zgodne. Pełny filtr A6 przechodzi 2 000/2 000; CX=1 pokrywa bezpośredni test.
- [x] Częściowo: `REPE/REPNE CMPSW` (`F3/F2 A7`) przechodzi 6/6 lokalnych fixture (testy 8, 9, 14, 17–19), z adresami wyrównanymi i nieparzystymi dla obu segmentów oraz kolejkami 5/6; oba odczyty, FLAGS, SI/DI/CX, kolejka i każdy T-state zgodne. Pełny filtr A7 przechodzi 2 000/2 000, w tym warianty z segment override.
- [x] Częściowo: `REPE/REPNE SCASW` (`F3/F2 AF`) przechodzi 3/3 lokalne fixture (testy 16–18), obejmując cel wyrównany i nieparzysty oraz kolejki 5/6; porównanie AX, FLAGS, DI/CX, RAM, kolejka i każdy T-state zgodne. Pełny filtr AF przechodzi 2 000/2 000.
- [x] Częściowo: `REPE/REPNE SCASB` przechodzi 4/4 lokalne fixture wielokrotne (AE testy 7, 16–18), z DF=0/1, celem wyrównanym i nieparzystym, kolejkami 5/6 oraz przypadkiem przerwania przez warunek; FLAGS, DI/CX, RAM, kolejka i każdy T-state zgodne.
- [x] Częściowo: `REPE/REPNE SCASW` przechodzi 5/5 lokalnych fixture wielokrotnych (AF testy 9, 15–18), w tym skrócone przebiegi warunkowe, DF=0/1 oraz cel wyrównany i nieparzysty; AX/FLAGS/DI/CX, RAM, kolejka i każdy T-state zgodne.
- [x] Częściowo: `REP/REPNE LODSB` (`F2/F3 AC`) przechodzi 4/4 źródłowe wielokrotne przypadki z próbki (CX 20, 48, 102, 116), DF=0/1, kolejki długości 5/6; AX/SI/CX, RAM, kolejka i wszystkie T-states zgodne. `LODSW`, CX=1 jest pokryte bezpośrednim testem dla wszystkich opcode; pełny AC przechodzi 2 000/2 000.
- [x] Częściowo: `REP/REPNE LODSW` (`F2/F3 AD`) przechodzi 3/3 źródłowe wielokrotne przypadki (CX 66, 68, 74), wszystkie ze źródłem nieparzystym, DF=0/1 i kolejką 5 bajtów; AX/SI/CX, rozdzielony odczyt obu bajtów, RAM, kolejka i każdy T-state zgodne. Źródła wyrównane, CX=1 i pełny filtr AD przechodzi 2 000/2 000.
- [x] Manifesty A9 deklarują SHA źródła, liczność próbek i limit T-state na podstawie najdłuższego rekordu; dwa testy integralności przechodzą (A9String 11/11 plików, A9Io 8/8 plików).
- [x] Zakres `INS/OUTS` rozstrzygnięto z instrukcji: 8086 nie używa `6C`–`6F`, 80186 dodaje INS/OUTS, a przypięte fixture 8086 opisują te wartości jako aliasy warunkowych skoków; dekodowanie aliasów odnotowano do A10.
- [x] `MOVSB/MOVSW`, `STOSB/STOSW` i `LODSB/LODSW`: po 20 fixture z przypiętego SHA, pełny stan CPU, RAM, kolejka oraz każdy T-state. Test lokalny wykonuje wszystkie 20 rekordów dla każdego z 10 opkodów stringów; pełne filtry `A5`, `AA`, `AD` przechodzą 6 000/6 000. Wielokrotne `REP STOSW` ma też 3/3 lokalne fixture.
- [x] `CMPSB/CMPSW` i `SCASB/SCASW`: po 20 fixture, wszystkie zdefiniowane FLAGS i każdy T-state. Test lokalny wykonuje wszystkie 20 rekordów dla każdego opkodu; pełne filtry upstream `A6`, `A7`, `AE`, `AF` przechodzą 8 000/8 000.
- [x] Każda rodzina string ma przypadki bez REP oraz z `REP`, `REPE/REPZ` i `REPNE/REPNZ`, o ile występują w upstream; wszystkie źródłowe fixture są uruchamiane bez skracania w 20 000/20 000 pełnym filtrze.
- [x] Powtarzane string: `CX=0`, `CX=1`, wiele iteracji, DF=0/1, warunek kontynuacji i przerwania powtarzania; SI/DI/CX oraz pamięć porównane dokładnie. Pełne upstream potwierdza dla wszystkich opcode `CX=0`, wiele iteracji i DF=0/1; dla `CMPS`/`SCAS` są przypadki kontynuacji i zakończenia warunkowego. Ponieważ upstream nie zawiera `CX=1`, dodano bezpośredni test pojedynczej iteracji dla wszystkich 10 opcode; 10/10 przechodzi.
- [x] Segment źródłowy stringów: DS domyślne i override; cel MOVS/STOS/SCAS zawsze ES; porównane fizyczne adresy, RAM i status segmentu na magistrali. 61/61 lokalnych bezpośrednio prefiksowanych fixture, 45/45 kombinacji segment+REP oraz wszystkie 20 000 źródłowych filtrów string przechodzą pełny stan, RAM, kolejkę i każdy T-state.
- [x] Kombinacje segment+REP dla wszystkich 10 opkodów string: 45/45 lokalnych fixture przechodzi pełny stan CPU, RAM, kolejkę i każdy T-state; test obejmuje kolejki początkowe q=5 i q=6.
- [x] `IN/OUT` immediate i DX, byte i word: wszystkie 20 lokalnych fixture dla każdego opkodu E4–E7 i EC–EF przechodzą pełny stan i każdy T-state; łącznie 160/160. Pełne filtry upstream E4–E7 i EC–EF: 16 000/16 000, 0 pominięć, 0 niezgodności.
- [x] Każdy z 160 przebiegów I/O ma nowy dziennik `IPortBus`; w kolejności sprawdzane są porty i dane, bez współdzielonego stanu między przypadkami.
- [x] Testy I/O nie opierają się wyłącznie na końcowym AX: runner porównuje cykle IOR/IOW, a dziennik weryfikuje port(y) i wartości.
- [x] Jawny limit T-state na przypadek obejmuje cały fixture bez skracania; raport błędu identyfikuje opcode, numer testu i hash. Manifest może podać `maxTStates`; pełne filtry użyły limitu równego długości najdłuższego rekordu danego opkodu i przebiegły bez timeoutu.
- [x] Pełne źródłowe filtry A9 uruchomione osobno dla każdej rodziny; raport podaje passed/skipped/mismatched. Stringi: 20 000/20 000; I/O E4–E7 i EC–EF: 16 000/16 000; razem 36 000/36 000, 0 pominięć, 0 niezgodności, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.
- [x] 2026-10-24 po rebase na A8: build Debug/Release: 0 ostrzeżeń i błędów; testy Release: 11 179/11 179, 0 skipów (Intel8086 8 379/8 379); `dotnet format --verify-no-changes --severity warn` czysty; pełne filtry upstream A9: 36 000/36 000; OpcodeInfo: 512/512 wierszy zgodnych, 0 niezgodności.

## Pułapki

1. `CX=0` z REP i zachowanie IP muszą odpowiadać źródłu; nie wykonać iteracji „dla wygody”.
2. SI/DI zmieniają się zależnie od DF i rozmiaru operandu.
3. Segment źródła i docelowy są różne role; segment override nie powinien być stosowany bezwarunkowo do obu.
4. REP może być obserwowany jako wykonanie częściowe na przerwaniach; to osobny kontrakt w A11.
5. I/O space nie jest RAM-em i musi mieć jawny model zwracanych danych. README upstream wymaga 0xFF dla odczytów IN bez wait states; test transferu słownego i banków magistrali pozostaje osobną asercją.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

### 2026-10-24 — diagnostyka segment+REP

Przygotowano i uruchomiono prototyp testu wybierający 45 lokalnych fixture z prefiksem segmentu i REP/REPNE oraz zmieniono kolejność diagnostyki w runnerze, aby pierwsza rozbieżność T-state była widoczna. Test zgłosił błąd dla wszystkich 10 grup opkodów; każda grupa zatrzymuje się przy pierwszym wadliwym rekordzie, więc nie potwierdzono wykonania wszystkich 45 przypadków. Fixture pokazują różne sekwencje zależne od długości kolejki; pierwsza implementacja emitowała niewłaściwe CODE/prefetch.

Po kolejnej korekcie rozdzielającej układ kolejki 5/6 bajtów nadal nie uzyskano zgodności: przebieg testu ponownie zgłosił 10/10 grup niepowodzeń; część przypadków przekroczyła limit przez brak zakończenia, pozostałe rozminęły się w prefiksowych cyklach. Prototyp wykonawczy i testowy wycofano, by nie zostawić regresji w branchu. Wnioski zachowano: osobno modelować globalny i lokalny licznik T-state oraz zweryfikować pojedynczy q=5 i q=6 przypadek, zanim uruchomi się pełny zestaw.

### 2026-10-24 — pierwsze przejście A4 z segment+REP

Dodano obsługę dwóch prefiksów i lokalne mapowanie licznika handlera REP względem globalnego czasu instrukcji. Pełna grupa A4 przechodzi; w przebiegu wszystkich grup wynik to 1/10. Pozostałe dziewięć grup zatrzymuje się na różnicach CODE-fetch (adres/pozycja) lub końcowej kolejce; część A4/CX=0 i pozostałe fixture nie uznaje się za zaliczone poza przechodzącą grupą. Diagnostyka T-state została przeniesiona przed porównanie rejestrów/kolejki w runnerze, dzięki czemu rozbieżność magistrali nie jest ukrywana przez stan końcowy. Następny krok: naprawić rozjazd q=5, sprawdzić q=6 po tej zmianie, a potem wyrównać końcowe zapełnienie kolejki i uruchomić wszystkie 45 rekordów.

### 2026-10-24 — pełne lokalne fixture segment+REP

Rozdzielono harmonogram początkowych faz dla q=5 i q=6, a licznik handlera REP mapuje się na lokalny T-state bez zmiany globalnego limitu instrukcji. Dla q=5 przesunięcie adresu CODE następuje po T2, a pobrane bajty wracają do kolejki. 45/45 fixture przechodzi we wszystkich 10 grupach (`A4`–`A7`, `AA`–`AF`), z pełnym porównaniem CPU, RAM, queue i każdego T-state. Wektor `MOVSW` A5 test 9 ujawnił dodatkowo zawijanie drugiego bajtu słowa przez 16-bitowy offset DI; adres drugiego transferu obliczany jest teraz w ES po zawinięciu offsetu, zamiast dodania 1 do adresu fizycznego. Regresja bezpośrednich override segmentu przechodzi 61/61, a suite Intel8086 7 331/7 331. Pełne filtry upstream A9 i cała solucja pozostają do weryfikacji.

Pełna weryfikacja solucji po tej zmianie: build Debug i Release po 0 ostrzeżeń/błędów; testy Release 10 131/10 131, 0 skipów; format czysty; OpcodeInfo 512/512 wierszy zgodnych, 0 mismatch.

### 2026-10-24 — rebase A9 na A8 i końcowa weryfikacja

Branch `A9-8086-string-io` oparto na zakończonym A8. Podczas adaptacji rozdzielonych plików zachowano obsługę prefiksów w partialach; poprawiono dekoder kombinacji segment+REP z `CX=0`, przesunięcie fazy zapisu `MOVSW` dla krótkiej kolejki oraz liczniki cykli I/O. Testy wektorów string A9 przechodzą 131/131; pełna suite Intel8086 przechodzi 8 379/8 379. Pełne filtry przypiętego źródła przechodzą: stringi 20 000/20 000, I/O 16 000/16 000, razem 36 000/36 000, 0 skipów i 0 niezgodności.

Pełna weryfikacja solucji: build Debug i Release bez ostrzeżeń/błędów; testy Release 11 179/11 179, 0 skipów; `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn` czysty; OpcodeInfo 512/512, 0 niezgodności.

### 2026-10-24 — analiza nieudanej próby REP MOVSB

Uruchomiono pięć źródłowych przypadków A4 z wieloma iteracjami (CX 36, 48, 54, 78 i 124): wynik próby wykonania 0/5. Dla początkowej kolejki 5 bajtów rozbieżność dotyczy liczności kolejki, a dla kolejki 6 bajtów wektor dochodzi do końcowych T-states, po czym różni się status fazy zapisu. Próbną implementację cofnięto; żadnego z tych wyników nie uznano za weryfikację REP MOVSB. Następny krok wymaga zarejestrowania rzeczywistych T-states rdzenia i zestawienia ich indeks po indeksie z rekordami A4, zanim harmonogram zostanie ponownie zmieniony.

### 2026-10-24 — REP MOVSB

Dodano wykonanie F2/F3 A4 dla niezerowego CX. Wektor ujawnił, że po wyzerowaniu CX w ostatniej iteracji dispatcher nie może ponownie wejść w ścieżkę startową `CX=0`; ograniczono ją do pierwszego T-state instrukcji. Uwzględniono dwa układy prefetch dla początkowej kolejki 5/6 bajtów. Pięć wielokrotnych wektorów przechodzi 5/5 z pełnym porównaniem T-states, RAM, SI/DI/CX i kolejki; wszystkie próbki mają DF=1. Oba przypadki MOVSB z CX=0 przechodzą 2/2. Powtarzane MOVSB z DF=0, pełny filtr A4 i inne formy REP są otwarte.

### 2026-10-24 — REP STOSB

Dodano F2/F3 AA dla niezerowego CX. Cztery wektory przechodzą 4/4 (CX 14, 78, 90, 112), z DF=0 i DF=1 oraz początkową kolejką 5/6. Weryfikacja celu nieparzystego wykryła błąd wspólnego zapisu bajtowego: `STOSB` zapisywał AH zamiast AL na nieparzysty adres; transfer poprawiono tak, by wartość RAM pochodziła zawsze z AL, a położenie bajtu na magistrali zależało od parzystości adresu. Regresja nieprefiksowanego STOS i wszystkie testy REP string przechodzą 17/17; pełna suite Intel8086 przechodzi 7 267/7 267. `STOSW` ma 3/3 lokalne przypadki REP; CX=1 przechodzi test dla wszystkich opcode, a pełny AA 2 000/2 000.

### 2026-10-24 — REP LODSB

Dodano F2/F3 AC dla niezerowego CX. Cztery przypadki przechodzą 4/4 (CX 20, 48, 102, 116), pokrywając oba kierunki DF i kolejki 5/6 bajtów; potwierdzają pełny stan AX/SI/CX, pamięć, queue i wszystkie T-states. Pełna suite Intel8086 po tej zmianie: 7 271/7 271; `LODSW`, CX=1 jest pokryte bezpośrednim testem dla wszystkich opcode; pełny AC przechodzi 2 000/2 000.

### 2026-10-24 — REP LODSW

Dodano F2/F3 AD dla źródła nieparzystego: osiem T-states odczytuje osobno bajty n i n+1, a wartość AX składa się po drugim T3. Trzy wektory przechodzą 3/3 (CX 66, 68, 74), DF=0/1, wraz z SI/CX, kolejką, RAM i wszystkimi T-states. Źródła wyrównane oraz pełny filtr AD nie są pokryte.

### 2026-10-24 — REP STOSW

Dodano F2/F3 AB dla zapisów word. Trzy wektory przechodzą 3/3: wyrównany cel używa jednej fazy word, a nieparzysty rozdziela bajty na dwa transfery; sprawdzono DF=0/1, DI/CX, pełną pamięć, kolejkę i wszystkie T-states. Pełny filtr AB i CX=1 pozostają otwarte.

Wszystkie dostępne nieprefiksowane fixture STOS/SCAS w próbkach A9 przechodzą 18/18; dodatkowo 61/61 bezpośrednich przypadków segment-prefiksowanych dla wszystkich 10 opcode string przechodzi. Pełna solucja Release 10 121/10 121 (0 skipów), Intel8086 7 321/7 321, buildy Debug/Release bez ostrzeżeń, format czysty, OpcodeInfo 512/512 bez niezgodności. Pełnych filtrów upstream A9 nie uruchomiono.

Build Debug/Release, wszystkie testy solucji, format i OpcodeInfo przechodzą; szczegóły liczbowe są w checkliście powyżej. Pełnych źródłowych filtrów A9 nie uruchomiono.

### 2026-10-24 — branch A9 oparty na A8

Branch `A9-8086-string-io` został przestawiony na zakończony commit A8 `8e5211c`. Włączono tylko adapter portów jako odrębny commit `cc53ab6`; wcześniejsze commity A9 dla Jcc i helperów A8 pominięto, bo ich pliki i wektory są już identyczne w A8. Potwierdzono bazę przez historię Git. Implementacja `IN/OUT`, łańcuchów i REP pozostaje otwarta.



### 2026-10-24 — nieprefiksowane MOVSW

Dodano `A5` dla wyrównanego transferu słowa i źródła pod nieparzystym adresem. Przy źródle nieparzystym dwa cykle pamięciowe pobierają osobno bajty słowa; BHE i dane magistrali porównywane są dokładnie. Cztery nieprefiksowane rekordy dostępne w próbce A5 przechodzą 4/4, w tym stan CPU, RAM, kolejka i każdy T-state. Pełne 2 000 rekordów A5 i destynacje nieparzyste pozostają otwarte.

### 2026-10-24 — wyjście słowne OUT

Dodano `OUT AX,imm8` i `OUT AX,DX`. Parzyste porty używają transferu word, a nieparzyste dwóch cykli z właściwymi liniami BHE; dane zachowują kolejność little-endian AX na portach n, n+1. Wszystkie 40 próbek przechodzi, a dziennik sprawdza wartości obu bajtów. Pełne filtry źródła pozostają otwarte.

### 2026-10-24 — wejście słowne IN

Dodano `IN AX,imm8` i `IN AX,DX`. Parzysty port wykonuje transfer word, a nieparzysty port dwa transfery bajtowe z prawidłowym BHE i złożeniem AX. Wszystkie 40 próbek lokalnych przechodzi, a dziennik potwierdza sekwencję portów `n`, `n+1` dla obu trybów. Pełne źródłowe filtry pozostają otwarte.

### 2026-10-24 — wyjście bajtowe OUT

Dodano `OUT imm8,AL` i `OUT DX,AL`, z cyklami IOW i wywołaniem magistrali w fazie danych. Wszystkie 40 lokalnych rekordów przechodzi, a dziennik wstrzykniętej magistrali potwierdza port oraz bajt AL. Wersje word i pełne filtry źródłowe pozostają otwarte.

### 2026-10-24 — wejście bajtowe IN

Rdzeń przyjmuje opcjonalny `IPortBus` przez adapter `Intel8086PortBus`. `IN AL,imm8` i `IN AL,DX` wykonują wejściowe cykle IOR, ustawiają AL bez zmiany AH, a niepodłączone wejście daje `0xFF`. Wszystkie 40 rekordów lokalnych przechodzi; test dziennika sprawdza port immediate i DX. `IN AX`, `OUT` i pełne źródłowe zbiory pozostają otwarte.

### 2026-10-24 — wielokrotne REPNE SCASB

Dodano cztery iteracje `REPNE SCASB`: po każdej iteracji porównanie AL z ES:DI, aktualizację FLAGS/DI/CX i sprawdzenie ZF oraz CX. Wektor upstream `AE` test 17 przechodzi z pełną sekwencją 71 T-states, w tym każdym odczytem pamięciowym. Pozostałe stringi powtarzane oraz wcześniejsze zatrzymanie przez warunek nie są jeszcze aktywowane.

### 2026-10-24 — REP string z CX=0

Dodano obsługę prefiksu F2/F3 dla stringów z `CX=0`: instrukcja nie wykonuje dostępu do danych, zachowuje SI/DI i FLAGS, zużywa dwa bajty instrukcji i wykonuje cykle prefetch zgodne z fixture. `REP MOVSB` oraz `REPNE SCASB` przechodzą 2/2. Powtórzenia z niezerowym CX nadal nie są implementowane.

### 2026-10-24 — próbki nieprefiksowanych CMPSB/CMPSW

Dodano `A6`/`A7`: odczyt operandów odpowiednio z DS:SI i ES:DI, porównanie źródło minus cel przez ALU, aktualizację FLAGS oraz SI/DI zależną od DF. Osiem wybranych wektorów przechodzi 8/8, w tym word z nieparzystego źródła. Weryfikacja obejmuje cykle i status segmentów na magistrali; pełna próbka i REP pozostają otwarte.

### 2026-10-24 — próbki nieprefiksowanych SCASB/SCASW

Dodano `AE`/`AF`: porównanie AL/AX z ES:DI przez istniejące ALU, zapis FLAGS i aktualizację DI zależną od DF. Wybrane wektory przechodzą 3/3; zweryfikowano status ES w całym cyklu odczytu, adres parzysty i rozdzielony odczyt słowa spod nieparzystego adresu. Pełna próbka i REP pozostają otwarte.

### 2026-10-24 — próbki nieprefiksowanych LODSB/LODSW

Dodano `AC`/`AD`: odczyt DS:SI, ładowanie AL lub AX oraz aktualizację SI z uwzględnieniem DF. Dziesięć wybranych przypadków przechodzi 10/10 wraz ze stanem CPU, RAM, kolejką i każdym T-state; dla `LODSW` sprawdzono adresy źródłowe parzyste i nieparzyste. Pełne próbki oraz segment override/REP pozostają otwarte.

### 2026-10-24 — próbki nieprefiksowanych STOSB/STOSW

Dodano wykonanie `STOSB` i `STOSW` oraz aktywowano cztery rekordy z przypiętych plików `AA`/`AB`. Porównanie CPU, RAM, kolejki i każdego T-state przechodzi 4/4; pokryto zapis słowa przy parzystym i nieparzystym adresie ES:DI. Pozostałe 36 rekordów próbek, pełne filtry, segment override i REP nie są zweryfikowane.

### 2026-10-24 — nieprefiksowane MOVSB

Dodano nieprefiksowane `A4` z odczytem DS:SI, zapisem ES:DI i aktualizacją SI/DI zależną od DF. Pełne porównanie wektorowe stanu CPU, RAM, kolejki i wszystkich T-states przechodzi dla 20/20 filtrowanych przypadków DF=0 oraz 2/2 nieprefiksowanych przypadków DF=1 z próbki upstream. Pokryte są początkowe kolejki długości 5 i 6; krótsza wymaga dodatkowego cyklu CODE/prefetch. Pełne 2 000 rekordów A4 pozostają nieuruchomione.

Weryfikacja bieżącej suite Intel8086 po MOVSB/MOVSW: 7 217/7 217, 0 skipów. Zakres A9 nadal nie obejmuje pozostałych stringów, REP ani wykonania IN/OUT.

### 2026-10-23 — adapter portów

Dodano `Intel8086PortBus` w warstwie Intel8086, bez zmiany neutralnego `IPortBus`: niezmapowane wejście zwraca `0xFF`, a podłączona magistrala przyjmuje `Input`/`Output`. Dwa testy adaptera przechodzą.

Rdzeń nie wykonuje jeszcze `IN`/`OUT`, nie zapisuje cykli I/O, a `MOVS/CMPS/STOS/LODS/SCAS` i REP nie są zaimplementowane. To jest adapter, nie weryfikacja opkodów A9. Pełne wektory nieuruchomione.

## Dziennik postępu

### 2026-10-24 — pierwszy fixture MOVSB z nadpisaniem SS

Dekoder przyjmuje bezpośrednie nadpisania segmentu dla `MOVSB`, `CMPSB`, `STOSW`, `LODSB` i `LODSW`; źródłowe odczyty używają właściwego segmentu, a zapis/cel porównania pozostaje ES. Wszystkie pasujące rekordy w lokalnych próbkach przechodzą 34/34 z pełnym stanem, RAM i każdym T-state. Pozostałe opcode i prefiksy segment+REP nie są obsłużone ani zweryfikowane.

### 2026-10-24 — bezpośrednie nadpisania segmentu dla wszystkich stringów

Rozszerzono dekoder i fazy prefiksu na wszystkie opcode `A4`–`AF`, uwzględniając różnice czasu dla krótkiej kolejki, rozdzielonego `MOVSW` na nieparzystym celu oraz słownego porównania/skanowania. Wszystkie bezpośrednie segment-prefiksowane przypadki w lokalnych próbkach przechodzą 61/61 z pełnym stanem, RAM, kolejką i każdym T-state. Kombinacje segment+REP i pełne źródłowe filtry pozostają otwarte.

Weryfikacja całej solucji: build Debug i Release bez ostrzeżeń i błędów; testy Release 10 121/10 121, w tym Intel8086 7 321/7 321, 0 skipów; format czysty; OpcodeInfo zgodne 512/512, 0 niezgodności.

Weryfikacja całej solucji: build Debug i Release bez ostrzeżeń i błędów; testy Release 10 112/10 112, w tym Intel8086 7 312/7 312, 0 skipów; format czysty; OpcodeInfo zgodne 512/512, 0 niezgodności. Pełnych filtrów upstream A9 nie uruchomiono.

| data | rodzina | pass/manifest | próbkowe przypadki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | adapter niezmapowanych portów | 2 testy adaptera | CPU runner A9: 0 | string/REP/IN/OUT i cykle otwarte; pełne wektory nieuruchomione |
