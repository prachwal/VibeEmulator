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
| aktywne próbki CPU w repo | 6 638 |

Metadata: 204 `normal`, 20 `alias`, 8 `prefix`, 8 `fpu`, 1 `undocumented` oraz 15 wpisów grupowych bez pola `status`, z podopkodami w `reg`. Nie klasyfikować wpisu bez `status` jako „brak danych”. Dla `B3` i `EA` surowe pliki mają po jednym rekordzie mniej niż deklaracja „2000 per opcode” w README; nie dopisywać ani nie usuwać rekordów.

## Zakres źródła i ograniczenia

README repo opisuje pomiary fizycznego P80C86A-2 w trybie Maximum Mode przez ArduinoX86 i Intel 8288; testy mają pełną początkową kolejkę prefetch, bez wait states. README podaje, że INTR, NMI i TF nie są ćwiczone. `metadata.json` ma wersję 1.0.0, mimo README/changelogu 1.0.1. Przykład README używa pól `hash` i `idx`, lecz sprawdzone rekordy na przypiętym commicie zawierają `test_hash` i `test_num`; parser musi używać rzeczywistego schematu plików. Te dane nie są dowodem dla wszystkich wariantów 8086 ani dla 8088.

Dokumentację Intel i sygnały wymagające jej potwierdzenia odnotowano w [`docs/cpu/sources.md`](../../../docs/cpu/sources.md); pełnej instrukcji producenta nie udało się pobrać (HTTP 403 w użytym źródle). Nie wyprowadzać brakujących szczegółów sprzętu z samych wektorów.

## Próbki i polecenia

Polityka próbki: pierwsze **20 rekordów** z każdego wybranego pliku w kolejności upstream, JSON zwarty. Pełne dane pozostają poza repo; testy xUnit nie pobierają sieci. Na etapie A1 uruchomiono kontrolny eksport pierwszych 20 z 323 plików do `/tmp/vibe-8086-a1-samples` (16 MB). A2 aktywuje `90.json` (20 rekordów). A3 aktywuje też `B0.json`–`B7.json`, rejestrowe `88/8A`, rejestrowe i pamięciowe `86`, pamięciowe `88/8A` z i bez prefiksów segmentowych, pamięciowe `LEA` i `D7`; A5 aktywuje też `B8.json`–`BF.json` (po 20 rekordów `MOV r16,imm16`) oraz `A5ModRm/89.json` i `8B.json` (po 20 rejestrowych form ModR/M); pozostałe formy zostają nieaktywne.

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

W comparatorze pole BHE jest ignorowane wyłącznie dla cykli `PASV`, gdzie nie ma aktywnego transferu; pozostałe pola cyklu oraz BHE dla cykli aktywnych są porównywane. Opcjonalne `flagsMask` w manifeście maskuje wyłącznie różnice flag CPU wskazane przez metadata źródłowe; brak pola oznacza porównanie wszystkich bitów. Runner zapisuje różnice również dla przypadków, których dekoder jeszcze nie obsługuje.

## Raport uruchomienia wektorów

| zakres | wynik |
|---|---|
| inwentarz przypiętego źródła | 323 pliki, 646 000 rekordów policzonych |
| eksport próbki A1 poza repo | 323 pliki po 20 rekordów, eksport zakończony |
| testy rdzenia / próbki aktywne w repo | A3/A4/A5/A6/A7 aktywne próbki: 6 638/6 638 rekordów CPU po rebase; Intel8086.Tests: 7 329/7 329, 0 skipów |
| pełne porównanie wykonania CPU dla wspieranych form | Historyczny wynik sprzed rebase: wcześniejsze podzbiory 142 590 oraz rejestrowe `81/83 /0–/7` 4 034/4 034; suma 146 624. Pełnych filtrów po rebase nie uruchomiono. Pamięciowe `81/83`: poza repo nieuruchomione. |

