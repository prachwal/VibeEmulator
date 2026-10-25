# A12 — audyt jawnych ograniczeń A10/A11

Stan na 2026-10-25. Ten audyt rozdziela zachowania sprawdzone od ograniczeń, których nie uznajemy za zgodne tylko dlatego, że pełny przypięty korpus SST przechodzi.

| Obszar | Co potwierdzają obecne testy / źródła | Ograniczenie pozostające po audycie |
|---|---|---|
| Wejście NMI/INTR | Manual Intela, tabela 4-6; testy A11 sprawdzają czas sumaryczny 50/61 taktów, stos/IVT, INTA i LOCK, priorytet oraz granice instrukcji. | Brak niezależnego pełnego śladu T-state wiążącego prefetch, odczyty IVT i trzy zapisy ramki. Nie deklarujemy dokładnej kolejności całej sekwencji wejścia. |
| HALT | Testy sprawdzają 2 takty instrukcji HLT, stan Halt, budzenie przez NMI/odmaskowane INTR oraz status `HALT` w Maximum Mode. Źródła: Intel 8086 Family User's Manual, tabela 2-21 i sekcja External Synchronization; datasheet, HALT. | Długi stan Halt i szczegółowa aktywność pinów/magistrali nie mają niezależnego śladu P80C86A-2. |
| WAIT po IRET | Manual Intela, sekcja WAIT/TEST; testy sprawdzają 3 takty WAIT, polling TEST co 5 taktów i adres WAIT zapisany podczas INTR. | Fizyczne pobranie/wznowienie po IRET zależy od hosta podającego kolejkę prefetch; automatyczny fetch po IRET nie jest dowiedziony. |
| READY i efekty uboczne | Manual/datasheet opisują READY przez zsynchronizowany układ 8284; test sprawdza Tw po T3, zatrzymanie dalszych faz i wznowienie. | Interfejs `IAddressBus` wykonuje odczyt/zapis w T3. READY nie cofa ani nie odkłada efektu ubocznego; zgodność z wolnymi urządzeniami mapowanymi w pamięci pozostaje niepotwierdzona. |
| RQ/GT | Datasheet i manual opisują arbitraż Maximum Mode. | Rdzeń nie udostępnia wejść RQ/GT ani oddawania magistrali; brak implementacji i testów. |
| Opóźnienie po zapisie segmentu | Test `POP SS` sprawdza odroczenie INTR do końca kolejnej instrukcji; manual opisuje także zapis MOV SS. | `MOV SS` nie jest obsługiwane; interakcja TF z POP SS i pozostałe zapisy segmentów nie są potwierdzone. Nie oznaczamy pełnej reguły jako zaimplementowanej. |
| Sekwencje prefiksów | `A10-prefix-sequence-decisions.json` audytuje 646 000 rekordów: 14 obecnych sekwencji, 580 par sekwencja/opcode; obsługiwane filtrowane kombinacje mają osobne raporty. Manual 8086 opisuje ograniczenie zachowania wielokrotnych prefiksów przy przerwanym stringu; referencja ASM86 zaznacza zależność od konkretnego procesora. | Przypięty korpus nie zawiera powtórzonych prefiksów, REP/REPNE przed segment override ani LOCK. Pozostałe kolejności wieloprefiksowe i ich cykle nie są dowodem zgodności. |

## Wynik

Otwarta praca „uzyskaj niezależne źródłowe ślady i rozstrzygnij wszystkie zachowania” nie jest zamknięta implementacyjnie. Zamknięty jest audyt: powyższe pozycje mają jawny status, zakres dowodów i powód pozostawienia luki. Nie wolno używać 100% korpusu SST jako twierdzenia o zgodności tych zachowań.

Źródła szczegółowe i testy: [A11 — reset, przerwania i stany](A11-reset-przerwania-i-stany.md), [decyzje sekwencji prefiksów A10](A10-prefix-sequence-decisions.json), [Intel 8086 Family User's Manual (1979)](https://www.inf.pucrs.br/~calazans/undergrad/orgcomp_EC/mat_microproc/intel-8086_family_Users_Manual.pdf) oraz [Intel 8086 datasheet](https://www.inf.pucrs.br/~calazans/undergrad/orgcomp_EC/mat_microproc/intel-8086_datasheet.pdf).
