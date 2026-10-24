using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Intel8086VectorRunner;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public class Intel8086VectorTests
{
    public static IEnumerable<object[]> ActiveVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            ushort flagsMask = entry.TryGetProperty("flagsMask", out JsonElement mask)
                ? mask.GetUInt16()
                : ushort.MaxValue;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in document.RootElement.EnumerateArray())
            {
                yield return [vector.Clone(), flagsMask];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ActiveVectors))]
    public void Implemented_Vector_MatchesStateQueueAndTStates(JsonElement vector, ushort flagsMask)
    {
        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, flagsMask: flagsMask);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Fact]
    public void Nop_Vector_ExceedingTStateLimit_FailsWithCaseIdentity()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "90.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement[0];

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, 2);

        Assert.False(result.Passed);
        Assert.Contains("test_num=0", result.Difference);
        Assert.Contains("T-states", result.Difference);
    }

    [Fact]
    public void Reset_SetsTestProgramCounterAndClearsPrefetchQueue()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));

        cpu.Reset(0x1234);

        Assert.Equal((ushort)0, cpu.State.CS);
        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Empty(cpu.State.PendingQueue.ToArray());
        Assert.Equal(0, cpu.Pending);
        Assert.Equal(0L, cpu.Cycles);
    }

    [Fact]
    public void Tick_WithEmptyPrefetchQueue_Throws()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));

        Assert.Throws<InvalidOperationException>(() => cpu.Tick());
    }

    [Fact]
    public void LockPrefix_AssertsMaximumModeLockForFollowingInstructionOnly()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.Queue = [0xF0, 0x90, 0x90, 0, 0, 0];
        cpu.State.QueueCount = 3;
        ushort initialFlags = cpu.State.Flags;

        for (int cycle = 0; cycle < 5; cycle++)
        {
            cpu.Tick();

            Assert.True(cpu.LastBusCycle.LockActive, $"LOCK must remain asserted through locked NOP T-state {cycle + 1}.");
        }

        Assert.Equal((ushort)2, cpu.State.IP);
        Assert.Equal((byte)0x90, cpu.State.PendingQueue[0]);
        Assert.Equal(initialFlags, cpu.State.Flags);
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void LockPrefix_RemainsAssertedAcrossLockedMemoryReadModifyWrite()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0100, 0x7F);
        memory.Write(0x0006, 0x90);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xFE, 0x06, 0x00, 0x01, 0x90];
        cpu.State.QueueCount = 6;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 40 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((byte)0x80, memory.Read(0x0100));
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void LockPrefix_DoesNotRepeatStringOperationWithoutRepPrefix()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0200, 0x5A);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xA4, 0x90, 0, 0, 0];
        cpu.State.QueueCount = 3;
        cpu.State.SI = 0x0200;
        cpu.State.DI = 0x0300;
        cpu.State.CX = 2;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 40 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((byte)0x5A, memory.Read(0x0300));
        Assert.Equal((byte)0x00, memory.Read(0x0301));
        Assert.Equal((ushort)0x0201, cpu.State.SI);
        Assert.Equal((ushort)0x0301, cpu.State.DI);
        Assert.Equal((ushort)2, cpu.State.CX);
        Assert.Equal(0, cpu.Pending);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void LockPrefix_WithRepMovsb_HoldsLockAcrossTheWholeStringBlock(int queueCount)
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0200, 0x11);
        memory.Write(0x0201, 0x22);
        memory.Write(0x0202, 0x33);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xF3, 0xA4, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = queueCount;
        cpu.State.SI = 0x0200;
        cpu.State.DI = 0x0300;
        cpu.State.CX = 3;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 100 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((ushort)0, cpu.State.CX);
        Assert.Equal((ushort)0x0203, cpu.State.SI);
        Assert.Equal((ushort)0x0303, cpu.State.DI);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, new[] { memory.Read(0x0300), memory.Read(0x0301), memory.Read(0x0302) });
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void Manifest_ListsOnlyActiveSamplesAndEachFileHasDeclaredCount()
    {
        string vectorsDirectory = Path.Combine(AppContext.BaseDirectory, "Vectors");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(vectorsDirectory, "manifest.json")));
        Assert.Equal("e71c68d215a6bb8c356bd4cb3842de3bef345ca9", manifest.RootElement.GetProperty("sourceSha").GetString());

        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            using var sample = JsonDocument.Parse(File.ReadAllText(Path.Combine(vectorsDirectory, file)));
            Assert.Equal(entry.GetProperty("records").GetInt32(), sample.RootElement.GetArrayLength());
        }
    }
}