Próbki `A3ModRm/88.json` i `A3ModRm/8A.json` są filtrowane: zawierają po 20 pierwszych rekordów spełniających `mod=11` z przypiętego pliku źródłowego, a nie pierwsze 20 rekordów pliku. `A3Xchg/86-register.json` i `86-memory.json` zawierają po 20 pierwszych rekordów rejestrowej (`mod=11`) i pamięciowej (`mod!=11`) formy `XCHG`; próbka 40/40, pełny plik 2000/2000. `A3MovMemory/88.json` i `A3MovMemory/8A.json` zawierają po 20 pierwszych nieprefiksowanych pamięciowych kodowań (`mod != 11`); pełne podzbiory 753 + 780 rekordów przechodzą 1533/1533. Pliki `88-prefix.json` i `8A-prefix.json` zawierają po 20 pierwszych segment-prefiksowanych kodowań pamięciowych; pełny filtr obejmuje 734 + 689 rekordów i przechodzi 1423/1423. `A3Lea/8D.json` zawiera pierwsze 20 rekordów pamięciowego LEA (`mod != 11`). Pełny filtr LEA daje 996/996, bez pominięć i niezgodności; segment w EA nie zmienia offsetu zapisywanego przez LEA. `D7.json` zawiera pierwszych 20 rekordów XLAT; pełny przypięty plik D7 daje 2000/2000, w tym kodowania z prefiksami segmentowymi. `A3MovImmediate/C6-*.json` zawierają filtrowane rekordy `/0`: po 20 form pamięciowych i rejestrowych bez prefiksu oraz po 20 pamięciowych i 18 rejestrowych z prefiksem. Aktywna próbka to 78/78; pełny filtr `/0` ze źródła daje 236/236, bez pominięć i niezgodności. Inne wartości pola `reg` w tym pliku nie są formą `C6 /0` zdefiniowaną w zakresie A3.

### Próbki helpera ALU (A4)

`A4Alu/` zawiera po 20 pierwszych rekordów upstream dla `04`, `14`, `1C`, `2C`, `3C`, `24`, `0C`, `34`. Testują wynik i określone flagi czystego helpera ALU; nie są aktywne w runnerze wykonania CPU i nie weryfikują cykli ani pełnego stanu. Dla plików z `flags-mask=0xFFEF` manifest helpera wyłącza tylko bit AF (0x0010), zgodnie z maską upstream.

`A4Cpu/` zawiera po 20 pierwszych przypadków bez prefiksów dla `04`, `0C`, `14`, `1C`, `24`, `2C`, `34` i `3C`. To próbki filtrowane według kodu na początku instrukcji, nie pierwsze rekordy całych plików. Pełne nieprefiksowane podzbiory upstream zawierają 8 029 przypadków; wszystkie przechodzą pełne porównanie CPU, w tym flagi i cykle. Próbki helperowe `A4Alu/` pozostają osobnym źródłem testów.

`A4ModRm/` zawiera po 20 pierwszych rekordów upstream dla 17 kodowań byte ALU/TEST, filtrowanych warunkiem `bytes[0] == opcode` oraz `ModR/M mod=11`. Nie są to pierwsze rekordy całych plików. Pełny przefiltrowany podzbiór to 4 355 rekordów, wszystkie zgodne z runnerem CPU; formy pamięciowe pozostają nieaktywne.

`A5ModRm/` zawiera po 20 pierwszych rekordów upstream dla `89` i `8B`, filtrowanych przez opcode bez prefiksu i `ModR/M mod=11`. Pełny przefiltrowany podzbiór to 490 rekordów (234 + 256), zgodnych w porównaniu rejestrów, kolejki i T-states. Formy pamięciowe pozostają otwarte.

### Próbki helpera ALU słownego (A6)

`A6Alu/` zawiera po 20 rekordów dla `05`, `15`, `1D`, `2D`, `3D`, `25`, `0D`, `35`. Testy porównują wynik helpera i określone flagi; nie uruchamiają CPU ani cykli. Maski niezdefiniowanego AF są wyłącznie te z metadanych (`0xFFEF`).

### Próbki helpera i CPU ALU słownego (A6)

`A6Alu/` zawiera 20 rekordów dla każdego z 8 kodowań ALU słownego, a `A6Cpu/` odpowiadające próbki CPU. Helper porównuje wynik i maskę flag; runner CPU porównuje rejestry, kolejkę i cykle. Pełny podzbiór AX-immediate: 7 986/7 986.

`A6ModRm/` zawiera po 20 przypadków dla 17 kodowań word ALU/TEST, filtrowanych exact opcode i `ModR/M mod=11`. Próbka przechodzi 340/340, pełny filtr 4 180/4 180; formy pamięciowe nie są objęte tym wynikiem.

