# A12 — niezależny program integracyjny 8086

## Cel

Dodać mały program wieloinstrukcyjny napisany wprost jako bajty maszynowe i sprawdzić jego wykonanie przez rdzeń na pamięci. Program jest oryginalnym materiałem repozytorium, bez zewnętrznych ROM-ów, urządzeń ani generatorów Pythona. Nie zastępuje wektorów SingleStepTests.

## Zakres

- Program wykonuje `MOV`, `ADD`, `CMP`, warunkowy `JNE` i `HLT`.
- Gałąź błędu ustawia charakterystyczną wartość w AX; test potwierdza wynik AX i zatrzymanie na właściwym HLT.
- Host ładuje bajty do pamięci i dostarcza sześciobajtowe okno prefetch na granicy każdej instrukcji. Rdzeń nie ma jeszcze autonomicznego, ogólnego mechanizmu uzupełniania kolejki; test nie udaje samodzielnego startu ROM.
- Test ma limit taktów, więc błędna pętla kończy się czytelnym błędem.

## Pochodzenie i licencja

Kod maszynowy oraz odpowiadający mu listing są autorskie dla tego repozytorium i udostępniane na licencji MIT projektu. Nie kopiują programu diagnostycznego ani śladów zewnętrznych.

## Kryterium akceptacji

1. Program z deterministycznym podawaniem kolejki kończy się w limicie 256 taktów i osiąga HLT.
2. AX wynosi 0005h po pomyślnej ścieżce; ścieżka błędu ustawiłaby DEADh.
3. Test przechodzi w projekcie `VibeEmulator.Intel8086.Tests`.
4. Dokumentacja podaje bajty, listing, adres uruchomienia, wynik oraz ograniczenie: to test integracyjny podzbioru ISA, nie test DOS, BIOS, autonomicznego fetch ani pełnej cyklowości całej maszyny.

## Plan commitów

1. Ten plan: osobny commit przed zmianą kodu.
2. Fixture/listing i test integracyjny: osobny commit implementacji.
3. Wynik testu i checklisty A12/A10/A11: osobny commit dokumentacji.
