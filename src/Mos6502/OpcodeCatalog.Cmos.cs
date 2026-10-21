namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
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
