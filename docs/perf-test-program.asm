; Program testowy wydajności (C64, 6510)
; Pętla NOP/JMP używana do pomiaru kosztu klatki.
; Adres ładowania: $0200
; Odpowiednik hex dla C64Trace: --hex "EA 4C 00 02" --org 0200

        * = $0200
loop:
        NOP             ; EA
        JMP loop        ; 4C 00 02
EOF