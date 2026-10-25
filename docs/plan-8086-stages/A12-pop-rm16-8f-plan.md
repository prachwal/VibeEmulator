# A12 — `POP r/m16` (`8F /0`)

## Cel

Uzupełnić pamięciową i rejestrową formę `POP r/m16` opkodu `8F`, w tym prawidłową kolejność odczytu słowa ze stosu, aktualizacji `SP`, zapisu do operandu, adresowania ModR/M, override segmentu docelowego, kolejki prefetch i wszystkich T-state magistrali. Pakiet jest częścią branchu `A12-8086-pelna-weryfikacja`; plan i implementację zapisać w osobnych commitach.

## Źródła i inwentaryzacja

- Przypięte źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, plik `v1/8F.json.gz` (2 000 rekordów).
- Programowa klasyfikacja bajtów instrukcji z tego pliku: 977 rekordów bez prefiksu, 1 023 z prefiksem segmentowym. Poprzedni pełny raport A12 przypisywał 977 nieprefiksowanych rekordów do skipu `8F`, a prefiksowane do kategorii prefiksów.
- Rozszerzenie ModR/M `/0` występuje w 238 rekordach: 116 bez prefiksu i 122 prefiksowane. Wszystkie formy `/0` występują dla `mod=00/01/10/11`: odpowiednio (bez prefiksu) 31/23/29/33 i (z prefiksem) 28/27/35/32.
- Pozostałe 1 762 rekordy używają `/1–/7`. Implementować tylko zdefiniowane `POP r/m16` `/0`; nie przypisywać rozszerzeniom `/1–/7` semantyki POP. Zostawić je jako jawne skipy do odrębnej decyzji o nielegalnych kodowaniach.
- Kontekst sprzętowy: tabela operacji ręcznika Intel 8086 i wektory SST są źródłami zachowania. `docs/cpu/sources.md` odnotowuje ręcznik i pin zestawu SingleStepTests; szczegóły cykli T-state, kolejki i zapisu mają wynikać z rekordów pinu, a rozbieżności z manualem opisać.

## Zakres semantyki

1. Dekodować `/0` dla wszystkich czterech wartości `mod`, rejestr ogólny przy `mod=11` i pamięć dla `mod=00/01/10`.
2. Odczytać słowo spod `SS:SP`, a następnie zwiększyć `SP` o 2 modulo 16 bitów. Dla celu `SP` zachować zachowanie wynikające z wektorów (wczytana wartość zastępuje końcowy SP).
3. Dla celu rejestrowego zapisać do wskazanego rejestru. Dla celu pamięciowego obliczyć EA zgodnie z dekoderem; domyślny segment EA wynika z rejestrów adresujących, a prefiks segmentowy wybiera segment celu. Źródłem pozostaje `SS:SP`.
4. Dla słowa pod nieparzystym adresem stosu i/lub celu pamięciowego odtworzyć osobne transfery bajtów, BHE, idle i kolejność z wektorów. Nie zakładać, że odczyt stosu i zapis celu można skrócić do atomowego dostępu.
5. Zachować FLAGS oraz pozostałe rejestry i pamięć poza skutkiem `POP`; uwzględnić przypadki aliasowania celu z operandem stosu i granice zawinięcia offsetu.
6. Prefiks segmentowy zmienia tylko segment celu pamięciowego; nie zmienia segmentu stosu. Poprawnie konsumować prefiks/opcode/ModR/M/displacement i zachować granice instrukcji oraz kolejkę.
7. ModR/M `/1–/7` pozostawić bez semantyki POP. Raportować ich skipy jawnie; nie liczyć ich jako zaliczonych rekordów `/0`.

## Plan realizacji

### P0 — plan i źródło

- [x] Zweryfikować aktualny branch, czyste drzewo i zależność A12 od poprzednich etapów.
- [x] Przejrzeć przypięte `8F.json.gz`, policzyć prefiksy, rozszerzenia i formy ModR/M; zapisać SHA i zakres w planie.
- [x] Zapisać ten plan w oddzielnym commicie przed implementacją.

### P1 — próbki i dekoder

