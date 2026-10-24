using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086StackSegmentTests
{
    [Fact]
    public void PopSs_PendingIntrWaitsUntilFollowingInstructionCompletes()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x1000, 0x00);
        memory.Write(0x1001, 0x20);
        memory.Write(0x80, 0x45);
        memory.Write(0x81, 0x23);
        memory.Write(0x82, 0x89);
        memory.Write(0x83, 0x67);
        var cpu = new Intel8086Core(memory)
        {
            IrqLevel = true,
            IrqVector = 0x20,
        };
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x17, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x2000, cpu.State.SS);
        Assert.Equal((ushort)0x1002, cpu.State.SP);
        Assert.Equal((ushort)0x0021, cpu.State.IP);

        List<Intel8086BusCycle> followingInstructionCycles = [];
        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            followingInstructionCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x0022, cpu.State.IP);
        Assert.Equal((ushort)0x1002, cpu.State.SP);
        Assert.DoesNotContain(followingInstructionCycles, cycle => cycle.BusStatus == "INTA");

        cpu.Tick();
        Assert.True(cpu.Pending > 0);
        List<Intel8086BusCycle> interruptCycles = [];
        for (int cycle = 0; cycle < 100 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            interruptCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x2345, cpu.State.IP);
        Assert.Equal((ushort)0x6789, cpu.State.CS);
        Assert.Equal((ushort)0x0FFC, cpu.State.SP);
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "INTA");
        Assert.Equal((byte)0x22, memory.Read(0x20FFC));
        Assert.Equal((byte)0x00, memory.Read(0x20FFD));
    }
}
