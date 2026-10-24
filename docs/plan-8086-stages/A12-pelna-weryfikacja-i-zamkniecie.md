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
- [ ] Zamknąć wszystkie prawidłowe 8086-opkody i formy pamięciowe, które obecnie są jawnie pomijane. Pełny zestaw nadal pomija 182 429 rekordów, więc poziom II/III/V i pełna zgodność procesora nie są osiągnięte.
- [ ] Wykonać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i warunkami sukcesu; wektory SingleStepTests nie zastępują niezależnego testu.
- [x] Po poprawce zakresu i dwóch fixture: build Debug i Release po 0 ostrzeżeń/błędów; pełny test Release 14 051/14 051; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [x] Po dodaniu `XCHG AX,r16`: Intel8086 Release 11 391/11 391; pełna solucja Release 14 191/14 191; build Debug/Release bez ostrzeżeń i błędów; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- [x] Walidator manifestu z pełnym źródłem: 901 wpisów / 17 769 wskazań, 323 pliki / 646 000 rekordów, SHA zgodny.
- [x] Plan nadrzędny i `Vectors/README.md` pokazują wynik A12, SHA i pozostałe ograniczenia.

Raport podaje profil porównania flag (dokładny CPU / maski określonych bitów), SHA źródła i runnera oraz osobno pokrycie ISA, plików i przypadków. Stosować rozłączne kategorie wyników i sprawdzić, że sumują się do liczby zaplanowanych przypadków.

## Tabela wyników

| zestaw | pliki | przypadki | passed | skipped | mismatched | timeout/parse error |
|---|---:|---:|---:|---:|---:|---:|
| próbki repo, w tym A12Regression oraz rejestrowy i segmentowy stos A5 | 493 | 11 391 testów Intel8086 | 11 391 | 0 | 0 | 0 / 0 |
| checkpoint pełnego zestawu przed portem PUSH/POP, SHA `e71c68d` | 323 | 646 000 | 397 167 | 242 443 | 6 390 | 0 / 0 |
| pełny zestaw po portach rejestrowego i segmentowego PUSH/POP, SHA `e71c68d` | 323 | 646 000 | 443 167 | 196 443 | 6 390 | 0 / 0 |
| pełny zestaw po poprawkach `81` i segment-prefiksowanego MUL/DIV, SHA `e71c68d` | 323 | 646 000 | 449 534 | 196 443 | 23 | 0 / 0 |
| pełny zestaw po udostępnieniu krótkiej kolejki dla segment+REP `F6/F7`, SHA `e71c68d` | 323 | 646 000 | 449 537 | 196 429 | 34 | 0 / 0 |
| pełny zestaw po poprawie wejścia w wyjątek segment+REP `IDIV`, SHA `e71c68d` | 323 | 646 000 | 449 548 | 196 429 | 23 | 0 / 0 |
| pełny zestaw po `IDIV` boundary i `XCHG AX,r16` (`91`–`97`), SHA `e71c68d` | 323 | 646 000 | 463 571 | 182 429 | 0 | 0 / 0 |
| programowe testy dodatkowe | — | — | — | — | — | — |

Raporty: checkpoint `/tmp/vibe-8086-a12-current-final.json`; po stack `/tmp/vibe-8086-a12-current-after-stack.json`; wcześniejszy pełny przebieg `/tmp/vibe-8086-a12-current-after-current-fixes.json`; po krótkiej kolejce `/tmp/vibe-8086-a12-current-after-short-queue-fix-full.json`; aktualny `/tmp/vibe-8086-a12-current-after-revert-unsigned-prefetch.json`. Wszystkie używają przypiętego źródła `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Ostatni runner zbudowano z bieżącego drzewa A12. Wynik sumuje się do 646 000 rekordów. Każdy skip i mismatch zawiera plik, `test_num`, `test_hash` i powód.

### Klasy jawnych pominięć w checkpointcie `61c0720`

| powód | przypadki | zakres do dokończenia |
|---|---:|---|
| prefiks segmentowy poza aktualnie obsługiwanymi instrukcjami | 129 192 | A10 oraz brakujące prefiksowane formy A3–A8 |
| opcode lub forma jawnie niezaimplementowana | 113 251 | m.in. FF poza `/0,/1`, `F6/F7`, `D0–D3` i pozostałe opkody bez obsługi (checkpoint sprzed portu segmentowego PUSH/POP) |
| **razem** | **242 443** | Jawne skipy nie są dowodem zgodności pominiętych przypadków. |

Wśród 6 390 mismatchy runner wskazuje 3 708 różnic w cyklach `PASV/Ti`, 2 659 różnic w cyklach zapisu pamięci `MEMW/T2` i 23 pozostałe różnice cykli/kolejki. To klasyfikacja po pierwszej różnicy każdego rekordu; przypadki wymagają osobnych minimalnych regresji i źródłowej analizy.

Zmiany A12 naprawiły znane rozbieżności DAA/DAS, `81 /7`, JCXZ, IRET i CALL. Bieżący pełny przebieg po portach rejestrowego i segmentowego stosu wykazuje 6 390 niezgodnych rekordów oraz 196 443 jawne skipy; pełna zgodność pozostaje niepotwierdzona.

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
