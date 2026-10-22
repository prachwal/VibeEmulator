using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public partial class Z80Z5ControlTests
{
    [Fact]
    public void Tick_RepeatedEiThenNop_KeepsDelayUntilInstructionAfterLastEi()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0xFB);
        bus.Memory.Write(1, 0xFB);
        bus.Memory.Write(2, 0);
        var core = new Z80Core(bus, new FlatPorts());

        RunInstruction(core, 4);
        Assert.True(core.State.IFF1);
        Assert.True(core.State.IFF2);
        Assert.Equal((byte)1, core.State.EI);
        RunInstruction(core, 4);
        Assert.Equal((byte)1, core.State.EI);
        RunInstruction(core, 4);

        Assert.Equal((byte)0, core.State.EI);
        Assert.True(core.State.IFF1);
        Assert.True(core.State.IFF2);
    }

    [Fact]
    public void Tick_DiAfterEi_ClearsBothFlipFlopsAndDelay()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0xFB);
        bus.Memory.Write(1, 0xF3);
        var core = new Z80Core(bus, new FlatPorts());

        RunInstruction(core, 4);
        RunInstruction(core, 4);

        Assert.False(core.State.IFF1);
        Assert.False(core.State.IFF2);
        Assert.Equal((byte)0, core.State.EI);
    }

    [Fact]
    public void Tick_Halt_LatchesStateAndResetResumesInstructionStream()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0x76);
        bus.Memory.Write(1, 0x3C);
        var core = new Z80Core(bus, new FlatPorts());

        RunInstruction(core, 4);

        Assert.True(core.Halted);
        Assert.False(core.Jammed);
        Assert.Equal((ushort)1, core.State.PC);
        // HALT wykonuje cykl pobrania i odświeżenia co 4 T-state, nie zwiększając PC.
        for (int i = 0; i < 8; i++)
        {
            core.Tick();
        }

        Assert.Equal(12, core.Cycles);
        Assert.Equal(0, core.Pending);
        Assert.Equal((ushort)1, core.State.PC);
        Assert.Equal((byte)0, core.State.A);
        Assert.Equal([(ushort)0, (ushort)0, (ushort)0], bus.Reads);
        core.Reset(1);
        Assert.False(core.Halted);
        RunInstruction(core, 4);
        Assert.Equal((byte)1, core.State.A);
    }
}
