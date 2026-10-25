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
| aktywne próbki CPU w repo | manifest główny 930 wpisów / 18 090 rekordów; manifesty katalogowe 1 064 wpisy / 17 099 wskazań (część indeksuje te same fixture'y); łącznie walidator potwierdza 1 994 wpisy / 35 189 wskazań |

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
| aktywne próbki CPU w repo | manifest główny 930 wpisów / 18 090 wskazań; pozostałe manifesty katalogowe 1 064 wpisy / 17 099 wskazań (część indeksuje te same fixture'y); razem 1 994 wpisy / 35 189 wskazań |
| rejestrowe `PUSH`/`POP` A5 | 16 plików × 20 próbek: 320/320; pełny filtr upstream: 32 000/32 000, 0 skipów i 0 mismatchy |
| rejestrowe i segmentowe `PUSH`/`POP` A5 | lokalne próbki 460/460; pełny filtr upstream 46 000/46 000, 0 skipów, mismatchy, timeoutów i błędów parsowania |
| przygotowane próbki A9 | 18 plików × 20 pierwszych rekordów (string `A4`–`A7`, `AA`–`AF`; I/O `E4`–`E7`, `EC`–`EF`) oraz 20 filtrowanych `MOVSB` bez prefiksu, DF=0; SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` |
| `MOV AL/AX ↔ moffs16` A12 | lokalne próbki 80/80; pełne przypięte pliki `A0`–`A3`: 8 000/8 000, 0 skipów, 0 mismatchy, 0 timeoutów i błędów parsowania; raport [`A12-moffs-report.json`](../../../docs/plan-8086-stages/A12-moffs-report.json) |
| pamięciowe `MOV 89/8B` A12 | pełne pliki źródłowe 4 000/4 000; lokalne aktywne próbki prefiksowane 160/160; pełny korpus +3 025 passed / −3 025 skipów do 558 450/646 000 (86,4%); raport [`A12-mov-memory-89-8b-report.json`](../../../docs/plan-8086-stages/A12-mov-memory-89-8b-report.json) |
| `LES/LDS` (`C4/C5`) A12 | lokalne pierwsze próbki 40/40; pełne pliki źródłowe 4 000/4 000; pełny korpus po `8F`: 562 688/646 000 passed (87,1%); raport [`A12-les-lds-c4-c5-report.json`](../../../docs/plan-8086-stages/A12-les-lds-c4-c5-report.json) |
| `POP r/m16` (`8F /0`) A12 | lokalne fixture’y 202/202; pełny filtr `/0` 238/238; cały plik: 238 passed, 1 762 jawne skipped (`/1–/7`), 0 mismatchy; pełny korpus po tym pakiecie 562 688/646 000 (87,1%); raport [`A12-pop-rm16-8f-report.json`](../../../docs/plan-8086-stages/A12-pop-rm16-8f-report.json) |
| `MOV r/m16,imm16` (`C7 /0`) A12 | lokalne fixture’y 226/226; pełny filtr `/0` 270/270; cały plik: 270 passed, 1 730 jawnych skipped (`/1–/7`), 0 mismatchy; pełny korpus 562 958/646 000 (87,1%); raport [`A12-mov-rm16-immediate-c7-report.json`](../../../docs/plan-8086-stages/A12-mov-rm16-immediate-c7-report.json) |
| segment-prefiksowane `FF /0,/1` A12 | lokalne fixture’y 640/640; pełne `FF.0` i `FF.1` 4 000/4 000, w tym 1 983 prefiksowane; potwierdzono istniejącą obsługę bez zmiany CPU; korpus pozostaje 562 958/646 000 (87,1%); raport [`A12-prefixed-inc-dec-ff01-report.json`](../../../docs/plan-8086-stages/A12-prefixed-inc-dec-ff01-report.json) |
| `SALC` (`D6`) A12 | pięć grup lokalnych fixture’ów 100/100; pełne źródło 2 000/2 000, w tym 981 bez prefiksu i 1 019 segment-prefiksowanych; pełny korpus po tym pakiecie 564 958/646 000 (87,5%), 81 042 skipów, bez mismatchy/timeoutów/parse errors; raport [`A12-salc-d6-report.json`](../../../docs/plan-8086-stages/A12-salc-d6-report.json) |
| segment-prefiksowane ESC (`D8–DF`) A12 | fixture’y 2 560/2 560; pełny prefiksowany filtr 8 012/8 012; kontrola nieprefiksowana 7 988/7 988; korpus 572 970/646 000 (88,7%), 73 030 skipów i 0 mismatchy/timeoutów/parse errors; raport [`A12-prefiksy-segmentowe-esc-d8-df-report.json`](../../../docs/plan-8086-stages/A12-prefiksy-segmentowe-esc-d8-df-report.json) |
| nieudokumentowane `SETMO/SETMOC` (`D0–D3 /6`) A12 | fixture’y 1 600/1 600 w 80 grupach; pełne pliki 8 000/8 000, zero skipów/mismatchy; korpus po pakiecie 580 970/646 000 (89,9%); raport [`A12-setmo-d0-d3-6-report.json`](../../../docs/plan-8086-stages/A12-setmo-d0-d3-6-report.json) |
| nieudokumentowane rozszerzenia `C6 /1–/7` A12 | fixture’y 1 463/1 463 w 140 grupach; pełny plik C6 2 000/2 000 (w tym 1 764 aliasy `/1–/7`); korpus po tym pakiecie 582 734/646 000 (90,2%); raport [`A12-c6-ignored-extension-report.json`](../../../docs/plan-8086-stages/A12-c6-ignored-extension-report.json) |
| nieudokumentowane rozszerzenia `C7 /1–/7` A12 | fixture’y 1 426/1 426 w 140 grupach; pełny C7 2 000/2 000 (w tym 1 730 aliasów); korpus w tamtym checkpointcie 584 464/646 000 (90,5%); raport [`A12-c7-ignored-extension-report.json`](../../../docs/plan-8086-stages/A12-c7-ignored-extension-report.json) |
| nieudokumentowane rozszerzenia `8F /1–/7` A12 | fixture’y 1 461/1 461 w 140 grupach; pełny plik 2 000/2 000, w tym 1 762 aliasy; korpus 586 226/646 000 (90,7%), 59 774 skipy segment-prefiksowe, 0 mismatchy/timeoutów/parse errors. Metadata upstream nadal oznacza `/1–/7` jako undefined; alias jest obserwacją P80C86A-2, nie deklaracją ISA. Raport [`A12-8f-ignored-extension-report.json`](../../../docs/plan-8086-stages/A12-8f-ignored-extension-report.json) |
| `TEST r/m,imm` (`F6/F7 /0,/1`) A12 | lokalne próbki 80/80; pełne przypięte pliki: 8 000/8 000, 0 skipów, mismatchy, timeoutów i błędów parsowania; raport [`A12-test-rm-immediate-f6f7-report.json`](../../../docs/plan-8086-stages/A12-test-rm-immediate-f6f7-report.json) |
| pełne porównanie wykonania CPU dla wspieranych form | NOP/B0–BF 34 000/34 000; 88/8A mod=11 519/519; AL-immediate 8 029/8 029; ALU ModR/M register-direct 4 355/4 355; MOV word ModR/M 89/8B mod=11 490/490; word ALU AX-immediate 7 986/7 986; word ALU ModR/M register-direct 4 180/4 180; word ALU pamięciowe ModR/M 12 937/12 937; `INC/DEC r16` 32 000/32 000; `FF /0,/1 mod=11` 506/506; `FE /0,/1 mod=11` 561/561; `F6/F7 /2,/3 mod=11` 987/987; `D0/D1 /0–/5,/7 mod=11` 3 549/3 549; `D2/D3 /0–/5,/7 mod=11` 3 478/3 478; `FE /0,/1` z i bez prefiksów 4 000/4 000; nieprefiksowane pamięciowe `FF /0,/1` 1 511/1 511; nieprefiksowane pamięciowe `F6/F7 /2,/3` 2 996/2 996; nieprefiksowane pamięciowe `D0–D3 /0–/5,/7` 21 067/21 067; nieprefiksowane rejestrowe `81/83 /0–/7` 4 034/4 034; razem 146 624 |
| wszystkie obserwowane prefiksowane rekordy | Pełny filtr 580 par sekwencja/opcode: 197 748 rekordów, 48 306 zgodnych, 149 442 rozbieżne, 0 skipów; 178 grup opcode w całości zgodne. Szczegóły per para i przykłady mismatchy w `docs/plan-8086-stages/A10-prefix-coverage-report.json`; grupy obejmują też niezaimplementowane rozszerzenia ModR/M. Sekwencje wieloprefiksowe nieobecne w pinie opisano jako known-gap w `docs/plan-8086-stages/A10-prefix-sequence-decisions.json`. |
| segment-prefiksowane `LOOP` (`E0–E3`) A12 | 16 grup fixture’ów 320/320; wszystkie cztery pełne pliki 8 000/8 000, w tym 3 943/3 943 form segment-prefiksowanych; historyczny wynik korpusu po pakiecie 590 169/646 000 (91,4%); raport [`A12-prefixed-loop-e0-e3-report.json`](../../../docs/plan-8086-stages/A12-prefixed-loop-e0-e3-report.json) |
| segment-prefiksowane `LEA 8D` A12 | 4 grupy fixture’ów 80/80; filtr prefiksowany 1 004/1 004; pełny plik 8D 2 000/2 000; korpus po pakiecie 595 207/646 000 (92,1%), 50 793 skipów, 0 mismatchy/timeoutów/parse errors; raport [`A12-prefixed-lea-8d-report.json`](../../../docs/plan-8086-stages/A12-prefixed-lea-8d-report.json) |
| segment-prefiksowane krótkie `JMP rel8` (`EB`) A12 | 4 grupy fixture’ów 80/80; filtr prefiksowany 1 018/1 018; pełny plik EB 2 000/2 000; korpus po pakiecie 594 203/646 000 (92,0%), 51 797 skipów, 0 mismatchy/timeoutów/parse errors; raport [`A12-prefixed-short-jump-eb-report.json`](../../../docs/plan-8086-stages/A12-prefixed-short-jump-eb-report.json) |
| segment-prefiksowane bezpośrednie `CALL/JMP` (`E8–EA`) A12 | 12 grup fixture’ów 240/240; filtr prefiksowany 3 016/3 016; wszystkie pliki E8–EA 6 000/6 000; pełny korpus po pakiecie 593 185/646 000 (91,8%), 52 815 skipów, 0 mismatchy/timeoutów/parse errors; raport [`A12-prefixed-direct-call-jump-e8-ea-report.json`](../../../docs/plan-8086-stages/A12-prefixed-direct-call-jump-e8-ea-report.json) |
| pełne źródłowe drzewo (A12) | Po segment-prefiksowanym `LEA 8D`: 323 pliki / 646 000 rekordów, **595 207 passed, 50 793 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania** (92,1%). Raport: [`A12-prefixed-lea-8d-report.json`](../../../docs/plan-8086-stages/A12-prefixed-lea-8d-report.json). Pełna zgodność ISA nie jest potwierdzona. |

Próbki `A3ModRm/88.json` i `A3ModRm/8A.json` są filtrowane: zawierają po 20 pierwszych rekordów spełniających `mod=11` z przypiętego pliku źródłowego, a nie pierwsze 20 rekordów pliku. Pełny filtr daje 243 przypadki `88` i 276 przypadków `8A`, wszystkie zgodne end-to-end. Pamięciowe formy `88/8A` nie są objęte tym filtrem.

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


`A6Group/` zawiera próbki rejestrowe dla `FF /0,/1,/6,/7`, `FE /0,/1`, `F6/F7 /2,/3` oraz pamięciowe FE/FF/F6/F7 i `D0–D3 /0–/5,/7`. Porównanie obejmuje stan, RAM, flagi, kolejkę i T-states: FF register-direct `/0,/1` 40/40 i pełny filtr 506/506; nieprefiksowane FF pamięciowe `/0,/1` 40/40 i 1 511/1 511; `FF /6` 40/40, pełny nieprefiksowany filtr 1 014/1 014 (252 rejestrowe, 762 pamięciowe); `FF /7` 40/40, pełny nieprefiksowany filtr 996/996 (238 rejestrowych, 758 pamięciowych); F6/F7 register-direct 80/80 i 987/987; nieprefiksowane F6/F7 pamięciowe 80/80 i 2 996/2 996; FE register-direct 40/40 i 561/561; wszystkie FE 4 000/4 000; pamięciowe D0–D3 560/560 i 21 067/21 067. Segment-prefiksowane `FF /6,/7` mają po 20 rejestrowych i pamięciowych próbek dla ES/CS/SS/DS oraz pełny filtr 1 990/1 990. Prefiksowane D0–D3 `/0–/5,/7` przechodzą 27 906/27 906; lokalne fixture’y `A12ShiftPrefix/` przechodzą 4 480/4 480, a pełny filtr `D0–D3` 56 000/56 000 legalnych form (8 000 `/6` jawnie pominiętych). Szczegóły w [`A12-prefiksy-segmentowe-D0-D3-report.json`](../../../docs/plan-8086-stages/A12-prefiksy-segmentowe-D0-D3-report.json).

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

`A12PrefixedEsc/` zawiera po 20 pierwszych wektorów dla każdej kombinacji ESC `D8`–`DF`, prefiksu ES/CS/SS/DS i trybu ModR/M (128 grup, 2 560 przypadków). Wszystkie fixture’y przechodzą 2 560/2 560; pełny filtr prefiksowany przechodzi 8 012/8 012. Obsługa obejmuje segment override dla form pamięciowych oraz zużycie prefiksu w formach `mod=11`; pełny korpus po zmianie przechodzi 572 970/646 000 (88,7%).

`A12Setmo/` zawiera 20 pierwszych przypadków w każdej z 80 grup opcode (`D0–D3`), prefiksu (brak/ES/CS/SS/DS) i trybu ModR/M; próbki przechodzą 1 600/1 600. Pełne pliki `D0.6`–`D3.6` przechodzą 8 000/8 000 i weryfikują nieudokumentowane `SETMO/SETMOC`, w tym formy rejestrowe/pamięciowe i `CL=0`. Pełny korpus po zmianie przechodzi 580 970/646 000 (89,9%); raport [`A12-setmo-d0-d3-6-report.json`](../../../docs/plan-8086-stages/A12-setmo-d0-d3-6-report.json).

`A6Group/F6.2.mem.json`, `F6.3.mem.json`, `F7.2.mem.json` i `F7.3.mem.json` zawierają po 20 pierwszych nieprefiksowanych rekordów pamięciowych z przypiętych plików źródłowych. Próbki przechodzą 80/80; pełny filtr łącznie obejmuje 2 996 rekordów, wszystkie zgodne. Prefiksy segmentowe pozostają do etapu A10.

`A6Group/FE.{0,1}.prefixed.{reg,mem}.json` zawierają po 20 przypadków z prefiksem segment override na każdą kombinację rozszerzenia i rodzaju operandu. Próbki przechodzą 80/80. Pełny filtr oryginalnych plików `FE.0.json` i `FE.1.json`, zawierających razem 4 000 przypadków prefiksowanych i nieprefiksowanych, przechodzi 4 000/4 000; część segment-prefiksowana przechodzi 1 985/1 985.

### Predykaty warunków skoku (A7)

`A7Branch/` zawiera po 20 rekordów dla opkodów `70`–`7F` (320). Testy CPU przechodzą 320/320 i porównują IP/CS, kolejkę, RAM oraz każdy T-state dla taken i not-taken. Test helpera predykatów obejmuje 319/319 jednoznacznych wektorów; displacement zerowy pominięto tylko w oracle kierunku. Pełnych plików źródłowych nie uruchomiono.

`A7Jump/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `EB`, `E9` i `EA` (60). Testy CPU przechodzą 60/60, porównując CS:IP, kolejkę, RAM i każdy T-state. Prefiksowane JMP pozostają nieaktywne do A10.

`A7CallRet/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `E8`, `9A`, `C3`, `C2`, `CB` i `CA` (120). Wszystkie są aktywne i przechodzą 120/120; źródło to przypięty SHA. Prefiksowane formy pozostają do A10.

`A7Loop/` zawiera po 20 pierwszych nieprefiksowanych rekordów dla `E0`–`E3` (80). Wszystkie są aktywne i przechodzą 80/80; asercje obejmują CX, FLAGS, kolejkę i każdy T-state. Próbki pochodzą z przypiętego SHA.

`A12PrefixedLoop/` zawiera po 20 rekordów dla każdej kombinacji prefiksu segmentowego `26/2E/36/3E` i opcodu `E0`–`E3` (16 grup, 320 rekordów). Lokalne fixture’y przechodzą 320/320; pełne przypięte pliki E0–E3 przechodzą 8 000/8 000, w tym wszystkie 3 943 rekordy segment-prefiksowane. Pełny korpus po pakiecie: 590 169/646 000 (91,4%), 55 831 skipped i 0 mismatchy/timeoutów/parse errors. Raport: [`A12-prefixed-loop-e0-e3-report.json`](../../../docs/plan-8086-stages/A12-prefixed-loop-e0-e3-report.json).

`A12PrefixedDirectTransfer/` zawiera po 20 rekordów dla 12 kombinacji prefiksu segmentowego i opcodu `E8`–`EA` (240 rekordów). Fixture’y przechodzą 240/240; pełny filtr segment-prefiksowany 3 016/3 016, a pliki E8–EA 6 000/6 000. Pełny korpus po pakiecie: 593 185/646 000 (91,8%), 52 815 skipped i 0 mismatchy/timeoutów/parse errors. Raport: [`A12-prefixed-direct-call-jump-e8-ea-report.json`](../../../docs/plan-8086-stages/A12-prefixed-direct-call-jump-e8-ea-report.json).

`A12PrefixedShortJump/` zawiera po 20 rekordów dla czterech segment-prefixów przed `EB` (80 rekordów). Fixture’y przechodzą 80/80, pełny filtr 1 018/1 018, plik EB 2 000/2 000. Pełny korpus po pakiecie: 594 203/646 000 (92,0%), 51 797 skipped i 0 mismatchy/timeoutów/parse errors. Raport: [`A12-prefixed-short-jump-eb-report.json`](../../../docs/plan-8086-stages/A12-prefixed-short-jump-eb-report.json).

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

`A12MovMemory/89.mem.{26,2E,36,3E}.json` i analogiczne fixture’y `8B` zawierają po 20 pierwszych upstream pamięciowych rekordów z każdym segment-prefiksem. Nowe lokalne próbki przechodzą 160/160; pełne źródła `89.json.gz` i `8B.json.gz` przechodzą 4 000/4 000, obejmując domyślne DS/SS, nadpisania segmentu, displacement, adresy nieparzyste oraz odczyt/zapis słowa. Raport: [`A12-mov-memory-89-8b-report.json`](../../../docs/plan-8086-stages/A12-mov-memory-89-8b-report.json).

`A12Stack/9C.json` i `9D.json` zawierają po 20 pierwszych rekordów `PUSHF` i `POPF`. Próbki przechodzą 40/40; pełne pliki źródłowe obejmują 4 000 przypadków i przechodzą bez skipów ani rozbieżności.

### Porty I/O (A9)

`IN AL,imm8`/`IN AL,DX`, `OUT imm8,AL`/`OUT DX,AL`, `IN AX,imm8`/`IN AX,DX` i `OUT AX,imm8`/`OUT AX,DX` przechodzą wszystkie lokalne próbki A9Io: 160/160. Porównywane są cykle IOR/IOW, stan CPU i kolejka; wejście bez urządzenia daje FF. Dziennik wstrzykniętej magistrali potwierdza porty i dane.

Bezpośrednie nadpisanie segmentu przechodzi 61/61 lokalnych przypadków dla wszystkich 10 opcode string: `MOVSB` 8, `MOVSW` 6, `CMPSB` 6, `CMPSW` 6, `STOSB` 5, `STOSW` 5, `LODSB` 9, `LODSW` 6, `SCASB` 5 i `SCASW` 5. Połączenie nadpisania segmentu z `REP/REPNE` przechodzi 45/45 lokalnych fixture (`A4`–`A7`, `AA`–`AF`), pełny stan CPU, RAM, kolejkę i wszystkie T-states; uwzględniono q=5 i q=6. Pełny upstream: wszystkie stringi 20 000/20 000, segment+REP/string 4 961/4 961.

`A12Call/FF.2.json` i `A12Call/FF.3.json` zawierają po 20 pierwszych rekordów upstream dla near/far indirect CALL. Pełne przypięte źródła przechodzą 2 000/2 000 dla każdego rozszerzenia ModR/M; łącznie korpus 646 000 rekordów po zmianie: 515 519 passed, 130 481 jawnych skipów i 0 mismatchy. Szczegóły i SHA: `docs/plan-8086-stages/A12-indirect-call-report.json`.

`A12Jump/FF.4.json` i `A12Jump/FF.5.json` zawierają po 20 pierwszych rekordów upstream dla near/far indirect JMP. W pełnym źródle oba pliki mają po 2 000 rekordów; postęp weryfikacji zapisano w planie A12.

`A12Jump/FF.4.json` i `A12Jump/FF.5.json` zawierają po 20 pierwszych rekordów upstream dla near/far indirect JMP. Pełne przypięte źródła przechodzą 2 000/2 000 dla każdego rozszerzenia; szczegóły i wyniki korpusu opisuje `docs/plan-8086-stages/A12-indirect-jump-ff45-report.json`.

`A12C6Ignored/` zawiera próbki do 20 pierwszych rekordów w każdej z 140 niepustych grup rozszerzenie `/1–/7`, prefiks segmentowy i `ModR/M.mod` (1 463 przypadki). Fixture’y przechodzą 1 463/1 463; pełny plik C6 przechodzi 2 000/2 000. Pinned README oznacza `/1–/7` jako formally undefined, lecz opisuje ignorowanie pola `reg` przez testowany układ.

`A12C7Ignored/` zawiera próbki do 20 rekordów dla każdej z 140 niepustych grup rozszerzenie `/1–/7`, prefiks segmentowy i `ModR/M.mod` (1 426 przypadków). Fixture’y przechodzą 1 426/1 426; pełny plik C7 przechodzi 2 000/2 000. Profile T-state dopisano na podstawie pełnych pinned śladów; 67 nowych kluczy nie koliduje z wcześniejszymi 157.

`A12PrefixedLea/` zawiera po 20 źródłowych rekordów dla każdego z czterech segment-prefixów przed opcode 8D (80). Fixture’y przechodzą 80/80; pełny filtr prefiksowany 1 004/1 004, cały plik 8D 2 000/2 000. Raport pakietu: [`A12-prefixed-lea-8d-report.json`](../../../docs/plan-8086-stages/A12-prefixed-lea-8d-report.json).


`A12PrefixedAlu/` zawiera po jednym aktywnym wektorze dla każdego z 50 opcode’ów ALU `00–3D, 84/85` i każdego prefiksu segmentowego ES/CS/SS/DS (200/200). Pełny filtr segment-prefiksowany przechodzi 49 782/49 782; wszystkie 50 pełnych plików źródłowych przechodzi 100 000/100 000. Pełny korpus: 644 989/646 000 (99,8%), 1 011 jawnych skipów prefiksowanego `CALL FAR 9A`, zero rozbieżności, timeoutów i błędów parsowania. Raport: [`A12-prefixed-alu-00-3d-84-85-report.json`](../../../docs/plan-8086-stages/A12-prefixed-alu-00-3d-84-85-report.json).


`A12PrefixedCallFar/` zawiera 80 aktywnych fixture’ów (`9A` × ES/CS/SS/DS, po 20). Pełny filtr prefiksowany przechodzi 1 011/1 011, cały plik 9A 2 000/2 000. Najnowszy pełny przypięty korpus: **646 000/646 000 passed, zero skipów, rozbieżności, timeoutów i błędów parsowania (100%)**. Raport: [`A12-prefixed-call-far-9a-report.json`](../../../docs/plan-8086-stages/A12-prefixed-call-far-9a-report.json). Wynik 100% odnosi się wyłącznie do pinowanego korpusu SingleStepTests; niezależny test ROM/programu i audyt jawnych ograniczeń pozostają w checklistach A12.
