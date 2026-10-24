# Stan wznowienia prac Intel 8086

Stan na 2026-10-25. Dokument zapisuje kontekst do kontynuowania prac w nowej sesji.

## Cel i organizacja branchy

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i oddzielnym commitem. Użytkownik wcześniej wymagał też bieżącego zaznaczania `## Testy i kryteria` w każdym planie.

Istnieją branche `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany` i `A12-8086-pelna-weryfikacja`. Historia A12 zawiera wcześniejsze etapy A0–A11.

## Stan repo

- Aktywny branch: `A12-8086-pelna-weryfikacja`.
- Ostatni commit przed bieżącymi zmianami: `61c59d4 fix(core): popraw cykle IDIV z prefiksami segment i REP`.
- Nie pushowano zmian.
- Bieżące zmiany po tym commicie są jeszcze niezatwierdzone. Przed commitem sprawdzić `git status` i `git diff`.
- Aktualna robocza zmiana rdzenia dotyczy `src/Intel8086/Intel8086Core.Execution.MultiplyDivide.cs` oraz `src/Intel8086/Intel8086Core.Execution.SoftwareInterrupt.cs`.
- `tools/Intel8086VectorRunner/Intel8086VectorRunner.cs` rozszerzono o diagnostykę rozbieżności: długość przebiegu oczekiwana/aktualna, pierwsze T1 odczytu wektora 0 i końcowe `CS:IP`, `SP`, `AX`.

## Postęp A12 w tej sesji

Punktem startowym było 23 mismatchy w pełnym zestawie 646 000 przypiętych wektorów SST (`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`). Wyizolowano dokładnie te rekordy do `/tmp/vibe8086-idiv-23`; pełna lista identyfikatorów jest w `/tmp/vibe-8086-a12-current-after-revert-unsigned-prefetch.json`.

Po korekcie harmonogramu Type 0 i odświeżania prefetch 21 z 23 wyizolowanych przypadków przechodzi. Ostatni kontrolny raport: `/tmp/vibe8086-idiv-23/report-final-turn.json` (łącznie 23, 21 passed, 0 skipped, 2 mismatched, 0 timeout, 0 parse errors). Raport pełnego zestawu z tą samą zmianą jest obecnie uruchomiony w sesji narzędziowej `33292`; przed dalszym użyciem sprawdzić, czy ta sesja nadal istnieje i odczytać jej wynik, nie uruchamiać ponownie tylko z powodu chwilowej przerwy.

Pozostałe przypadki:

| Źródło | Przypadek | Wynik referencyjny | Aktualny wynik |
|---|---|---:|---:|
| `F6.7.json` | `test_num=259`, `IDIV byte [SS:BP-1FFEh]` | 156 T-states, wektor 0 zaczyna się w cyklu 115 | 116 T-states; T1 wektora 0 nie występuje |
| `F6.7.json` | `test_num=1330`, `IDIV CH` | 153 T-states, wektor 0 zaczyna się w cyklu 100 | 101 T-states; T1 wektora 0 nie występuje |

Ich hashe SST są odpowiednio `2bf85c12cc23051ffb1fa0712a778162777711b08bc334c9495293af4a0fc24c` i `8eac0b0e86b15f0699cd8d9499050fb175141689d31b6edeb19e9c82be8c63c7`. Aktualne końcowe `CS:IP` pozostają wartościami początkowymi, więc ścieżka wejścia Type 0 w tych dwóch nieprefiksowanych przypadkach nie zachodzi.

Źródłowe rekordy są dostępne w `/tmp/vibe-8086-a12-source/v1/F6.7.json.gz`. Nie modyfikować rekordów SST; regresje powinny zachować oryginalny JSON i hash.

## Weryfikacja wykonana w tej sesji

- Projekt Intel8086 Release: zbudowany bez ostrzeżeń i błędów.
- Testy `VibeEmulator.Intel8086.Tests` Release: 11 249/11 249, 0 failed, 0 skipped. Test był uruchomiony z pełną budową referencji, więc użył świeżej DLL rdzenia.
- Runner Release: build 0 ostrzeżeń, 0 błędów.
- Wycinek 23 przypadków: 21/23 zgodnych.
- Pełny przebieg 646 000 wektorów zakończył się: 449 569 passed, 196 429 skipped, 2 mismatched, 0 timeoutów i 0 błędów parsowania. Raport: `/tmp/vibe-8086-a12-full-after-21.json`, log: `/tmp/vibe-8086-a12-full-after-21.log`. Runner zwraca kod 1 z powodu tych dwóch znanych mismatchy.
- Bieżąca pełna bramka: Debug i Release build 0 ostrzeżeń/błędów; Release testy 14 049/14 049; formatter verify bez zmian; OpcodeInfo 512/512 zgodnych.

## Następne kroki

1. Zbadać, dlaczego nieprefiksowane Type 0 dla dwóch przypadków `F6.7` nie ustawia CS:IP i kończy się przed odczytem wektora. Źródłem prawdy są przypięte wektory i `docs/cpu/`; nie dodawać wyjątków zależnych od `test_num` ani nie maskować cykli.
2. Dodać lokalne offline regresje dla zachowania naprawionego w tej sesji i dwóch granic, jeśli da się zachować zwykłe testy xUnit bez fałszywego zaliczenia.
3. Uaktualnić `Vectors/README.md` o wynik pełnego przebiegu oraz ograniczenia.
4. Kontynuować otwarte kryteria A12: prawidłowe pomijane opkody/formy oraz niezależny ROM/program. A12 pozostaje niezamknięty.

Szczegółowe kryteria i historia wyników są w [A12-pelna-weryfikacja-i-zamkniecie.md](A12-pelna-weryfikacja-i-zamkniecie.md). Plan nadrzędny znajduje się w `docs/plan-8086-stages/`.
