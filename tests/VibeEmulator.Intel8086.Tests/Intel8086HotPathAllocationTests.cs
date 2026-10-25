using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086HotPathAllocationTests
{
    [Fact]
    public void Tick_WithNopPrefetchQueue_DoesNotAllocateAfterWarmup()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        byte[] queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        for (int index = 0; index < 1_000; index++)
        {
            ExecuteNop(cpu, queue);
        }

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            ExecuteNop(cpu, queue);
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(0, allocatedBytes);
    }

    private static void ExecuteNop(Intel8086Core cpu, byte[] queue)
    {
        cpu.State.Queue = queue;
        cpu.State.QueueHead = 0;
        cpu.State.QueueCount = queue.Length;
        cpu.Tick();
        while (cpu.Pending > 0)
        {
            cpu.Tick();
        }
    }
}
