namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap I: niezdefiniowane NOP-y 65C02 (44) — po nich 65C02 ma 256/256 bajtów zdefiniowanych.
    /// Cykle i długość wynikają z trybu: <c>impl</c> 1 bajt/1 cykl (tylko fetch — stąd <c>OpKind.NopByte</c>),
    /// <c>imm</c> 2/2, <c>zp</c> 2/3, <c>zp,X</c> 2/4, <c>abs</c> 3/4 (wektory wdc65c02).
    /// <para>
    /// <b>$5C:</b> datasheet WDC i Oxyron podają 8 cykli, wektory (WDC/Rockwell/Synertek) 4. Zgodnie z regułą
    /// repo <b>wektory są źródłem prawdy</b> — implementacja ma 4 cykle, a rozbieżność jest zapisana w kolumnie
    /// <c>uwagi</c> w CSV (`docs/cpu/opcodes/wdc65c02.csv`) i w <c>docs/cpu/opcodes.md</c>. Nie „poprawiać” CSV.
    /// </para>
    /// <para>Te same bajty w NMOS to KIL (etap K) — wpisy są wyłącznie w tabeli 65C02.</para>
    /// </summary>
    public static partial class Cmos
    {
        [Opcode(0x02, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop02Immediate = new(0x02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x03, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop03Implied = new(0x03, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x0B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop0BImplied = new(0x0B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x13, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop13Implied = new(0x13, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x1B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop1BImplied = new(0x1B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x22, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop22Immediate = new(0x22, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x23, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop23Implied = new(0x23, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x2B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop2BImplied = new(0x2B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x33, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop33Implied = new(0x33, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x3B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop3BImplied = new(0x3B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x42, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop42Immediate = new(0x42, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x43, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop43Implied = new(0x43, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x44, CpuSet.Cmos65C02, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop44ZeroPage = new(0x44, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop);

        [Opcode(0x4B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop4BImplied = new(0x4B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x53, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop53Implied = new(0x53, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x54, CpuSet.Cmos65C02, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop54ZeroPageX = new(0x54, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x5B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop5BImplied = new(0x5B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x5C, CpuSet.Cmos65C02, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop5CAbsolute = new(0x5C, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop);

        [Opcode(0x62, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop62Immediate = new(0x62, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x63, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop63Implied = new(0x63, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x6B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop6BImplied = new(0x6B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x73, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop73Implied = new(0x73, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x7B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop7BImplied = new(0x7B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x82, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode Nop82Immediate = new(0x82, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x83, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop83Implied = new(0x83, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x8B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop8BImplied = new(0x8B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x93, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop93Implied = new(0x93, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0x9B, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode Nop9BImplied = new(0x9B, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xA3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopA3Implied = new(0xA3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xAB, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopABImplied = new(0xAB, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xB3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopB3Implied = new(0xB3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xBB, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopBBImplied = new(0xBB, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xC2, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopC2Immediate = new(0xC2, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0xC3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopC3Implied = new(0xC3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xD3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopD3Implied = new(0xD3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xD4, CpuSet.Cmos65C02, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopD4ZeroPageX = new(0xD4, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xDC, CpuSet.Cmos65C02, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopDCAbsolute = new(0xDC, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop);

        [Opcode(0xE2, CpuSet.Cmos65C02, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopE2Immediate = new(0xE2, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0xE3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopE3Implied = new(0xE3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xEB, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopEBImplied = new(0xEB, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xF3, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopF3Implied = new(0xF3, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xF4, CpuSet.Cmos65C02, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopF4ZeroPageX = new(0xF4, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xFB, CpuSet.Cmos65C02, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0)]
        public static readonly CpuOpcode NopFBImplied = new(0xFB, "NOP", AddressingMode.Implied, OpKind.NopByte, CpuAction.Nop, 0);

        [Opcode(0xFC, CpuSet.Cmos65C02, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopFCAbsolute = new(0xFC, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop);

    }
}
