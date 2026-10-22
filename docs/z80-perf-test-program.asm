; Program testowy wydajności rdzenia Z80.
; Benchmark tools/Z80PerfAudit uruchamia oba programy pod 0000h.

        ORG 0000h
loop_nop:
        NOP
        JP loop_nop

; Drugi workload jest osobnym obrazem, także ładowanym od 0000h.
        ORG 0000h
; Pętla rejestrowa (26 T-state):
; LD A,B / ADD A,C / INC D / XOR E / JP loop_alu
loop_alu:
        LD A,B
        ADD A,C
        INC D
        XOR E
        JP loop_alu
