# A12 — segment-prefiksowany bezpośredni `CALL FAR 9A`

## Cel, branch i stan wejściowy

Na branchu `A12-8086-pelna-weryfikacja` obsłużyć ostatnią pomijaną grupę pełnego przypiętego korpusu 8086: 1 011 źródłowych instrukcji `CALL FAR ptr16:16` z jednym prefiksem ES/CS/SS/DS. Pozostałe 989 rekordów pliku 9A przechodzi już bez prefiksu. Celem jest 1 011/1 011 prefiksowanych rekordów i 646 000/646 000 całego korpusu, jeżeli wszystkie ślady potwierdzą aktualny model.

Pakiet dostaje osobny plan, checklistę, commit implementacji i commit dokumentacji; pracuje na gałęzi A12, która zawiera wcześniejsze etapy A12. Bez pushowania.

## Źródło i inwentarz

- Repozytorium: `SingleStepTests/8086`, pin `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`.
- Źródło: `v1/9A.json.gz`, chip `Intel P80C86A-2 782003 J20`, 2 000 rekordów.
- Rozkład: bez prefiksu 989; ES `26` 259; CS `2E` 252; SS `36` 246; DS `3E` 254. Razem segment-prefiksowane: 1 011.
- Prefiksowany rozmiar kolejki początkowej: 5 lub 6 bajtów. Zaobserwowane długości śladu: 31, 33, 39 i 41 T-states; zależność od kolejki oraz wyrównania zapisu stosu musi zostać wyprowadzona z pełnych śladów, nie z dodania stałej liczby cykli.
- Dwie lub cztery pierwsze chwile kolejki mogą być cyklami `Ti` przed pobraniem właściwego opcode’u. Następnie źródło pokazuje pobranie bajtów `9A`, offsetu i segmentu celu, zapis adresu powrotu i CS wywołującego na stosie oraz opróżnienie kolejki do celu far.
- Bieżący pełny korpus: 644 989/646 000 (99,8%); 1 011 pominięć dotyczy wyłącznie tej grupy. Raport poprzedniego pakietu: [segment-prefiksowane ALU](A12-prefixed-alu-00-3d-84-85-report.json).

## Zakres implementacji

1. Rozpoznawać jeden segment-prefiks przed `9A`; zachować prefiks jako pierwszy bajt instrukcji i ustawić długość instrukcji na 6 bajtów.
2. Czytać 16-bitowy offset i segment celu po prefiksie, z uwzględnieniem kolejki 5/6 bajtów i granicy zawinięcia IP.
3. Nałożyć zachowanie prefiksu na istniejący cycle-exact far call: T-states pobrań, statusy `Ti`/`CODE`, odczyty kolejki, kolejność dwóch zapisów na stosie, kolejność SP i `CS:IP`, opróżnienie kolejki oraz adresy fizyczne.
4. Prefiks segmentowy nie zmienia segmentu `SS` używanego przez zapis ramki; jego koszt i zachowanie kolejki wynikają ze śladów, a nie z dostępu danych operandów.
5. Zachować wszystkie 989 przypadków bez prefiksu oraz wcześniejsze ślady E8–EA i pozostałe instrukcje skoków.

## Kryteria i checklisty

Szczegółowa checklista punkt po punkcie: [A12-checklista-pakietu-prefixed-call-far-9a.md](A12-checklista-pakietu-prefixed-call-far-9a.md). Każde ukończenie aktualizuje checkbox, dowód i procent pakietu; wynik korpusu aktualizuje się wyłącznie na podstawie pełnego raportu.

- [ ] Inwentarz i plan zatwierdzone przed kodem; 1 011 rekordów źródłowych policzone per prefix.
- [ ] Dodać 80 deterministycznych fixture’ów (20 na każdy prefix); lokalne asercje obejmują pełne rejestry, RAM, kolejkę i każdy T-state.
- [ ] Wyprowadzić profile z pełnych 1 011 śladów: kolejka 5/6, długości 31/33/39/41, stos parzysty/nieparzysty, byte fetch, oba zapisy słowa.
- [ ] Rozszerzyć dekodowanie i wykonywanie tak, by prefiksowane `9A` nie trafiało na ścieżkę „unsupported”; sprawdzić CS:IP, SP, ramkę stosu i flush kolejki.
- [ ] Pełny filtr segment-prefiksowany `9A`: 1 011/1 011, zero skipów, rozbieżności, timeoutów i błędów parsowania.
- [ ] Pełny plik `9A`: 2 000/2 000, zero regresji dla 989 rekordów bez prefiksu.
- [ ] Pełny korpus: 646 000/646 000, zero skipów, rozbieżności, timeoutów i błędów parsowania.
- [ ] Bramki jakości i synchronizacja README, macierzy ISA, raportu, checklisty A12 oraz stanu wznowienia.
- [ ] Oddzielne commity implementacji i dokumentacji; hashe planu/implementacji/dokumentacji w raporcie; checklista zamknięta.

## Zoptymalizowana kolejność weryfikacji

Użyć najpierw lokalnych fixture’ów do odkrycia profili. Po ustabilizowaniu wykonać raz pełny filtr 9A, raz cały plik 9A, raz korpus 646 000 i jeden zestaw bramek jakości. Nie uruchamiać pełnego korpusu w iteracjach diagnostycznych.

## Polecenia

```bash
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --filter FullyQualifiedName~PrefixedCallFar --nologo -m:1
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
git diff --check
```
