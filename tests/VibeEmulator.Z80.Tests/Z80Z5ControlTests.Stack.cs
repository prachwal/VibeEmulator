using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public partial class Z80Z5ControlTests
{
    [Fact]
    public void Tick_CallThenRetAcrossStackBoundary_RestoresPcAndSpWithHighThenLowWrites()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0x100, 0xCD);
        bus.Memory.Write(0x101, 0);
        bus.Memory.Write(0x102, 2);
        bus.Memory.Write(0x200, 0xC9);
        var core = new Z80Core(bus, new FlatPorts());
        core.Reset(0x100);
        core.State.SP = 1;

        RunInstruction(core, 17);

        Assert.Equal([(0, (byte)1), (0xFFFF, (byte)3)], bus.Writes);
        Assert.Equal((ushort)0x200, core.State.PC);
        Assert.Equal((ushort)0x200, core.State.WZ);
        Assert.Equal((ushort)0xFFFF, core.State.SP);
        RunInstruction(core, 10);

        Assert.Equal((ushort)0x103, core.State.PC);
        Assert.Equal((ushort)0x103, core.State.WZ);
        Assert.Equal((ushort)1, core.State.SP);
        Assert.Equal(27, core.Cycles);
    }

    [Theory]
    [InlineData(0xC7, 0)]
    [InlineData(0xFF, 0x38)]
    public void Tick_RstAtPcAndStackBoundary_PushesWrappedReturnAddress(byte opcode, ushort target)
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0xFFFF, opcode);
        var core = new Z80Core(bus, new FlatPorts());
        core.Reset(0xFFFF);
        core.State.SP = 0;

        RunInstruction(core, 11);

        Assert.Equal([(0xFFFF, (byte)0), (0xFFFE, (byte)0)], bus.Writes);
        Assert.Equal(target, core.State.PC);
        Assert.Equal(target, core.State.WZ);
        Assert.Equal((ushort)0xFFFE, core.State.SP);
    }

    [Fact]
    public void Tick_FalseReturnCondition_DoesNotReadStackOrChangeWz()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0xC0);
        var core = new Z80Core(bus, new FlatPorts());
        core.State.F = 0x40;
        core.State.SP = 0xFFFF;
        core.State.WZ = 0xBEEF;

        RunInstruction(core, 5);

        Assert.Equal([(ushort)0], bus.Reads);
        Assert.Empty(bus.Writes);
        Assert.Equal((ushort)1, core.State.PC);
        Assert.Equal((ushort)0xFFFF, core.State.SP);
        Assert.Equal((ushort)0xBEEF, core.State.WZ);
    }

    [Fact]
    public void Tick_WaitBeforeCallInternalCycle_PreservesPendingAndWritesReturnOnce()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0xCD);
        bus.Memory.Write(1, 0x34);
        bus.Memory.Write(2, 0x12);
        var core = new Z80Core(bus, new FlatPorts());
        core.State.SP = 0x8000;
        for (int i = 0; i < 10; i++)
        {
            core.Tick();
        }

        Assert.Equal(7, core.Pending);
        core.Stall();
        Assert.Equal(7, core.Pending);
        Assert.Empty(bus.Writes);
        for (int i = 0; i < 7; i++)
        {
            core.Tick();
        }

        Assert.Equal(0, core.Pending);
        Assert.Equal(18, core.Cycles);
        Assert.Equal((ushort)0x1234, core.State.PC);
        Assert.Equal([(0x7FFF, (byte)0), (0x7FFE, (byte)3)], bus.Writes);
    }
}
