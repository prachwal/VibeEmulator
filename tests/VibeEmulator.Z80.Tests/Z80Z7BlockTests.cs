using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z7BlockTests
{
    [Fact]
    public void Tick_LdirWithOneByte_DoesNotRepeat()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0xED);
        memory.Write(1, 0xB0);
        memory.Write(0x1000, 0xA5);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.B = 0;
        core.State.C = 1;
        core.State.H = 0x10;
        core.State.L = 0;
        core.State.D = 0x20;
        core.State.E = 0;

        int cycles = RunInstruction(core);

        Assert.Equal(16, cycles);
        Assert.Equal((ushort)2, core.State.PC);
        Assert.Equal((ushort)0, (ushort)((core.State.B << 8) | core.State.C));
        Assert.Equal((byte)0xA5, memory.Read(0x2000));
    }

    [Fact]
    public void Tick_CpirWithOneByte_DoesNotRepeat()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0xED);
        memory.Write(1, 0xB1);
        memory.Write(0x1000, 0x24);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.B = 0;
        core.State.C = 1;
        core.State.H = 0x10;
        core.State.L = 0;
        core.State.A = 0x55;

        int cycles = RunInstruction(core);

        Assert.Equal(16, cycles);
        Assert.Equal((ushort)2, core.State.PC);
        Assert.Equal((byte)0, core.State.B);
        Assert.Equal((byte)0, core.State.C);
        Assert.Equal((ushort)0x1001, (ushort)((core.State.H << 8) | core.State.L));
    }

    [Fact]
    public void Tick_InirWithOneByte_DoesNotRepeat()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0xED);
        memory.Write(1, 0xB2);
        var ports = new FlatPorts();
        ports.Output(0x0134, 0x69);
        var core = new Z80Core(memory, ports);
        core.State.B = 1;
        core.State.C = 0x34;
        core.State.H = 0x10;
        core.State.L = 0;

        int cycles = RunInstruction(core);

        Assert.Equal(16, cycles);
        Assert.Equal((ushort)2, core.State.PC);
        Assert.Equal((byte)0, core.State.B);
        Assert.Equal((byte)0x69, memory.Read(0x1000));
    }

    [Fact]
    public void Tick_OtirWithOneByte_DoesNotRepeat()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0xED);
        memory.Write(1, 0xB3);
        memory.Write(0x1000, 0x96);
        var ports = new FlatPorts();
        var core = new Z80Core(memory, ports);
        core.State.B = 1;
        core.State.C = 0x56;
        core.State.H = 0x10;
        core.State.L = 0;

        int cycles = RunInstruction(core);

        Assert.Equal(16, cycles);
        Assert.Equal((ushort)2, core.State.PC);
        Assert.Equal((byte)0, core.State.B);
        Assert.Equal((byte)0x96, ports.Input(0x0056));
    }

    private static int RunInstruction(Z80Core core)
    {
        int cycles = 0;
        do
        {
            core.Tick();
            cycles++;
        }
        while (core.Pending != 0 && cycles < 64);

        return cycles;
    }
}