### Próbki INC/DEC słownych rejestrów (A6)

`A6IncDec/` zawiera po 20 rekordów dla kodowań `40`–`4F`. Runner CPU porównuje rejestry, FLAGS, kolejkę i dwa T-states; pełne pliki przechodzą 32 000/32 000, próbki 320/320.

### Próbki grupy FF /0,/1 (A6)

`A6Group/` zawiera po 20 przypadków `FF /0` i `FF /1` dla `mod=11`. Próbki porównują stan, kolejkę i trzy T-states (40/40); pełny filtr daje 506/506. Pozostałe grupy i formy pamięciowe `FF` są poza zakresem tego podetapu.

`A6Group/FE.0.json` i `FE.1.json` zawierają po 20 rekordów `mod=11` dla byte `INC/DEC`. Próbki przechodzą 40/40; pełny filtrowany runner przechodzi 561/561 (258 dla `/0`, 303 dla `/1`), bez skipów i mismatchy.

`A6Group/` zawiera również `F6/F7 /2,/3` dla `mod=11`: próbki 80/80 i pełny filtrowany zestaw 987/987. Pozostałe rozszerzenia oraz formy pamięciowe pozostają poza zakresem.

`A6Group/FE.0.mem.json` i `FE.1.mem.json` zawierają po 20 rekordów pamięciowych z przypiętego źródła. Pełny filtr obejmuje 737 przypadków `/0` i 717 `/1`, bez prefiksów; runner zakończył się 1 454/1 454 bez skipów i mismatchy w historycznym raporcie podetapu.

`A6Shift/` zawiera po 20 przypadków dla `D0/D1/D2/D3 /0–/5,/7 mod=11`; próbki przechodzą 560/560, a pełne filtrowane podzbiory 7 027/7 027. Dla `D0/D1` wpisy `/4`, `/5`, `/7` przekazują `flagsMask=65519` (ignorowany AF); dla `D2/D3` `/0–/3` przekazują `63487` (ignorowany OF), a `/4,/5,/7` `63471` (ignorowane OF i AF), zgodnie z `metadata.json` upstream. Audyt `D2/D3` wykazał CL 0–62 oraz długość `8` (CL=0) lub `8+4×CL`. Formy pamięciowe nie należą do tego wyniku.
Fixture `A6Group/FE.0.mem.json` i `FE.1.mem.json` zawierają po 20 rekordów pamięciowych z przypiętego źródła. Pełny filtr obejmuje 737 przypadków `/0` i 717 `/1`, bez prefiksów; runner zakończył się 1 454/1 454 bez skipów i mismatchy.

`A6Group/FF.0.mem.json` i `FF.1.mem.json` zawierają po 20 nieprefiksowanych przypadków pamięciowych. Pełny filtr ma 768 rekordów `/0` i 743 `/1` (łącznie 1 511/1 511); słowa wyrównane i pod nieparzystym adresem przechodzą pełne porównanie CPU. Prefiksy segmentowe FF nie są objęte wynikiem.

`A6Group/F6.2.mem.json`, `F6.3.mem.json`, `F7.2.mem.json` i `F7.3.mem.json` zawierają po 20 pierwszych nieprefiksowanych rekordów pamięciowych z przypiętych plików źródłowych. Próbki przechodzą 80/80; pełny filtr łącznie obejmuje 2 996 rekordów, wszystkie zgodne. Prefiksy segmentowe pozostają do etapu A10.

`A6Group/FE.{0,1}.prefixed.{reg,mem}.json` zawierają po 20 przypadków z prefiksem segment override na każdą kombinację rozszerzenia i rodzaju operandu. Próbki przechodzą 80/80. Pełny filtr oryginalnych plików `FE.0.json` i `FE.1.json`, zawierających razem 4 000 przypadków prefiksowanych i nieprefiksowanych, przechodzi 4 000/4 000.

`A6Group/FE.{0,1}.prefixed.{reg,mem}.json` zawierają po 20 przypadków z prefiksem segment override na każdą kombinację rozszerzenia i rodzaju operandu. Próbki prefiksowane liczą 80 rekordów; pełny filtr wszystkich prefiksowanych i nieprefiksowanych form FE obejmuje 4 000 przypadków.

