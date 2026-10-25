# Etap A12 — pełna weryfikacja i zamknięcie

Poziom: **V. Gotowość projektu** · Zależność: A0–A11

## Plan walidacji

1. Potwierdzić SHA w README wektorów i checkout danych dokładnie na tym SHA.
2. Uruchomić próbki repo offline; podać liczbę plików i rekordów oraz wynik każdego etapu.
3. Uruchomić wszystkie źródłowe `*.json.gz` w deklarowanym zakresie poza repo przez streaming runner, łącznie z plikami rozszerzeń ModR/M. Jeśli pierwotna inwentaryzacja wykaże inną liczbę, raportować realną liczbę per plik.
4. Raportować oddzielnie passed, skipped, mismatched, parse errors i timeout; żadnej kategorii nie wliczać do passed.
5. Każdy skip zawiera powód, konkretny przypadek/hash, rozbieżność i etap naprawy. Nie wprowadzać masowego skip dla nierozpoznanych pól.
6. Dla każdego mismatch zapisać minimalny przypadek, oczekiwany/aktualny stan, pierwszą różnicę cyklu i analizę źródła. Nie modyfikować wektora.
7. Wykonać niezależny test programowy/ROM tylko wtedy, gdy źródło, licencja, uruchamianie i warunki sukcesu są jawne.
8. Uruchomić wymagane polecenia jakości repo z README planu; zanotować 0 ostrzeżeń/błędów i liczbę testów.
9. Sprawdzić linki, manifest, przypięte licencje, dokumenty instrukcji i stan gałęzi. Nie pushować.

## Podetap: `FF /6` — `PUSH r/m16`

Cel: usunąć jawne skipy dla nieprefiksowanego `PUSH r/m16` i dodać obsługę bezpośrednią rejestru oraz pamięci w 8086.

Zakres obejmuje nieprefiksowane rozszerzenie ModR/M `/6` w pliku przypiętego źródła `FF.6.json.gz`: rejestry (`mod=3`), wszystkie formy pamięci 16-bitowego adresowania (w tym displacement i `mod=00,r/m=110`), transfer słowa do `SS:SP`, zawijanie adresu fizycznego oraz kolejkę prefetch. Inwentaryzacja pliku wykazała 2 000 rekordów: 1 014 nieprefiksowanych (`252` rejestrowe i `762` pamięciowe) oraz 986 z prefiksem segmentowym. Ten podetap wdraża tylko 1 014 nieprefiksowanych rekordów; prefiksowane formy pozostają osobno raportowanym zakresem A12. Zachowanie i harmonogram cykli muszą wynikać z tych 1 014 rekordów przypiętych SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.

### Testy i kryteria podetapu `FF /6`

- [x] Dwie próbki lokalne (20 rejestrowych i 20 pamięciowych) pochodzą dokładnie ze źródła przypiętego SHA i zachowują niezmienione rekordy upstream.
- [x] Wszystkie 1 014 nieprefiksowanych rekordów `FF.6.json.gz` przechodzą porównanie rejestrów, pamięci, kolejki i każdego T-state; 0 skipów, mismatchy, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-ff6-full-report.json`.
- [x] Pełny źródłowy wycinek obejmuje `mod=00/01/10/11`, wszystkie `r/m`, 17 bezpośrednich `DS:disp16`, oba parytety operandów pamięci i obu adresów stosu; w przypiętych rekordach nie występuje transfer zawijający granicę 20-bitową (najwyższy adres T1 zapisu stosu: `0xFFF28`).
- [x] Pełny zestaw 646 000 rekordów przebiega ponownie: 492 533 passed, 153 467 skipped, 0 mismatchy, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-full-after-ff6.json`. Skipy zmalały o 1 014.
- [x] Test Intel8086 12 911/12 911 i pełna solucja Release 15 711/15 711; build Debug/Release po 0 ostrzeżeń/błędów; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych.
- [x] Implementację i aktualizację raportu zatwierdzono osobnym commitem po osobnym commicie planu; nie pushować.

## Następny podetap: `FF /7` — alias `PUSH r/m16`

Cel: objąć obserwowany alias `FF /7`, który przypięte metadata mapuje na `FF /6` (`PUSH r/m16`), z osobnym dowodem wektorowym. Użyć tej samej semantyki i ścieżki cykli co `/6`, ale zachować osobne testy `/7`.

