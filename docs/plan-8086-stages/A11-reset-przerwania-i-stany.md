# Etap A11 — reset, przerwania i stany procesora

Poziom: **IV. Zachowanie procesora poza pojedynczą instrukcją** · Wektory SST: ograniczone

Wektory SingleStepTests/8086 nie ćwiczą wszystkich wejść asynchronicznych. A11 wymaga osobnych scenariuszy i dokumentacji producenta; status „brak mismatchów SST” nie jest dowodem dla resetu, INTR, NMI ani TF.

## Zakres

- RESET i stan startowy: wektor resetu, rejestry, kolejka oraz pierwszy fetch.
- `INTR`, `NMI`, `INTA`, `IF`, `TF` i granice przyjęcia sygnału.
- `HLT`, wyjście z HLT przez reset/przerwanie, pin/status bus.
- `WAIT`/`TEST` w modelu 8086 i połączenie z coprocessorem, wyłącznie zakresie zweryfikowanym.
- READY/wait states i arbitraż magistrali tylko po wskazaniu wejść/wyjść właściwych dla Maximum Mode w dokumentacji producenta. Nie przejmować HOLD/HLDA z innego trybu bez weryfikacji.
- REP restart/resume w kontekście przerwań, jeśli źródła określają dokładną architekturę.

README upstream jawnie wyklucza `WAIT` (9B) i `HLT` (F4); nie czekać na ich pliki podczas audytu A10. Obie instrukcje wymagają źródeł producenta i osobnych scenariuszy; dla HLT źródła cyklu i wyjścia ze stanu halt są już zebrane poniżej.

## Plan testów

1. Zbudować scenariusz deterministyczny, który ustawia linie wejściowe przez jawny interfejs; bez czasu ściennego.
2. Każdy test opisuje cykl asercji/zwolnienia sygnału, oczekiwany cykl akceptacji i źródło tej granicy.
3. IRQ: test IF clear/set, wejście po instrukcji, INTA i frame na stosie.
4. NMI: test zbocza/warunku aktywacji według dokumentacji, niezależny od IF.
5. Granice po STI i zapisie SS: scenariusze odroczenia przerwań i interakcji TF. Reguła STI jest testowana. Manual 8086 określa opóźnienie także po dowolnym MOV do rejestru segmentowego i POP rejestru segmentowego, ale wymagane instrukcje MOV/POP segmentu nie są jeszcze zaimplementowane w zakresie A5.
6. TF: pojedynczy krok i stacking state po instrukcji; oddzielić od zwykłego wektora `INT`.
7. HLT: test utrzymania stanu i każdego udokumentowanego sposobu wznowienia.
8. RESET: asercja w różnych fazach, jeśli manual określa; test resetu symulatora nie jest automatycznie równoznaczny z resetem pinowym.
9. READY/arbitraż/WAIT: włączać tylko po rozstrzygnięciu ich wymagań oraz obserwowalnych pinów dla modelu Maximum Mode.

## Testy i kryteria

