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

## Kryteria akceptacji

- [ ] Manifest plików kompletny wobec źródłowego drzewa, a osobna macierz ISA uwzględnia instrukcje bez wektorów.
- [ ] W deklarowanym zakresie brak mismatched, parse errors, timeoutów i niewyjaśnionych pominięć; jawna luka uniemożliwia ogłoszenie pełnej zgodności tego zakresu.
- [ ] Próbki lokalne odtwarzalne offline; pełne testy poza repo uruchomione. Status „nieuruchomione” zachowuje uczciwy raport, ale blokuje zamknięcie A12 i deklarację poziomu V.
- [ ] Raport końcowy rozdziela poziomy I–V i nie nadinterpretuje coverage.
- [ ] Zapisano znane uproszczenia i pozostałe niezweryfikowane zachowania.
- [ ] Build Debug/Release, test Release i formatowanie zgodne z definicją repo.
- [ ] Plan nadrzędny i `Vectors/README.md` pokazują aktualny stan, SHA i zakres.

Raport podaje profil porównania flag (dokładny CPU / maski określonych bitów), SHA źródła i runnera oraz osobno pokrycie ISA, plików i przypadków. Stosować rozłączne kategorie wyników i sprawdzić, że sumują się do liczby zaplanowanych przypadków.

## Tabela wyników

| zestaw | pliki | przypadki | passed | skipped | mismatched | timeout/parse error |
|---|---:|---:|---:|---:|---:|---:|
| próbka w repo | — | — | — | — | — | — |
| pełny zestaw poza repo | — | — | — | — | — | — |
| programowe testy dodatkowe | — | — | — | — | — | — |

## Pułapki

1. Nie łączyć wyników próbek z pełnym zestawem w jedną sumę bez oznaczenia źródła.
2. Zmiana runnera może zmienić wynik; zapisać wersję runnera/commit wraz z raportem.
3. Testy wygenerowane przez inny emulator nie są niezależne, jeśli dzielą ten sam błąd referencyjny.
4. Zielona solucja nie dowodzi cyklowej zgodności, jeśli comparator nie bada cykli/prefetch.

## Dziennik postępu

| data | zestaw | passed/skipped/mismatched | jakość repo | konkluzja |
|---|---|---|---|---|
| — | — | nieuruchomiono | nieuruchomiono | etap planowany |
