namespace VibeEmulator.Mos6502;

/// <summary>
/// Grupy instrukcji — jednostki kompozycji tablicy opkodów.
/// Każda grupa to niezależny fragment mapy rozkazów, który można dołożyć do buildera.
/// </summary>
public static class OpcodeGroups
{
    public static void LoadStore(OpcodeTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Define(0xA9, "LDA", AddressingMode.Immediate, Operations.Lda)
            .Define(0xA5, "LDA", AddressingMode.ZeroPage, Operations.Lda)
            .Define(0xAD, "LDA", AddressingMode.Absolute, Operations.Lda)
            .Define(0xA2, "LDX", AddressingMode.Immediate, Operations.Ldx)
            .Define(0xA6, "LDX", AddressingMode.ZeroPage, Operations.Ldx)
            .Define(0xA0, "LDY", AddressingMode.Immediate, Operations.Ldy)
            .Define(0x85, "STA", AddressingMode.ZeroPage, Operations.Sta)
            .Define(0x8D, "STA", AddressingMode.Absolute, Operations.Sta)
            .Define(0x86, "STX", AddressingMode.ZeroPage, Operations.Stx)
            .Define(0x84, "STY", AddressingMode.ZeroPage, Operations.Sty);
    }

    public static void Arithmetic(OpcodeTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Define(0x69, "ADC", AddressingMode.Immediate, Operations.Adc)
            .Define(0x65, "ADC", AddressingMode.ZeroPage, Operations.Adc)
            .Define(0xE9, "SBC", AddressingMode.Immediate, Operations.Sbc)
            .Define(0xE5, "SBC", AddressingMode.ZeroPage, Operations.Sbc)
            .Define(0xE6, "INC", AddressingMode.ZeroPage, Operations.Inc)
            .Define(0xEE, "INC", AddressingMode.Absolute, Operations.Inc)
            .Define(0xC6, "DEC", AddressingMode.ZeroPage, Operations.Dec)
            .Define(0xCE, "DEC", AddressingMode.Absolute, Operations.Dec)
            .Define(0xE8, "INX", AddressingMode.Implied, Operations.Inx)
            .Define(0xCA, "DEX", AddressingMode.Implied, Operations.Dex)
            .Define(0xC8, "INY", AddressingMode.Implied, Operations.Iny)
            .Define(0x88, "DEY", AddressingMode.Implied, Operations.Dey);
    }

    public static void ControlFlow(OpcodeTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Define(0x4C, "JMP", AddressingMode.Absolute, Operations.Jmp)
            .Define(0xEA, "NOP", AddressingMode.Implied, Operations.Nop)
            .Define(0x00, "BRK", AddressingMode.Implied, Operations.Brk)
            .Define(0xAA, "TAX", AddressingMode.Implied, Operations.Tax)
            .Define(0xA8, "TAY", AddressingMode.Implied, Operations.Tay)
            .Define(0x8A, "TXA", AddressingMode.Implied, Operations.Txa)
            .Define(0x98, "TYA", AddressingMode.Implied, Operations.Tya);
    }

    /// <summary>Dodatki 65C02 względem standardu NMOS: STZ, INC A, DEC A.</summary>
    public static void Cmos65C02Additions(OpcodeTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Define(0x64, "STZ", AddressingMode.ZeroPage, Operations.Stz)
            .Define(0x74, "STZ", AddressingMode.ZeroPageX, Operations.Stz)
            .Define(0x9C, "STZ", AddressingMode.Absolute, Operations.Stz)
            .Define(0x9E, "STZ", AddressingMode.AbsoluteX, Operations.Stz)
            .Define(0x1A, "INC", AddressingMode.Implied, Operations.IncA)
            .Define(0x3A, "DEC", AddressingMode.Implied, Operations.DecA);
    }

    /// <summary>Pełny standardowy zestaw — kompozycja wszystkich grup.</summary>
    public static void Standard(OpcodeTableBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Compose(LoadStore)
            .Compose(Arithmetic)
            .Compose(ControlFlow);
    }
}
