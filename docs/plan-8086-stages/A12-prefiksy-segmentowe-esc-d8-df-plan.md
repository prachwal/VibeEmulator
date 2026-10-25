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

- [x] Dodać deterministyczne fixture’y: 20 pierwszych rekordów dla każdej z 128 grup opcode/prefiks/mod, z oryginalnymi `test_num` i hashami (2 560 rekordów).
- [x] Rozpoznać segment-prefiksowane ESC dla wszystkich D8–DF w dekoderze; dekoder zachowuje ESC jako opcode CPU.
- [x] Przekazać `_segmentOverride` do dekodera adresu efektywnego i statusu magistrali w `DecodeAdditionalGroupOperands`.
- [x] Odtworzyć T-state i kolejkę segment-prefiksowanej formy rejestrowej `mod=11`: fixture’y 640/640 zgodne.
- [x] Odtworzyć z wektorów T-state i kolejkę pamięciowych form ESC, z uwzględnieniem prefiksu, displacementu, parzystości adresu słowa i uzupełnienia kolejki; fixture’y końcowe 1 920/1 920 zgodne.

### P2 — pełny filtr źródłowy

- [x] Uruchomić fixture’y lokalne dla wszystkich 128 grup: 2 560/2 560; brakujące grupy: 0.
- [x] Uruchomić prefiksowany filtr D8–DF: dokładnie 8 012/8 012, zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Osobno sprawdzić cztery prefiksy, wszystkie mody, register-direct, adresy BP/SS, aligned/unaligned EA i wszystkie opcodes D8–DF w źródłowym filtrze.
- [x] Ponownie sprawdzić nieprefiksowane 7 988/7 988 jako kontrolę regresji ESC.

### P3 — pełny korpus i checklisty

- [x] Uruchomić pełny korpus 646 000 po ustabilizowaniu zmian: 572 970 passed, 73 030 skipped, 0 mismatched/timeout/parse errors (88,7%).
- [x] Zaktualizować macierz, tabelę skipów, README wektorów, raport A12 i checklistę z `%` po zakończeniu punktu.

### P4 — Testy i kryteria

- [x] Build Debug i Release: zero ostrzeżeń/błędów.
- [x] Pełne testy Release 22 240/22 240; testy Intel8086 19 440/19 440; formatter verify przechodzi.
- [x] OpcodeInfo 512/512, walidator manifestu (1 462 wpisy / 28 599 wskazań) oraz `git diff --check` przechodzą.
- [x] Zapisać implementację i fixture’y w osobnym commicie po planie; raport wskazuje hash implementacji `5ebb8828924fe8a697534d2951b1909b6964aec0`; bez push.

## Kryterium ukończenia

Wszystkie 8 012 segment-prefiksowanych przypadków D8–DF zgadzają stan CPU, pamięć, kolejkę i każdy T-state; kontrolny nieprefiksowany filtr ESC pozostaje zielony; pełny korpus, wszystkie wymagane bramki i aktualny procent są zapisane. Plan oraz implementacja mają oddzielne commity.
