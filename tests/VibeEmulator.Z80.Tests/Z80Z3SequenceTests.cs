using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80Z3SequenceTests
{
    [Theory]
    [InlineData(0x37, 0x2D)]
    [InlineData(0x3F, 0x2D)]
    public void Tick_NopBetweenFlagInstructions_ClearsQBeforeCarryInstruction(byte opcode, byte expectedFlags)
    {
        var memory = new FlatMemory();
        // XOR B daje F=0x2C; NOP zeruje Q.
        memory.Write(0, 0xA8);
        memory.Write(1, 0x00);
        memory.Write(2, opcode);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.A = 0;
        core.State.B = 0x28;

        RunInstruction(core, 4);
        Assert.Equal((byte)0x2C, core.State.Q);
        RunInstruction(core, 4);
        Assert.Equal((byte)0, core.State.Q);
        // A=0 pozwala odróżnić X/Y odziedziczone z F od X/Y pochodzących z A.
        core.State.A = 0;
        RunInstruction(core, 4);

        Assert.Equal(expectedFlags, core.State.F);
        Assert.Equal(expectedFlags, core.State.Q);
        Assert.Equal(12, core.Cycles);
    }

    [Fact]
    public void Tick_IncrementHlWaitBeforeWrite_PreservesPendingAndWritesModifiedValue()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0x34);
        memory.Write(0x1234, 0x7F);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.H = 0x12;
        core.State.L = 0x34;
        core.State.F = 1;
        for (int i = 0; i < 8; i++)
        {
            core.Tick();
        }

        Assert.Equal(3, core.Pending);
        Assert.Equal((byte)0x7F, memory.Read(0x1234));
        core.Stall();
        Assert.Equal(3, core.Pending);
        for (int i = 0; i < 3; i++)
        {
            core.Tick();
        }

        Assert.Equal(0, core.Pending);
        Assert.Equal(12, core.Cycles);
        Assert.Equal((byte)0x80, memory.Read(0x1234));
        Assert.Equal((byte)0x95, core.State.F);
        Assert.Equal((byte)0x95, core.State.Q);
    }

    private static void RunInstruction(Z80Core core, int expectedTStates)
    {
        int cycles = 0;
        do
        {
            core.Tick();
            cycles++;
        }
        while (core.Pending != 0 && cycles < 64);

        Assert.Equal(expectedTStates, cycles);
        Assert.Equal(0, core.Pending);
    }
}