- [x] Dodać do lokalnego manifestu pierwsze rekordy `/0` z każdej dostępnej kombinacji prefiksu i ModR/M: 20 grup, 202 rekordy łącznie. Fixture’y są niezmienionymi rekordami źródłowymi powiązanymi przez `testNumbers`/hashe.
- [x] Dodać stan transferu instrukcji `8F /0` oddzielony od obecnego `POP r16` i `POPF`; aktywować wyłącznie po potwierdzeniu ModR/M `/0`.
- [x] Rozszerzyć dekodowanie o ModR/M, displacement i prefiks; dekodowanie nie odczytuje operandów z pamięci.
- [x] Zaimplementować fazy odczytu `SS:SP`, aktualizację rejestru/SP oraz fazy zapisu EA dla `mod=00/01/10` zgodnie z pełnymi śladami SST.
- [x] Obsłużyć `mod=11`, w tym `POP SP`, według pełnych rekordów SST.

### P2 — iteracja na wektorach

- [x] Uruchomić lokalne fixture’y: 202/202 passed, 0 mismatchy, timeoutów i błędów parsowania.
- [x] Utworzyć pełny filtr wyłącznie dla 238 poprawnych rekordów `/0`: 238/238 passed, 0 skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Potwierdzić przypadki brzegowe w pełnych wektorach: `POP SP` 11 rekordów, parzyste/nieparzyste `SS:SP` 113/125, BP-domyślne SS 31, segment-prefiksy 122, przypadki graniczne SP 11.
- [x] Potwierdzić na całym `8F.json.gz`: 238 passed i 1 762 jawne skipped dla `/1–/7`, bez mismatchy, timeoutów i błędów parsowania.

### P3 — pełny korpus i dokumentacja

- [x] Uruchomić pełny korpus 646 000 rekordów z tym samym maskowanym manifestem A12 co checkpoint po LES/LDS.
- [x] Zaktualizować liczbę `passed/skipped/mismatched/timeouts/parseErrors`, pokrycie `passed / 646 000`, powody skipów i macierz ISA.
- [x] Zaktualizować README wektorów, raport JSON, checklistę A12 i stan wznowienia.

### P4 — bramki i commity

- [x] `dotnet build VibeEmulator.slnx -c Debug --nologo` — 0 ostrzeżeń i błędów.
- [x] `dotnet build VibeEmulator.slnx -c Release --nologo` — 0 ostrzeżeń i błędów.
- [x] `dotnet test VibeEmulator.slnx -c Release --nologo` — 21 273/21 273 testów zielonych.
- [x] `dotnet format VibeEmulator.slnx --verify-no-changes --severity warn` — bez zmian.
- [x] `dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code` — 512/512 zgodnych, 0 rozbieżności.
- [x] Zweryfikować manifest wektorów (1 277 wpisów, 25 073 rekordy wskazań, 323 pliki/646 000 źródłowych rekordów) i `git diff --check`.
- [ ] Zatwierdzić implementację i dokumentację pakietu w osobnym commicie, po wcześniejszym osobnym commicie planu. Nie wypychać.

## Kryteria ukończenia

- Wszystkie 238 rekordów `/0` z przypiętego pliku 8F przechodzi pełne porównanie CPU, kolejki i T-state.
- `/1–/7` nie dostają nieudokumentowanej semantyki i pozostają widoczne w raportach.
- Pełny korpus i każda bramka z P4 mają zapisane liczby; checklista A12 oznacza punkt jako ukończony i podaje zaktualizowane pokrycie.
- Plan i implementacja mają oddzielne identyfikatory commitów na branchu A12.

## Raporty

- Baseline całego pliku przed zmianą: 2 000 rekordów, 0 passed, 2 000 skipped, 0 mismatchy/timeoutów/błędów parsowania. To zawiera nieprefiksowane i segment-prefiksowane formy `/0` oraz nieimplementowane rozszerzenia `/1–/7`; baseline legalnego podzbioru `/0` jest osobno wykazany jako 238 skipów w inwentarzu źródła.
- Poprawne podzbiory `/0` i pełny korpus będą raportowane osobno po implementacji.