Zakres: tylko nieprefiksowane rekordy z `FF.7.json.gz`. Inwentaryzacja przypiętego SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` wykazała 2 000 rekordów: 996 nieprefiksowanych (238 rejestrowych, 758 pamięciowych) i 1 004 segment-prefiksowane. Prefiksowane aliasy pozostają osobną luką; nie rozszerzać ich statusu bez odrębnej weryfikacji.

### Testy i kryteria podetapu `FF /7`

- [x] Lokalne próbki 20 rejestrowych i 20 pamięciowych pochodzą dokładnie z `FF.7.json.gz` przypiętego SHA.
- [x] Wszystkie 996 nieprefiksowanych wektorów przechodzą pełne porównanie stanu, RAM, kolejki i każdego T-state: 0 skipów, mismatchy, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-ff7-full-report.json`.
- [x] Pełny przebieg 646 000 rekordów z kanonicznym manifestem flag: 493 529 passed, 152 471 skipped, 0 mismatched, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-full-after-ff7-canonical.json`. Zmiana wobec poprzedniego pełnego przebiegu: +996 passed, −996 skipped. 1 004 prefiksowane rekordy `/7` pozostają skipami.
- [x] Build Debug/Release po 0 ostrzeżeń/błędów; pełne testy Release 15 751/15 751 (Intel8086 12 951/12 951); formatter verify bez zmian; OpcodeInfo 512/512 zgodnych; walidator manifestu 987 wpisów / 19 471 wskazań, 323 pliki źródła / 646 000 rekordów.
- [x] Plan zapisany osobnym commitem `92d969f`, implementacja osobnym commitem `ff22d92`; nie pushować.

## Następny podetap: segment-prefiksowane `FF /6` i `/7`

Cel: zweryfikować `PUSH r/m16` z prefiksem ES, CS, SS lub DS dla obu kodowań rozszerzenia ModR/M `/6` i aliasu `/7`. Bazą semantyczną są już przechodzące nieprefiksowane formy. Zakres nie obejmuje REP/LOCK ani innych prefiksów.

Zakres z przypiętego źródła SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: 1 990 rekordów segment-prefiksowanych — 986 `FF /6` (229 rejestrowych, 757 pamięciowych) i 1 004 `FF /7` (259 rejestrowych, 745 pamięciowych). Podział według bajtu prefiksu: ES `0x26` 499, CS `0x2E` 527, SS `0x36` 471, DS `0x3E` 493. Zachować testy dla każdego prefiksu, rozszerzenia i klasy operandów. Cykle/prefetch wynikają z tych rekordów.

### Testy i kryteria segment-prefiksowanego FF

- [x] Dodano 16 dokładnych fixture upstream po 20 rekordów: osobno rejestr/pamięć × `/6`/`/7` × ES/CS/SS/DS; walidacja sprawdziła 320 rekordów i manifest.
- [x] Wszystkie 1 990 rekordów przechodzą pełne porównanie stanu, RAM, kolejki i każdego T-state: `FF /6` 986/986, `/7` 1 004/1 004, bez skipów, mismatchy, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-ff-prefix-full-report.json`.
- [x] Pełny zestaw 646 000 rekordów z kanonicznym manifestem metadata: 495 519 passed, 150 481 skipped, 0 mismatched, timeoutów i błędów parsowania; raport `/tmp/vibe-a12-full-after-ff-prefix.json`. Zmiana wobec 493 529/152 471: +1 990 passed, −1 990 skipped.
- [x] Build Debug/Release: po 0 ostrzeżeń/błędów; pełna solucja Release 16 071/16 071, w tym Intel8086 13 271/13 271; formatter verify bez zmian; OpcodeInfo 512/512; walidator manifestu 1 003 wpisy / 19 791 rekordów; testy audytora ISA 12/12.
- [x] Plan `c65ca0e` i implementacja `faa641d` zapisane oddzielnymi commitami na branchu A12; nie pushowano.

## Następny podetap: `A9` — `TEST AX,imm16`

Cel: wdrożyć akumulatorową word-formę `TEST AX,imm16` zgodnie z pełnym przypiętym źródłem, aktualizując określone flagi bez zapisu wyniku do `AX`.

Zakres: wszystkie rekordy `A9.json.gz` z przypiętego SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Inwentaryzacja źródłowego pliku wykazała dokładnie 2 000 rekordów i brak prefiksów; nie ma podziału na formy ModR/M ani prefiksowane. Podstawą semantyki, kolejki prefetch i T-state’ów jest komplet tych rekordów.

### Testy i kryteria podetapu `TEST AX,imm16`

- [x] Dodano próbkę 20 niezmienionych rekordów z początku `A9.json.gz` do aktywnego lokalnego manifestu; programowa zgodność z 20 upstream rekordami potwierdzona.
- [x] Wszystkie 2 000 rekordów `A9.json.gz` przechodzą porównanie rejestrów, FLAGS, RAM, kolejki i każdego T-state: 2 000 passed, 0 skipped/mismatched/timeout/parse errors; raport `/tmp/vibe-a12-a9-full-report.json`.
- [x] `AX` pozostaje niezmieniony, a wynik AND wyznacza flagi według metadanych przypiętego źródła (`flags-mask=0xFFEF`, ignorowany AF).
- [x] Pełny zestaw 646 000 rekordów uruchomiony ponownie: 497 519 passed, 148 481 skipped, 0 mismatched, 0 timeouts, 0 parse errors; raport `/tmp/vibe-a12-full-after-a9.json`. Względem poprzedniego przebiegu passed wzrosło o 2 000, skipy spadły o 2 000.
- [x] Zaktualizowano README wektorów, checklistę i plik wznowienia; lokalny manifest ma 1 004 aktywne fixture / 19 811 rekordów.
- [x] Plan `00560a7` i implementacja `d94ae3c` zapisane w osobnych commitach na branchu A12; build Debug/Release 0 ostrzeżeń/błędów, pełne testy Release 16 091/16 091 (Intel8086 13 291/13 291), formatter verify czysty, manifest 1 004/19 811 i OpcodeInfo 512/512 zielone.

## Zakończony podetap: `A0`–`A3` — `MOV AL/AX ↔ moffs`

Cel: zaimplementować cztery formy bezpośredniego transferu między akumulatorem i adresem `moffs16`: `A0`/`A1` odczyt do `AL`/`AX`, `A2`/`A3` zapis `AL`/`AX` do pamięci. Uwzględnić DS domyślnie oraz nadpisania segmentowe ES/CS/SS/DS, zachowanie FLAGS, magistralę 8/16-bit, kolejkę i prefetch.

Zakres: pełne cztery przypięte pliki `A0.json.gz`–`A3.json.gz`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; 2 000 rekordów na opkod, razem 8 000. Źródłowa inwentaryzacja wykazała wyłącznie formy bez prefiksu albo z pojedynczym prefiksem segmentowym: A0 1 014/986, A1 1 014/986, A2 974/1 026, A3 978/1 022 (bez prefiksu/z prefiksem). Prefiksy segmentowe rozkładają się na ES/CS/SS/DS; LOCK i REP nie występują.

