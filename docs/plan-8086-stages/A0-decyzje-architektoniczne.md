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

## Decyzje wymagane przed A2

| temat | decyzja do zapisania | kryterium |
|---|---|---|
| kontrakt rdzenia | rozstrzygnąć dokładny `Pending`, `ushort InstructionPc`, `Reset(ushort)` i rozdzielenie `CS:IP`; wybrać zgodny adapter lub osobny/rozszerzony kontrakt | test kontraktowy i brak zmian API wspólnego bez uzasadnienia |
| czas | zdefiniować jeden `Tick` jako najmniejszy krok obserwowany przez runner | asercja czasu i wyjść po każdym kroku |
| magistrala | wybrać kontrakt adresów 20-bitowych (`IMemoryBus` ma `ushort`), transakcje słowo/bajt i mapowanie pinów | porównywalność z cyklami upstream |
| kolejka | jawny typ danych, stan indeksów i kolejność pobrania | możliwość seedowania i porównania `initial/final.queue` |
| przerwania | osobny interfejs wejść zewnętrznych; nie mieszać linii w stanie rejestrów | testy deterministyczne, A11 |
| wariant | dokładnie Intel P80C86A-2, Maximum Mode, bez generycznego modelu rodziny | taki zakres opisuje README SingleStepTests/8086 |

## Kryteria akceptacji

- [ ] Tabela decyzji powyżej jest uzupełniona o wstępne rozwiązanie, źródło i plan testu/prototypu A2; rozstrzygnięcia zależne od A1 wracają do tej tabeli przed implementacją.
- [ ] Każda sprzeczność z kontraktami `ICpuCore` i `IMemoryBus` ma wybraną ścieżkę rozwiązania; potwierdzenie prototypem A2 blokuje rozpoczęcie A3.
- [ ] Zapisano granice poza zakresem: 8088, Minimum Mode, 80186+, 80286+, 8087 i maszyny docelowe.
- [ ] Otwarta pozostaje tylko kwestia, której nie da się rozstrzygnąć bez inwentaryzacji A1; ma właściciela i kryterium zamknięcia.

## Pułapki

1. Nie utożsamiać granicy instrukcji z wykonaniem jej wszystkich T-state ani z pojedynczym dostępem do RAM.
2. Nie pozwolić, by loader wektorów ustalał semantykę CPU: loader tylko mapuje pola JSON na jawny stan.
3. Nie projektować od razu wspólnej abstrakcji CPU dla niepowiązanych architektur.
4. Nie deklarować cyklowej zgodności, zanim zdefiniowano które piny i momenty są porównywane.

## Dziennik postępu

| data | decyzja | wynik | źródło / test |
|---|---|---|---|
| — | — | plan; implementacji brak | wypełnić podczas A0 |
