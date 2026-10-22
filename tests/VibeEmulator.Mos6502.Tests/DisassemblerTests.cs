namespace VibeEmulator.Mos6502.Tests;

public class DisassemblerTests
{
    [Fact]
    public void Decode_EveryTableEntry_ProducesTextAndExpectedLength()
    {
        foreach (CpuVariant variant in new[] { CpuVariants.Nmos6502, CpuVariants.Cmos65C02 })
        {
            foreach (CpuOpcode entry in variant.Opcodes)
            {
                byte[] memory = [entry.Opcode, 0x34, 0x12];
                string text = Disassembler.Decode(variant, address => memory[address], 0, out int length);

                Assert.True(text != "???", $"{variant.Name}: opkod {entry.Opcode:X2} ({entry.Mnemonic}, {entry.Mode}) bez tekstu.");
                Assert.StartsWith(entry.Mnemonic, text);
                Assert.Equal(ExpectedLength(entry.Mode), length);
            }
        }
    }

    [Theory]
    [InlineData(0xA9, 0x7F, 0x00, "LDA #$7F", 2)]
    [InlineData(0xA5, 0x10, 0x00, "LDA zp $10", 2)]
    [InlineData(0xB5, 0x10, 0x00, "LDA zp,X $10", 2)]
    [InlineData(0xAD, 0x34, 0x12, "LDA abs $1234", 3)]
    [InlineData(0xBD, 0x34, 0x12, "LDA abs,X $1234", 3)]
    [InlineData(0x8D, 0x00, 0x03, "STA abs $0300", 3)]
    [InlineData(0x85, 0x10, 0x00, "STA zp $10", 2)]
    [InlineData(0xEE, 0x00, 0x03, "INC abs $0300", 3)]
    [InlineData(0xE8, 0x00, 0x00, "INX", 1)]
    [InlineData(0xEA, 0x00, 0x00, "NOP", 1)]
    [InlineData(0x00, 0x00, 0x00, "BRK", 1)]
    [InlineData(0x4C, 0x00, 0x02, "JMP $0200", 3)]
    [InlineData(0xB6, 0x10, 0x00, "LDX zp,Y $10", 2)]
    [InlineData(0xB9, 0x34, 0x12, "LDA abs,Y $1234", 3)]
    [InlineData(0xA1, 0x10, 0x00, "LDA (zp,X) $10", 2)]
    [InlineData(0xB1, 0x10, 0x00, "LDA (zp),Y $10", 2)]
    [InlineData(0x8E, 0x00, 0x03, "STX abs $0300", 3)]
    [InlineData(0x18, 0x00, 0x00, "CLC", 1)]
    [InlineData(0xBA, 0x00, 0x00, "TSX", 1)]
    public void Decode_KnownOpcode_MatchesReferenceFormat(int opcode, int operandLow, int operandHigh, string expected, int expectedLength)
    {
        byte[] memory = [(byte)opcode, (byte)operandLow, (byte)operandHigh];

        string text = Disassembler.Decode(address => memory[address], 0, out int length);

        Assert.Equal(expected, text);
        Assert.Equal(expectedLength, length);
    }

    [Fact]
    public void Decode_UnknownOpcode_ReturnsQuestionMarksWithLengthOne()
    {
        // Etap K: wariant NMOS ma wpis na każdy bajt, więc „nieznany opkod” testujemy na wariancie
        // z niepełną tabelą — dekoder musi wtedy pokazać `???` o długości 1, nie rzucić wyjątku.
        byte[] memory = [0x02, 0x34, 0x12];
        var variant = new CpuVariant(
            "test bez 0x02",
            [new CpuOpcode(0xEA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)],
            RmwStyle.DoubleWrite,
            IndexedDummyRead.GhostByte);

        string text = Disassembler.Decode(variant, address => memory[address], 0, out int length);

        Assert.Equal("???", text);
        Assert.Equal(1, length);
    }

    [Fact]
    public void Decode_NullPeek_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Disassembler.Decode((Func<ushort, byte>)null!, 0, out _));
    }

    private static int ExpectedLength(AddressingMode mode) => mode switch
    {
        AddressingMode.Implied => 1,
        AddressingMode.Immediate
            or AddressingMode.ZeroPage
            or AddressingMode.ZeroPageX
            or AddressingMode.ZeroPageY
            or AddressingMode.IndexedIndirect
            or AddressingMode.IndirectIndexed
            or AddressingMode.Relative
            or AddressingMode.IndirectZeroPage => 2,
        AddressingMode.Absolute
            or AddressingMode.AbsoluteX
            or AddressingMode.AbsoluteY
            or AddressingMode.Indirect
            or AddressingMode.ZeroPageRelative
            or AddressingMode.IndirectAbsoluteX => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Nieobsługiwany tryb w tablicy."),
    };
}