Rozkład cykli w źródle uwzględnia kolejkę, segment i parzystość adresu. A0 ma 10 lub 12 T-state’ów, A1 10/12/14/16, A2 11/13, A3 11/13/15/17. Słowa pod nieparzystym adresem wykonują dwa transfery magistrali; profile i kolejność bajtów muszą wynikać z wszystkich rekordów, nie z samej długości instrukcji.

### Testy i kryteria podetapu `MOV moffs`

- [x] Dodano po 20 dokładnych pierwszych rekordów upstream dla A0/A1/A2/A3 do aktywnego manifestu; źródłowe dane próbek porównano bez zmian, Intel8086 Release przechodzi 13 371/13 371.
- [x] Pełne filtry każdego pliku: A0 2 000/2 000, A1 2 000/2 000, A2 2 000/2 000, A3 2 000/2 000; łącznie 8 000 passed, 0 skipped/mismatched/timeout/parse errors. Raport `A12-moffs-report.json`.
- [x] Porównano AX/FLAGS, RAM, adres fizyczny, kolejkę i każdy T-state; pełny zbiór obejmuje DS/ES/CS/SS, adresy parzyste/nieparzyste, słowa pod nieparzystym adresem oraz zawinięcie offsetu `FFFFh`.
- [x] Pełny zestaw 646 000 uruchomiono ponownie: 505 519 passed, 140 481 skipped, 0 mismatched/timeout/parse errors; względem 497 519/148 481 poprawa o 8 000. Raport `/tmp/vibe-a12-full-after-moffs.json`.
- [x] Zaktualizowano README, macierz ISA, raport podetapu, plan i stan wznowienia; checklisty odhaczono wyłącznie dla zebranych wyników.
- [x] Plan `b0790e4` i implementacja są osobnymi zmianami na branchu A12; Debug/Release 0 ostrzeżeń/błędów, pełna solucja Release 16 171/16 171 (Intel8086 13 371/13 371), formatter verify, walidator manifestu 1 008/19 891 i OpcodeInfo 512/512 zielone.

## Bieżący podetap: `87` — `XCHG r/m16,r16`

Plan zakresu, inwentaryzację 2 000 wektorów, kryteria i komendy zapisano w
[`A12-xchg-rm16-plan.md`](A12-xchg-rm16-plan.md). Podetap obejmuje formę rejestrową,
wszystkie formy pamięciowe, segment-prefiksy i cyklowe transfery słowa.

### Testy i kryteria podetapu `XCHG r/m16,r16`

- [x] Dodano 20 dokładnych pierwszych rekordów upstream do manifestu; Intel8086 Release przechodzi 13 391/13 391.
- [x] Pełny plik `87.json.gz`: 2 000/2 000, 0 skipów, mismatchy, timeoutów i błędów parsowania; raport `A12-xchg-word-report.json`.
- [x] Profile cykli i prefetch dla form rejestrowych, pamięciowych, prefiksowanych, parzystych i nieparzystych adresów pochodzą z przypiętego źródła; pełne wektory sprawdzają każde T-state, rejestry, RAM i kolejkę.
- [x] Pełny korpus po poprawce regresji ścieżki `86`: 646 000 rekordów, 507 519 passed, 138 481 skipped, 0 mismatchy/timeoutów/błędów parsowania; raport `/tmp/vibe-a12-full-after-xchg87-final.json`.
- [x] Końcowe bramki: build Debug/Release po 0 ostrzeżeń i błędów; pełna solucja Release 16 191/16 191, Intel8086 13 391/13 391; formatter verify czysty; manifest 1 009/19 911; OpcodeInfo 512/512 zgodnych.
- [x] Plan zapisano osobnym commitem `51d24a2`; implementację i raport zapisano osobno po pomyślnych bramkach.

## Zakończony podetap: `8C`/`8E` — transfery rejestrów segmentowych

Plan inwentaryzacji, zakresu, checklisty i poleceń zapisano w
[`A12-mov-segment-registers-plan.md`](A12-mov-segment-registers-plan.md).

### Testy i kryteria podetapu `MOV Sreg ↔ r/m16`

- [x] Dodano po 20 niezmienionych wektorów upstream dla `8C` i `8E`; lokalny projekt Intel8086 przechodzi 13 431/13 431.
- [x] Pełne przypięte pliki `8C.json.gz` i `8E.json.gz` przechodzą 4 000/4 000, bez skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Sprawdzono formy rejestrowe/pamięciowe, aliasy `reg`, wszystkie zaobserwowane pojedyncze prefiksy segmentowe, parzystość transferu słowa, BHE, kolejkę i każdy T-state; pełny runner porównuje stan CPU i RAM.
- [x] Pełny korpus 646 000 rekordów: 511 519 passed, 134 481 skipped, 0 mismatched, timeoutów i błędów parsowania; wobec wyniku po `87`: +4 000 passed i −4 000 skipped. Raport `A12-mov-segment-report.json`.
- [x] Build Debug i Release: po 0 ostrzeżeń/błędów; pełne testy Release 16 231/16 231 (Intel8086 13 431/13 431); formatter verify bez zmian; OpcodeInfo 512/512; walidator manifestu 1 011 wpisów / 19 951 rekordów.
- [x] Plan `a0fe0cb` i implementację, fixture, raport oraz macierz ISA zatwierdzono osobnymi commitami na branchu A12; nie pushowano.

## Ukończony podetap: pośrednie `CALL r/m16` (`FF /2` i `/3`)

### Testy i kryteria podetapu pośredniego `CALL` (`FF /2,/3`)

