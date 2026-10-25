# Wektory Intel 8086 — SingleStepTests/8086

Źródło: [SingleStepTests/8086](https://github.com/SingleStepTests/8086), commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; licencja MIT zachowana w `LICENSE-SingleStepTests.txt`. Wektorów nie wolno ręcznie edytować. Format źródłowy to skompresowane `v1/*.json.gz`; `v1_binary/*.MOO.gz` zawiera równoległe dane w formacie MOO i nie jest drugim zestawem przypadków.

## Inwentarz źródła

Inwentarz wygenerowano 2026-10-23 skryptem `tools/Intel8086VectorTools/inventory_and_samples.py` z checkoutu powyższego SHA. Pełna lista plików, opkodów/rozszerzeń ModR/M, statusów metadata, liczby rekordów oraz rozmiarów jest w [`source-inventory.json`](source-inventory.json); zachowuje odrębność inwentarza źródłowego od aktywnych próbek testów.

| pomiar | wynik |
|---|---:|
| wersja README upstream / `metadata.json` | 1.0.1 / 1.0.0 |
| model opisany w `metadata.json` | Intel P80C86A-2 782003 J20, generator `arduino808X` |
| klucze opkodów w metadata | 256 |
| pliki `*.json.gz` | 323 |
| rekordy JSON | 646 000 (323 pliki × 2000) |
| bajty skompresowanych plików wektorów JSON | 181 450 153 |
| bajty nieskompresowanych JSON | 2 442 107 061 |
| pliki `*.MOO.gz` / bajty skompresowane | 323 / 97 819 805 |
| aktywne próbki CPU w repo | manifest główny 595 wpisów / 11 762 rekordy; manifesty katalogowe 415 wpisów / 8 187 wskazań (część indeksuje te same fixture'y); Intel8086 Release: 13 431 testów |

Metadata: 204 `normal`, 20 `alias`, 8 `prefix`, 8 `fpu`, 1 `undocumented` oraz 15 wpisów grupowych bez pola `status`, z podopkodami w `reg`. Nie klasyfikować wpisu bez `status` jako „brak danych”. Walidator ponownie policzył wszystkie rekordy z checkoutu; `B3` i `EA` mają po 2000 rekordów.

## Zakres źródła i ograniczenia

README repo opisuje pomiary fizycznego P80C86A-2 w trybie Maximum Mode przez ArduinoX86 i Intel 8288; testy mają pełną początkową kolejkę prefetch, bez wait states. README podaje, że INTR, NMI i TF nie są ćwiczone. `metadata.json` ma wersję 1.0.0, mimo README/changelogu 1.0.1. Przykład README używa pól `hash` i `idx`, lecz sprawdzone rekordy na przypiętym commicie zawierają `test_hash` i `test_num`; parser musi używać rzeczywistego schematu plików. Te dane nie są dowodem dla wszystkich wariantów 8086 ani dla 8088.

Dokumentację Intel i sygnały wymagające jej potwierdzenia odnotowano w [`docs/cpu/sources.md`](../../../docs/cpu/sources.md); pełnej instrukcji producenta nie udało się pobrać (HTTP 403 w użytym źródle). Nie wyprowadzać brakujących szczegółów sprzętu z samych wektorów.

## Próbki i polecenia

Polityka próbki: pierwsze **20 rekordów** z każdego wybranego pliku w kolejności upstream, JSON zwarty; dla przefiltrowanych form opcode eksportuje się pierwsze 20 rekordów spełniających warunek. Pełne dane pozostają poza repo; testy xUnit nie pobierają sieci. Na etapie A1 uruchomiono kontrolny eksport pierwszych 20 z 323 plików do `/tmp/vibe-8086-a1-samples` (16 MB). A2 aktywuje `90.json`; A3 `B0.json`–`B7.json`, `88/8A mod=11` oraz formy ALU opisane niżej; A5 `B8.json`–`BF.json` i `89/8B mod=11`. Pozostałe formy pozostają nieaktywne.

```bash
git clone --filter=blob:none --no-checkout https://github.com/SingleStepTests/8086.git /tmp/vibe-8086-a1
git -C /tmp/vibe-8086-a1 fetch origin e71c68d215a6bb8c356bd4cb3842de3bef345ca9
git -C /tmp/vibe-8086-a1 checkout --detach e71c68d215a6bb8c356bd4cb3842de3bef345ca9
git -C /tmp/vibe-8086-a1 rev-parse HEAD
python3 tools/Intel8086VectorTools/inventory_and_samples.py \
  --source /tmp/vibe-8086-a1/v1 \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --inventory /tmp/vibe-8086-source-inventory.json \
  --samples-output /tmp/vibe-8086-a1-samples \
  --count 20
```

Skrypt sprawdza SHA checkoutu, parsuje `metadata.json`, liczy rzeczywiste rekordy kluczem `test_num`, zachowuje źródłowe nazwy (wielkie litery i rozszerzenia typu `80.0`) oraz może wycinać próbki bez materializowania pełnych 2,4 GB JSON w pamięci. Próbki generuje do podanego katalogu; nie nadpisuje danych źródłowych.

W comparatorze pole BHE jest ignorowane wyłącznie dla cykli `PASV`, gdzie nie ma aktywnego transferu; pozostałe pola cyklu oraz BHE dla cykli aktywnych są porównywane. Opcjonalne `flagsMask` w manifeście maskuje różnice flag CPU wskazane przez metadata źródłowe; tę samą maskę stosuje się do zapisanego FLAGS na stosie i jego danych magistrali przy Type 0. Brak pola oznacza porównanie wszystkich bitów. `ignoredFlagsMask`, jeśli podano osobno, określa bity ignorowane przy zapisie FLAGS. Runner zapisuje różnice również dla przypadków, których dekoder jeszcze nie obsługuje.

## Raport uruchomienia wektorów

| zakres | wynik |
|---|---|
| inwentarz przypiętego źródła | 323 pliki, 646 000 rekordów policzonych |
| eksport próbki A1 poza repo | 323 pliki po 20 rekordów, eksport zakończony |
| aktywne próbki CPU w repo | manifest główny 595 wpisów / 11 762 rekordy; manifesty katalogowe 415 wpisów / 8 187 wskazań (część indeksuje te same fixture'y); Intel8086 Release: 13 431 testów |
| rejestrowe `PUSH`/`POP` A5 | 16 plików × 20 próbek: 320/320; pełny filtr upstream: 32 000/32 000, 0 skipów i 0 mismatchy |
| rejestrowe i segmentowe `PUSH`/`POP` A5 | lokalne próbki 460/460; pełny filtr upstream 46 000/46 000, 0 skipów, mismatchy, timeoutów i błędów parsowania |
| przygotowane próbki A9 | 18 plików × 20 pierwszych rekordów (string `A4`–`A7`, `AA`–`AF`; I/O `E4`–`E7`, `EC`–`EF`) oraz 20 filtrowanych `MOVSB` bez prefiksu, DF=0; SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` |
| `MOV AL/AX ↔ moffs16` A12 | lokalne próbki 80/80; pełne przypięte pliki `A0`–`A3`: 8 000/8 000, 0 skipów, 0 mismatchy, 0 timeoutów i błędów parsowania; raport [`A12-moffs-report.json`](../../../docs/plan-8086-stages/A12-moffs-report.json) |
| pełne porównanie wykonania CPU dla wspieranych form | NOP/B0–BF 34 000/34 000; 88/8A mod=11 519/519; AL-immediate 8 029/8 029; ALU ModR/M register-direct 4 355/4 355; MOV word ModR/M 89/8B mod=11 490/490; word ALU AX-immediate 7 986/7 986; word ALU ModR/M register-direct 4 180/4 180; word ALU pamięciowe ModR/M 12 937/12 937; `INC/DEC r16` 32 000/32 000; `FF /0,/1 mod=11` 506/506; `FE /0,/1 mod=11` 561/561; `F6/F7 /2,/3 mod=11` 987/987; `D0/D1 /0–/5,/7 mod=11` 3 549/3 549; `D2/D3 /0–/5,/7 mod=11` 3 478/3 478; `FE /0,/1` z i bez prefiksów 4 000/4 000; nieprefiksowane pamięciowe `FF /0,/1` 1 511/1 511; nieprefiksowane pamięciowe `F6/F7 /2,/3` 2 996/2 996; nieprefiksowane pamięciowe `D0–D3 /0–/5,/7` 21 067/21 067; nieprefiksowane rejestrowe `81/83 /0–/7` 4 034/4 034; razem 146 624 |
| wszystkie obserwowane prefiksowane rekordy | Pełny filtr 580 par sekwencja/opcode: 197 748 rekordów, 48 306 zgodnych, 149 442 rozbieżne, 0 skipów; 178 grup opcode w całości zgodne. Szczegóły per para i przykłady mismatchy w `docs/plan-8086-stages/A10-prefix-coverage-report.json`; grupy obejmują też niezaimplementowane rozszerzenia ModR/M. Sekwencje wieloprefiksowe nieobecne w pinie opisano jako known-gap w `docs/plan-8086-stages/A10-prefix-sequence-decisions.json`. |
| pełne źródłowe drzewo (A12) | Po `XCHG r/m16,r16` (`87`): 323 pliki / 646 000 rekordów, **507 519 passed, 138 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**. Filtry `80` i `82`: po 16 000/16 000; `FF /6`: 2 000/2 000; `FF /7`: 2 000/2 000; `A9`: 2 000/2 000; `A0`–`A3`: 8 000/8 000; `87`: 2 000/2 000; `91`–`97`: 14 000/14 000; `9C`/`9D`: 4 000/4 000. Pozostałe jawne skipy są otwarte; pełna zgodność ISA nie jest potwierdzona. Raport: `/tmp/vibe-a12-full-after-xchg87-final.json`; szczegóły: [`A12-pelna-weryfikacja-i-zamkniecie.md`](../../../docs/plan-8086-stages/A12-pelna-weryfikacja-i-zamkniecie.md). |

Próbki `A3ModRm/88.json` i `A3ModRm/8A.json` są filtrowane: zawierają po 20 pierwszych rekordów spełniających `mod=11` z przypiętego pliku źródłowego, a nie pierwsze 20 rekordów pliku. Pełny filtr daje 243 przypadki `88` i 276 przypadków `8A`, wszystkie zgodne end-to-end. Formy z operandem pamięci pozostają nieaktywne.

### Próbki helpera ALU (A4)

`A4Alu/` zawiera po 20 pierwszych rekordów upstream dla `04`, `14`, `1C`, `2C`, `3C`, `24`, `0C`, `34`. Testują wynik i określone flagi czystego helpera ALU; nie są aktywne w runnerze wykonania CPU i nie weryfikują cykli ani pełnego stanu. Dla plików z `flags-mask=0xFFEF` manifest helpera wyłącza tylko bit AF (0x0010), zgodnie z maską upstream.

`A4Cpu/` zawiera po 20 pierwszych przypadków bez prefiksów dla `04`, `0C`, `14`, `1C`, `24`, `2C`, `34` i `3C`. To próbki filtrowane według kodu na początku instrukcji, nie pierwsze rekordy całych plików. Pełne nieprefiksowane podzbiory upstream zawierają 8 029 przypadków; wszystkie przechodzą pełne porównanie CPU, w tym flagi i cykle. Próbki helperowe `A4Alu/` pozostają osobnym źródłem testów.

`A4ModRm/` zawiera po 20 pierwszych rekordów upstream dla 17 kodowań byte ALU/TEST, filtrowanych warunkiem `bytes[0] == opcode` oraz `ModR/M mod=11`. Nie są to pierwsze rekordy całych plików. Pełny przefiltrowany podzbiór to 4 355 rekordów, wszystkie zgodne z runnerem CPU; formy pamięciowe pozostają nieaktywne.

`A5ModRm/` zawiera po 20 pierwszych rekordów upstream dla `89` i `8B`, filtrowanych przez opcode bez prefiksu i `ModR/M mod=11`. Pełny przefiltrowany podzbiór to 490 rekordów (234 + 256), zgodnych w porównaniu rejestrów, kolejki i T-states. Formy pamięciowe pozostają otwarte.

### Próbki helpera ALU słownego (A6)

`A6Alu/` zawiera po 20 rekordów dla `05`, `15`, `1D`, `2D`, `3D`, `25`, `0D`, `35`. Testy porównują wynik helpera i określone flagi; nie uruchamiają CPU ani cykli. Maski niezdefiniowanego AF są wyłącznie te z metadanych (`0xFFEF`).


`A6Cpu/` zawiera po 20 pierwszych rekordów bez prefiksów dla word AX-immediate `05`, `0D`, `15`, `1D`, `25`, `2D`, `35`, `3D`. Wszystkie przechodzą pełne porównanie CPU: stan, kolejka i cztery T-states (160/160 próbki; pełny filtr 7 986/7 986). Inne formy A6 są aktywowane w fixture `A6ModRm/`, `A6IncDec/`, `A6Group/` i `A6Shift/`.


`A6ModRm/` zawiera po 20 pierwszych przypadków dla 17 opcode word ALU/TEST, filtrowanych dokładnym opcode i `ModR/M mod=11`. Wszystkie próbki przechodzą porównanie stanu, kolejki i trzech T-states (340/340); pełny filtr daje 4 180/4 180. Pamięciowe formy ModR/M nie są objęte tym wynikiem.


`A6IncDec/` zawiera po 20 pierwszych rekordów dla `INC/DEC r16` (`40`–`4F`). Pełne uruchomienie wszystkich 16 plików źródłowych przeszło 32 000/32 000; próbki 320/320 porównują rejestry, FLAGS, kolejkę i oba T-states.


`A6Group/` zawiera próbki rejestrowe dla `FF /0,/1,/6,/7`, `FE /0,/1`, `F6/F7 /2,/3` oraz pamięciowe FE/FF/F6/F7 i `D0–D3 /0–/5,/7`. Porównanie obejmuje stan, RAM, flagi, kolejkę i T-states: FF register-direct `/0,/1` 40/40 i pełny filtr 506/506; nieprefiksowane FF pamięciowe `/0,/1` 40/40 i 1 511/1 511; `FF /6` 40/40, pełny nieprefiksowany filtr 1 014/1 014 (252 rejestrowe, 762 pamięciowe); `FF /7` 40/40, pełny nieprefiksowany filtr 996/996 (238 rejestrowych, 758 pamięciowych); F6/F7 register-direct 80/80 i 987/987; nieprefiksowane F6/F7 pamięciowe 80/80 i 2 996/2 996; FE register-direct 40/40 i 561/561; wszystkie FE 4 000/4 000; pamięciowe D0–D3 560/560 i 21 067/21 067. Segment-prefiksowane `FF /6,/7` mają po 20 rejestrowych i pamięciowych próbek dla ES/CS/SS/DS oraz pełny filtr 1 990/1 990. Prefiksowane D0–D3 `/0–/5,/7` pozostają jawnie znaną luką.

`A6WordAlu/` zawiera po 20 nieprefiksowanych rekordów pamięciowych dla 17 word-ALU opcode. Próbki przechodzą 340/340, pełny filtr 12 937/12 937; porównanie obejmuje stan, pamięć, kolejkę i każdy T-state. `CMP/TEST` oraz kierunek do rejestru są read-only na magistrali; pozostałe operacje wykonują zapis wyniku.

`A6WordImmediate/` zawiera rejestrowe próbki `81/83 /0–/7` (320 rekordów, 320/320; pełny nieprefiksowany filtr `mod=11`: 4 034/4 034) oraz 48 pamięciowych fixture dla `mod=00/01/10` (po 20, razem 960). Bieżący aktywny przebieg: 4 880/4 880 zaliczonych, 0 skipów; wszystkie 960/960 pamięciowe fixture zgodne w stanie, RAM, FLAGS, kolejce i każdym T-state.

Fixture `A6Group/FE.0.json` i `FE.1.json` zawierają po 20 rekordów `mod=11` wybranych ze źródła SHA powyżej. Próbki przechodzą 40/40; pełny filtrowany runner przechodzi 561/561 (258 dla `/0`, 303 dla `/1`), bez skipów i mismatchy.

`A6Shift/` zawiera po 20 przypadków dla `D0/D1/D2/D3 /0–/5,/7 mod=11`; próbki przechodzą 560/560, a pełne filtrowane podzbiory 7 027/7 027. Dla `D0/D1` wpisy `/4`, `/5`, `/7` przekazują `flagsMask=65519` (ignorowany AF); dla `D2/D3` `/0–/3` przekazują `63487` (ignorowany OF), a `/4,/5,/7` `63471` (ignorowane OF i AF), zgodnie z `metadata.json` upstream. Audyt `D2/D3` wykazał CL 0–62 oraz długość `8` (CL=0) lub `8+4×CL`. Formy pamięciowe nie należą do tego wyniku.
Fixture `A6Group/FE.0.mem.json` i `FE.1.mem.json` zawierają po 20 rekordów pamięciowych z przypiętego źródła. Pełny filtr obejmuje 737 przypadków `/0` i 717 `/1`, bez prefiksów; runner zakończył się 1 454/1 454 bez skipów i mismatchy.

`A6Group/FF.0.mem.json` i `FF.1.mem.json` zawierają po 20 nieprefiksowanych przypadków pamięciowych. Pełny filtr ma 768 rekordów `/0` i 743 `/1` (łącznie 1 511/1 511); słowa wyrównane i pod nieparzystym adresem przechodzą pełne porównanie CPU.

### Prefiksy i dekoder (A10)

`A10Prefix/` zawiera po 20 przypadków pamięciowych i rejestrowych dla `FE /0,/1` dla każdego z czterech prefiksów segmentowych; wszystkie 320 próbek jest aktywnych i przypiętych. Są tam także próbki `FF /0,/1`, rejestrowe `F6/F7 /2,/3`, 1 280 rejestrowych `81/83` i 320 segment-prefiksowanych rejestrowych MOV `88/89/8A/8B` (20 na opcode/prefiks). Filtr pełnego źródła dla MOV przechodzi 1 010/1 010, a pełny filtr kolejności segment+REP+string przechodzi 4 961/4 961 (raport [`A10-segment-rep-string-report.json`](../../../docs/plan-8086-stages/A10-segment-rep-string-report.json)). Pełne segment-prefiksowane `FE /0,/1` przechodzi 1 985/1 985; `FF.0.json`/`FF.1.json` 4 000/4 000; `F6/F7 /2–/7` 11 849/11 849; pamięciowe `81/83 /0–/7` 11 870/11 870; rejestrowe `81/83` 4 075/4 075. Cały lokalny manifest A10Prefix przechodzi 2 480/2 480. Porównanie obejmuje stan końcowy, RAM, FLAGS, kolejkę i każdy T-state. Testy Intel8086 Release: 10 716/10 716; test LOCK+REP 2/2 dla początkowej kolejki 5/6 bajtów.

Eksport tych 320 fixture'ów z przypiętego źródła odtwarza:

```bash
python3 tools/Intel8086VectorTools/export_segment_prefixed_mov_vectors.py \
  --source /tmp/vibe-8086-a1/v1 \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --output /tmp/a10-mov-samples
```

Skrypt zapisuje pierwszych 20 rekordów w kolejności upstream dla każdej pary segment-prefiks/opcode i zachowuje `test_num`.

Pełny filtr kolejności segment+REP/string odtwarza:

```bash
python3 tools/Intel8086VectorTools/export_segment_rep_string_vectors.py \
  --source /tmp/vibe-8086-a1/v1 \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --output /tmp/a10-prefix-segment-rep-string
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/a10-prefix-segment-rep-string \
  --manifest /tmp/a10-prefix-segment-rep-string/manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a10-segment-rep-string-report.json
```

`A10Alias/` aktywuje po 20 rekordów dla każdego aliasowego Jcc 60h–6Fh i RETN/RETF C0/C1/C8/C9; `testNumbers` wiąże fixture'y z przypiętym źródłem. Pełny filtr nieprefiksowanych Jcc przechodzi 26 004/26 004, RET aliasy 8 000/8 000, a kanoniczne RET C2/C3/CA/CB również 8 000/8 000. Poprawka RET obejmuje zawijanie adresu fetchu i stosu w segmencie. Cały lokalny zestaw Intel8086: 10 716/10 716.

`A10AliasSegment/` zawiera po 20 rekordów dla każdej z 24 zaobserwowanych par segment override + alias Jcc (opkody `69`, `6B`–`6F`; cztery prefiksy segmentowe). Wszystkie 480 fixture'ów jest powiązanych z `test_num`; pełny filtr upstream przechodzi 5 996/5 996 bez skipów i rozbieżności.

```bash
python3 tools/Intel8086VectorTools/export_segment_prefixed_jcc_vectors.py \
  --source /tmp/vibe-8086-a1/v1 \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --output /tmp/a10-segment-jcc-vectors
```

Pełny prefiksowany audyt A10 można odtworzyć poza repo. Bieżący wynik to 580 grup / 197 748 rekordów, 48 306 zgodnych, 149 442 rozbieżne, 0 skipów; 178 grup w całości zgodnych, 402 z co najmniej jedną rozbieżnością. Runner kończy się kodem 1, bo pełny zakres zawiera niezaimplementowane formy; raport i macierz zachowują każdą rozbieżność.

```bash
python3 tools/Intel8086VectorTools/audit_prefix_sequences.py \
  --source /tmp/vibe-8086-a1/v1 \
  --sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  > /tmp/a10-prefix-audit.json
python3 tools/Intel8086VectorTools/export_prefixed_vectors.py \
  --source /tmp/vibe-8086-a1/v1 \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --output /tmp/a10-all-prefixed-vectors
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/a10-all-prefixed-vectors \
  --manifest /tmp/a10-all-prefixed-vectors/manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a10-all-prefixed-report.json \
  > /tmp/a10-all-prefixed-run.log 2>&1
python3 tools/Intel8086VectorTools/report_prefixed_coverage.py \
  --manifest /tmp/a10-all-prefixed-vectors/manifest.json \
  --run-report /tmp/a10-all-prefixed-report.json \
  --prefix-audit /tmp/a10-prefix-audit.json \
  --output docs/plan-8086-stages/A10-prefix-coverage-report.json
```

ESC D8h–DFh zachowuje zachowanie magistrali procesora (bez emulacji zewnętrznego 8087); pełny nieprefiksowany filtr przechodzi 7 988/7 988.

`A6Group/F6.2.mem.json`, `F6.3.mem.json`, `F7.2.mem.json` i `F7.3.mem.json` zawierają po 20 pierwszych nieprefiksowanych rekordów pamięciowych z przypiętych plików źródłowych. Próbki przechodzą 80/80; pełny filtr łącznie obejmuje 2 996 rekordów, wszystkie zgodne. Prefiksy segmentowe pozostają do etapu A10.

`A6Group/FE.{0,1}.prefixed.{reg,mem}.json` zawierają po 20 przypadków z prefiksem segment override na każdą kombinację rozszerzenia i rodzaju operandu. Próbki przechodzą 80/80. Pełny filtr oryginalnych plików `FE.0.json` i `FE.1.json`, zawierających razem 4 000 przypadków prefiksowanych i nieprefiksowanych, przechodzi 4 000/4 000; część segment-prefiksowana przechodzi 1 985/1 985.

### Predykaty warunków skoku (A7)

`A7Branch/` zawiera po 20 rekordów dla opkodów `70`–`7F` (320). Testy CPU przechodzą 320/320 i porównują IP/CS, kolejkę, RAM oraz każdy T-state dla taken i not-taken. Test helpera predykatów obejmuje 319/319 jednoznacznych wektorów; displacement zerowy pominięto tylko w oracle kierunku. Pełnych plików źródłowych nie uruchomiono.

`A7Jump/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `EB`, `E9` i `EA` (60). Testy CPU przechodzą 60/60, porównując CS:IP, kolejkę, RAM i każdy T-state. Prefiksowane JMP pozostają nieaktywne do A10.

`A7CallRet/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `E8`, `9A`, `C3`, `C2`, `CB` i `CA` (120). Wszystkie są aktywne i przechodzą 120/120; źródło to przypięty SHA. Prefiksowane formy pozostają do A10.

`A7Loop/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `E0`–`E3` (80). Wszystkie są aktywne i przechodzą 80/80; asercje obejmują CX, FLAGS, kolejkę i każdy T-state. Próbki pochodzą z przypiętego SHA.

`A7Interrupts/` zawiera po 20 rekordów dla `CD`, `CC`, `CE` i `CF`. Wszystkie są aktywne i przechodzą 80/80, obejmując IVT, ramkę stosu, oba parytety SP, flags IF/TF, obie ścieżki INTO, kolejkę i każdy T-state.

`A7Flags/` zawiera po 20 rekordów dla `F5`, `F8`, `F9`, `FA`, `FB`, `FC`, `FD`, `9E` i `9F`. Wszystkie są aktywne i przechodzą 180/180; testują CF/IF/DF oraz transfer pól flag do i z AH.

### Arytmetyka specjalna (A8)

`A8Arithmetic/` zawiera aktywne po 20 pierwszych rekordów źródła dla `98`, `99`, `D4`, `D5`, `27`, `2F`, `37`, `3F`, po 20 nieprefiksowanych `mod=11` dla `F6/F7 /4–/7`, po 20 pamięciowych `mod=00/01/10` dla `F6/F7 /4–/7`, dwa dokładne rekordy graniczne `IDIV` z Type 0 oraz wszystkie 12 przypadków `D4 00`, po 20 nieprefiksowanych przypadków dla każdego `D8`–`DF` (ESC) i źródłowe rekordy REP-IDIV: F6/F7 `/7` (141 rekordów). Aktywne próbki CPU A8 przechodzą 803/803 (w tym dwa przypadki graniczne oraz test limitu T-states runnera). Helpery MUL/IMUL/DIV/IDIV byte/word oraz testy graniczne `IDIV` egzekwują 8086-owy zakres ilorazu: `-127..127` oraz `-32767..32767`; przypadek `-128`/`-32768` zgłasza Type 0, zgodnie z podręcznikiem 8086 i SST. Pełny przebieg A12 całego przypiętego źródła po `PUSHF`/`POPF` przechodzi 475 612/646 000, pomija 170 388 i ma 0 mismatchy.

### Alias grupy ALU immediate (A12)

`A12Group/82.0.json`–`82.7.json` zawierają po 20 pierwszych nieprefiksowanych rekordów dla każdego rozszerzenia ModR/M opcode `82`. Dodatkowe `80.seg{26,2E,36,3E}.{0..7}.json` i `82.seg{26,2E,36,3E}.{0..7}.json` pokrywają każdą kombinację prefiksu segmentowego z rozszerzeniem `/0`–`/7` po 20 wektorów. Łącznie test lokalny obejmuje 1 280 segment-prefiksowanych wektorów `80`/`82`; pełny filtr źródła przechodzi 16 000/16 000 dla każdego opkodu. Profile T-state prefiksowanego `82` opierają się na wszystkich 7 959 rekordach segment-prefiksowanych z przypiętego źródła `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.

`A12Group/A9.json` zawiera 20 pierwszych rekordów upstream `TEST AX,imm16`, aktywnych w testach CPU. Próbka przechodzi 20/20; pełny nieprefiksowany plik z przypiętego źródła przechodzi 2 000/2 000, porównując FLAGS z maską `0xFFEF` (AF pomijane według metadata), stan, pamięć, kolejkę i każdy T-state.

### Bezpośrednie adresowanie akumulatora (A12)

`A12Group/A0.json`–`A3.json` zawierają po 20 pierwszych rekordów upstream dla `MOV AL/AX ↔ moffs16`. Próbki przechodzą 80/80; pełne pliki `A0`–`A3` przechodzą 8 000/8 000 bez skipów ani rozbieżności. Wektory sprawdzają nadpisania DS/ES/CS/SS, parzyste i nieparzyste adresy, rozdzielony transfer słowa oraz zawinięcie offsetu `FFFFh`; raport: [`A12-moffs-report.json`](../../../docs/plan-8086-stages/A12-moffs-report.json).

`A12Group/87.json` zawiera 20 pierwszych upstream wektorów `XCHG r/m16,r16`; próbka przechodzi 20/20, pełne przypięte źródło przechodzi 2 000/2 000. Pełny zestaw objął formy rejestrowe i pamięciowe, segment-prefiksy oraz słowa pod parzystymi i nieparzystymi adresami; raport: [`A12-xchg-word-report.json`](../../../docs/plan-8086-stages/A12-xchg-word-report.json).

`A12Group/8C.json` i `8E.json` zawierają po 20 pierwszych upstream wektorów transferu rejestrów segmentowych. Próbki przechodzą 40/40, pełne przypięte źródło przechodzi 4 000/4 000, w tym formy rejestrowe, pamięciowe, segment-prefiksy i transfery słowa pod adresami nieparzystymi; raport: [`A12-mov-segment-report.json`](../../../docs/plan-8086-stages/A12-mov-segment-report.json).

`A12Stack/9C.json` i `9D.json` zawierają po 20 pierwszych rekordów `PUSHF` i `POPF`. Próbki przechodzą 40/40; pełne pliki źródłowe obejmują 4 000 przypadków i przechodzą bez skipów ani rozbieżności.

### Porty I/O (A9)

`IN AL,imm8`/`IN AL,DX`, `OUT imm8,AL`/`OUT DX,AL`, `IN AX,imm8`/`IN AX,DX` i `OUT AX,imm8`/`OUT AX,DX` przechodzą wszystkie lokalne próbki A9Io: 160/160. Porównywane są cykle IOR/IOW, stan CPU i kolejka; wejście bez urządzenia daje FF. Dziennik wstrzykniętej magistrali potwierdza porty i dane.

Bezpośrednie nadpisanie segmentu przechodzi 61/61 lokalnych przypadków dla wszystkich 10 opcode string: `MOVSB` 8, `MOVSW` 6, `CMPSB` 6, `CMPSW` 6, `STOSB` 5, `STOSW` 5, `LODSB` 9, `LODSW` 6, `SCASB` 5 i `SCASW` 5. Połączenie nadpisania segmentu z `REP/REPNE` przechodzi 45/45 lokalnych fixture (`A4`–`A7`, `AA`–`AF`), pełny stan CPU, RAM, kolejkę i wszystkie T-states; uwzględniono q=5 i q=6. Pełny upstream: wszystkie stringi 20 000/20 000, segment+REP/string 4 961/4 961.

`A12Call/FF.2.json` i `A12Call/FF.3.json` zawierają po 20 pierwszych rekordów upstream dla near/far indirect CALL. Pełne przypięte źródła przechodzą 2 000/2 000 dla każdego rozszerzenia ModR/M; łącznie korpus 646 000 rekordów po zmianie: 515 519 passed, 130 481 jawnych skipów i 0 mismatchy. Szczegóły i SHA: `docs/plan-8086-stages/A12-indirect-call-report.json`.
