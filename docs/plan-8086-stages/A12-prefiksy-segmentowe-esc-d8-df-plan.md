# A12 — segment-prefiksowane ESC (`D8–DF`)

## Cel

Zweryfikować i domknąć segment-prefiksowane wykonanie ESC (`D8–DF`) na podstawie przypiętych śladów P80C86A-2. Rdzeń już realizuje nieprefiksowaną ścieżkę ESC; pakiet obejmuje pobranie prefiksu, wybór segmentu dla pamięciowego EA, formy rejestrowe i dokładne T-state/prefetch. Plan i implementacja to osobne commity na branchu `A12-8086-pelna-weryfikacja`.

## Źródła i inwentaryzacja

- Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, pliki `v1/D8.json.gz`–`v1/DF.json.gz` (po 2 000 rekordów).
- Segment-prefiksowane rekordy: `D8` 973, `D9` 989, `DA` 1 026, `DB` 1 011, `DC` 954, `DD` 1 022, `DE` 990, `DF` 1 047; razem **8 012**.
- W każdym opcode źródło zawiera wszystkie cztery segmenty ES/CS/SS/DS oraz `mod=00/01/10/11`; rozkłady per plik są zapisane w raporcie końcowym.
- Macierz ISA dokumentuje pełną zgodność nieprefiksowanego zachowania CPU ESC dla 7 988 rekordów z D8–DF. Semantyka zewnętrznego 8087 pozostaje poza zakresem.
- Segment override ma zastosowanie tylko do efektywnego adresu formy pamięciowej. Nie wolno zmieniać `reg`, FLAGS, stanu 8087 poza granicą interfejsu ESC ani pozostałych rejestrów.

## Zakres

1. Przyjąć jeden prefiks ES/CS/SS/DS przed dowolnym `D8–DF`.
2. Dla `mod=00/01/10` obliczyć ten sam offset i wybrać wskazany segment; zachować domyślne DS/SS bez override.
3. Dla `mod=11` zachować ścieżkę register-direct ESC, ale zużyć prefiks i odtworzyć jego wpływ na kolejkę/czas dokładnie według SST.
4. Zachować wszystkie transfery i prefetch: EA, displacement, adres fizyczny, status segmentu, adresowanie nieparzyste, kolejność CODE i T-state.
5. Nie implementować semantyki koprocesora 8087 ani zmieniać nieprefiksowanego zachowania.

## Plan realizacji

### P0 — plan i inwentarz

- [x] Potwierdzić czysty branch, wcześniejszy checkpoint oraz pin źródła.
- [x] Policzyć segment-prefiksowane rekordy dla wszystkich ośmiu opcode’ów i rozkład ModR/M.
- [x] Zapisać plan w osobnym commicie przed implementacją.

### P1 — fixture’y i rdzeń

- [ ] Dodać deterministyczne fixture’y: maksymalnie pierwsze 20 rekordów dla każdej grupy opcode/prefiks/mod, z oryginalnymi `test_num` i hashami.
- [ ] Rozpoznać segment-prefiksowane ESC dla wszystkich D8–DF w dekoderze; nie dopuścić, by FPU/ESC extension został błędnie zdekodowany jako inny opcode.
- [ ] Przekazać `_segmentOverride` do dekodera adresu efektywnego i statusu magistrali w `DecodeAdditionalGroupOperands`.
- [ ] Odtworzyć z wektorów T-state i kolejkę form rejestrowych oraz pamięciowych, z uwzględnieniem prefiksu i displacementu.

### P2 — pełny filtr źródłowy

- [ ] Uruchomić fixture’y lokalne dla wszystkich grup; raportować sumę i brakujące grupy.
- [ ] Uruchomić prefiksowany filtr D8–DF: dokładnie 8 012/8 012, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [ ] Osobno sprawdzić cztery prefiksy, wszystkie mody, register-direct, adresy BP/SS, aligned/unaligned EA i wszystkie opcodes D8–DF.
- [ ] Ponownie sprawdzić nieprefiksowane 7 988/7 988 jako kontrolę regresji ESC.

### P3 — pełny korpus i checklisty

- [ ] Uruchomić pełny korpus 646 000 po ustabilizowaniu zmian; zanotować passed/skipped/mismatched/timeout/parse errors i procent.
- [ ] Zaktualizować macierz, tabelę skipów, README wektorów, raport A12 i checklistę z `%` po zakończeniu punktu.

### P4 — Testy i kryteria

- [ ] Build Debug i Release: zero ostrzeżeń/błędów.
- [ ] Pełne testy Release i formatter verify przechodzą.
- [ ] OpcodeInfo 512/512, walidator manifestu oraz `git diff --check` przechodzą.
- [ ] Zapisać implementację, fixture’y i raport w oddzielnym commicie po planie; nie wypychać.

## Kryterium ukończenia

Wszystkie 8 012 segment-prefiksowanych przypadków D8–DF zgadzają stan CPU, pamięć, kolejkę i każdy T-state; kontrolny nieprefiksowany filtr ESC pozostaje zielony; pełny korpus, wszystkie wymagane bramki i aktualny procent są zapisane. Plan oraz implementacja mają oddzielne commity.
