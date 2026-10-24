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
| aktywne próbki w repo na etapie A1 | 0 |

Metadata: 204 `normal`, 20 `alias`, 8 `prefix`, 8 `fpu`, 1 `undocumented` oraz 15 wpisów grupowych bez pola `status`, z podopkodami w `reg`. Nie klasyfikować wpisu bez `status` jako „brak danych”. Dla `B3` i `EA` surowe pliki mają po jednym rekordzie mniej niż deklaracja „2000 per opcode” w README; nie dopisywać ani nie usuwać rekordów.

## Zakres źródła i ograniczenia

README repo opisuje pomiary fizycznego P80C86A-2 w trybie Maximum Mode przez ArduinoX86 i Intel 8288; testy mają pełną początkową kolejkę prefetch, bez wait states. README podaje, że INTR, NMI i TF nie są ćwiczone. `metadata.json` ma wersję 1.0.0, mimo README/changelogu 1.0.1. Przykład README używa pól `hash` i `idx`, lecz sprawdzone rekordy na przypiętym commicie zawierają `test_hash` i `test_num`; parser musi używać rzeczywistego schematu plików. Te dane nie są dowodem dla wszystkich wariantów 8086 ani dla 8088.

Dokumentację Intel i sygnały wymagające jej potwierdzenia odnotowano w [`docs/cpu/sources.md`](../../../docs/cpu/sources.md); pełnej instrukcji producenta nie udało się pobrać (HTTP 403 w użytym źródle). Nie wyprowadzać brakujących szczegółów sprzętu z samych wektorów.

## Próbki i polecenia

Polityka próbki: pierwsze **20 rekordów** z każdego wybranego pliku w kolejności upstream, JSON zwarty. Pełne dane pozostają poza repo; testy xUnit nie pobierają sieci. Na etapie A1 uruchomiono kontrolny eksport pierwszych 20 z 323 plików do `/tmp/vibe-8086-a1-samples` (16 MB). A2 aktywuje `90.json` (20 rekordów). A3 aktywuje też `B0.json`–`B7.json` (po 20 rekordów, rejestrowe `MOV r8, imm8`); pozostałe pliki pozostają nieaktywne.

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
| testy rdzenia / próbki aktywne w repo | NOP 20/20, B0–B7 160/160, MOV ModR/M 40/40, LEA 20/20 i XLAT 20/20; 260 aktywnych rekordów |
| pełne porównanie wykonania CPU dla wspieranych form | B0–B7 16 000/16 000; 88/8A mod=11 519/519; LEA `mod != 11` 996/996; D7 2 000/2 000 |

Próbki `A3ModRm/88.json` i `A3ModRm/8A.json` są filtrowane: zawierają po 20 pierwszych rekordów spełniających `mod=11` z przypiętego pliku źródłowego, a nie pierwsze 20 rekordów pliku. `A3Lea/8D.json` zawiera pierwsze 20 rekordów pamięciowego LEA (`mod != 11`). Pełny filtr LEA daje 996/996, bez pominięć i niezgodności; segment w EA nie zmienia offsetu zapisywanego przez LEA. `D7.json` zawiera pierwszych 20 rekordów XLAT; pełny przypięty plik D7 daje 2000/2000, w tym kodowania z prefiksami segmentowymi.
