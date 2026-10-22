; Program testowy wydajności — dotyka każdego zoptymalizowanego elementu
; Elementy: C64Machine.Tick (cache _interruptLines, _port, bez Sid6581.Tick),
;           C64Bus.Read/Write (dekoder I/O/RAM/ROM, open bus),
;           ProcessorPort (flagi banków, Tick co cykl).
;
; Adres ładowania: $0200
; Hex (C64Trace): --hex "A9 37 8D 01 00 A9 2F 8D 00 00 AD 12 D0 8D 00 D4 AD 0D DC 8D 00 DD EE 00 03 4C 00 02" --org 0200

        * = $0200
start:
        LDA #$37        ; A9 37   ; port danych
        STA $01         ; 8D 01 00
        LDA #$2F        ; A9 2F   ; DDR
        STA $00         ; 8D 00 00
        LDA $D012       ; AD 12 D0 ; VIC read  -> C64Bus.Read (I/O)
        STA $D400       ; 8D 00 D4 ; SID write -> C64Bus.Write (I/O)
        LDA $DC0D       ; AD 0D DC ; CIA1 read
        STA $DD00       ; 8D 00 DD ; CIA2 write
        INC $0300       ; EE 00 03 ; RMW (read+write) RAM
        JMP start       ; 4C 00 02