using System.Text.Json;
using System.Text.Json.Serialization;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80VectorTests
{
    [Fact]
    public void Manifest_FilesExistAndHaveConfiguredSampleSize()
    {
        foreach (string fileName in Z80VectorManifest.VectorFiles)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Vectors", fileName);
            Assert.True(File.Exists(path), $"Brak pliku z manifestu: {fileName}.");
            List<Z80VectorTest> tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(File.ReadAllText(path))!;
            Assert.Equal(Z80VectorManifest.SampleSize, tests.Count);
        }
    }

    [Fact]
    public void Loader_HandlesNullAndPorts()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "sample ports.json");
        string json = File.ReadAllText(path);
        var tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(json);
        Assert.NotNull(tests);
        Assert.Single(tests);
        Assert.NotNull(tests[0].Ports);
        Assert.True(tests[0].Cycles[0][0].ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void Loader_HandlesFileNameWithSpaces()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "sample with spaces.json");
        string json = File.ReadAllText(path);
        var tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(json);
        Assert.NotNull(tests);
        Assert.Empty(tests);
    }

    [Fact]
    public void Stall_AdvancesClockWithoutChangingCpuState()
    {
        var core = new Z80Core(new FlatMemory(), new FlatPorts());
        core.State.PC = 0x1234;
        core.State.A = 0xA5;
        core.State.R = 0x7F;
        long cycles = core.Cycles;
        int pending = core.Pending;

        core.Stall();

        Assert.Equal(cycles + 1, core.Cycles);
        Assert.Equal(pending, core.Pending);
        Assert.Equal((ushort)0x1234, core.State.PC);
        Assert.Equal((byte)0xA5, core.State.A);
        Assert.Equal((byte)0x7F, core.State.R);
    }

    [Fact]
    public void Tick_DoesNotAllocateAfterWarmup()
    {
        var core = new Z80Core(new FlatMemory(), new FlatPorts());
        for (int i = 0; i < 1_000; i++)
        {
            core.Tick();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10_000; i++)
        {
            core.Tick();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Vector_00_Nop_MatchesReference()
    {
        Z80VectorTestRunner.Run("00.json");
    }
}

public sealed class Z80VectorTest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("initial")]
    public Z80VectorState Initial { get; set; } = new();

    [JsonPropertyName("final")]
    public Z80VectorState Final { get; set; } = new();

    [JsonPropertyName("cycles")]
    public List<JsonElement> Cycles { get; set; } = new();

    [JsonPropertyName("ports")]
    public List<JsonElement>? Ports { get; set; }
}

public sealed class Z80VectorState
{
    [JsonPropertyName("pc")] public int Pc { get; set; }
    [JsonPropertyName("sp")] public int Sp { get; set; }
    [JsonPropertyName("a")] public int A { get; set; }
    [JsonPropertyName("b")] public int B { get; set; }
    [JsonPropertyName("c")] public int C { get; set; }
    [JsonPropertyName("d")] public int D { get; set; }
    [JsonPropertyName("e")] public int E { get; set; }
    [JsonPropertyName("f")] public int F { get; set; }
    [JsonPropertyName("h")] public int H { get; set; }
    [JsonPropertyName("l")] public int L { get; set; }
    [JsonPropertyName("i")] public int I { get; set; }
    [JsonPropertyName("r")] public int R { get; set; }
    [JsonPropertyName("ix")] public int Ix { get; set; }
    [JsonPropertyName("iy")] public int Iy { get; set; }
    [JsonPropertyName("af_")] public int Af_ { get; set; }
    [JsonPropertyName("bc_")] public int Bc_ { get; set; }
    [JsonPropertyName("de_")] public int De_ { get; set; }
    [JsonPropertyName("hl_")] public int Hl_ { get; set; }
    [JsonPropertyName("wz")] public int Wz { get; set; }
    [JsonPropertyName("im")] public int Im { get; set; }
    [JsonPropertyName("iff1")] public int Iff1 { get; set; }
    [JsonPropertyName("iff2")] public int Iff2 { get; set; }
    [JsonPropertyName("ei")] public int Ei { get; set; }
    [JsonPropertyName("p")] public int P { get; set; }
    [JsonPropertyName("q")] public int Q { get; set; }
    [JsonPropertyName("ram")] public List<int[]> Ram { get; set; } = new();
}

public sealed class Z80BusAccess
{
    [JsonPropertyName("")] // placeholder, cycles is array not object
    public int Dummy { get; set; }
}