- [x] Źródła dla RESET odczytane: tabela 2-4 manuala Intela ustala `CS:IP=FFFF:0000`, `DS/SS/ES=0000`, `FLAGS=0000`; wartości AX/BX/CX/DX/SP/BP/SI/DI są nieokreślone.
- [x] `ResetHardware()` ustawia udokumentowany stan, opróżnia kolejkę i kasuje cały stan faz wewnętrznych; test obejmuje reset w trakcie instrukcji z aktywnym LOCK. To nie emuluje impulsu pinowego ani pierwszego fetchu.
- [x] Wejście `ResetLevel` resetuje rdzeń i przerywa bieżącą instrukcję, pozostaje w stanie resetu przy poziomie HIGH, a po impulsie 5 zegarów (spełniającym datasheetowe „greater than 4”) wykonuje modelowaną 10-zegarową sekwencję i wystawia pierwszy fetch `CODE/T1` spod FFFF0h. Test weryfikuje T1–T4 i słowo z magistrali.
- [x] NMI: dodatnie zbocze zatrzaśnięte, przyjęcie po instrukcji niezależnie od IF, typ 2, stos FLAGS/CS/IP i brak INTA; test sprawdza stan i transakcje pamięci.
- [x] Priorytet NMI > INTR sprawdzony przy równoczesnym, aktywnym żądaniu obu linii.
- [x] TF: typ 1 po pojedynczej instrukcji, stacking FLAGS/CS/IP, brak INTA oraz pierwsza instrukcja procedury trap bez natychmiastowego ponowienia.
- [x] TF przy równoczesnym NMI: NMI wchodzi pierwsze, a type 1 przed pierwszą instrukcją handlera NMI.
- [x] TF przywrócone przez IRET powoduje trap po kolejnej instrukcji programu; test jawnie ustawia kolejkę wejściową, bo automat prefetch nie pobiera jeszcze rzeczywistych bajtów z magistrali.
- [x] INTR: poziomowe żądanie przyjmowane tylko przy IF=1, typ przekazany przez kontroler podczas T3 drugiego INTA, dwa potwierdzenia i okno LOCK Maximum Mode; test porównuje pełną sekwencję T1–T4 obu INTA, trzy idle T-state, dane wektora i granice LOCK z fig. 4-19.
- [x] STI opóźnia przyjęcie oczekującego INTR do zakończenia następnej instrukcji; test `STI; NOP` potwierdza brak INTA podczas obu instrukcji i start INTR dopiero potem.
- [x] HLT: F4 zużywa 2 zegary, zwiększa IP i wchodzi w stan Halt; zamaskowane INTR go nie budzi, NMI i włączone INTR przyjmują przerwanie od razu po wznowieniu, a `ResetHardware()` kasuje stan Halt. W Maximum Mode wejściowy bus status jest `HALT` z impulsem ALE.
- [x] WAIT/TEST: 9B kosztuje 3 zegary przed oczekiwaniem; TEST niski przy sprawdzeniu pozwala kontynuować, stan wysoki jest nieaktywny i model ponawia próbkę co 5 zegarów bez transakcji magistralnej. INTR podczas oczekiwania zapisuje do ramki adres samego WAIT; testy pokrywają obie wartości TEST, próbkowanie i wznowienie architektoniczne.
- [x] READY: READY HIGH oznacza gotowość; LOW próbkowane w T2 wstawia co najmniej jeden `Tw` po T3, a kolejne `Tw` trwają do READY HIGH. Test potwierdza utrzymanie T-state/pending i wznowienie transferu.
- [x] Testy scenariuszy przerwań obejmują frame na stosie, IVT, IF/TF, kolejkę i obserwowalne stany magistrali; nie zastępuje to niezależnego śladu całego wejścia.
- [x] Niezaimplementowane lub niepotwierdzone zachowania są wyszczególnione jako otwarte kryteria i ograniczenia checkpointu.
- [x] Linie w testach są ustawiane jawnie i deterministycznie, bez zależności od czasu ściennego.
- [x] Poziom, zbocze i próbkowanie modelowanych wejść są opisane w tabeli kontraktu sygnałów poniżej; brakujące linie READY/arbitrażu są jawnie poza implementacją.
- [x] Wspólny kontrakt `IInterruptLines` nie otrzymał nowych pól; jego semantyka jest neutralna polaryzacyjnie, a cały zestaw regresyjny został uruchomiony.
- [x] Czas sumaryczny NMI wynosi 50 zegarów, a INTR 61 zegarów po rozpoznaniu, zgodnie z tabelą 4-6; testy asertują oba czasy, bez czasu kończenia przerwanej instrukcji.
- [ ] Pełna kolejność każdego T-state wejścia NMI/INTR (wektor, stos, prefetch) wymaga niezależnego śladu; zweryfikowano czas sumaryczny i fazy INTA/LOCK, nie każdy cykl magistrali.
- [x] Każdy modelowany sygnał ma opis poziomu/zbocza, próbkowania i źródła w tabeli kontraktu wejść; READY/arbitraż są jawnie niezaimplementowane.
- [x] Scenariusze testowe obejmują stos, IVT, IF/TF, kolejkę i stany magistrali; nie dowodzą pełnego przebiegu cyklowego przerwań.
- [x] Brakujące źródła i ślady są oznaczone jako niezweryfikowane, bez deklaracji pełnej zgodności.
- [x] Linie zewnętrzne w testach zmieniają się tylko w jawnych punktach scenariusza.
- [x] Wspólny kontrakt `IInterruptLines` nie został rozszerzony; pełny zestaw regresyjny jest zielony.