`A6Group/FF.0.mem.json` i `FF.1.mem.json` zawierają po 20 nieprefiksowanych przypadków pamięciowych. Pełny filtr ma 768 rekordów `/0` i 743 `/1` (łącznie 1 511/1 511); słowa wyrównane i pod nieparzystym adresem przechodzą pełne porównanie CPU w historycznym raporcie etapu. Prefiksy segmentowe FF nie są objęte wynikiem.

`A6Group/F6.2.mem.json`, `F6.3.mem.json`, `F7.2.mem.json` i `F7.3.mem.json` zawierają po 20 pierwszych nieprefiksowanych rekordów pamięciowych. Próbki przechodzą 80/80; pełny filtr obejmuje 2 996 rekordów (748, 766, 743 i 739), wszystkie zgodne w historycznym raporcie. Prefiksy segmentowe pozostają do etapu A10.

`A6Group/D0-D3.*.mem.json` zawierają po 20 przypadków dla 28 kodowań ModR/M. Historyczny raport podetapu: próbki 560/560, pełny filtr 21 067/21 067; obejmuje count=0, flag masks z upstream metadata i transfery word pod nieparzystym adresem. Prefiksy segmentowe nie należą do tego wyniku.

`A6WordAlu/` zawiera po 20 nieprefiksowanych rekordów pamięciowych dla 17 word-ALU opcode. Próbki 340/340, pełny filtr 12 937/12 937 w historycznym raporcie; porównanie obejmuje stan, pamięć, kolejkę i T-states. `CMP/TEST` oraz kierunek do rejestru są read-only na magistrali; pozostałe operacje wykonują zapis wyniku.

`A6WordImmediate/` zawiera po 20 rejestrowych rekordów `81/83 /0–/7` (320) oraz po 20 pamięciowych dla `mod=00/01/10` (960). Wszystkie aktywne próbki po rebase przechodzą 5 798/5 798, 0 skipów; testy Intel8086: 6 170/6 170. `83` rejestrowe ma cztery T-states z nakładaniem prefetch; pamięciowy `CMP /7` nie zapisuje RAM, pozostałe rozszerzenia wykonują RMW, także dla nieparzystego adresu. Historyczny pełny filtr rejestrowy przed rebase: 4 034/4 034; pełny filtr pamięciowy i ponowienie pełnych filtrów po rebase nieuruchomione.

### Predykaty warunków skoku (A7)

`A7Branch/` zawiera po 20 rekordów dla opkodów `70`–`7F` (320). Testy CPU przechodzą 320/320 i porównują IP/CS, kolejkę, RAM i każdy T-state dla taken oraz not-taken. Test helpera obejmuje 319/319 jednoznacznych wektorów; displacement zerowy pominięto tylko w oracle kierunku. `A7Jump/` zawiera po 20 nieprefiksowanych rekordów dla `EB`, `E9`, `EA`; CPU przechodzi 60/60. Pełnych plików źródłowych nie uruchomiono.

`A7CallRet/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `E8`, `9A`, `C3`, `C2`, `CB` i `CA` (120). `E8`, `C3` i `C2` są aktywne i przechodzą 60/60; `9A`, `CB` i `CA` również przechodzą 60/60. Aktywny wynik całkowity: 6 398/6 398.

`A7Loop/` zawiera po 20 wektorów dla `E0`–`E3` (80); wszystkie przechodzą 80/80 w runnerze cyklowym.

`A7Interrupts/` zawiera próbki po 20 rekordów dla `CD`, `CC`, `CE` i `CF`. `CD`, `CC`, `CE` oraz `CF` są aktywne: 80/80 łącznie. Testy porównują stos, FLAGS/CS:IP, IVT, kolejkę i wszystkie T-states.

`A7Flags/` zawiera po 20 nieprefiksowanych rekordów dla dziewięciu form (`F5`, `F8`–`FD`, `9E`, `9F`); 180/180 przechodzi z pełnym porównaniem flag, kolejki i T-states.

`A8Arithmetic/` zawiera helperowe próbki `CBW`, `CWD`, `AAM`, `AAD`, `DAA`, `DAS`, `AAA`, `AAS` (po 20, 160 łącznie). Helpery nie dowodzą zachowania cykli; CBW/CWD są dodatkowo wykonywane przez CPU.

W A8 `98` i `99` mają aktywne próbki CPU po 20 rekordów; oba przechodzą 20/20. `A8Arithmetic/` helpery nie są CPU testem.