- [x] Po 20 niezmienionych fixture z każdego przypiętego pliku źródłowego; lokalne wektory 40/40.
- [x] Pełne filtry: `FF /2` 2 000/2 000 i `FF /3` 2 000/2 000; 0 skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Cały korpus 646 000: 515 519 passed, 130 481 skipped, 0 mismatchy/timeoutów/błędów parsowania; względem poprzedniego przebiegu +4 000 passed / −4 000 skipped.
- [x] Build Debug/Release: 0 ostrzeżeń i błędów; testy Release 16 271/16 271 (Intel8086 13 471/13 471); formatter verify czysty; walidator 1 013 wpisów / 19 991 rekordów; OpcodeInfo 512/512.
- [x] Raport szczegółowy: [`A12-indirect-call-report.json`](A12-indirect-call-report.json).
- [x] Plan i implementacja są w osobnych commitach na branchu A12; brak push.

## Ukończony podetap: pośrednie `JMP r/m16` (`FF /4` i `/5`)

### Testy i kryteria podetapu pośredniego `JMP` (`FF /4,/5`)

- [x] Po 20 niezmienionych fixture z każdego pliku źródłowego; lokalny projekt przechodzi 40/40 nowych wektorów.
- [x] Pełne filtry `/4` 2 000/2 000 i `/5` 2 000/2 000; zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Cały korpus 646 000: 519 519 passed, 126 481 skipped, zero mismatchy/timeoutów/błędów parsowania; zmiana +4 000 passed / −4 000 skipped.
- [x] Szczegóły źródła i profili: [`A12-indirect-jump-ff45-report.json`](A12-indirect-jump-ff45-report.json).
- [x] Buildy Debug/Release, testy Release, formatter, manifest i OpcodeInfo przechodzą; plan i implementacja osobno zatwierdzone na A12.


