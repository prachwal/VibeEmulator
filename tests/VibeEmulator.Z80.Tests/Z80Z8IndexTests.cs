using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z8IndexTests
{
    [Fact]
    public void Tick_LdHFromIndexedAddress_UpdatesHWithoutChangingIndexHigh()
    {
        var memory = new FlatMemory();
        memory.Write(0, 0xDD);
        memory.Write(1, 0x66);
        memory.Write(2, 0x02);
        memory.Write(0x1100, 0x5A);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.IX = 0x10FE;
        core.State.H = 0xA5;
        int cycles = 0;

        do
        {
            core.Tick();
            cycles++;
        }
        while (core.Pending != 0 && cycles < 32);

        Assert.Equal(19, cycles);
        Assert.Equal((byte)0x5A, core.State.H);
        Assert.Equal((ushort)0x10FE, core.State.IX);
    }
}
