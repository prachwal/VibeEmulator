# Etap A0 — decyzje architektoniczne

Poziom: **fundament** · Opkody: **0** · Wektory: **0**

Stan kontraktów sprawdzono w [`ICpuCore.cs`](../../src/Cpu.Abstractions/ICpuCore.cs), [`IInterruptLines.cs`](../../src/Cpu.Abstractions/IInterruptLines.cs) i [`Memory.cs`](../../src/Memory/Memory.cs). Wspólny `IInterruptLines` opisuje aktywne poziomy niskie i zbocze NMI; zgodność z wejściami 8086 wymaga jawnego mapowania zamiast przejęcia komentarzy jako specyfikacji.

A0 nie implementuje CPU. Ustala granice projektu i kontrakty, które inaczej wymuszą kosztowne zmiany po dodaniu kolejki prefetch i T-state.

## Zakres i kolejność

1. Przeczytać `ICpuCore<TState>`, `IInterruptLines`, `IMemoryBus`, rdzenie Z80 i Mos6502 oraz sposób dodawania projektów do `.slnx`.
2. Sporządzić diagram zależności: `Intel8086` zależy wyłącznie od `Memory` i `Cpu.Abstractions`; bez odwołań do `Core`, `Mos6502` i `Z80`.
3. Ustalić publiczne API stanu (import/eksport dla wektorów), resetu, cyklu i przerwań.
4. Zdecydować semantykę `Tick()` i `Pending` wstępnie przed A2, następnie potwierdzić prototypem NOP w A2: `Tick` musi dać obserwowalny postęp w każdym T-state, a `Pending` mieć precyzyjnie opisaną relację do następnej granicy instrukcji.
5. Ustalić, czy cykl magistrali/T-state jest zwracany jako immutable snapshot, czy udostępniany jako właściwości rdzenia; wybrać model bez alokacji.
6. Opisać stan resetowany i stan wektora inicjalizowanego testowo osobno; harness nie może symulować resetu sprzętowego, jeżeli rekord startuje z zadanym `CS:IP` i kolejką.
7. Zdefiniować źródła prawdy i listę pytań do A1: model segment:offset, zawijanie 20-bit, A0/BHE, maksymalny rozmiar kolejki, fetch i sygnały Maximum Mode.

## Decyzje A0