### Kontrakt wejść A11

| wejście | polaryzacja / zdarzenie | próbkowanie w modelu | źródło i ograniczenie |
|---|---|---|---|
| `ResetLevel` | HIGH aktywne | raz na `Tick`; legalny test utrzymuje HIGH przez 5 taktów, po zwolnieniu modeluje 10 taktów sekwencji wewnętrznej | Intel 8086 datasheet, RESET HIGH `>4 CLK`, synchronizacja wewnętrzna, ok. 10 CLK po zwolnieniu; power-on 50 μs poza modelem |
| `NmiLevel` | zbocze LOW→HIGH; min. impuls HIGH 2 takty | próbka raz na `Tick`, zbocze zostaje zatrzaśnięte | Intel 8086 Family User's Manual, sekcje 2-22–2-29; wąskie impulsy poniżej dwóch taktów nie są filtrowane |
| `IrqLevel` | HIGH aktywne, poziomowe; akceptacja tylko IF=1 | obserwowane co `Tick`, przyjęcie na granicy instrukcji; typ z `IrqVector` próbkowany na drugim INTA T3 | Intel 8086 Family User's Manual, sekcje 2-22–2-29; zewnętrzny kontroler INTA nie jest osobnym urządzeniem modelu |
| `TestLevel` | HIGH nieaktywne; LOW zwalnia WAIT | sprawdzenie na wejściu do WAIT i ponownie co 5 taktów oczekiwania | Intel 8086 Family User's Manual, „WAIT and TEST”; minimalne utrzymanie aktywnego TEST przez 5 CLK nie jest analogowo weryfikowane |
| `ReadyLevel` | HIGH gotowe; LOW żąda wait state | próbkowane w T2 transferu; minimum jeden `Tw` po T3 i dalsze `Tw` do HIGH | Intel 8086 datasheet i Family User's Manual, układ 8284 synchronizuje READY; test modeluje już zsynchronizowany poziom. T-transfer magistrali jest widoczny, ale side-effect `IAddressBus` następuje w fazie T3 i nie jest odraczany przez Tw. |
| RQ/GT | brak publicznego wejścia | niepróbkowane | arbitraż Maximum Mode pozostaje niezaimplementowany do zebrania źródeł i testów |

## Pułapki

1. Instrukcja `INT n` to nie to samo co asynchroniczne `INTR`.
2. NMI/IRQ mogą zależeć od granicy instrukcji i bieżącego fetch/prefetch; nie przyjmować „natychmiast”.
3. Kształt interrupt frame i stos trzeba ustalić z manuala i potwierdzić independently.
4. WAIT bez zewnętrznego coprocessora może wymagać modelu wejścia TEST; nie zastępować go pustym NOP-em.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

