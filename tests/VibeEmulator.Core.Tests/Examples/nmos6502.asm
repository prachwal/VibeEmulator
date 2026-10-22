; Początek $0200, licznik $0300. Wartość końcowa: 100 dziesiętnie ($64).
; Wspólny podzbiór instrukcji wszystkich trzech wariantów.
LDA #$00
STA $0300
loop:
INC $0300
LDA $0300
CMP #$64
BNE loop
; Koniec $020F: harness zatrzymuje się na granicy instrukcji.
