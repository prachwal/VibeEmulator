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
9. Sprawdzić linki, manifest, przypięte licencje, dokumenty instrukcji i stan gałęzi. Nie commitować ani nie pushować.

## Testy i kryteria

- [x] Potwierdzono przypięty checkout `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, 323 pliki JSON.GZ i 646 000 rekordów (2000 na plik); raport zawiera sumę kontrolną źródła.
- [x] Pełny streaming runner objął wszystkie 323 pliki, również rozszerzenia ModR/M; kategorie rozłączne i sumują się do 646 000.
- [x] Audytor rozdziela passed, jawne skipy, mismatchy, timeouty i błędy parsowania oraz zapisuje `test_num`, hash i powód dla przypadku niezaliczonego.
- [x] Pełny przebieg checkpointu `61c0720` objął 646 000 rekordów: 397 167 passed, 242 443 skipped, 6 390 mismatched, 0 timeoutów i 0 błędów parsowania.
- [ ] Ponowić pełny przebieg dla bieżącego drzewa po przeniesieniu rejestrowego `PUSH/POP` i pozostałych poprawek z A12; checkpoint powyżej nie opisuje bieżącego kodu.
- [x] Dodano siedem dokładnych rekordów z pinu źródłowego do lokalnych testów offline: DAA, DAS, dwa `81 /7`, JCXZ, IRET i CALL.
- [ ] Zbadać i usunąć 6 390 mismatchy pełnego zestawu. Większość wskazuje różnice w cyklach zapisu i końcowej kolejce prefetch; pełna zgodność nie jest potwierdzona.
- [ ] Zamknąć wszystkie prawidłowe 8086-opkody i formy pamięciowe, które obecnie są jawnie pomijane. Pełny zestaw nadal pomija 242 443 rekordy, więc poziom II/III/V i pełna zgodność procesora nie są osiągnięte.
- [ ] Wykonać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i warunkami sukcesu; wektory SingleStepTests nie zastępują niezależnego testu.
- [ ] Pełny build Debug/Release, test Release, formatter i OpcodeInfo dla bieżącego drzewa po przenoszeniu funkcji (checkpoint przed portem: pełna solucja 13 553/13 553, Intel8086 10 753/10 753).
- [x] Walidator manifestu z pełnym źródłem: 892 wpisy / 17 594 wskazania, 323 pliki / 646 000 rekordów, SHA zgodny.
- [x] Plan nadrzędny i `Vectors/README.md` pokazują wynik A12, SHA i pozostałe ograniczenia.

Raport podaje profil porównania flag (dokładny CPU / maski określonych bitów), SHA źródła i runnera oraz osobno pokrycie ISA, plików i przypadków. Stosować rozłączne kategorie wyników i sprawdzić, że sumują się do liczby zaplanowanych przypadków.

## Tabela wyników

| zestaw | pliki | przypadki | passed | skipped | mismatched | timeout/parse error |
|---|---:|---:|---:|---:|---:|---:|
| próbki repo, w tym A12Regression i A5StackRegisters | 477 | 11 073 testy CPU | 11 073 | 0 | 0 | 0 / 0 |
| checkpoint pełnego zestawu przed portem PUSH/POP, SHA `e71c68d` | 323 | 646 000 | 397 167 | 242 443 | 6 390 | 0 / 0 |
| programowe testy dodatkowe | — | — | — | — | — | — |

Raport checkpointu: `/tmp/vibe-8086-a12-current-final.json`; log: `/tmp/vibe-8086-a12-current-final.log`. Runner `61c0720` użył źródła `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Od tego przebiegu dodano rejestrowe `PUSH/POP`, dlatego liczby należy traktować wyłącznie jako bazę przed portem; końcowy pełny przebieg dla bieżącego drzewa pozostaje do uruchomienia. Każdy wpis pominięty lub niezgodny zawiera plik, `test_num`, `test_hash` i powód.

### Klasy jawnych pominięć w checkpointcie `61c0720`

