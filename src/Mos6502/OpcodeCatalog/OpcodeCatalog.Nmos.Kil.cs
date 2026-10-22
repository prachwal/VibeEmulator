namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap K: KIL/JAM — 12 opkodów NMOS, które zatrzymują rdzeń. Wektory (02.json … F2.json,
    /// 10 000 testów każdy) pokazują 11 cykli: fetch, dummy read spod PC, potem odczyty
    /// <c>$FFFF, $FFFE, $FFFE, $FFFF×6</c>; PC rośnie o 1, reszta stanu bez zmian.
    /// Wpis jest jawny (nie „nieznany bajt”), a zatrzymanie nie jest wyjątkiem.
    /// <para>Te same bajty w 65C02 to prawdziwe instrukcje (np. <c>$92</c> STA (zp)), a STP (<c>$DB</c>)
    /// jest osobną instrukcją — KIL istnieje tylko w wariancie NMOS.</para>
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x02, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied02 = new(0x02, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x12, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied12 = new(0x12, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x22, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied22 = new(0x22, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x32, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied32 = new(0x32, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x42, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied42 = new(0x42, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x52, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied52 = new(0x52, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x62, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied62 = new(0x62, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x72, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied72 = new(0x72, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0x92, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImplied92 = new(0x92, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0xB2, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImpliedB2 = new(0xB2, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0xD2, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImpliedD2 = new(0xD2, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

        [Opcode(0xF2, CpuSet.Nmos, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9)]
        public static readonly CpuOpcode KilImpliedF2 = new(0xF2, "KIL", AddressingMode.Implied, OpKind.Kil, CpuAction.Kil, 9);

    }
}
