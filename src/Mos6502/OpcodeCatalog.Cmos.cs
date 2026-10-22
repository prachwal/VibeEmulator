namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>Opkody wyłącznie 65C02 (zestaw `c02`) — dodatki względem tabeli NMOS.</summary>
    /// <remarks>Etap H (55 opkodów) jest w <c>OpcodeCatalog.Cmos.H.cs</c> — klasa jest <c>partial</c>.</remarks>
    public static partial class Cmos
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

        private static CpuOpcode[]? _additions;

        /// <summary>Dodatki 65C02, w kolejności tabeli wariantu.</summary>
        /// <remarks>
        /// Właściwość, nie pole: tablica obejmuje pola z obu plików <c>partial</c> (etapy 2a i H),
        /// a kolejność inicjalizacji statycznych pól między plikami nie jest gwarantowana.
        /// </remarks>
        public static CpuOpcode[] Additions => _additions ??=
        [
            StzZeroPage, StzZeroPageX, StzAbsolute, StzAbsoluteX, IncAccumulator, DecAccumulator,
            TSBZeroPage, TRBZeroPage, TSBAbsolute, TRBAbsolute,
            Rmb0ZeroPage, Rmb1ZeroPage, Rmb2ZeroPage, Rmb3ZeroPage, Rmb4ZeroPage, Rmb5ZeroPage, Rmb6ZeroPage, Rmb7ZeroPage,
            Smb0ZeroPage, Smb1ZeroPage, Smb2ZeroPage, Smb3ZeroPage, Smb4ZeroPage, Smb5ZeroPage, Smb6ZeroPage, Smb7ZeroPage,
            OraIndirectZeroPage, AndIndirectZeroPage, EorIndirectZeroPage, AdcIndirectZeroPage,
            StaIndirectZeroPage, LdaIndirectZeroPage, CmpIndirectZeroPage, SbcIndirectZeroPage,
            BitZeroPageX, BitAbsoluteX, BitImmediate, JmpIndirectAbsoluteX, BraRelative,
            Bbr0ZeroPageRelative, Bbr1ZeroPageRelative, Bbr2ZeroPageRelative, Bbr3ZeroPageRelative,
            Bbr4ZeroPageRelative, Bbr5ZeroPageRelative, Bbr6ZeroPageRelative, Bbr7ZeroPageRelative,
            Bbs0ZeroPageRelative, Bbs1ZeroPageRelative, Bbs2ZeroPageRelative, Bbs3ZeroPageRelative,
            Bbs4ZeroPageRelative, Bbs5ZeroPageRelative, Bbs6ZeroPageRelative, Bbs7ZeroPageRelative,
            PhxImplied, PhyImplied, PlxImplied, PlyImplied, WaiImplied, StpImplied,
            Nop02Immediate, Nop03Implied, Nop0BImplied, Nop13Implied, Nop1BImplied, Nop22Immediate,
            Nop23Implied, Nop2BImplied, Nop33Implied, Nop3BImplied, Nop42Immediate, Nop43Implied,
            Nop44ZeroPage, Nop4BImplied, Nop53Implied, Nop54ZeroPageX, Nop5BImplied, Nop5CAbsolute,
            Nop62Immediate, Nop63Implied, Nop6BImplied, Nop73Implied, Nop7BImplied, Nop82Immediate,
            Nop83Implied, Nop8BImplied, Nop93Implied, Nop9BImplied, NopA3Implied, NopABImplied,
            NopB3Implied, NopBBImplied, NopC2Immediate, NopC3Implied, NopD3Implied, NopD4ZeroPageX,
            NopDCAbsolute, NopE2Immediate, NopE3Implied, NopEBImplied, NopF3Implied, NopF4ZeroPageX,
            NopFBImplied, NopFCAbsolute,
        ];
    }
}
