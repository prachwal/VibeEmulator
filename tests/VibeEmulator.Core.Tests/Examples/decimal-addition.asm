; Ten sam program dla trzech wariantów. Początek $0200, wynik $0300.
SED
CLC
LDA #$09
ADC #$01
STA $0300
; Koniec $0209: harness zatrzymuje się na granicy instrukcji.