| powód | przypadki | zakres do dokończenia |
|---|---:|---|
| prefiks segmentowy poza aktualnie obsługiwanymi instrukcjami | 129 192 | A10 oraz brakujące prefiksowane formy A3–A8 |
| opcode lub forma jawnie niezaimplementowana | 113 251 | m.in. FF poza `/0,/1`, `F6/F7`, `D0–D3`, segmentowy PUSH/POP i pozostałe opkody bez obsługi |
| **razem** | **242 443** | Jawne skipy nie są dowodem zgodności pominiętych przypadków. |

Wśród 6 390 mismatchy runner wskazuje 3 708 różnic w cyklach `PASV/Ti`, 2 659 różnic w cyklach zapisu pamięci `MEMW/T2` i 23 pozostałe różnice cykli/kolejki. To klasyfikacja po pierwszej różnicy każdego rekordu; przypadki wymagają osobnych minimalnych regresji i źródłowej analizy.

Zmiany A12 naprawiły znane rozbieżności DAA/DAS, `81 /7`, JCXZ, IRET i CALL, lecz pełny checkpoint przed portem rejestrowego stosu wykazywał 6 390 niezgodnych rekordów. Wynik potwierdzał zgodność 397 167 rekordów w tamtym drzewie; 242 443 jawne skipy i mismatchy blokowały deklarację pełnej zgodności. Po porcie PUSH/POP pełne porównanie trzeba powtórzyć.

| fixture regresyjny | test/hash źródłowy | zaobserwowany przypadek |
|---|---|---|
| [27-117.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/27-117.json) | 117 / `3e4193b17b79244f62960cccb53e27329509749f246414a4f2fbc05144c5e86d` | DAA: `AL=9A`, `AF=1`, bez carry |
| [2F-184.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/2F-184.json) | 184 / `9ca2411c717cbe7c6c43fbb4f03b9bd2be3db258081d7f3ad422dbff914a0da1` | DAS: `AL=9E`, `AF=1`, bez carry |
| [81.7-171.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/81.7-171.json) | 171 / `e03c7189fe541bd122788b170f47fe27219082512c284fdfe44b9c11e46cd521` | CMP direct: stan końcowy kolejki prefetch |
| [81.7-816.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/81.7-816.json) | 816 / `13d458847e66def00d751e3e7c4f1cb7581e8d10b7bcca9869e23bed9ca84151` | CMP przy IP bliskim `FFFFh`: zawinięcie fetchu w segmencie kodu |
| [E3-191.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/E3-191.json) | 191 / `b7e89be891484f3fc3f6e36833aec344553df58d490edcc673800fecdb9593d8` | JCXZ z pełną kolejką: decyzja skoku i fetch celu |
| [CF-477.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/CF-477.json) | 477 / `b1392c082a3809d7a9e6fe41ab20ce032cb282eb470a7982612fe5d48ff42688` | IRET do `IP=FFFFh`: fetch zawija offset segmentu |
| [E8-850.json](../../tests/VibeEmulator.Intel8086.Tests/Vectors/A12Regression/E8-850.json) | 850 / `a345b93d7cef0718c516c47af6bb58306ad9fe811a2adb0a47700ce56dd1fd5f` | CALL przy `SP=1`: zawinięcie drugiego bajtu słowa stosu |

## Pułapki

1. Nie łączyć wyników próbek z pełnym zestawem w jedną sumę bez oznaczenia źródła.
2. Zmiana runnera może zmienić wynik; zapisać wersję runnera/commit wraz z raportem.
3. Testy wygenerowane przez inny emulator nie są niezależne, jeśli dzielą ten sam błąd referencyjny.
4. Zielona solucja nie dowodzi cyklowej zgodności, jeśli comparator nie bada cykli/prefetch.

## Dziennik postępu

| data | zestaw | passed/skipped/mismatched | jakość repo | konkluzja |
|---|---|---|---|---|
| 2026-10-24 | pełne `v1/*.json.gz`, bieżący runner A12 | 397 167 / 242 443 / 6 390; timeout 0; parse 0 | runner Release build: 0 ostrzeżeń, 0 błędów; weryfikacja manifestu: 876 wpisów/17 274 wskazania | 646 000 przypadków; rozbieżności oraz jawne skipy pozostawiają zgodność i poziom V otwarte |
