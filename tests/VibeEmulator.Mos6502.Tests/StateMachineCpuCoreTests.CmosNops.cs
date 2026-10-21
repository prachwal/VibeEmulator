namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy niezdefiniowanych NOP-ów 65C02 (etap I). Cykle i długości z wektorów wdc65c02
/// (próbka + pełne 44 × 10 000). Po tym etapie 65C02 ma 256/256 bajtów zdefiniowanych.
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Theory]
    [InlineData(0x03, 1, 0x0201)] // impl: tylko fetch, 1 cykl
    [InlineData(0x02, 2, 0x0202)] // imm
    [InlineData(0x44, 3, 0x0202)] // zp
    [InlineData(0x54, 4, 0x0202)] // zp,X
    [InlineData(0x5C, 4, 0x0203)] // abs — wektory 4 cykle (datasheet podaje 8)
    [InlineData(0xDC, 4, 0x0203)] // abs
    public void UndefinedNop_Cmos_TakesCyclesFromMode(int opcode, int expectedCycles, int expectedPc)
    {
        var bus = new TestBus();
        bus.Load(0x0200, [(byte)opcode, 0x34, 0x12]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(expectedCycles, cycles);
        Assert.Equal((ushort)expectedPc, cpu.State.PC);
    }

    [Theory]
    [InlineData(0x03)]
    [InlineData(0x02)]
    [InlineData(0x44)]
    [InlineData(0x54)]
    [InlineData(0x5C)]
    public void UndefinedNop_Cmos_ChangesNoRegisterOrFlag(int opcode)
    {
        var bus = new TestBus();
        bus.Load(0x0200, [(byte)opcode, 0x34, 0x12]);
        bus.Load(0x0034, [0xFF]);
        bus.Load(0x0096, [0xFF]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x11;
        cpu.State.X = 0x62;
        cpu.State.Y = 0x33;
        cpu.State.SP = 0xFB;
        cpu.State.P = (VibeEmulator.Mos6502.StatusFlags)0xA5;

        RunInstruction(cpu);

        Assert.Equal((byte)0x11, cpu.State.A);
        Assert.Equal((byte)0x62, cpu.State.X);
        Assert.Equal((byte)0x33, cpu.State.Y);
        Assert.Equal((ushort)0xFB, cpu.State.SP);
        Assert.Equal((VibeEmulator.Mos6502.StatusFlags)0xA5, cpu.State.P);
    }

    [Fact]
    public void UndefinedNop_Absolute_DummyReadsLastOperandByte()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x5C, 0x34, 0x12]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        bus.Reads.Clear();
        RunInstruction(cpu);

        // Wektory wdc65c02: fetch $0200, low $0201, high $0202, dummy $0202 (ostatni bajt operandu).
        Assert.Equal([0x0200, 0x0201, 0x0202, 0x0202], bus.Reads);
    }

    [Fact]
    public void AllUndefinedNops_AreDefinedInCmosVariant()
    {
        foreach (byte opcode in new byte[]
                 {
                     0x02, 0x03, 0x0B, 0x13, 0x1B, 0x22, 0x23, 0x2B, 0x33, 0x3B, 0x42, 0x43, 0x44, 0x4B, 0x53,
                     0x54, 0x5B, 0x5C, 0x62, 0x63, 0x6B, 0x73, 0x7B, 0x82, 0x83, 0x8B, 0x93, 0x9B, 0xA3, 0xAB,
                     0xB3, 0xBB, 0xC2, 0xC3, 0xD3, 0xD4, 0xDC, 0xE2, 0xE3, 0xEB, 0xF3, 0xF4, 0xFB, 0xFC,
                 })
        {
            CpuOpcode? entry = VibeEmulator.Mos6502.CpuVariants.Cmos65C02.Describe(opcode);

            Assert.NotNull(entry);
            Assert.Equal("NOP", entry!.Value.Mnemonic);
        }

        // 256/256: każdy bajt ma wpis w wariancie 65C02.
        int defined = 0;
        for (int opcode = 0; opcode <= 0xFF; opcode++)
        {
            if (VibeEmulator.Mos6502.CpuVariants.Cmos65C02.Describe((byte)opcode) is not null)
            {
                defined++;
            }
        }

        Assert.Equal(256, defined);
    }
}
