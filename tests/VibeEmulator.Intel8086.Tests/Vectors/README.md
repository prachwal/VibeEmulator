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
| aktywne próbki CPU w repo | 2 338 |

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

W obecnym comparatorze A2 pole BHE jest ignorowane wyłącznie dla cykli `PASV`, gdzie nie ma aktywnego transferu; pozostałe pola cyklu oraz BHE dla cykli aktywnych są porównywane. Runner zapisuje różnice również dla przypadków, których dekoder jeszcze nie obsługuje.

## Raport uruchomienia wektorów

| zakres | wynik |
|---|---|
| inwentarz przypiętego źródła | 323 pliki, 646 000 rekordów policzonych |
| eksport próbki A1 poza repo | 323 pliki po 20 rekordów, eksport zakończony |
| testy rdzenia / próbki aktywne w repo | A3 458/458 i AL-immediate 160/160, ALU ModR/M register-direct 340/340; 958 aktywnych rekordów w 48 plikach |
| pełne porównanie wykonania CPU dla wspieranych form | A3 pełne podzbiory opisane powyżej; AL-immediate 8 029/8 029; ALU ModR/M register-direct 4 355/4 355; MOV word 89/8B mod=11: 490/490; word ALU AX-immediate: 7 986/7 986; word ALU ModR/M: 4 180/4 180 |

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
