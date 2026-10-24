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
- [x] Częściowo: nieprefiksowane `STOSB/STOSW` przechodzą 4/4 wektory (`AA` test 0; `AB` testy 0, 16, 19), w tym zapis słowa wyrównany i rozdzielony dla nieparzystego celu; porównano stan, RAM, kolejkę i każdy T-state. Pełne fixture i prefiksy pozostają otwarte.
- [x] Częściowo: nieprefiksowane `LODSB/LODSW` przechodzą 10/10 wybranych rekordów (`AC` testy 6, 10, 13, 15; `AD` testy 1, 3, 12, 14, 16, 18); pełny stan, RAM, kolejka i każdy T-state zgodne, w tym odczyt słowa z nieparzystego adresu. Pełne fixture, prefiksy i override segmentu pozostają otwarte.
- [x] Częściowo: nieprefiksowane `SCASB/SCASW` przechodzą 3/3 wybrane wektory (`AE` test 11; `AF` testy 4, 17); pełny stan, FLAGS, RAM, kolejka i każdy T-state zgodne, w tym word przy parzystym i nieparzystym ES:DI. Pełne 20 rekordów każdej formy pozostają otwarte.
- [x] Manifesty A9 deklarują SHA źródła, liczność próbek i limit T-state na podstawie najdłuższego rekordu; dwa testy integralności przechodzą (A9String 11/11 plików, A9Io 8/8 plików).
- [x] Zakres `INS/OUTS` rozstrzygnięto z instrukcji: 8086 nie używa `6C`–`6F`, 80186 dodaje INS/OUTS, a przypięte fixture 8086 opisują te wartości jako aliasy warunkowych skoków; dekodowanie aliasów odnotowano do A10.
- [ ] `MOVSB/MOVSW`, `STOSB/STOSW` i `LODSB/LODSW`: po 20 fixture z przypiętego SHA, pełny stan CPU, RAM, kolejka oraz każdy T-state.
- [ ] `CMPSB/CMPSW` i `SCASB/SCASW`: po 20 fixture, wszystkie zdefiniowane FLAGS i każdy T-state.
- [ ] Każda rodzina string ma przypadki bez REP oraz z `REP`, `REPE/REPZ` i `REPNE/REPNZ`, o ile występują w upstream; fixture pozostają danymi źródłowymi bez ręcznego skracania.
- [ ] Powtarzane string: `CX=0`, `CX=1`, wiele iteracji, DF=0/1, warunek kontynuacji i przerwania powtarzania; SI/DI/CX oraz pamięć porównane dokładnie.
- [ ] Segment źródłowy stringów: DS domyślne i override; cel MOVS/STOS/SCAS zawsze ES; porównane fizyczne adresy, RAM i status segmentu na magistrali.
- [ ] `IN`/`OUT` immediate i DX, byte i word: lokalne fixture z kolejnością operacji portu, adresem, wartością, kierunkiem, stanem CPU i cyklami I/O.
- [ ] Magistrala portów testowana przez deterministyczny dziennik lokalny dla każdego przebiegu; brak współdzielonego stanu między testami.
- [ ] Brak testów `IN/OUT` opartych wyłącznie na końcowym AX; cykle IOR/IOW i adres/dane portu są porównywane.
- [ ] Jawny limit T-state na przypadek obejmuje cały fixture bez skracania; raport błędu identyfikuje opcode, numer testu i hash.
- [ ] Pełne źródłowe filtry A9 uruchomione osobno dla każdej rodziny; raport podaje passed/skipped/mismatched. Pełny zbiór spoza repo pozostaje jawnie nieuruchomiony, jeśli go nie pobrano.
- [ ] Build Debug/Release, pełne testy solucji, format i OpcodeInfo przechodzą bez ostrzeżeń i błędów.

## Pułapki

1. `CX=0` z REP i zachowanie IP muszą odpowiadać źródłu; nie wykonać iteracji „dla wygody”.
2. SI/DI zmieniają się zależnie od DF i rozmiaru operandu.
3. Segment źródła i docelowy są różne role; segment override nie powinien być stosowany bezwarunkowo do obu.
4. REP może być obserwowany jako wykonanie częściowe na przerwaniach; to osobny kontrakt w A11.
5. I/O space nie jest RAM-em i musi mieć jawny model zwracanych danych. README upstream wymaga 0xFF dla odczytów IN bez wait states; test transferu słownego i banków magistrali pozostaje osobną asercją.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

### 2026-10-24 — branch A9 oparty na A8

Branch `A9-8086-string-io` został przestawiony na zakończony commit A8 `8e5211c`. Włączono tylko adapter portów jako odrębny commit `cc53ab6`; wcześniejsze commity A9 dla Jcc i helperów A8 pominięto, bo ich pliki i wektory są już identyczne w A8. Potwierdzono bazę przez historię Git. Implementacja `IN/OUT`, łańcuchów i REP pozostaje otwarta.



### 2026-10-24 — nieprefiksowane MOVSW

Dodano `A5` dla wyrównanego transferu słowa i źródła pod nieparzystym adresem. Przy źródle nieparzystym dwa cykle pamięciowe pobierają osobno bajty słowa; BHE i dane magistrali porównywane są dokładnie. Cztery nieprefiksowane rekordy dostępne w próbce A5 przechodzą 4/4, w tym stan CPU, RAM, kolejka i każdy T-state. Pełne 2 000 rekordów A5 i destynacje nieparzyste pozostają otwarte.

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

| data | rodzina | pass/manifest | próbkowe przypadki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | adapter niezmapowanych portów | 2 testy adaptera | CPU runner A9: 0 | string/REP/IN/OUT i cykle otwarte; pełne wektory nieuruchomione |