Szczegółowa inwentaryzacja, kolejność prac, kryteria i polecenia są w [`A12-indirect-jump-ff45-plan.md`](A12-indirect-jump-ff45-plan.md). Źródło przypięte SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` zawiera po 2 000 rekordów `/4` i `/5`; wszystkie 4 000 były skipami przed implementacją.

## Zaplanowany podetap: `TEST r/m, immediate` (`F6/F7 /0` i `/1`)

Plan, inwentaryzacja źródła i kryteria są w [A12-test-rm-immediate-f6f7-plan.md](A12-test-rm-immediate-f6f7-plan.md). Zakres obejmuje cztery przypięte pliki (`F6.0`, `F6.1`, `F7.0`, `F7.1`), 8 000 rekordów, alias `/1`, operandy rejestrowe i pamięciowe oraz prefiksy segmentowe. Inwentaryzacja: 2 055 form rejestrowych, 5 945 pamięciowych; 4 002 nieprefiksowane i 3 998 z prefiksem segmentowym. Plan zapisano osobnym commitem `7e495ce`, implementację `efdd9c8`. Implementacja przechodzi lokalne fixture’y **80/80** i pełne filtry źródłowe **8 000/8 000**. Pełny korpus 646 000: **527 519 passed, 118 481 skipped**, 0 mismatchy, timeoutów i błędów parsowania. Wszystkie końcowe bramki jakości są zielone. Raport: [A12-test-rm-immediate-f6f7-report.json](A12-test-rm-immediate-f6f7-report.json).

### Testy i kryteria `F6/F7 /0,/1`

- [x] Cztery próbki po 20 rekordów są identyczne z przypiętym źródłem; manifest wskazuje 80 rekordów.
- [x] Próbki przechodzą lokalne testy stanu, pamięci, FLAGS, kolejki i cykli: 80/80.
- [x] Pełne filtry `F6.0`, `F6.1`, `F7.0`, `F7.1`: 8 000/8 000, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Pokryte są oba rozmiary immediate, rejestry, pamięć ze wszystkimi trybami ModR/M, cztery prefiksy segmentowe, alias `/1`, odczyt bez zapisu i flagi według masek źródła.
- [x] Pełny korpus po F6/F7: 646 000 rekordów, 527 519 passed (+8 000), 118 481 skipped (−8 000), 0 mismatchy, timeoutów i błędów parsowania; raport zachowany.
- [x] Bramka końcowa: Intel8086 Release 13 591/13 591; solucja Release 16 391/16 391; Debug/Release 0 ostrzeżeń/błędów; formatter verify czysty; manifest 1 023 wpisy / 20 191 rekordów; OpcodeInfo 512/512 (500 adnotacji cykli).
- [x] Testy Intel8086 i solucji Release, buildy Debug/Release, formatter, manifest i OpcodeInfo zielone; wyniki zapisane w dzienniku.
- [x] Plan `7e495ce` i implementacja `efdd9c8` zapisane osobnymi commitami; checkpoint i wyniki udokumentowane na A12, niczego nie wypchnięto.

Raport pełnych filtrów źródłowych, fixture’ów, pełnego korpusu i bramek repozytorium zapisano w [raporcie podetapu](A12-test-rm-immediate-f6f7-report.json). Implementacja `efdd9c8` jest zatwierdzona osobno od planu `7e495ce`.

## Testy i kryteria

- [x] Potwierdzono przypięty checkout `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, 323 pliki JSON.GZ i 646 000 rekordów (2000 na plik); raport zawiera sumę kontrolną źródła.
- [x] Pełny streaming runner objął wszystkie 323 pliki, również rozszerzenia ModR/M; kategorie rozłączne i sumują się do 646 000.
- [x] Audytor rozdziela passed, jawne skipy, mismatchy, timeouty i błędy parsowania oraz zapisuje `test_num`, hash i powód dla przypadku niezaliczonego.
- [x] Pełny przebieg checkpointu `61c0720` objął 646 000 rekordów: 397 167 passed, 242 443 skipped, 6 390 mismatched, 0 timeoutów i 0 błędów parsowania.
- [x] Ponowiony pełny przebieg na bieżącym drzewie po portach rejestrowego i segmentowego `PUSH/POP`: 646 000 rekordów, 443 167 passed, 196 443 skipped, 6 390 mismatched, 0 timeoutów i 0 błędów parsowania. Pozostałe mismatchy i skipy są otwarte.
- [x] Naprawiono 81-prefiksowane dostępy pamięciowe oraz cykle segment-prefiksowanego rejestrowego MUL/DIV; dodano próbkę 24 dokładnych wektorów upstream dla opkodu `81`.
- [x] Najnowszy pełny przebieg po tych poprawkach: 646 000 rekordów, 449 534 passed, 196 443 skipped, 23 mismatched, 0 timeoutów i 0 błędów parsowania.
- [x] Usunięto odrzucanie krótkiej kolejki przy segment+REP `F6/F7 /7`; po uruchomieniu tych form pełny przebieg ma 449 537 passed, 196 429 skipped, 34 mismatched, 0 timeoutów i 0 błędów parsowania. 14 dawnych odrzuceń zmieniło się w 3 zaliczone i 11 rozbieżnych wykonań; poprawa pokrycia nie jest jeszcze zgodnością cyklową.
- [x] Poprawiono kolejność cykli i pobieranie ostatniego bajtu kolejki przy błędzie dzielenia dla segment+REP `IDIV`; 11 przypadków, które poprzednio różniły się, przechodzi w aktualnym pełnym przebiegu.
- [x] Pełny przebieg po korekcie harmonogramu Type 0/prefetch: 646 000 rekordów, 449 569 passed, 196 429 skipped, 2 mismatched, 0 timeoutów i 0 błędów parsowania; pozostałe rozbieżności to dwa opisane niżej przypadki `F6.7`.
- [x] Dodano 11 poprawionych wektorów błędu dzielenia z pinu źródłowego jako lokalną regresję offline.
- [x] Dodano siedem dokładnych rekordów z pinu źródłowego do lokalnych testów offline: DAA, DAS, dwa `81 /7`, JCXZ, IRET i CALL.
- [x] Test Intel8086 Release po 11 regresjach błędów dzielenia i korekcie Type 0: 11 249/11 249; wspólny pełny filtr stosu: 46 000/46 000, 0 skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Wyizolowano poprzednie 23 rozbieżne rekordy `F6.7`/`F7.7` na pinie źródła. Kontrolny przebieg tych rekordów po korekcie Type 0 i prefetch: 21/23 zgodnych.
- [x] Usunięto dwie rozbieżności Type 0 (`F6.7` test_num 259: `IDIV byte [SS:BP-1FFEh]`; test_num 1330: `IDIV CH`) przez dopasowanie zakresu ilorazu `IDIV` do 8086: minima `-128`/`-32768` zgłaszają Type 0. Wycinek przechodzi 23/23; pełne źródło: 646 000, 449 571 passed, 196 429 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania.
- [x] Zaimplementowano jednobajtowe `XCHG AX,r16` (`91`–`97`); próbki repo 140/140, pełne przypięte wektory 14 000/14 000 zgodne ze stanem, kolejką i każdym T-state.
- [x] Pełny przebieg po `XCHG AX,r16`: 646 000 rekordów, 463 571 passed, 182 429 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania; skipy spadły o 14 000.
- [x] Dodano alias `82` bajtowej grupy ALU immediate we wszystkich rozszerzeniach ModR/M; próbki lokalne 160/160.
- [x] Pełny filtr `82`: 16 000 rekordów, 8 041 passed, 7 959 skipped (prefiksy segmentowe), 0 mismatched, timeoutów i błędów parsowania. Wszystkie nieprefiksowane przypadki przeszły.
- [x] Naprawiono zawijanie 16-bitowego IP przy aktualizacji adresu prefetch po cyklu pobrania grupy ALU immediate; dokładny rekord 82.2 test_num 816 i pełny filtr 82 nie wykazują rozbieżności.
- [x] Najnowszy pełny przebieg po `82`: 646 000 rekordów, 471 612 passed, 174 388 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania; raport `/tmp/vibe-8086-a12-full-after-op82.json`.
- [x] Bramka po aliasie `82`: Intel8086 Release 11 551/11 551; pełna solucja Release 14 351/14 351; build Debug/Release 0 ostrzeżeń i błędów; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [x] Dodano `PUSHF`/`POPF` (`9C`/`9D`), próbki repo 40/40; `POPF` maskuje bity zarezerwowane FLAGS według wektorów.
- [x] Pełny filtr `9C`/`9D`: 4 000/4 000, 0 skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny przebieg po `PUSHF`/`POPF`: 646 000 rekordów, 475 612 passed, 170 388 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania; raport `/tmp/vibe-8086-a12-full-after-pushf-popf.json`.
- [x] Dodano po 20 lokalnych wektorów dla każdego prefiksu ES/CS/SS/DS × rozszerzenia ModR/M `/0`–`/7` dla `80` i `82`: 64 pliki, 1 280 wektorów; test Intel8086 Release przechodzi 12 871/12 871.
- [x] Pełne filtry źródłowe `80` i `82`: po 16 000/16 000 zgodnych, 0 skipów, 0 mismatchy, 0 timeoutów i 0 błędów parsowania. `82` profil cykli pochodzi z przypiętego źródła `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; wszystkie cztery prefiksy mają pełne pokrycie.
- [x] Pełny przebieg 323 plików po segment-prefiksowanych `80`/`82`: 646 000 rekordów, 491 519 passed, 154 481 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania; raport `/tmp/vibe-a12-full-final-report.json`. Wobec przebiegu po `PUSHF`/`POPF` doszło 15 907 zgodnych przypadków, a skipy zmniejszyły się o tę samą liczbę.
- [x] Dodano nieprefiksowane `FF /6` (`PUSH r/m16`): lokalne próbki 40/40, pełny podzbiór 1 014/1 014, obejmujący 252 formy rejestrowe i 762 pamięciowe.
- [x] Pełny przebieg 323 plików po nieprefiksowanym `FF /6`: 646 000 rekordów, 492 533 passed, 153 467 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania; raport `/tmp/vibe-a12-full-after-ff6.json`.
- [x] Końcowe bramki dla `FF /6`: build Debug i Release po 0 ostrzeżeń/błędów; pełna solucja Release 15 711/15 711; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [ ] Zamknąć pozostałe prawidłowe 8086-opkody i formy pamięciowe, które są jawnie pomijane. Pełny zestaw po prefiksowanych `D0–D3` pomija 90 575 rekordów (86,0% passed), więc pełna zgodność procesora nie jest osiągnięta; aktualną listę prac prowadzi [checklista dalszych prac A12](A12-dalsze-prace-checklist.md).
- [ ] Wykonać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i warunkami sukcesu; wektory SingleStepTests nie zastępują niezależnego testu.
- [x] Po poprawce zakresu i dwóch fixture: build Debug i Release po 0 ostrzeżeń/błędów; pełny test Release 14 051/14 051; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [x] Po dodaniu `XCHG AX,r16`: Intel8086 Release 11 391/11 391; pełna solucja Release 14 191/14 191; build Debug/Release bez ostrzeżeń i błędów; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [x] Walidator manifestu z pełnym źródłem: 901 wpisów / 17 769 wskazań, 323 pliki / 646 000 rekordów, SHA zgodny.
- [x] Plan nadrzędny i `Vectors/README.md` pokazują wynik A12, SHA i pozostałe ograniczenia.

Raport podaje profil porównania flag (dokładny CPU / maski określonych bitów), SHA źródła i runnera oraz osobno pokrycie ISA, plików i przypadków. Stosować rozłączne kategorie wyników i sprawdzić, że sumują się do liczby zaplanowanych przypadków.

## Tabela wyników

| zestaw | pliki | przypadki | passed | skipped | mismatched | timeout/parse error |
|---|---:|---:|---:|---:|---:|---:|
| próbki repo, w tym A12Regression, prefiksowane `80`/`82`, `FF /6,/7`, `A9`, `87`, `9C` i `9D` | 589 | 13 391 testów Intel8086 | 13 391 | 0 | 0 | 0 / 0 |
| checkpoint pełnego zestawu przed portem PUSH/POP, SHA `e71c68d` | 323 | 646 000 | 397 167 | 242 443 | 6 390 | 0 / 0 |
| pełny zestaw po portach rejestrowego i segmentowego PUSH/POP, SHA `e71c68d` | 323 | 646 000 | 443 167 | 196 443 | 6 390 | 0 / 0 |
| pełny zestaw po poprawkach `81` i segment-prefiksowanego MUL/DIV, SHA `e71c68d` | 323 | 646 000 | 449 534 | 196 443 | 23 | 0 / 0 |
| pełny zestaw po udostępnieniu krótkiej kolejki dla segment+REP `F6/F7`, SHA `e71c68d` | 323 | 646 000 | 449 537 | 196 429 | 34 | 0 / 0 |
| pełny zestaw po poprawie wejścia w wyjątek segment+REP `IDIV`, SHA `e71c68d` | 323 | 646 000 | 449 548 | 196 429 | 23 | 0 / 0 |
| pełny zestaw po `IDIV` boundary i `XCHG AX,r16` (`91`–`97`), SHA `e71c68d` | 323 | 646 000 | 463 571 | 182 429 | 0 | 0 / 0 |
| pełny zestaw po `82` i poprawce zawijania IP, SHA `e71c68d` | 323 | 646 000 | 471 612 | 174 388 | 0 | 0 / 0 |
| pełny zestaw po `PUSHF`/`POPF` (`9C`/`9D`), SHA `e71c68d` | 323 | 646 000 | 475 612 | 170 388 | 0 | 0 / 0 |
| pełny zestaw po segment-prefiksowanych `80`/`82`, SHA `e71c68d` | 323 | 646 000 | 491 519 | 154 481 | 0 | 0 / 0 |
| pełny zestaw po nieprefiksowanym `FF /6`, SHA `e71c68d` | 323 | 646 000 | 492 533 | 153 467 | 0 | 0 / 0 |
| pełny zestaw po nieprefiksowanym `FF /7`, SHA `e71c68d` | 323 | 646 000 | 493 529 | 152 471 | 0 | 0 / 0 |
| pełny zestaw po segment-prefiksowanych `FF /6,/7`, SHA `e71c68d` | 323 | 646 000 | 495 519 | 150 481 | 0 | 0 / 0 |
| pełny zestaw po `TEST AX,imm16` (`A9`), SHA `e71c68d` | 323 | 646 000 | 497 519 | 148 481 | 0 | 0 / 0 |
| pełny zestaw po `XCHG r/m16,r16` (`87`), SHA `e71c68d` | 323 | 646 000 | 507 519 | 138 481 | 0 | 0 / 0 |
| pełny zestaw po `MOV Sreg` (`8C`/`8E`), SHA `e71c68d` | 323 | 646 000 | 511 519 | 134 481 | 0 | 0 / 0 |
| pełny zestaw po pośrednim `CALL FF /2,/3`, SHA `e71c68d` | 323 | 646 000 | 515 519 | 130 481 | 0 | 0 / 0 |
| pełny zestaw po pośrednim `JMP FF /4,/5`, SHA `e71c68d` | 323 | 646 000 | 519 519 | 126 481 | 0 | 0 / 0 |
| pełny zestaw po prefiksowanych `D0–D3`, SHA `e71c68d` | 323 | 646 000 | 555 425 | 90 575 | 0 | 0 / 0 |
| programowe testy dodatkowe | — | — | — | — | — | — |

Raporty wcześniejszych checkpointów są zachowane w `/tmp`; najnowszy pełny przebieg to `/tmp/vibe-a12-full-f6f7-report.json`, a podsumowanie etapu znajduje się w [raporcie F6/F7](A12-test-rm-immediate-f6f7-report.json). Wszystkie używają przypiętego źródła `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Wynik sumuje się do 646 000 rekordów. Każdy skip i mismatch zawiera plik, `test_num`, `test_hash` i powód.

