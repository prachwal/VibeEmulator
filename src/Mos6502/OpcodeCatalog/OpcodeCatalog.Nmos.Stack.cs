namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap D: stos (4 opkody). Push = zapis pod $0100+S i S--; pull = dummy read spod $0100+S,
    /// S++, odczyt spod $0100+S. Cykle: push 3, pull 4 (fetch + dummy PC + operacje stosu).
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x48, CpuSet.Nmos, "PHA", AddressingMode.Implied, OpKind.Push, CpuAction.Pha)]
        public static readonly CpuOpcode PhaImplied = new(0x48, "PHA", AddressingMode.Implied, OpKind.Push, CpuAction.Pha);

        [Opcode(0x08, CpuSet.Nmos, "PHP", AddressingMode.Implied, OpKind.Push, CpuAction.Php)]
        public static readonly CpuOpcode PhpImplied = new(0x08, "PHP", AddressingMode.Implied, OpKind.Push, CpuAction.Php);

        [Opcode(0x68, CpuSet.Nmos, "PLA", AddressingMode.Implied, OpKind.Pull, CpuAction.Pla)]
        public static readonly CpuOpcode PlaImplied = new(0x68, "PLA", AddressingMode.Implied, OpKind.Pull, CpuAction.Pla);

        [Opcode(0x28, CpuSet.Nmos, "PLP", AddressingMode.Implied, OpKind.Pull, CpuAction.Plp)]
        public static readonly CpuOpcode PlpImplied = new(0x28, "PLP", AddressingMode.Implied, OpKind.Pull, CpuAction.Plp);
    }
}
