# Etap A9 — instrukcje łańcuchowe i wejście/wyjście

Poziom: **II. Zestaw bazowy, część 7** · Zależność: A3/A5/A7

## Zakres

Łańcuchy: `MOVS`, `CMPS`, `STOS`, `LODS`, `SCAS` w wariantach byte/word; `REP`, `REPE/REPZ`, `REPNE/REPNZ`; I/O: `IN` i `OUT` przez immediate port oraz `DX`. `INS`/`OUTS` są wstępnie poza baseline 8086; potwierdzić ich brak w instrukcji 8086 i wprowadzenie w późniejszej rodzinie podczas A1. Ewentualny konflikt z wektorem wymaga udokumentowania, nie cichego rozszerzenia CPU.

## Kolejność

1. Zaimplementować pojedynczy transfer string byte (`MOVS`) i wskaźniki SI/DI/D direction flag.
2. Dodać warianty word oraz `STOS`/`LODS` z testami segmentów źródła i celu.
3. Dodać `CMPS`/`SCAS`, porównanie flags i warunek powtarzania.
4. Dodać `REP` jako sekwencję iteracji; `REPE/REPNE` sprawdzają flagę po iteracji zgodnie z fixture.
5. Wprowadzić przerwanie między iteracjami tylko w modelu linii A11; nie symulować go w pojedynczej instrukcji bez dowodu.
6. Wykorzystać istniejący [`IPortBus`](../../src/Memory/IPortBus.cs) (`Input/Output`, adres `ushort`, dane bajtowe) lub uzasadnić rozszerzenie dla transferów słownych; zdefiniować deterministyczny adapter zgodny z warstwami repo; `IN`/`OUT` immediate i przez DX.
7. Porównać cykle I/O, kierunek, adres portu, dane i stany queue.

## Kryteria akceptacji

- [ ] Testy pojedynczej iteracji i wielu iteracji, `CX=0/1/mała wartość`, DF w obu stanach.
- [ ] Testy REPE/REPNE dla stop condition true i false.
- [ ] RAM sprawdzony dla wszystkich zmienionych i chronionych adresów.
- [ ] Porty sprawdzane przez sekwencję odczytów/zapisów, bez globalnego stanu test runnera.
- [ ] Fixture `IN/OUT` nie jest zastępowane samym końcowym stanem rejestru.
- [ ] A9 ma jawny limit T-state w harnessie; długie źródłowe fixture nie są skracane.

## Pułapki

1. `CX=0` z REP i zachowanie IP muszą odpowiadać źródłu; nie wykonać iteracji „dla wygody”.
2. SI/DI zmieniają się zależnie od DF i rozmiaru operandu.
3. Segment źródła i docelowy są różne role; segment override nie powinien być stosowany bezwarunkowo do obu.
4. REP może być obserwowany jako wykonanie częściowe na przerwaniach; to osobny kontrakt w A11.
5. I/O space nie jest RAM-em i musi mieć jawny model zwracanych danych. README upstream wymaga 0xFF dla odczytów IN bez wait states; test transferu słownego i banków magistrali pozostaje osobną asercją.

Źródło uwag o zakresie SST: [README i metadane przypiętego upstream](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9); odczytano 2026-10-23.

## Dziennik postępu

### 2026-10-23 — adapter portów

Dodano `Intel8086PortBus` w warstwie Intel8086, bez zmiany neutralnego `IPortBus`: niezmapowane wejście zwraca `0xFF`, a podłączona magistrala przyjmuje `Input`/`Output`. Dwa testy adaptera przechodzą.

Rdzeń nie wykonuje jeszcze `IN`/`OUT`, nie zapisuje cykli I/O, a `MOVS/CMPS/STOS/LODS/SCAS` i REP nie są zaimplementowane. To jest adapter, nie weryfikacja opkodów A9. Pełne wektory nieuruchomione.

## Dziennik postępu

| data | rodzina | pass/manifest | próbkowe przypadki | uwagi |
|---|---|---|---|---|
| 2026-10-23 | adapter niezmapowanych portów | 2 testy adaptera | CPU runner A9: 0 | string/REP/IN/OUT i cykle otwarte; pełne wektory nieuruchomione |
