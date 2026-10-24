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
- [x] Po naprawach 60 rozbieżności z pierwszego przebiegu pełny wynik końcowy to 372 600 passed, 273 400 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania. Wcześniejsze 60 różnic obejmowało DAA/DAS, JCXZ, `81 /7`, IRET i CALL; wszystkie mają dokładne regresje w `Vectors/A12Regression/`.
- [x] Każdy skip raportu zawiera plik, `test_num`, hash i konkretny powód wyjątku. Powody są jawne, ale nie oznaczają zgodności pominiętych form.
- [x] Dodano siedem dokładnych rekordów z pinu źródłowego do lokalnych testów offline: DAA, DAS, dwa `81 /7`, JCXZ, IRET i CALL.
- [ ] Zamknąć wszystkie prawidłowe 8086-opkody i formy pamięciowe, które obecnie są jawnie pomijane. Pełny zestaw nadal pomija 273 400 rekordów, więc poziom II/III/V i pełna zgodność procesora nie są osiągnięte.
- [ ] Wykonać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i warunkami sukcesu; wektory SingleStepTests nie zastępują niezależnego testu.
- [x] Build Debug i Release: 0 ostrzeżeń, 0 błędów; pełna solucja Release: 13 543/13 543 testów; `dotnet format ... --verify-no-changes --severity warn --no-restore`: bez zmian/ostrzeżeń; OpcodeInfo: 512/512.
- [x] Walidator manifestu z pełnym źródłem: 876 wpisów / 17 274 wskazań, 323 pliki / 646 000 rekordów, SHA zgodny.
- [x] Plan nadrzędny i `Vectors/README.md` pokazują wynik A12, SHA i pozostałe ograniczenia.

Raport podaje profil porównania flag (dokładny CPU / maski określonych bitów), SHA źródła i runnera oraz osobno pokrycie ISA, plików i przypadków. Stosować rozłączne kategorie wyników i sprawdzić, że sumują się do liczby zaplanowanych przypadków.

## Tabela wyników

| zestaw | pliki | przypadki | passed | skipped | mismatched | timeout/parse error |
|---|---:|---:|---:|---:|---:|---:|
| próbki repo, w tym A12Regression | 461 | 10 743 testy CPU | 10 743 | 0 | 0 | 0 / 0 |
| pełny zestaw poza repo, SHA `e71c68d` | 323 | 646 000 | 372 600 | 273 400 | 0 | 0 / 0 |
| programowe testy dodatkowe | — | — | — | — | — | — |

Raport szczegółowy runnera: `/tmp/vibe-8086-a12-full-report-final.json`; stdout/stderr: `/tmp/vibe-8086-a12-full-final.log`. Raport jest z przebiegu kodu po naprawach, przed commitem A12. Każdy wpis skip zawiera plik, `test_num`, `test_hash` i powód.

### Klasy jawnych pominięć

| powód | przypadki | zakres do dokończenia |
|---|---:|---|
| prefiks segmentowy obsługiwany tylko dla FE i stringów | 133 589 | A10 i brakujące pamięciowe formy A3–A8; najpierw rozdzielić prefiks ignorowany od rzeczywiście nieobsługiwanej postaci pamięciowej |
| opkod/forma jawnie niezaimplementowana | 100 028 | A3–A5 i A7–A8; m.in. MOV/LEA/XLAT, ALU immediate, XCHG, rejestry/stos/segmenty i TEST immediate |
| pamięciowe formy MOV | 3 051 | A3/A5: 88/89/8A/8B ModR/M pamięciowe |
| pozostałe pamięciowe formy ModR/M | 12 731 | A3/A4/A5/A8: ALU, TEST, immediate oraz formy pamięciowe |
| FF poza /0,/1 | 12 000 | A5/A7: pośrednie CALL/JMP/PUSH zgodnie z liczbami i cyklami źródła |
| F6/F7 nierozpoznane formy | 8 000 | A4/A8: dokończyć wszystkie wskazane rozszerzenia ModR/M i osobno oznaczyć undocumented |
| D0–D3 /6 | 4 001 | Undocumented w metadanych źródła; wymaga niezależnego źródła dla decyzji, nie zaliczać do zgodnego 8086 bez niego |
| **razem** | **273 400** | Wszystkie powody są jawne, lecz pominięte przypadki pozostają poza dowodem zgodności. |

Pierwszy pełny przebieg miał 60 mismatchy. Zostały usunięte przez zmianę logiki DAA/DAS, kolejki dla `81 /7`, timingu JCXZ i zawijania adresu przy IP/stack boundary. Ponowne uruchomienie całego pinu wykazało 0 mismatchy. To nie oznacza pełnej zgodności CPU: metryka potwierdza tylko 372 600 przypadków wykonanych przez bieżący zakres rdzenia.

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
| 2026-10-24 | pełne `v1/*.json.gz` | 372 600 / 273 400 / 0 | build runner Release: 0 ostrzeżeń, 0 błędów | 646 000 przypadków; wszystkie 60 różnic naprawiono; 273 400 jawnych skipów pozostawia pełną zgodność otwartą |