| data | sygnał/scenariusz | testy | źródło | status |
|---|---|---|---|---|
| 2026-10-24 | sprzętowy RESET: pin, przerwanie aktywnej instrukcji, wymagany impuls i pierwszy fetch | `ResetHardware_SetsDocumentedResetVectorAndClearsQueue`, `ResetHardware_ClearsInFlightInstructionAndLockSignal`, `ResetPin_FiveClockPulseAbortsInstructionAndFetchesFromResetVector`; 3/3 | Intel 8086 Family User's Manual, Oct. 1979, tabela 2-4; Intel 8086 datasheet, „Processor Reset and Initialization” | stan architektoniczny, 5 taktów RESET HIGH (datasheet: >4), 10 idle po zwolnieniu oraz fetch T1–T4 od FFFF0h sprawdzone. 10 taktów jest deterministyczną interpretacją opisanego jako „approximately” czasu; krótszy impuls i opóźnienie power-on 50 μs pozostają poza zakresem. |
| 2026-10-24 | NMI: zbocze dodatnie, typ 2, frame, IVT, parytet stosu i czas | `Nmi_RisingEdgeIsServicedAfterInstructionAndUsesTypeTwoVector`; testuje FLAGS/CS/IP, IF/TF, brak INTA oraz 50 taktów dla stosu parzystego i 62 dla nieparzystego | Intel 8086 Family User's Manual, sekcje 2-22–2-29, tabela 4-6; NMI wymaga impulsu ≥2 zegary i nie używa INTA; słowo pod nieparzystym adresem kosztuje +4 takty | stan architektoniczny, czas sumaryczny i kara trzech nieparzystych zapisów słowa sprawdzone; pełna kolejność T-state nie ma niezależnego śladu. Impulsy krótsze niż 2 takty nie są filtrowane analogowo. |
| 2026-10-24 | INTR: poziom, bramka IF, dwa INTA, typ, stos/IVT, parytet i czas | `Intr_WhenIfIsClearDoesNotStartAcknowledge`, `Intr_WhenIfIsSetUsesTwoAcknowledgeCyclesAndExternalVector`; pełna sekwencja T-state obu INTA, trzech idle T-state, wektora i LOCK jest sprawdzana w teście | Intel 8086 Family User's Manual, tabela 4-6 i fig. 4-19; LOCK od T2 pierwszego do T2 drugiego INTA; nieparzysty word transfer +4 takty | testowany trace obejmuje cały INTA i okno LOCK, a także odczyty obu słów IVT oraz sumaryczny timing. Pełna niezależna sekwencja pozostałych faz wejścia (prefetch i trzy zapisy ramki w relacji do odczytów IVT) nadal wymaga źródłowego śladu. |
| 2026-10-24 | priorytet NMI przed INTR przy jednoczesnym zgłoszeniu | `Nmi_HasPriorityOverIntr` potwierdza wektor typu 2 i brak cyklu INTA | Intel 8086 Family User's Manual, Oct. 1979, sekcje 2-22–2-29, tabela priorytetów | zgodność architektoniczna sprawdzona; test nie obejmuje wyścigu zboczy wewnątrz jednego zegara. |
| 2026-10-24 | HLT: czas instrukcji, utrzymanie Halt, warunki wznowienia i status Maximum Mode | `Hlt_ConsumesTwoCyclesAndWaitsForUnmaskedInterrupt`, `Hlt_EnabledIntrWakesAndUsesExternalVector`, `ResetHardware_ClearsHaltedState`; wektory SST HLT nie istnieją | Intel 8086 Family User's Manual, tabela 2-21 i sekcje „External Synchronization”; Intel 8086 datasheet, HALT | 2 zegary instrukcji i zachowanie architektoniczne potwierdzone testami. Model utrzymuje `HALT` podczas kolejnych taktów; szczegółowa długość/status magistrali w HALT nie ma śladu P80C86A-2 i pozostaje niezweryfikowana. |
| 2026-10-24 | TF/single-step: typ 1 po instrukcji, kolejność względem NMI i opóźnienie po IRET | `TrapFlag_SingleStepsAfterInstructionAndStacksTypeOneFrame`, `TrapFlag_WhenNmiArrivesEntersNmiThenTrapBeforeHandlerInstruction`, `TrapFlag_RestoredByIretTrapsAfterFollowingProgramInstruction`; 3/3 | Intel 8086 Family User's Manual, Oct. 1979, sekcja 2-28; dane `INT1` i IRET przygotowane jawnie w kolejce, bez uznawania tego za test fizycznego fetchu | architektoniczna granica TF i ramka type 1 sprawdzone. POPF, opóźnienie przerwań po STI oraz reguły MOV/POP SS nie są jeszcze zaimplementowane/zweryfikowane. |
| 2026-10-24 | WAIT/TEST: bazowe 3 zegary, okres próbkowania TEST co 5 zegarów oraz INTR podczas wait | `Wait_WhenTestIsInactivePollsEveryFiveClocksAndResumes`, `Wait_WhenTestIsActiveAtInstructionCheckCompletesInThreeClocks`, `Wait_EnabledIntrDuringWaitSavesWaitInstructionAddress`; 3/3 | Intel 8086 Family User's Manual, Oct. 1979, sekcja „WAIT and TEST”; stan HIGH oznacza TEST nieaktywny, oczekiwanie odpytuje linię co 5 zegarów, przerwanie podczas wait zapisuje adres WAIT | logika oczekiwania i ramka powrotu sprawdzone. Odtwarzanie instrukcji po IRET zależy od kolejki dostarczonej przez system i nie jest jeszcze śladem fizycznego fetchu; minimalne utrzymanie TEST aktywnego przez 5 zegarów pozostaje warunkiem środowiska, nie analogowym filtrem w rdzeniu. |
| 2026-10-24 | READY/wait state: próbkowanie LOW w T2, co najmniej jeden Tw po T3 oraz trzymanie Tw przy niskim poziomie | `Ready_LowInsertsTwStatesAfterMemoryReadT3UntilReadyReturnsHigh`; test potwierdza wstrzymanie Pending, kolejne Tw i wznowienie | Intel 8086 datasheet, „Basic System Timing”; Family User's Manual, fig. 4-13; układ 8284 synchronizuje READY, urządzenie może wymusić wait state | podstawowy generator Tw sprawdzony. Model nie odkłada side-effect `IAddressBus` z T3 do READY HIGH, więc wynik nie jest dowodem dla pamięci/urządzeń o efektach odczytu/zapisu ani arbitrażu RQ/GT. |
| 2026-10-24 | INTR po STI: oczekujący poziom jest obsłużony po kolejnej instrukcji | `Intr_PendingAtStiIsDeferredUntilFollowingInstructionCompletes`; `STI; NOP`, brak INTA podczas obu instrukcji i przyjęcie na następnej granicy | Intel 8086 Family User's Manual, Oct. 1979, sekcja STI i 2-24 | reguła odroczenia STI sprawdzona; NMI i programowe przerwania nie są przez nią maskowane. Manual wymaga analogicznego odroczenia po MOV do segmentu i POP segmentu; brak tych opkodów w rdzeniu jest otwartą zależnością A5, więc nie oznaczamy tego zachowania jako zaimplementowanego. |
| 2026-10-24 | kontrola A11 po odtworzeniu dostępu do hosta .NET | Release build 0 ostrzeżeń/błędów; Debug build 0/0; pełny Release 13 536/13 536; formatter verify OK; OpcodeInfo 512/512; `git diff --check` OK | polecenia wymagane przez AGENTS.md i `.agents/skills/standardy-projektu/kodowanie.md` | wszystkie bramki repozytorium przeszły. Nie zamyka to otwartego kryterium niezależnej kolejności T-state wejścia NMI/INTR ani zależności MOV/POP segmentu. |
| 2026-10-24 | port A11 na aktualny podział plików rdzenia, po bazie A10 `c5de77a` | Debug/Release build 0 ostrzeżeń i błędów; pełny Release 13 546/13 546; Intel8086 10 746/10 746; formatter verify OK; OpcodeInfo 512/512; `git diff --check` OK | plan A11 i kontrakt `IInterruptLines`; testy scenariuszy linii przerwań i resetu | bramki bieżącego drzewa są zielone. Nie zamyka to otwartego kryterium niezależnej kolejności T-state wejścia NMI/INTR ani zależności MOV/POP segmentu. |