### Klasy jawnych pominięć w checkpointcie `61c0720`

| powód | przypadki | zakres do dokończenia |
|---|---:|---|
| prefiks segmentowy poza aktualnie obsługiwanymi instrukcjami | 129 192 | A10 oraz brakujące prefiksowane formy A3–A8 |
| opcode lub forma jawnie niezaimplementowana | 113 251 | m.in. FF poza `/0,/1`, `F6/F7`, `D0–D3` i pozostałe opkody bez obsługi (checkpoint sprzed portu segmentowego PUSH/POP) |
| **razem** | **242 443** | Jawne skipy nie są dowodem zgodności pominiętych przypadków. |

Wśród 6 390 mismatchy runner wskazuje 3 708 różnic w cyklach `PASV/Ti`, 2 659 różnic w cyklach zapisu pamięci `MEMW/T2` i 23 pozostałe różnice cykli/kolejki. To klasyfikacja po pierwszej różnicy każdego rekordu; przypadki wymagają osobnych minimalnych regresji i źródłowej analizy.

Zmiany A12 naprawiły znane rozbieżności DAA/DAS, `81 /7`, JCXZ, IRET i CALL. Powyższe 6 390 niezgodności i 196 443 jawne skipy pochodzą z historycznego checkpointu `61c0720`; aktualny wynik po `TEST r/m,imm` to 0 niezgodności i 118 481 skipów. Pełna zgodność pozostaje niepotwierdzona.

