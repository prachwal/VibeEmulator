# Plan kaskadowy rdzenia Intel 8086

Plan nowego, niezależnego rdzenia Intel 8086 zgodnego z cyklami i magistralą, walidowanego wektorami SingleStepTests. Układ etapów wzorowany na [planie Z80](../plan-z80-stages/README.md). Każdy etap ma być osobnym, przeglądalnym zakresem zmian. Ten plik jest planem i dziennikiem stanu; zachowanie procesora wynika z przypiętych wektorów i dokumentacji producenta, nigdy z pamięci.

## Cel i źródła

Rdzeń 8086 dla wariantu Intel P80C86A-2 w trybie Maximum Mode, z kolejką prefetch, 20-bitową magistralą adresową i obserwowalnymi cyklami. Pierwszy cel to poprawne wykonanie zestawów instrukcji i porównanie pełnego stanu/cykli wektorów; bez integracji z konkretnym komputerem.

Źródła wstępnie sprawdzone 2026-10-23:

- [SingleStepTests/8086](https://github.com/SingleStepTests/8086), katalog `v1/`, wersja opisana w README jako 1.0.1, licencja MIT. Wektory pochodzą z fizycznego Intel P80C86A-2 w trybie Maximum Mode, przez interfejs ArduinoX86; repo podaje 2000 testów na opkod. Format JSON zawiera `bytes`, początkowy/końcowy stan rejestrów, RAM, kolejkę prefetch i cykle magistrali. W recenzji sprawdzono README, changelog, licencję, metadane, drzewo Git oraz pliki `90`, `88` i `F7.7` na commicie [`e71c68d215a6bb8c356bd4cb3842de3bef345ca9`](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9). To pin roboczy do potwierdzenia w A1: drzewo `v1/` zawiera 323 pliki `*.json.gz` i `metadata.json` (łącznie 181 480 926 bajtów skompresowanych plików i metadanych); pełnej liczby rekordów ani rozmiaru po dekompresji nie zmierzono. Trzy odczytane pliki mają po 2000 rekordów. Rzeczywiste identyfikatory to `test_hash` i `test_num`, choć przykład w README używa `hash` i `idx`.
- [Intel, The 8086 Family User's Manual, October 1979 (9800722-03)](https://bitsavers.org/components/intel/8086/9800722-03_The_8086_Family_Users_Manual_Oct79.pdf) — kopia źródła pierwotnego wskazana do analizy w A1; nie została przeczytana; próba dostępu w tej recenzji zwróciła HTTP 403. Nie opierać implementacji na samym zestawie wektorów.
- Dokumentacja repo: [standardy projektu](../../.agents/skills/standardy-projektu/SKILL.md), [wskazówki wektorów](../../.github/instructions/single-step-vectors.instructions.md), [wskazówki C#](../../.github/instructions/csharp.instructions.md).

**Zakres wektorów jest wstępny.** Nie wpisuj liczby opkodów/plików do implementacji, dopóki A1 nie zinwentaryzuje przypiętego drzewa. Wektory reprezentują konkretne P80C86A-2; nie dowodzą automatycznie zachowania każdego 8086/8088. Wektory sprzętowe są mocnym źródłem, ale konfiguracja Maximum Mode, brak wait states oraz niećwiczone INTR/NMI/TF ograniczają pokrycie.

## Stan etapów

| etap | stan | zakres | wynik wektorów |
|---|---|---|---|
| A0 | zakończony (decyzje) | kontrakt rdzenia, czas, kolejka, 20-bitowa magistrala i reset | prototyp do potwierdzenia w A2 |
| A1 | zakończony i poprawiony po audycie | pin źródła, licencja, format i pełny inwentarz | 323 pliki / 646 000 rekordów; próbki aktywne od A2 |
| A2 | zakończony | projekty, stan CPU, 20-bitowa pamięć, cykl magistrali i harness | NOP: 20/20; pełne 646 000 nieuruchomiono |
| A3 | częściowo zrealizowany; otwarty | MOV immediate, rejestrowy ModR/M i dekoder EA | B0–B7: 16 000/16 000; 88/8A mod=11: 519/519; EA 29/29 unit; pamięć otwarta |
| A4 | częściowo zrealizowany; otwarty | AL-immediate oraz ALU byte ModR/M rejestrowe | 8 029 + 4 355 pełnych rekordów; pamięć/immediate do r/m otwarte |
| A5 | częściowo zrealizowany; otwarty | `MOV r16,imm16`, ModR/M word oraz rejestrowy `PUSH/POP` 50–5F | B8–BF 16 000/16 000; 89/8B 490/490; rejestrowy stos próbki 320/320, pełny filtr 32 000/32 000; pamięć, segmenty i FLAGS pozostają otwarte |
| A6 | zakończony w zakresie etapu (prefiksy do A10) | word ALU, INC/DEC, NOT/NEG i shift/rotate count=1/CL | aktywny filtr CPU 4 880/4 880; pamięciowe `81/83` 960/960; pełne zestawy źródłowe poza repo nieuruchomione |
| A7 | ukończony | Jcc, JMP, CALL/RET, LOOP, FLAGS oraz przerwania programowe | Jcc short 320/320, JMP `EB/E9/EA` 60/60, CALL/RET 120/120, LOOP/JCXZ 80/80, INT/INT3/INTO/IRET 80/80, FLAGS 180/180; filtr CPU 5 720/5 720 |
| A8 | zakończony (`8e5211c`, branch `A8-8086-multiply-divide-bcd`) | CBW/CWD 40/40, DAA/DAS/AAA/AAS 80/80, rejestrowe i pamięciowe MUL/IMUL /4–/5 160/160, AAM/AAD 52/52, pełny D4/D5 4 000/4 000, rejestrowe DIV/IDIV 80/80, pamięciowe DIV/IDIV 80/80; REP-IDIV 141/141; pełne filtry MUL/IMUL: reg 1 013/1 013, mem 3 028/3 028; DIV/IDIV mem 2 882/2 882; helpery 320/320; ESC 160/160, pełny filtr 7 988/7 988 | testy specjalne 801/801; Intel8086 7 206/7 206; solucja Release 10 006/10 006; buildy Debug/Release 0 ostrzeżeń i błędów; format OK; OpcodeInfo 512/512 |
| A9 | ukończony (`add5f81`, branch `A9-8086-string-io`) | łańcuchy, REP/REPE/REPNE, segment override, IN/OUT | pełne filtry 36 000/36 000, 0 pominięć/niezgodności; Release 10 151/10 151; build Debug/Release bez ostrzeżeń |
| A10 | zakończony w zakresie; niezaobserwowane sekwencje jawnie odroczone do A12 (`A10-8086-prefixes-decoder`, baza A9 `add5f81`) | audyt manifestu i metadata, kombinacje prefiksów, klasyfikacja aliasów/LOCK | manifest: 869 wpisów/17 267 wskazań; pełny przebieg 580 par prefiks/opcode: 48 306/197 748, 0 skipów, 149 442 mismatchy sklasyfikowane w raporcie; 178 całych grup opcode zaliczonych; aliasowe Jcc 26 004/26 004, RETN/RETF 8 000/8 000, ESC 7 988/7 988; segment-prefiksowane rejestrowe MOV 1 010/1 010 i segment+REP string 4 961/4 961; Intel8086 Release **10 716/10 716**, solucja **13 516/13 516**; buildy Debug/Release 0 ostrzeżeń; formatter, OpcodeInfo 512/512, Python 43/43; macierz 323/323 form sklasyfikowanych; filtry segmentowe: 81/83 memory 11 870/11 870, register 4 075/4 075, FE 1 985/1 985, FF 4 000/4 000, F6/F7 11 849/11 849; lokalny A10Prefix 2 480/2 480; LOCK+REP test 2/2 dla kolejek 5/6. Bieżące buildy Debug/Release: 0 ostrzeżeń/błędów; pełna solucja Release 13 516/13 516; Python 43/43; formatter verify i OpcodeInfo 512/512 OK. Niezaobserwowane sekwencje i zachowanie po przerwaniu są opisane jako known-gap w [A10-prefix-sequence-decisions.json](A10-prefix-sequence-decisions.json); A11 obejmie wznowienie, a A12 audyt tych luk. Pełny audyt wykazał 149 442 mismatchy w 402/580 grupach. |
| A11 | zakres checkpointu zrealizowany; luki sprzętowe jawnie przekazane do A12 (`A11-8086-reset-przerwania-stany`, baza A10 `c5de77a`) | RESET, INTR/NMI/TF, HALT, WAIT, READY i zachowania poza pojedynczym krokiem | Intel8086 **10 746/10 746**, solucja Release **13 546/13 546**, buildy Debug/Release 0 ostrzeżeń/błędów; pełna kolejność wejścia NMI/INTR, efekty magistrali pod READY, MOV/POP segmentu i arbitraż RQ/GT pozostają jawnie otwarte |
| A12 | audyt pełnego zestawu w toku; kryteria zgodności pozostają otwarte | pełne zestawy poza repo, naprawa rozbieżności i zapisanie znanych luk | Ostatni pełny checkpoint `61c0720` (przed portem rejestrowego PUSH/POP): 646 000 rekordów, **397 167 passed, 242 443 skipped, 6 390 mismatched**; manifest obecnie 892 wpisy/17 594 wskazania; rejestrowe PUSH/POP: 32 000/32 000. Pełny przebieg i bramki całej solucji dla bieżącego drzewa wymagają ponowienia po przeniesieniu pozostałych zmian; zgodności nie deklarować. Szczegóły w [A12](A12-pelna-weryfikacja-i-zamkniecie.md). |

Etapy A3–A10 są porcjowane dalej na grupy instrukcji w dzienniku bieżącego etapu przed ich implementacją. Wspólne zależności wdrażać przed pierwszym użyciem: parser prefiksów segmentowych w A2/A3, REP potrzebny przez wektory IDIV w A8, REP łańcuchów w A9. A10 audytuje i domyka te mechanizmy.

## Poziomy ukończenia

| poziom | definicja |
|---|---|
| I. Rdzeń uruchamialny | fetch/decode, stan i NOP, deterministyczny harness, jedna instrukcja na uruchomienie |
| II. Zestaw bazowy | komplet zdefiniowanych instrukcji 8086 w manifestach i zgodność stanu/RAM |
| III. Cykle i kolejka | zgodność każdego porównywalnego cyklu, sygnałów, transferów 8/16-bit oraz kolejki prefetch |
| IV. Zachowanie procesora | RESET, HALT, WAIT i linie przerwań z testami niezależnymi od brakujących wektorów |
| V. Gotowość projektu | pełne wektory, testy programowe, Debug/Release, formatowanie, zero ostrzeżeń |

Nie deklarować poziomu III na podstawie samego końcowego stanu. Próbka w repo nie jest pełnym zestawem.

## Architektura docelowa

- Nowe projekty `src/Intel8086` i `tests/VibeEmulator.Intel8086.Tests`; zgodność z `ICpuCore<TState>` z `src/Cpu.Abstractions` jest decyzją A0, a nie gotowym założeniem. Nie rozszerzać `Mos6502` ani `Z80` o logikę 8086.
- Zależności: `Intel8086` → `Memory`, `Cpu.Abstractions`; testy → `Intel8086`, biblioteki testowe. Rejestry segmentowe, wskaźnik instrukcji, flagi, kolejka prefetch i cykl magistrali są częścią stanu 8086.
- Obecny [`ICpuCore`](../../src/Cpu.Abstractions/ICpuCore.cs) wymaga dokładnego `Pending`, `ushort InstructionPc` i `Reset(ushort)`. A0 ma rozstrzygnąć reprezentację `CS:IP`, reset pinowy/testowy i przewidywanie końca instrukcji przy prefetch/READY. Jeśli nie da się zachować znaczenia kontraktu, zaprojektować osobny kontrakt lub uzasadnione rozszerzenie z testami regresji; nie zwracać fikcyjnego `Pending=1`. Ustalić semantykę `Tick()`/`Pending` po analizie kontraktu i wektorów. Granularność ma umożliwiać obserwację magistrali w każdym T-state; cykl magistrali 8086 składa się z T-state, więc nie utożsamiać `Tick` z całym M-cycle.
- [`IMemoryBus`](../../src/Memory/Memory.cs) przyjmuje `ushort`, więc sam adapter ani `FlatMemory(0x100000)` nie udostępnia pełnego adresu. A0 musi wybrać nowy kontrakt szerszych adresów w `Memory` lub kontrakt lokalny 8086, zachowując dotychczasowe API. Magistrala adresuje 1 MiB i odwzorowuje `segment:offset` na 20-bitowy adres z zawijaniem zgodnym z wariantem potwierdzonym źródłem. Obsłużyć banki bajtów, A0/BHE i transfery słów przez granicę adresu.
- Model kolejki prefetch musi dać się jawnie zainicjować i porównać z `initial.queue` / `final.queue`; fetch/decode nie może omijać widocznego ruchu magistrali.
- Nie zakładać tabeli cykli na opkod. Fazy wynikają z udokumentowanego zachowania magistrali i wektorów; niepewne zachowania oznaczyć jako niezweryfikowane.
- Pliki C# dzielić przez `partial`, miękki limit 300 linii, twardy podział powyżej 400. `Tick()` i kod pod nim bez alokacji.

## Pliki etapów

Każdy etap ma własny zakres, nowe elementy, kryteria akceptacji, pułapki i dziennik postępu:

| etap | dokument | główny rezultat |
|---|---|---|
| A0 | [A0-decyzje-architektoniczne.md](A0-decyzje-architektoniczne.md) | rozstrzygnięte kontrakty rdzenia, cyklu i magistrali |
| A1 | [A1-zrodlo-i-inwentaryzacja-wektorow.md](A1-zrodlo-i-inwentaryzacja-wektorow.md) | pin źródła, manifest i format wektorów |
| A2 | [A2-szkielet-i-harness.md](A2-szkielet-i-harness.md) | projekty i zgodność NOP end-to-end |
| A3 | [A3-transfery-8-bit-i-modrm.md](A3-transfery-8-bit-i-modrm.md) | transfery bajtów i adresowanie efektywne |
| A4 | [A4-alu-8-bit.md](A4-alu-8-bit.md) | ALU bajtowe i flagi |
| A5 | [A5-transfery-16-bit-i-stos.md](A5-transfery-16-bit-i-stos.md) | transfery słów, segmenty i stos |
| A6 | [A6-alu-16-bit-i-przesuniecia.md](A6-alu-16-bit-i-przesuniecia.md) | ALU słowne, inkrementacja i przesunięcia |
| A7 | [A7-sterowanie-przeplywem.md](A7-sterowanie-przeplywem.md) | skoki, wywołania i przerwania programowe |
| A8 | [A8-mnozenie-dzielenie-i-bcd.md](A8-mnozenie-dzielenie-i-bcd.md) | arytmetyka specjalna 8086 |
| A9 | [A9-lancuchy-i-io.md](A9-lancuchy-i-io.md) | operacje łańcuchowe i porty |
| A10 | [A10-prefiksy-i-domkniecie-dekodera.md](A10-prefiksy-i-domkniecie-dekodera.md) | prefiksy i kompletna macierz kodowania |
| A11 | [A11-reset-przerwania-i-stany.md](A11-reset-przerwania-i-stany.md) | zdarzenia zewnętrzne i cykl życia CPU |
| A12 | [A12-pelna-weryfikacja-i-zamkniecie.md](A12-pelna-weryfikacja-i-zamkniecie.md) | pełny raport zgodności i definicja ukończenia |

A0 zapisuje decyzje wstępne, A1 dostarcza format i inwentaryzację, a prototyp A2 potwierdza kontrakty przed A3. Etapy instrukcji wymagają pełnego inwentarza źródła z A1, lecz aktywują tylko własne pozycje manifestu. Otwarta luka blokująca może pozostać jawna podczas dalszych prac, ale blokuje deklarację odpowiedniego poziomu ukończenia i zamknięcie A12.

## Etapy szczegółowe

### A0 — decyzje przed kodem

1. Odczytać `ICpuCore<TState>`, analogiczne integracje Z80 i aktualne `.slnx`/konwencje testów.
2. Zapisać publiczne API, semantykę `Tick`, granicę instrukcji, model kolejki i resetu, adapter magistrali/portów oraz limit czasu wektora.
3. Określić, jak rozdzielić stan semantyczny od wyjść pinów; porównywać nie tylko końcowe rejestry.
4. Zakres pierwszej wersji to 8086; 8088, 80186+, 80286+, tryb Minimum, coprocessor 8087 i integracja z maszyną są poza zakresem.

### A1 — przypięcie danych

1. Pobrać repozytorium do pustego katalogu tymczasowego, zapisać commit SHA i odczytać README, `CHANGELOG.md`, `LICENSE`, `v1` oraz metadane.
2. Programowo policzyć pliki, testy/opkod, formaty, rozmiar oraz pliki brakujące/puste. Sprawdzić wszystkie nazwy i ewentualne warianty testów.
3. Przeanalizować JSON-y dla rejestrów, RAM, kolejki, cykli, wyjątków, hashy i flag. Ustalić maski/nieokreślone bity z metadanych upstream, jeśli występują.
4. Ustalić pierwsze N rekordów na plik w repo (domyślnie 20, decyzję uzasadnić rozmiarem) i poza repo pełne testy. Dane tylko jako zwarty, niezmieniony prefiks JSON.
5. Zapisać pin, źródło, licencję, rozmiar, manifest i ograniczenia w `tests/VibeEmulator.Intel8086.Tests/Vectors/README.md`; dodać licencję upstream zgodnie z istniejącą praktyką.

### A2 — szkielet i harness

1. Dodać projekty, referencje i wpisy do `VibeEmulator.slnx`.
2. Dodać `Intel8086State`, inicjalizację/testową wymianę stanu, magistralę 20-bitową wybraną w A0 i jawną kolejkę prefetch.
3. Zaimplementować tylko fetch i NOP potrzebne do sprawdzenia granicy wektora; najpierw testy ręczne magistrali i kolejki, potem fixture JSON.
4. Zachować pełny inwentarz źródła A1 i osobny status aktywacji próbki. Test kontroluje pliki i liczbę rekordów aktywnych pozycji; nie usuwa niezaimplementowanych pozycji z inwentarza.
5. Runner porównuje nazwę przypadku, rejestry, flagi z maską źródłową, RAM, kolejkę i wszystkie wspierane pola każdego cyklu; komunikat różnicy podaje opkod, indeks/hash, cykl i expected/actual.
6. W testach nie ma sieci ani zależności od cwd. Pełne dane uruchamiać osobnym runnerem poza repo.

### A3 — transfery 8-bit i dekodowanie ModR/M

Kolejność grup: `MOV` rejestr/rejestr i rejestr/pamięć; `MOV` immediate; prefiksy segmentowe; `LEA`; bajtowe `XCHG`; `XLAT`; dekodowanie ModR/M oraz adres efektywny 16-bit, displacement i segment domyślny/nadpisany. Grupy potwierdzić wobec manifestu i instrukcji Intel przed kodowaniem.

### A4 — ALU 8-bit

Kolejność grup: `ADD/ADC`, `SUB/SBB/CMP`, `AND/OR/XOR/TEST`; warianty r/m, rejestr i immediate; flagi `CF/PF/AF/ZF/SF/OF`. Każda grupa ma testy brzegów oraz próbkę wektorów. Semantyka bitów flag z wektorów/źródła, nie z implementacji innego CPU.

### A5 — operacje 16-bitowe i stos

`MOV r16/rm16/imm16`, segmenty, `PUSH/POP`, `PUSHF/POPF`, słowny `XCHG`, `LDS/LES`, testy EA z A3, transfery pamięci słów parzystych/nieparzystych, dostęp stosu przez `SS:SP`. Dodać testy transferu przez zawinięcie 20-bitowego adresu i banków magistrali, po ustaleniu ich semantyki źródłowej.

### A6 — ALU 16-bit, inkrementacja i przesunięcia

Warianty słowne operacji A4; `INC/DEC` bez modyfikacji CF; `NOT/NEG`; `SHL/SAL/SHR/SAR`, `ROL/ROR/RCL/RCR`, licznik stały 1 i licznik z `CL`; formy z dowolnym immediate wyłączyć z bazowego planu do potwierdzenia ISA w A1. Pokryć wartości count 0, 1 i graniczne. Maskowanie `CL` przez generator danych odróżnić od semantyki CPU. Nie przenosić reguł nowszych x86.

### A7 — sterowanie przepływem i przerwania programowe

`Jcc`, `JMP` short/near/far, `CALL/RET` near/far, `LOOP/LOOPE/LOOPNE/JCXZ`, `INT/INT3/INTO/IRET`, `CLC/STC/CMC`, `CLD/STD`, `CLI/STI`, `LAHF/SAHF`. Wektory wyjątków obsłużyć dokładnie w granicach ich fixture; asynchroniczne sygnały należą do A11.

### A8 — arytmetyka specjalna i instrukcje systemowe 8086

`MUL/IMUL/DIV/IDIV`, `CBW/CWD`, `DAA/DAS/AAA/AAS/AAM/AAD`, `BOUND` poza bazowym zakresem; kodowanie sklasyfikować w A1 bez nadawania mu semantyki nowszego CPU, `ESC` jeśli występuje w wektorach. Potwierdzić zakres w manualu przed planowaniem symboli; przypadki wyjątków mają osobne testy i opis nieokreślonych flag.

### A9 — łańcuchy i I/O

`MOVS/CMPS/STOS/LODS/SCAS` bajt/słowo; prefiksy `REP/REPE/REPNE`; `INS/OUTS` poza bazowym zakresem; kodowania sklasyfikować w A1 bez nadawania im semantyki nowszego CPU; `IN/OUT` immediate i przez `DX`; kontrakt portów. `CX`/warunki przerwania powtórzeń testować małymi wartościami z fixture, a pełne długie przypadki ograniczyć timeoutem harnessu, nie skracaniem wektora.

### A10 — prefiksy, dekoder i komplet manifestu

Obsłużyć prefiksy segmentowe, LOCK i REP zgodnie z 8086 i pokryciem; kombinacje/nieudokumentowane bajty tylko gdy są w przypiętych testach i zostały sklasyfikowane. Zautomatyzować kontrolę: wszystkie testy w źródłowym manifeście mają status implemented, explicitly unsupported/out-of-scope lub known source gap. Brakujący wektor nie oznacza braku instrukcji.

### A11 — linie, reset i zachowanie ciągłe

Osobne testy dla resetu, READY i właściwego dla wybranego trybu arbitrażu magistrali, NMI, INTR/INTA, TF/single-step, IF, HLT i WAIT oraz wpływu kolejki na granice instrukcji. Wektory 1-step nie pokrywają automatycznie przerwań asynchronicznych. Dla każdego zachowania wymagane jest źródło producenta lub niezależny test sprzętowy/programowy; nie przedstawiać braku wektorów jako potwierdzenia.

### A12 — pełna weryfikacja

1. Uruchomić próbki offline w repo i wszystkie rekordy pełnych plików poza repo na tym samym przypiętym SHA; raportować passed/skipped/mismatched osobno dla każdej rodziny i opkodu.
2. Uruchomić zewnętrzne programowe zestawy dla 8086, jeśli ich pochodzenie/licencja/kontrakt są rozpoznane. Nie wymyślać wyników ani nie uznawać emulatora jako jedynego źródła prawdy.
3. Zbadać wszystkie rozbieżności; nie edytować upstream JSON. Poprawić rdzeń albo udokumentować ograniczenie źródła z konkretnym dowodem.
4. Zamknąć build Debug/Release, testy Release, formatowanie, manifest/licencje i dokumentację. Zaktualizować tabelę etapów, plan nadrzędny i README wektorów.

## Rytuał każdego etapu

1. Przed kodem dopisać do dziennika podetapy/grupy z konkretnymi opkodami i plikami wektorów.
2. Pobrać tylko przypięte dane do pustego katalogu poza repo; dodać próbkę źródłową bez ręcznej edycji.
3. Dodać testy jednostkowe dla logiki nowej grupy i testy wektorowe dla każdej pozycji manifestu.
4. Porównać wynik: stan CPU, flagi maskowane wyłącznie zgodnie ze źródłem, RAM, kolejkę i cykle/piny. Brak dowodu opisuje się jawnie.
5. Zapisać passed/skipped/mismatched, zakres próbki/pełnego zestawu i znane odstępstwa w tabeli oraz dzienniku etapu.
6. Uruchomić polecenia jakości na końcu dokumentu. Nie commitować ani nie pushować bez jawnej prośby.

## Ryzyka i bariery

| ryzyko | wymagane działanie |
|---|---|
| Semantyka `ICpuCore` może nie pasować do kolejki prefetch i osobnych T-state | prototyp API i test kontraktu w A0/A2; nie zmieniać kontraktu globalnego bez potrzeby uzasadnionej innymi rdzeniami |
| Rozkład bus cycles zależy od kolejki, parzystości adresu i współbieżnych transferów | model kolejki/magistrali od A2; nie ograniczać runnera do końcowego stanu |
| Dane testowe dotyczą konkretnego modelu i trybu Maximum | jawny wariant, ograniczenia README i porównanie z dokumentacją Intel |
| Rozmiar pełnych JSON-ów może być wielogigabajtowy | pomiar w A1, próbka w repo, pełny zestaw poza repo, streaming parser/runner i limity dysku |
| Nieudokumentowane/undefined instrukcje i flagi | pole maski źródła albo jawna klasyfikacja; zero po cichu pomijanych asercji |
| Różne formaty numeracji/adresu i bus pins | osobny adapter specyficzny dla 8086; normalizować tylko w porównywarce, zachowując surowe dane |
| A11 nie ma pełnych wektorów asynchronicznych | dokumentacja Intel i dedykowane scenariusze z jawnym źródłem; oznaczyć niezweryfikowane, jeśli brak niezależnego dowodu |

## Komplet poleceń

Uruchamiaj z katalogu głównego repo. Poniższe bloki są punktami wejścia i kryteriami wykonania planu; komendy etapów zostaną doprecyzowane po A1, gdy przypięty manifest będzie znany.

### Branch i inwentaryzacja upstream (A1)

```bash
git status --short --branch
mkdir -p /tmp/vibe-8086-source /tmp/vibe-8086-full
git clone --filter=blob:none --no-checkout https://github.com/SingleStepTests/8086.git /tmp/vibe-8086-source
git -C /tmp/vibe-8086-source fetch origin e71c68d215a6bb8c356bd4cb3842de3bef345ca9
git -C /tmp/vibe-8086-source checkout --detach e71c68d215a6bb8c356bd4cb3842de3bef345ca9
git -C /tmp/vibe-8086-source rev-parse HEAD
git -C /tmp/vibe-8086-source status --short
cat /tmp/vibe-8086-source/README.md
cat /tmp/vibe-8086-source/CHANGELOG.md
find /tmp/vibe-8086-source/v1 -type f -name '*.json.gz' -print0 | sort -z | xargs -0 -r -n1 basename
find /tmp/vibe-8086-source/v1 -type f -name '*.json.gz' -print0 | xargs -0 -r du -ch | tail -n 1
```

Po inspekcji ręcznie zapisać konkretny SHA w `Vectors/README.md`; dalsze pobieranie musi używać SHA, nie gałęzi `main`.

### Pobranie próbek po przypięciu (schemat A1/A2)

```bash
SOURCE_SHA='<SHA zapisany w tests/VibeEmulator.Intel8086.Tests/Vectors/README.md>'
git -C /tmp/vibe-8086-source fetch origin "$SOURCE_SHA"
git -C /tmp/vibe-8086-source checkout --detach "$SOURCE_SHA"
git -C /tmp/vibe-8086-source rev-parse HEAD
SOURCE_DIR='/tmp/vibe-8086-source'
SAMPLE_DIR='/tmp/vibe-8086-samples'
python3 - "$SOURCE_DIR" "$SAMPLE_DIR" <<'PY'
import gzip
import json
import pathlib
import sys

source = pathlib.Path(sys.argv[1]) / 'v1'
target = pathlib.Path(sys.argv[2])
paths = sorted(source.glob('*.json.gz'))
if not paths:
    raise SystemExit('Brak źródłowych *.json.gz — przerwano bez generowania próbek')
# A1: wycina próbki do katalogu tymczasowego; A2 kopiuje tylko aktywne pliki manifestu.
for path in paths:
    with gzip.open(path, 'rt', encoding='utf-8') as stream:
        records = json.load(stream)
    if not isinstance(records, list) or not records:
        raise ValueError(f'Niepoprawny lub pusty zestaw: {path}')
    out = target / path.stem
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(records[:20], separators=(',', ':')) + '\n', encoding='utf-8')
PY
```

Skrypt obsługuje gzip i ładuje jeden pełny plik naraz; pamięć i limit rozmiaru zmierzyć w A1. `metadata.json` i licencję zachować osobno z tego samego SHA. Nazwy źródłowe (wielkie litery i rozszerzenia grup, np. `F7.7`) zachować w manifeście; nie stosować automatycznie konwencji 65x02. Przed uruchomieniem skryptu potwierdzić schemat JSON, przypadki pustych plików, pliki binarne/MOO i wybór opkodów manifestu. Wartość `SOURCE_SHA` wklejać z README wektorów. Końcowe `rev-parse` musi wypisać ten sam SHA; nie generować próbek z `main`.

### Budowanie i testy projektu

```bash
dotnet sln VibeEmulator.slnx add src/Intel8086/VibeEmulator.Intel8086.csproj
dotnet sln VibeEmulator.slnx add tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj
dotnet sln VibeEmulator.slnx add tools/Intel8086VectorRunner/VibeEmulator.Intel8086VectorRunner.csproj
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
```

### Pełny zestaw poza repo

```bash
git -C /tmp/vibe-8086-source fetch origin '<przypięty SHA>'
git -C /tmp/vibe-8086-source checkout --detach '<przypięty SHA>'
mkdir -p /tmp/vibe-8086-full
cp -a /tmp/vibe-8086-source/v1 /tmp/vibe-8086-full/v1
dotnet run --project tools/Intel8086VectorRunner/VibeEmulator.Intel8086VectorRunner.csproj -c Release -- \
  --vectors /tmp/vibe-8086-full/v1 --manifest tests/VibeEmulator.Intel8086.Tests/Vectors/manifest.json \
  --source-sha '<przypięty SHA>' --report /tmp/vibe-8086-full/report.json
```

Runner ma powstać w A2 i być rozszerzany z etapami: streaming JSON z `*.json.gz`, deterministyczny limit T-state na przypadek, licznik passed/skipped/mismatched/parse-error/timeout. Błędy, timeouty i niezgodności dają kod != 0; tryb zamknięcia A12 wymaga również braku pominiętych przypadków w deklarowanym zakresie. Pełne dane pobrać z przypiętego SHA do katalogu tymczasowego; raport zachować przed sprzątaniem danych. Nie uruchamiać tej komendy, dopóki runner i format danych nie istnieją.

### Definicja ukończenia repo

```bash
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

Po zmianie hooków dodatkowo `for t in .github/hooks/test-*.sh; do bash "$t"; done`; po zmianie serwera MCP dodatkowo `node tools/obsidian-mcp/test.js`. Dla rdzenia 8086 `OpcodeInfo --verify-code` nie zastępuje manifestu — w A2 należy dodać osobny test kompletności manifestu wobec przypiętego źródła.

## Dziennik

| data | etap/grupa | implementacja | wektory (przeszło/pominięto/niezgodne) | weryfikacja/uwagi |
|---|---|---|---|---|
| 2026-10-23 | A0–A12 | plan utworzony; implementacji brak | nie uruchomiono | Recenzja: roboczy SHA i format gzip sprawdzone; pełna inwentaryzacja rekordów, implementacja i wektory nieuruchomione |
