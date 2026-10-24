using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086MemoryTransferTests
{
    [Fact]
    public void MovImmediateMemory_WrapsPhysicalAddressAtOneMiB()
    {
        var memory = new FlatAddressMemory(0x100000);
        var state = Intel8086State.Initial();
        state.DS = 0xFFFF;
        state.QueueCount = 5;
        state.Queue[0] = 0xC6;
        state.Queue[1] = 0x06;
        state.Queue[2] = 0x10;
        state.Queue[3] = 0x00;
        state.Queue[4] = 0x55;
        var cpu = new Intel8086Core(memory) { State = state };

        var cycles = new List<Intel8086BusCycle>();
        do
        {
            cpu.Tick();
            cycles.Add(cpu.LastBusCycle);
        }
        while (cpu.Pending != 0);

        Intel8086BusCycle writeStart = cycles.Single(cycle => cycle.BusStatus == "MEMW" && cycle.TState == "T1");
        Assert.Equal((byte)1, writeStart.PinBits);
        Assert.Equal(0u, writeStart.MultiplexedBus);
        Assert.Equal((byte)1, writeStart.Bhe);
        Assert.Equal((byte)0x55, memory.Read(0));
        Assert.Equal((byte)0, memory.Read(0xFFFFF));
    }
}
