using VibeEmulator.Core.C64;
using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.Tests;

public class DisassemblerTests
{
    [Fact]
    public void Decode_EveryTableEntry_ProducesTextAndExpectedLength()
    {
        foreach (CpuOpcode entry in StateMachineCpuCore.Table)
        {
            int opcode = FindOpcode(entry);
            Assert.True(opcode >= 0, $"Wpis {entry.Mnemonic} ({entry.Mode}) nie ma bajtu opkodu w Describe.");

            byte[] memory = [(byte)opcode, 0x34, 0x12];
            string text = Disassembler.Decode(address => memory[address], 0, out int length);

            Assert.NotEqual("???", text);
            Assert.StartsWith(entry.Mnemonic, text);
            Assert.Equal(ExpectedLength(entry.Mode), length);
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
        byte[] memory = [0x02, 0x34, 0x12];

        string text = Disassembler.Decode(address => memory[address], 0, out int length);

        Assert.Null(StateMachineCpuCore.Describe(0x02));
        Assert.Equal("???", text);
        Assert.Equal(1, length);
    }

    [Fact]
    public void Decode_BusReadsKernalAtE000()
    {
        byte[] kernal = new byte[0x2000];
        kernal[0] = 0xA9;
        kernal[1] = 0x55;
        C64Machine machine = C64Machine.CreateCycleExact(null, kernal, null);

        string text = Disassembler.Decode(machine.Bus, 0xE000, out int length);

        Assert.Equal("LDA #$55", text);
        Assert.Equal(2, length);
    }

    [Fact]
    public void Decode_NullPeek_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Disassembler.Decode((Func<ushort, byte>)null!, 0, out _));
    }

    private static int FindOpcode(CpuOpcode entry)
    {
        for (int opcode = 0; opcode < 256; opcode++)
        {
            if (StateMachineCpuCore.Describe((byte)opcode) == entry)
            {
                return opcode;
            }
        }

        return -1;
    }

    private static int ExpectedLength(AddressingMode mode) => mode switch
    {
        AddressingMode.Implied => 1,
        AddressingMode.Immediate or AddressingMode.ZeroPage or AddressingMode.ZeroPageX => 2,
        AddressingMode.Absolute or AddressingMode.AbsoluteX => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Nieobsługiwany tryb w tablicy."),
    };
}
