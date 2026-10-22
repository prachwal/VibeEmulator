namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap C: rozgałęzienia warunkowe (8 opkodów). Nowy tryb <c>Relative</c> i <c>OpKind.Branch</c>;
    /// cykle zależą od warunku i przekroczenia strony (2/3/4), więc formuła w CodeAnnotations liczy minimum.
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x10, CpuSet.Nmos, "BPL", AddressingMode.Relative, OpKind.Branch, CpuAction.Bpl, 1)]
        public static readonly CpuOpcode BplRelative = new(0x10, "BPL", AddressingMode.Relative, OpKind.Branch, CpuAction.Bpl, 1);

        [Opcode(0x30, CpuSet.Nmos, "BMI", AddressingMode.Relative, OpKind.Branch, CpuAction.Bmi, 1)]
        public static readonly CpuOpcode BmiRelative = new(0x30, "BMI", AddressingMode.Relative, OpKind.Branch, CpuAction.Bmi, 1);

        [Opcode(0x50, CpuSet.Nmos, "BVC", AddressingMode.Relative, OpKind.Branch, CpuAction.Bvc, 1)]
        public static readonly CpuOpcode BvcRelative = new(0x50, "BVC", AddressingMode.Relative, OpKind.Branch, CpuAction.Bvc, 1);

        [Opcode(0x70, CpuSet.Nmos, "BVS", AddressingMode.Relative, OpKind.Branch, CpuAction.Bvs, 1)]
        public static readonly CpuOpcode BvsRelative = new(0x70, "BVS", AddressingMode.Relative, OpKind.Branch, CpuAction.Bvs, 1);

        [Opcode(0x90, CpuSet.Nmos, "BCC", AddressingMode.Relative, OpKind.Branch, CpuAction.Bcc, 1)]
        public static readonly CpuOpcode BccRelative = new(0x90, "BCC", AddressingMode.Relative, OpKind.Branch, CpuAction.Bcc, 1);

        [Opcode(0xB0, CpuSet.Nmos, "BCS", AddressingMode.Relative, OpKind.Branch, CpuAction.Bcs, 1)]
        public static readonly CpuOpcode BcsRelative = new(0xB0, "BCS", AddressingMode.Relative, OpKind.Branch, CpuAction.Bcs, 1);

        [Opcode(0xD0, CpuSet.Nmos, "BNE", AddressingMode.Relative, OpKind.Branch, CpuAction.Bne, 1)]
        public static readonly CpuOpcode BneRelative = new(0xD0, "BNE", AddressingMode.Relative, OpKind.Branch, CpuAction.Bne, 1);

        [Opcode(0xF0, CpuSet.Nmos, "BEQ", AddressingMode.Relative, OpKind.Branch, CpuAction.Beq, 1)]
        public static readonly CpuOpcode BeqRelative = new(0xF0, "BEQ", AddressingMode.Relative, OpKind.Branch, CpuAction.Beq, 1);
    }
}