### Ograniczenia checkpointu A11

- Generator 8284 i opóźnienie power-on RESET 50 μs nie są modelowane; długość wewnętrznej sekwencji po zwolnieniu jest przybliżona w datasheet i przyjęta deterministycznie jako 10 zegarów.
- NMI ma przebieg architektoniczny z harmonogramem faz współdzielonym z programowym `INT`, a INTR dodatkowo dwa INTA; sumaryczne czasy 50/61 zgadzają się z tabelą 4-6, ale pełna kolejność T-state nie ma niezależnego śladu. HLT ma cykl wejścia i wznowienie architektoniczne, ale stany magistrali w długim Halt nie mają niezależnego śladu. WAIT/TEST ma deterministyczny model linii i okresu próbkowania, ale nie implementuje czasu minimalnego impulsu TEST ani fizycznego retrigger/fetch WAIT po IRET. READY wstawia Tw i blokuje progresję T-state, ale operacje `IAddressBus` nie są odraczane do READY HIGH; pełna zgodność z urządzeniami o efektach ubocznych nie jest potwierdzona. Brak arbitrażu RQ/GT i opóźnienia po MOV/POP SS. TF nie jest weryfikowany wektorami SST; scenariusze ręczne obejmują typ 1, priorytet NMI oraz powrót IRET, lecz nie POPF.
- A11 pozostaje otwarty; brakujące zachowania nie mają wektorów aktywnych i wymagają kolejnych prac nad cyklowym rdzeniem.