| temat | decyzja A0 | potwierdzenie / test w A2 |
|---|---|---|
| kontrakt rdzenia | `Intel8086Core` implementuje `ICpuCore<Intel8086State>`. `InstructionPc` zwraca offset `IP` początku instrukcji; konkretny rdzeń udostępnia dodatkowo `InstructionCs`, więc para identyfikująca instrukcję to `CS:InstructionPc`. `Reset(ushort pc)` jest resetem emulowanego rdzenia do stanu początkowego kontraktu z `CS=0`, `IP=pc`; właściwy pinowy reset 8086 otrzyma odrębne API `ResetHardware()` w A11. Nie rozszerzać `ICpuCore` w A0. | Test w A2 asercjonuje reset programowy, CS:IP i `InstructionCs`; A11 porówna `ResetHardware` ze źródłem producenta. |
| czas | Jeden `Tick()` wykonuje dokładnie jeden T-state; `Cycles` liczy T-state. `Pending` oznacza liczbę T-state do zakończenia aktualnej instrukcji i wynosi 0 na granicy instrukcji. Oczekujące READY/wait-state zwiększają czas instrukcji i muszą być widoczne; dokładna aktualizacja pending przy prefetch potwierdzona prototypem. | A2 asercjonuje `Cycles` i wszystkie pola cyklu po każdym Tick oraz brak fikcyjnego stałego `Pending`. |
| magistrala | Dodać neutralny, rozszerzony kontrakt adresowy w `Memory` przyjmujący fizyczny adres jako `uint`; zachować bez zmian `IMemoryBus` z `ushort`. W A2 dodać płaską pamięć 1 MiB opartą o ten kontrakt. Rdzeń emituje cykle/banki zgodnie z pełnym źródłowym rekordem; mapowania sygnałów zostaną ustalone w A1 z `metadata.json` i cykli. | Testy Memory dla adresów `0x00000`, `0x0FFFF`, `0x10000`, `0xFFFFF` i odrzucenia spoza zakresu; testy A2 porównają surowe cykle. |
| kolejka | Jawny stan kolejki w `Intel8086State` (bajty, head/count lub ekwiwalent bez alokacji) i odrębne wyjścia QS w snapshotie cyklu. Loader może seedować queue dokładnie z wektora; dekoder pobiera wyłącznie przez kolejkę, fetch napełnia ją przez magistralę. Długość/kolejność/flush nie ustalane z pamięci, czekają na A1/manual. | Testy A2: pełna kolejka, odczyt pierwszego bajtu, napełnianie po miejscu zwolnionym i flush przy NOP boundary zgodny z fixture. |
| wyjścia cyklu | Rdzeń udostępnia `readonly` snapshot strukturalny `LastBusCycle` (bez alokacji) po każdym `Tick`, z surowymi polami reprezentowanymi w wektorze. Comparator ignoruje pole wyłącznie, gdy upstream jawnie oznacza je jako nieistotne. | Fixture NOP porównuje każde pole snapshotu i test alokacji mierzy ścieżkę `Tick`. |
| pamięć i porty | Pamięć fizyczna używa nowego neutralnego kontraktu `uint`; I/O pozostaje osobną przestrzenią przez generyczny kontrakt portów w `Memory` (sprawdzić `IPortBus` przed użyciem). Nie zakładać portu jako adresu RAM. | Test doubles asercjonują osobno memory accesses i I/O accesses, bez zależności od `Core`. |
| przerwania | Nie implementować `IInterruptLines` bez mapowania elektrycznego: jego komentarze opisują aktywny niski IRQ i opadające NMI, co nie jest kontraktem do bezpośredniego przypisania na 8086. A11 dostanie wejścia specyficzne 8086, jeśli analiza manuala potwierdzi potrzebne sygnały. | A0/A2 nie zmieniają kontraktu; osobne deterministyczne testy linii w A11. |
| wariant | Zakres pojedynczego rdzenia to Intel P80C86A-2 w Maximum Mode, zgodnie z opisem [upstream README](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9). Nie nazywać go emulatorem ogólnej rodziny x86 ani 8088. | Manifest, metadata i dokumentacja danych w A1 potwierdzają przypięty model/tryb. |

## Kryteria akceptacji

- [x] Tabela decyzji jest wypełniona z uzasadnieniem i testem potwierdzającym; kwestia zależna od wektorów/manuala jawnie przekazana A1/A2/A11.
- [x] Rozbieżności `ICpuCore` (`ushort InstructionPc`, `Reset(ushort)`) i `IMemoryBus` (16-bit address) mają rozwiązanie bez zmiany istniejących kontraktów.
- [x] Zapisano granice poza zakresem: 8088, Minimum Mode, 80186+, 80286+, 8087 i maszyny docelowe.
- [x] Otwarte pytania mają etap i kryterium zamknięcia; A2 nie może zatwierdzić kontraktów bez testów prototypu.

## Pułapki

1. Nie utożsamiać granicy instrukcji z wykonaniem jej wszystkich T-state ani z pojedynczym dostępem do RAM.
2. Nie pozwolić, by loader wektorów ustalał semantykę CPU: loader tylko mapuje pola JSON na jawny stan.
3. Nie projektować od razu wspólnej abstrakcji CPU dla niepowiązanych architektur.
4. Nie deklarować cyklowej zgodności, zanim zdefiniowano które piny i momenty są porównywane.

## Dziennik postępu

| data | decyzja | wynik | źródło / test |
|---|---|---|---|
| 2026-10-23 | kontrakty rdzenia, adresy, czas, kolejka i linie | A0 zakończony jako decyzje; kod CPU celowo nie dodano | `ICpuCore`, `IInterruptLines`, `Memory.cs`, `IPortBus.cs`, `Z80Core`, `Z80State`; prototypy i asercje przypisane do A2/A11 |
