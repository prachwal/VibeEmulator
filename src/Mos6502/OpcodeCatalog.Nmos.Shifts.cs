namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap B: przesunięcia, rotacje i RMW indeksowane (24 opkody).
    /// Wariant akumulatorowy używa <c>OpKind.Accumulator</c>, pamięciowy — <c>ReadModifyWrite</c>.
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x06, CpuSet.Nmos, "ASL", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Asl)]
        public static readonly CpuOpcode AslZeroPage = new(0x06, "ASL", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Asl);

        [Opcode(0x0A, CpuSet.Nmos, "ASL", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Asl, 1)]
        public static readonly CpuOpcode AslImplied = new(0x0A, "ASL", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Asl, 1);

        [Opcode(0x0E, CpuSet.Nmos, "ASL", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Asl)]
        public static readonly CpuOpcode AslAbsolute = new(0x0E, "ASL", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Asl);

        [Opcode(0x16, CpuSet.Nmos, "ASL", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Asl)]
        public static readonly CpuOpcode AslZeroPageX = new(0x16, "ASL", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Asl);

        [Opcode(0x1E, CpuSet.Nmos, "ASL", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Asl)]
        public static readonly CpuOpcode AslAbsoluteX = new(0x1E, "ASL", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Asl);

        [Opcode(0x26, CpuSet.Nmos, "ROL", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rol)]
        public static readonly CpuOpcode RolZeroPage = new(0x26, "ROL", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rol);

        [Opcode(0x2A, CpuSet.Nmos, "ROL", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Rol, 1)]
        public static readonly CpuOpcode RolImplied = new(0x2A, "ROL", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Rol, 1);

        [Opcode(0x2E, CpuSet.Nmos, "ROL", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rol)]
        public static readonly CpuOpcode RolAbsolute = new(0x2E, "ROL", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rol);

        [Opcode(0x36, CpuSet.Nmos, "ROL", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rol)]
        public static readonly CpuOpcode RolZeroPageX = new(0x36, "ROL", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rol);

        [Opcode(0x3E, CpuSet.Nmos, "ROL", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rol)]
        public static readonly CpuOpcode RolAbsoluteX = new(0x3E, "ROL", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rol);

        [Opcode(0x46, CpuSet.Nmos, "LSR", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Lsr)]
        public static readonly CpuOpcode LsrZeroPage = new(0x46, "LSR", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Lsr);

        [Opcode(0x4A, CpuSet.Nmos, "LSR", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Lsr, 1)]
        public static readonly CpuOpcode LsrImplied = new(0x4A, "LSR", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Lsr, 1);

        [Opcode(0x4E, CpuSet.Nmos, "LSR", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Lsr)]
        public static readonly CpuOpcode LsrAbsolute = new(0x4E, "LSR", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Lsr);

        [Opcode(0x56, CpuSet.Nmos, "LSR", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Lsr)]
        public static readonly CpuOpcode LsrZeroPageX = new(0x56, "LSR", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Lsr);

        [Opcode(0x5E, CpuSet.Nmos, "LSR", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Lsr)]
        public static readonly CpuOpcode LsrAbsoluteX = new(0x5E, "LSR", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Lsr);

        [Opcode(0x66, CpuSet.Nmos, "ROR", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Ror)]
        public static readonly CpuOpcode RorZeroPage = new(0x66, "ROR", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Ror);

        [Opcode(0x6A, CpuSet.Nmos, "ROR", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Ror, 1)]
        public static readonly CpuOpcode RorImplied = new(0x6A, "ROR", AddressingMode.Implied, OpKind.Accumulator, CpuAction.Ror, 1);

        [Opcode(0x6E, CpuSet.Nmos, "ROR", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Ror)]
        public static readonly CpuOpcode RorAbsolute = new(0x6E, "ROR", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Ror);

        [Opcode(0x76, CpuSet.Nmos, "ROR", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Ror)]
        public static readonly CpuOpcode RorZeroPageX = new(0x76, "ROR", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Ror);

        [Opcode(0x7E, CpuSet.Nmos, "ROR", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Ror)]
        public static readonly CpuOpcode RorAbsoluteX = new(0x7E, "ROR", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Ror);

        [Opcode(0xD6, CpuSet.Nmos, "DEC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Dec)]
        public static readonly CpuOpcode DecZeroPageX = new(0xD6, "DEC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Dec);

        [Opcode(0xDE, CpuSet.Nmos, "DEC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Dec)]
        public static readonly CpuOpcode DecAbsoluteX = new(0xDE, "DEC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Dec);

        [Opcode(0xF6, CpuSet.Nmos, "INC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Inc)]
        public static readonly CpuOpcode IncZeroPageX = new(0xF6, "INC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Inc);

        [Opcode(0xFE, CpuSet.Nmos, "INC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Inc)]
        public static readonly CpuOpcode IncAbsoluteX = new(0xFE, "INC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Inc);
    }
}
