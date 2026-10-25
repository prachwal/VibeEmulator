# A12 — pamięciowe `MOV r/m16,r16` i `MOV r16,r/m16` (`89/8B`)

## Cel i zakres

Włączyć pełne pamięciowe formy opcode’ów `89` i `8B` w rdzeniu Intel 8086, bez zmiany już działających form rejestrowych. Zakres obejmuje ModR/M `mod=00/01/10`, displacement 8/16-bit i direct `[disp16]`, wszystkie kombinacje adresowania, domyślne DS/SS, prefiksy ES/CS/SS/DS, adresy parzyste i nieparzyste, rozdzielone transfery słowa, kolejkę prefetch i każdy T-state magistrali.

Źródło to przypięte SingleStepTests/8086, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. Nie wyprowadzać profili timingowych z tabeli instrukcji: stan, bajty RAM, FLAGS, kolejka i piny każdego T-state muszą zgadzać się ze źródłowymi wektorami.

## Inwentaryzacja bazowa

- `89.json.gz`: 2 000 rekordów; `8B.json.gz`: 2 000; razem 4 000.
- Bazowy pełny filtr przed zmianą: **975 passed, 3 025 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**. Skipy to pamięciowe formy: nieprefiksowane `89` — 764, nieprefiksowane `8B` — 754, pamięciowe `89/8B` z segment-prefiksem — 1 507. Istniejące 975 rejestrowych form przechodzą już teraz.
- Rozkład rekordów nieprefiksowanych według `mod`: `89` = 247/247/270/234, `8B` = 237/246/271/256 dla `mod=00/01/10/11`.
- Wszystkie cztery prefiksy segmentowe występują dla obu opcode’ów i wszystkich czterech wartości `mod`; pełny filtr pozostaje rozstrzygającym kryterium dla szczegółowych podziałów `r/m`, kolejki i adresu fizycznego.
- W repo istnieje już po 20 bazowych rekordów `A5ModRm/89.json` i `A5ModRm/8B.json`, a także po 80 rejestrowych rekordów prefiksowanych dla `89/8B`. Dodać lokalne próbki pamięciowe dla prefiksów na podstawie przypiętego źródła; rekordy zachować bez modyfikacji.

## Implementacja

1. Usunąć blokadę dekodera dla pamięciowego ModR/M `89/8B`; użyć istniejącej ścieżki `_movMemoryTransfer`, zachowując prefiks, ModR/M i displacement w tych samych pozycjach kolejki co dla 8-bitowych form `88/8A`.
2. Uogólnić stan transferu MOV o szerokość słowa. `89` zapisuje rejestr ogólny do pamięci, `8B` odczytuje słowo do rejestru ogólnego; żadna z instrukcji nie zmienia FLAGS.
3. Dla słowa używać kolejnych fizycznych adresów magistrali (drugi bajt także przy offset `FFFFh`), prawidłowego BHE i osobnych cykli dla nieparzystego adresu. Nie zakładać atomowego dostępu słownego z `IAddressBus`.
4. Wyznaczać adres efektywny i segment statusu przez istniejący dekoder ModR/M; zachować domyślny SS dla adresowań BP oraz override ES/CS/SS/DS. Prefiks nie może zmieniać form rejestrowych.
5. Rozszerzyć harmonogram `TickMemoryMov`/profile o transfery słowa, opierając start i zakończenie magistrali, opóźnienie nieparzystego adresu, fetch i końcowy stan kolejki wyłącznie na wszystkich 4 000 wektorach. Zostawić ścieżki `88/8A` bez regresji.
6. Dodać do lokalnego manifestu wierne próbki prefiksowanych form pamięciowych `89/8B` (20 na opcode/prefiks) i aktywować istniejące bazowe próbki pamięciowe. Manifest zachowuje SHA, numery/hash rekordów źródła oraz metadane maski FLAGS.

## Kolejność prac i polecenia

1. Ten plan zatwierdzić osobnym commitem na branchu `A12-8086-pelna-weryfikacja`.
2. Dodać/aktywować lokalne próbki i testy wektorowe; sprawdzić manifest i testy Intel8086.
3. Uruchomić pełny filtr `89/8B` (oba pliki po 2 000 rekordów), poprawiając rozbieżności według pierwszego wskazanego testu/cyklu. Po ustabilizowaniu filtra nie powtarzać go przy każdej drobnej edycji: iterować na wskazanym fixture lub raporcie diagnostycznym, a pełny filtr uruchomić po domknięciu obu opcode’ów.
4. Uruchomić cały korpus raz. Jeśli wszystkie 3 025 dotąd pomijane formy przejdą, oczekiwane jest **558 450 passed, 87 550 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania** (86,4%). Każda różnica wymaga analizy regresji i korekty dokumentacji.
5. Zaktualizować macierz ISA, README wektorów, raport, checklistę A12 i stan wznowienia; oznaczyć punkt jako ukończony dopiero po pełnym raporcie. Plan i implementacja mają osobne commity.
6. W końcowej bramce pakietu wykonać Intel8086 tests, build Debug/Release, pełną solucję Release tests, format verify, walidator manifestu i OpcodeInfo.

Przykładowy pełny filtr:

```bash
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 \
  --manifest /tmp/a12-mov-89-8b-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a12-mov-89-8b-full.json
```

## Testy i kryteria

- [x] Plan zapisany w osobnym commicie `d483857` na branchu A12 przed zmianami implementacji.
- [x] W manifeście aktywne są bazowe `89/8B` i 160 prefiksowanych pamięciowych próbek ES/CS/SS/DS; rekordy pochodzą z przypiętego pinu, testy są offline.
- [x] `89` zapisuje słowo z właściwego rejestru, `8B` odczytuje słowo do właściwego rejestru; FLAGS pozostają bez zmian — filtr pełnego źródła przechodzi bez rozbieżności.
- [x] Wszystkie kombinacje ModR/M, displacement, DS/SS, override, wyrównania słowa i wrap fizycznego adresu przechodzą pełne porównanie 4 000 rekordów.
- [x] Nowe lokalne fixture’y 160/160; projekt Intel8086 Release 18 231/18 231.
- [x] Pełny filtr przypiętych `89/8B`: 4 000/4 000 passed, 0 skipped/mismatched/timeout/parse errors; testy Intel8086 potwierdzają brak regresji.
- [x] Pełny korpus: 558 450 passed, 87 550 skipped, 0 mismatchy/timeoutów/parse errors (86,4%); Debug/Release 0 ostrzeżeń i błędów; solucja Release tests 21 031/21 031; formatter verify, manifest 1 255/24 831 i OpcodeInfo 512/512 zielone. Checklista i README zaktualizowane.
- [ ] Plan i implementacja zapisane oddzielnymi commitami; branch nie jest wypchnięty.

Raport pakietu: [`A12-mov-memory-89-8b-report.json`](A12-mov-memory-89-8b-report.json). Oczekuje na wpisanie hasha commitu implementacji po jego utworzeniu.
