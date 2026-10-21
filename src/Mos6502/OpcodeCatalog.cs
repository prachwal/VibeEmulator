namespace VibeEmulator.Mos6502;

/// <summary>
/// Katalog zarejestrowanych opkodów: jedno pole z adnotacją <see cref="OpcodeAttribute"/> na opkod.
/// Tabele wariantów (<see cref="CpuVariants"/>) składają się dokładnie z tych pól, więc adnotacja
/// i wpis tabeli opisują ten sam opkod. Narzędzie <c>tools/OpcodeInfo --verify-code</c> czyta
/// adnotacje refleksją i porównuje je z <c>docs/cpu/opcodes/*.csv</c>.
/// </summary>
/// <remarks>
/// Każde nowe pole musi mieć adnotację i musi trafić do tablicy <c>Opcodes</c>/<c>Additions</c>;
/// zgodność pilnuje <c>OpcodeCatalogTests</c>. Adnotacja nie jest czytana przez <c>Tick()</c>.
/// <para>
/// Pole i adnotacja powtarzają te same wartości (atrybut wymaga stałych), więc każdy opkod jest zapisany dwa razy.
/// Przy 63 opkodach koszt jest akceptowalny; po przekroczeniu ~80 wpisów usuń duplikację, budując tablicę
/// z adnotacji raz w inicjalizatorze statycznym (albo porzucając adnotację na rzecz testu porównującego
/// <see cref="CpuVariants"/> z CSV). Do tego czasu nie dodawaj trzeciej kopii danych.
/// </para>
/// </remarks>
public static class OpcodeCatalog
{
    /// <summary>Opkody NMOS 6502/6510 (zestaw `nmos`).</summary>
    public static class Nmos
    {
        [Opcode(0xA9, CpuSet.Nmos, "LDA", AddressingMode.Immediate, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaImmediate = new(0xA9, "LDA", AddressingMode.Immediate, OpKind.Read, CpuAction.Lda);

        [Opcode(0xA5, CpuSet.Nmos, "LDA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaZeroPage = new(0xA5, "LDA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lda);

        [Opcode(0xB5, CpuSet.Nmos, "LDA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaZeroPageX = new(0xB5, "LDA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Lda);

        [Opcode(0xAD, CpuSet.Nmos, "LDA", AddressingMode.Absolute, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaAbsolute = new(0xAD, "LDA", AddressingMode.Absolute, OpKind.Read, CpuAction.Lda);

        [Opcode(0xBD, CpuSet.Nmos, "LDA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaAbsoluteX = new(0xBD, "LDA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Lda);

        [Opcode(0xB9, CpuSet.Nmos, "LDA", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaAbsoluteY = new(0xB9, "LDA", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Lda);

        [Opcode(0xA1, CpuSet.Nmos, "LDA", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaIndexedIndirect = new(0xA1, "LDA", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Lda);

        [Opcode(0xB1, CpuSet.Nmos, "LDA", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaIndirectIndexed = new(0xB1, "LDA", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Lda);

        [Opcode(0xA2, CpuSet.Nmos, "LDX", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldx)]
        public static readonly CpuOpcode LdxImmediate = new(0xA2, "LDX", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldx);

        [Opcode(0xA6, CpuSet.Nmos, "LDX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldx)]
        public static readonly CpuOpcode LdxZeroPage = new(0xA6, "LDX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldx);

        [Opcode(0xB6, CpuSet.Nmos, "LDX", AddressingMode.ZeroPageY, OpKind.Read, CpuAction.Ldx)]
        public static readonly CpuOpcode LdxZeroPageY = new(0xB6, "LDX", AddressingMode.ZeroPageY, OpKind.Read, CpuAction.Ldx);

        [Opcode(0xAE, CpuSet.Nmos, "LDX", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldx)]
        public static readonly CpuOpcode LdxAbsolute = new(0xAE, "LDX", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldx);

        [Opcode(0xBE, CpuSet.Nmos, "LDX", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Ldx)]
        public static readonly CpuOpcode LdxAbsoluteY = new(0xBE, "LDX", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Ldx);

        [Opcode(0xA0, CpuSet.Nmos, "LDY", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldy)]
        public static readonly CpuOpcode LdyImmediate = new(0xA0, "LDY", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldy);

        [Opcode(0xA4, CpuSet.Nmos, "LDY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldy)]
        public static readonly CpuOpcode LdyZeroPage = new(0xA4, "LDY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldy);

        [Opcode(0xB4, CpuSet.Nmos, "LDY", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Ldy)]
        public static readonly CpuOpcode LdyZeroPageX = new(0xB4, "LDY", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Ldy);

        [Opcode(0xAC, CpuSet.Nmos, "LDY", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldy)]
        public static readonly CpuOpcode LdyAbsolute = new(0xAC, "LDY", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldy);

        [Opcode(0xBC, CpuSet.Nmos, "LDY", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Ldy)]
        public static readonly CpuOpcode LdyAbsoluteX = new(0xBC, "LDY", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Ldy);

        [Opcode(0x85, CpuSet.Nmos, "STA", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaZeroPage = new(0x85, "STA", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sta);

        [Opcode(0x95, CpuSet.Nmos, "STA", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaZeroPageX = new(0x95, "STA", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sta);

        [Opcode(0x8D, CpuSet.Nmos, "STA", AddressingMode.Absolute, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaAbsolute = new(0x8D, "STA", AddressingMode.Absolute, OpKind.Write, CpuAction.Sta);

        [Opcode(0x9D, CpuSet.Nmos, "STA", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaAbsoluteX = new(0x9D, "STA", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Sta);

        [Opcode(0x99, CpuSet.Nmos, "STA", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaAbsoluteY = new(0x99, "STA", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Sta);

        [Opcode(0x81, CpuSet.Nmos, "STA", AddressingMode.IndexedIndirect, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaIndexedIndirect = new(0x81, "STA", AddressingMode.IndexedIndirect, OpKind.Write, CpuAction.Sta);

        [Opcode(0x91, CpuSet.Nmos, "STA", AddressingMode.IndirectIndexed, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaIndirectIndexed = new(0x91, "STA", AddressingMode.IndirectIndexed, OpKind.Write, CpuAction.Sta);

        [Opcode(0x86, CpuSet.Nmos, "STX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stx)]
        public static readonly CpuOpcode StxZeroPage = new(0x86, "STX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stx);

        [Opcode(0x96, CpuSet.Nmos, "STX", AddressingMode.ZeroPageY, OpKind.Write, CpuAction.Stx)]
        public static readonly CpuOpcode StxZeroPageY = new(0x96, "STX", AddressingMode.ZeroPageY, OpKind.Write, CpuAction.Stx);

        [Opcode(0x8E, CpuSet.Nmos, "STX", AddressingMode.Absolute, OpKind.Write, CpuAction.Stx)]
        public static readonly CpuOpcode StxAbsolute = new(0x8E, "STX", AddressingMode.Absolute, OpKind.Write, CpuAction.Stx);

        [Opcode(0x84, CpuSet.Nmos, "STY", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sty)]
        public static readonly CpuOpcode StyZeroPage = new(0x84, "STY", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sty);

        [Opcode(0x94, CpuSet.Nmos, "STY", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sty)]
        public static readonly CpuOpcode StyZeroPageX = new(0x94, "STY", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sty);

        [Opcode(0x8C, CpuSet.Nmos, "STY", AddressingMode.Absolute, OpKind.Write, CpuAction.Sty)]
        public static readonly CpuOpcode StyAbsolute = new(0x8C, "STY", AddressingMode.Absolute, OpKind.Write, CpuAction.Sty);

        [Opcode(0x69, CpuSet.Nmos, "ADC", AddressingMode.Immediate, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcImmediate = new(0x69, "ADC", AddressingMode.Immediate, OpKind.Read, CpuAction.Adc);

        [Opcode(0xE9, CpuSet.Nmos, "SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcImmediate = new(0xE9, "SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc);

        [Opcode(0xE6, CpuSet.Nmos, "INC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Inc)]
        public static readonly CpuOpcode IncZeroPage = new(0xE6, "INC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Inc);

        [Opcode(0xEE, CpuSet.Nmos, "INC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Inc)]
        public static readonly CpuOpcode IncAbsolute = new(0xEE, "INC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Inc);

        [Opcode(0xC6, CpuSet.Nmos, "DEC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dec)]
        public static readonly CpuOpcode DecZeroPage = new(0xC6, "DEC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dec);

        [Opcode(0xCE, CpuSet.Nmos, "DEC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dec)]
        public static readonly CpuOpcode DecAbsolute = new(0xCE, "DEC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dec);

        [Opcode(0xE8, CpuSet.Nmos, "INX", AddressingMode.Implied, OpKind.Implied, CpuAction.Inx, 1)]
        public static readonly CpuOpcode InxImplied = new(0xE8, "INX", AddressingMode.Implied, OpKind.Implied, CpuAction.Inx, 1);

        [Opcode(0xCA, CpuSet.Nmos, "DEX", AddressingMode.Implied, OpKind.Implied, CpuAction.Dex, 1)]
        public static readonly CpuOpcode DexImplied = new(0xCA, "DEX", AddressingMode.Implied, OpKind.Implied, CpuAction.Dex, 1);

        [Opcode(0xC8, CpuSet.Nmos, "INY", AddressingMode.Implied, OpKind.Implied, CpuAction.Iny, 1)]
        public static readonly CpuOpcode InyImplied = new(0xC8, "INY", AddressingMode.Implied, OpKind.Implied, CpuAction.Iny, 1);

        [Opcode(0x88, CpuSet.Nmos, "DEY", AddressingMode.Implied, OpKind.Implied, CpuAction.Dey, 1)]
        public static readonly CpuOpcode DeyImplied = new(0x88, "DEY", AddressingMode.Implied, OpKind.Implied, CpuAction.Dey, 1);

        [Opcode(0xAA, CpuSet.Nmos, "TAX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tax, 1)]
        public static readonly CpuOpcode TaxImplied = new(0xAA, "TAX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tax, 1);

        [Opcode(0xA8, CpuSet.Nmos, "TAY", AddressingMode.Implied, OpKind.Implied, CpuAction.Tay, 1)]
        public static readonly CpuOpcode TayImplied = new(0xA8, "TAY", AddressingMode.Implied, OpKind.Implied, CpuAction.Tay, 1);

        [Opcode(0x8A, CpuSet.Nmos, "TXA", AddressingMode.Implied, OpKind.Implied, CpuAction.Txa, 1)]
        public static readonly CpuOpcode TxaImplied = new(0x8A, "TXA", AddressingMode.Implied, OpKind.Implied, CpuAction.Txa, 1);

        [Opcode(0x98, CpuSet.Nmos, "TYA", AddressingMode.Implied, OpKind.Implied, CpuAction.Tya, 1)]
        public static readonly CpuOpcode TyaImplied = new(0x98, "TYA", AddressingMode.Implied, OpKind.Implied, CpuAction.Tya, 1);

        [Opcode(0xBA, CpuSet.Nmos, "TSX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tsx, 1)]
        public static readonly CpuOpcode TsxImplied = new(0xBA, "TSX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tsx, 1);

        [Opcode(0x9A, CpuSet.Nmos, "TXS", AddressingMode.Implied, OpKind.Implied, CpuAction.Txs, 1)]
        public static readonly CpuOpcode TxsImplied = new(0x9A, "TXS", AddressingMode.Implied, OpKind.Implied, CpuAction.Txs, 1);

        [Opcode(0x18, CpuSet.Nmos, "CLC", AddressingMode.Implied, OpKind.Implied, CpuAction.Clc, 1)]
        public static readonly CpuOpcode ClcImplied = new(0x18, "CLC", AddressingMode.Implied, OpKind.Implied, CpuAction.Clc, 1);

        [Opcode(0x38, CpuSet.Nmos, "SEC", AddressingMode.Implied, OpKind.Implied, CpuAction.Sec, 1)]
        public static readonly CpuOpcode SecImplied = new(0x38, "SEC", AddressingMode.Implied, OpKind.Implied, CpuAction.Sec, 1);

        [Opcode(0x58, CpuSet.Nmos, "CLI", AddressingMode.Implied, OpKind.Implied, CpuAction.Cli, 1)]
        public static readonly CpuOpcode CliImplied = new(0x58, "CLI", AddressingMode.Implied, OpKind.Implied, CpuAction.Cli, 1);

        [Opcode(0x78, CpuSet.Nmos, "SEI", AddressingMode.Implied, OpKind.Implied, CpuAction.Sei, 1)]
        public static readonly CpuOpcode SeiImplied = new(0x78, "SEI", AddressingMode.Implied, OpKind.Implied, CpuAction.Sei, 1);

        [Opcode(0xB8, CpuSet.Nmos, "CLV", AddressingMode.Implied, OpKind.Implied, CpuAction.Clv, 1)]
        public static readonly CpuOpcode ClvImplied = new(0xB8, "CLV", AddressingMode.Implied, OpKind.Implied, CpuAction.Clv, 1);

        [Opcode(0xD8, CpuSet.Nmos, "CLD", AddressingMode.Implied, OpKind.Implied, CpuAction.Cld, 1)]
        public static readonly CpuOpcode CldImplied = new(0xD8, "CLD", AddressingMode.Implied, OpKind.Implied, CpuAction.Cld, 1);

        [Opcode(0xF8, CpuSet.Nmos, "SED", AddressingMode.Implied, OpKind.Implied, CpuAction.Sed, 1)]
        public static readonly CpuOpcode SedImplied = new(0xF8, "SED", AddressingMode.Implied, OpKind.Implied, CpuAction.Sed, 1);

        [Opcode(0xEA, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImplied = new(0xEA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0x00, CpuSet.Nmos, "BRK", AddressingMode.Implied, OpKind.Implied, CpuAction.Brk, 6)]
        public static readonly CpuOpcode BrkImplied = new(0x00, "BRK", AddressingMode.Implied, OpKind.Implied, CpuAction.Brk, 6);

        [Opcode(0x4C, CpuSet.Nmos, "JMP", AddressingMode.Absolute, OpKind.Jump, CpuAction.Jmp)]
        public static readonly CpuOpcode JmpAbsolute = new(0x4C, "JMP", AddressingMode.Absolute, OpKind.Jump, CpuAction.Jmp);

        /// <summary>Wszystkie opkody zestawu NMOS, w kolejności tabeli wariantu.</summary>
        public static readonly CpuOpcode[] Opcodes =
        [
            LdaImmediate, LdaZeroPage, LdaZeroPageX, LdaAbsolute, LdaAbsoluteX, LdaAbsoluteY,
            LdaIndexedIndirect, LdaIndirectIndexed,
            LdxImmediate, LdxZeroPage, LdxZeroPageY, LdxAbsolute, LdxAbsoluteY,
            LdyImmediate, LdyZeroPage, LdyZeroPageX, LdyAbsolute, LdyAbsoluteX,
            StaZeroPage, StaZeroPageX, StaAbsolute, StaAbsoluteX, StaAbsoluteY,
            StaIndexedIndirect, StaIndirectIndexed,
            StxZeroPage, StxZeroPageY, StxAbsolute,
            StyZeroPage, StyZeroPageX, StyAbsolute,
            AdcImmediate, SbcImmediate,
            IncZeroPage, IncAbsolute, DecZeroPage, DecAbsolute,
            InxImplied, DexImplied, InyImplied, DeyImplied,
            TaxImplied, TayImplied, TxaImplied, TyaImplied, TsxImplied, TxsImplied,
            ClcImplied, SecImplied, CliImplied, SeiImplied, ClvImplied, CldImplied, SedImplied,
            NopImplied, BrkImplied, JmpAbsolute,
        ];
    }

    /// <summary>Opkody wyłącznie 65C02 (zestaw `c02`) — dodatki względem tabeli NMOS.</summary>
    public static class Cmos
    {
        [Opcode(0x64, CpuSet.Cmos65C02, "STZ", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stz)]
        public static readonly CpuOpcode StzZeroPage = new(0x64, "STZ", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stz);

        [Opcode(0x74, CpuSet.Cmos65C02, "STZ", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Stz)]
        public static readonly CpuOpcode StzZeroPageX = new(0x74, "STZ", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Stz);

        [Opcode(0x9C, CpuSet.Cmos65C02, "STZ", AddressingMode.Absolute, OpKind.Write, CpuAction.Stz)]
        public static readonly CpuOpcode StzAbsolute = new(0x9C, "STZ", AddressingMode.Absolute, OpKind.Write, CpuAction.Stz);

        [Opcode(0x9E, CpuSet.Cmos65C02, "STZ", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Stz)]
        public static readonly CpuOpcode StzAbsoluteX = new(0x9E, "STZ", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Stz);

        [Opcode(0x1A, CpuSet.Cmos65C02, "INC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.IncA)]
        public static readonly CpuOpcode IncAccumulator = new(0x1A, "INC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.IncA);

        [Opcode(0x3A, CpuSet.Cmos65C02, "DEC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.DecA)]
        public static readonly CpuOpcode DecAccumulator = new(0x3A, "DEC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.DecA);

        /// <summary>Dodatki 65C02, w kolejności tabeli wariantu.</summary>
        public static readonly CpuOpcode[] Additions =
        [
            StzZeroPage, StzZeroPageX, StzAbsolute, StzAbsoluteX, IncAccumulator, DecAccumulator,
        ];
    }
}