| fixture regresyjny | test/hash źródłowy | zaobserwowany przypadek |
|---|---|---|
| [27-117.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/27-117.json) | 117 / `3e4193b17b79244f62960cccb53e27329509749f246414a4f2fbc05144c5e86d` | DAA: `AL=9A`, `AF=1`, bez carry |
| [2F-184.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/2F-184.json) | 184 / `9ca2411c717cbe7c6c43fbb4f03b9bd2be3db258081d7f3ad422dbff914a0da1` | DAS: `AL=9E`, `AF=1`, bez carry |
| [81.7-171.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/81.7-171.json) | 171 / `e03c7189fe541bd122788b170f47fe27219082512c284fdfe44b9c11e46cd521` | CMP direct: stan końcowy kolejki prefetch |
| [81.7-816.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/81.7-816.json) | 816 / `13d458847e66def00d751e3e7c4f1cb7581e8d10b7bcca9869e23bed9ca84151` | CMP przy IP bliskim `FFFFh`: zawinięcie fetchu w segmencie kodu |
| [E3-191.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/E3-191.json) | 191 / `b7e89be891484f3fc3f6e36833aec344553df58d490edcc673800fecdb9593d8` | JCXZ z pełną kolejką: decyzja skoku i fetch celu |
| [CF-477.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/CF-477.json) | 477 / `b1392c082a3809d7a9e6fe41ab20ce032cb282eb470a7982612fe5d48ff42688` | IRET do `IP=FFFFh`: fetch zawija offset segmentu |
| [E8-850.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/E8-850.json) | 850 / `a345b93d7cef0718c516c47af6bb58306ad9fe811a2adb0a47700ce56dd1fd5f` | CALL przy `SP=1`: zawinięcie drugiego bajtu słowa stosu |
| [81-segment-prefix-memory-timing.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/81-segment-prefix-memory-timing.json) | 24 dokładne rekordy upstream, wskazane w manifeście | timing pamięci dla sześciu segment-prefiksowanych form opcode `81` |
| [combined-segment-rep-divide-errors.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/combined-segment-rep-divide-errors.json) | 11 rekordów z `F6.7` i `F7.7`, hash w każdym rekordzie | kolejność prefiksów, odświeżanie kolejki i wejście w wyjątek dzielenia |

## Pułapki

1. Nie łączyć wyników próbek z pełnym zestawem w jedną sumę bez oznaczenia źródła.
2. Zmiana runnera może zmienić wynik; zapisać wersję runnera/commit wraz z raportem.
3. Testy wygenerowane przez inny emulator nie są niezależne, jeśli dzielą ten sam błąd referencyjny.
4. Zielona solucja nie dowodzi cyklowej zgodności, jeśli comparator nie bada cykli/prefetch.

## Dziennik postępu

