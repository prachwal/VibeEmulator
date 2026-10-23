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
