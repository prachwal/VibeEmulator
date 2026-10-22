# Licznik 0–100: assembler → bin → maszyna → diagnostyka

Przykłady dla [NMOS 6502](../tests/VibeEmulator.Core.Tests/Examples/nmos6502.asm),
[CMOS 65C02](../tests/VibeEmulator.Core.Tests/Examples/wdc65c02.asm) i
[Ricoh 2A03](../tests/VibeEmulator.Core.Tests/Examples/nes2a03.asm) używają wspólnego podzbioru instrukcji:

```asm
; Kod od $0200, licznik w $0300
LDA #$00
STA $0300
loop:
INC $0300
LDA $0300
CMP #$64
BNE loop
```

`$64` oznacza 100 dziesiętnie. Po wyjściu z pętli harness zatrzymuje wykonanie na granicy instrukcji
przy PC=$020F; kod nie zawiera instrukcji zatrzymującej procesor.

[Test E2E](../tests/VibeEmulator.Core.Tests/CounterEndToEndTests.cs) czyta osobny plik `.asm` dla każdego
wariantu, kompiluje go [minimalnym assemblerem testowym](../tests/VibeEmulator.Core.Tests/CounterAssembler.cs),
zapisuje prawdziwy plik `.bin` w katalogu tymczasowym i ponownie czyta go z dysku. Następnie buduje maszynę
CPU + płaski RAM 64 KiB + diagnostyka magistrali i uruchamia ją przez `Tick()` z limitem 2000 cykli.
Nie jest to pełna maszyna C64 ani NES. Pliki tymczasowe są sprzątane po teście.
Assembler obsługuje wyłącznie składnię przykładu (etykiety, immediate, absolute, BNE relative),
a opkody wybiera z tabeli wskazanego `CpuVariant`.

Uruchomienie z prezentacją diagnostyki:

```bash
dotnet test tests/VibeEmulator.Core.Tests -c Release --nologo \
  --filter FullyQualifiedName~CounterEndToEnd --logger 'console;verbosity=detailed'
```

Asercje i wynik przebiegu: binarny kod ma 15 bajtów (`A9008D0003EE0003AD0003C964D0F6`),
każdy wariant wykonuje 402 instrukcje w 1505 cyklach i kończy z `$0300 = 100`.
Diagnostyka wypisuje również pełną sekwencję zapisów: NMOS i 2A03 mają 201 zapisów
(RMW zapisuje starą i nową wartość), CMOS ma 101 zapisów. Test porównuje całą sekwencję,
w tym początkowe zero. Źródła oczekiwań: [tabele opkodów](cpu/opcodes.md),
kolumny cykli w [NMOS CSV](cpu/opcodes/nmos6502.csv) i [WDC CSV](cpu/opcodes/wdc65c02.csv)
oraz `CpuVariant.Rmw`. To test przykładowego programu; pełnych wektorów SingleStepTests nie uruchamia.

## Test rozróżniający wszystkie trzy CPU

[Program decimal-addition.asm](../tests/VibeEmulator.Core.Tests/Examples/decimal-addition.asm)
wykonuje `SED; CLC; LDA #$09; ADC #$01; STA $0300`.
[Test E2E](../tests/VibeEmulator.Core.Tests/CounterEndToEndTests.Decimal.cs) kompiluje ten sam plik
osobno dla każdego wariantu, zapisuje i ładuje `.bin`, a następnie mierzy cykle każdej instrukcji.

| Wariant | `$0300` | Cykle `ADC` | Cykle programu |
|---|---|---|---|
| NMOS 6502 | `$10` | 2 | 12 |
| CMOS 65C02 | `$10` | 3 | 13 |
| Ricoh 2A03 | `$0A` | 2 | 12 |

Oczekiwania wynikają z [trybu dziesiętnego](cpu/decimal-mode.md) i tabel opkodów podlinkowanych wyżej.
Test sprawdza także akumulator, pojedynczy zapis wyniku, ustawioną flagę D, końcowy PC=$0209
oraz jeden dostęp do magistrali na cykl. Powyższe polecenie uruchamia oba przykłady i wypisuje wyniki.