| data | zestaw | passed/skipped/mismatched | jakość repo | konkluzja |
|---|---|---|---|---|
| 2026-10-24 | pełne `v1/*.json.gz`, wcześniejszy checkpoint A12 | 397 167 / 242 443 / 6 390; timeout 0; parse 0 | runner Release build: 0 ostrzeżeń, 0 błędów; historyczna weryfikacja manifestu: 876 wpisów/17 274 wskazania | 646 000 przypadków; rozbieżności oraz jawne skipy pozostawiają zgodność i poziom V otwarte |
| 2026-10-24 | pełne `v1/*.json.gz`, po poprawkach segment-prefiksowanego `81` i MUL/DIV | 449 534 / 196 443 / 23; timeout 0; parse 0 | pełny Release build: 0 ostrzeżeń, 0 błędów; test Intel8086: 11 238/11 238 | 646 000 przypadków; 23 rozbieżności oraz jawne skipy nadal pozostawiają zgodność i poziom V otwarte |
| 2026-10-24 | pełne `v1/*.json.gz`, po włączeniu krótkiej kolejki dla segment+REP `F6/F7` | 449 537 / 196 429 / 34; timeout 0; parse 0 | pełny Release build: 0 ostrzeżeń, 0 błędów; test Intel8086: 11 238/11 238 | 14 wcześniej odrzuconych przypadków uruchamia się; 3 przechodzą i 11 ujawnia rozbieżności cykli/kolejki; pełna zgodność pozostaje otwarta |
| 2026-10-24 | pełne `v1/*.json.gz`, po poprawie wyjątku dla segment+REP `IDIV` | 449 548 / 196 429 / 23; timeout 0; parse 0 | Release runner build: 0 ostrzeżeń, 0 błędów | 11 poprzednich rozbieżności `IDIV` naprawionych; 23 pozostałe, zgodność pozostaje otwarta |
| 2026-10-25 | pełne `v1/*.json.gz`, po korekcie harmonogramu Type 0 i prefetch | 449 569 / 196 429 / 2; timeout 0; parse 0 | Debug/Release build 0 ostrzeżeń/błędów; test Release 14 049/14 049; formatter czysty | 21 z 23 rozbieżności usuniętych; 2 nieprefiksowane przypadki `F6.7` pozostają otwarte |
| 2026-10-25 | pełne `v1/*.json.gz`, po poprawie granic `IDIV` 8086 | 449 571 / 196 429 / 0; timeout 0; parse 0 | test Intel8086 przed dodaniem fixture: 11 249/11 249 | Dwa Type 0 mismatchy usunięte; 196 429 skipów nadal blokuje zamknięcie zakresu |
| 2026-10-25 | po dodaniu dwóch fixture `IDIV` 8086 i zmianie zakresu helpera | Intel8086 11 251/11 251; pełna solucja 14 051/14 051 | Debug/Release build 0 ostrzeżeń/błędów; formatter czysty; OpcodeInfo 512/512 | Testy lokalne i pełny SST potwierdzają poprawkę; skipy i kryteria niezależnej walidacji nadal otwarte |
| 2026-10-25 | pełny `v1/*.json.gz` po `XCHG AX,r16`, `82` i `PUSHF`/`POPF` | 475 612 / 170 388 / 0; timeout 0; parse 0 | Intel8086 11 591/11 591; pełna solucja 14 391/14 391; Debug/Release 0 ostrzeżeń/błędów; formatter czysty; OpcodeInfo 512/512 | 4 000 kolejnych przypadków zgodnych; 7 959 prefiksowanych `82` nadal jawnie pominiętych |
| 2026-10-25 | pełny `v1/*.json.gz` po `XCHG r/m16,r16` (`87`) | 507 519 / 138 481 / 0; timeout 0; parse 0 | Intel8086 13 391/13 391; pełny filtr `87` 2 000/2 000 | Nowe `87` zgodne w całości; 747 rozbieżności przejściowych `86` znalezionych przez pełny przebieg i naprawionych przed wynikiem końcowym |
| 2026-10-25 | pełny `v1/*.json.gz` po pośrednim `JMP FF /4,/5` | 519 519 / 126 481 / 0; timeout 0; parse 0 | Intel8086 13 511/13 511; pełny filtr 4 000/4 000; solucja Release 16 311/16 311; Debug/Release 0 ostrzeżeń/błędów; formatter, manifest i OpcodeInfo zielone | 4 000 kolejnych rekordów źródłowych zgodnych; 126 481 jawnych skipów i niezależny test programu/ROM nadal otwarte |
| 2026-10-25 | pełne `v1/*.json.gz` po `TEST r/m,imm` (`F6/F7 /0,/1`) | 527 519 / 118 481 / 0; timeout 0; parse 0 | lokalne fixture’y 80/80; pełne filtry 8 000/8 000; Intel8086 13 591/13 591; solucja 16 391/16 391; Debug/Release 0 ostrzeżeń/błędów; formatter, manifest i OpcodeInfo zielone | 8 000 kolejnych rekordów zgodnych; jawne skipy i niezależny test programu/ROM nadal otwarte |


## Zakończony podetap: prefiksy segmentowe `D0–D3`

- [x] 4 480 fixture’ów (224 grupy po 20 rejestrowych i 20 pamięciowych) przechodzi lokalny runner 4 480/4 480.
- [x] Pełny filtr upstream 64 000: 56 000 passed, 8 000 `/6` skipped, 0 mismatchy, timeoutów i błędów parsowania.
- [x] Pełny korpus: 555 425 passed, 90 575 skipped, 0 mismatchy, timeoutów i błędów parsowania; raport `/tmp/a12-full-after-d0d3.json`. Zmiana: +27 906 passed, −27 906 skipped względem poprzedniego raportu.
- [x] Build Debug/Release po 0 ostrzeżeń/błędów; testy Release 20 871/20 871 (Intel8086 18 071/18 071); formatter verify czysty; manifest 1 247 wpisów/24 671 rekordów; OpcodeInfo 512/512 zgodne.
- [ ] Plan i implementacja do zapisania oddzielnymi commitami na branchu `A12-8086-pelna-weryfikacja`.
- Raport: [`A12-prefiksy-segmentowe-D0-D3-report.json`](A12-prefiksy-segmentowe-D0-D3-report.json).
