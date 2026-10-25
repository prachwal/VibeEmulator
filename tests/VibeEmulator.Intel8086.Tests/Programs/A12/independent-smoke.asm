; SPDX-License-Identifier: MIT
; Oryginalny program testowy przygotowany dla Vibe Emulator.
bits 16
org 0

start:
    mov ax, 2
    add ax, 3
    cmp ax, 5
    jne failure
    hlt

failure:
    mov ax, 0xDEAD
    hlt
