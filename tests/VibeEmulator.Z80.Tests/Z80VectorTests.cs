using System.Text.Json;
using System.Text.Json.Serialization;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80VectorTests
{
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
    public void Vector_00_Nop_MatchesReference()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "00.json");
        string json = File.ReadAllText(path);
        var tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(json)
            ?? throw new InvalidDataException($"Pusty plik: {path}");

        Assert.Equal(20, tests.Count);

        foreach (Z80VectorTest test in tests)
        {
            var bus = new FlatMemory(0x10000);
            foreach (int[] entry in test.Initial.Ram)
            {
                bus.Write((ushort)entry[0], (byte)entry[1]);
            }

            var ports = new FlatPorts();
            var core = new Z80Core(bus, ports);
            ref Z80State state = ref core.State;
            state.PC = (ushort)test.Initial.Pc;
            state.SP = (ushort)test.Initial.Sp;
            state.A = (byte)test.Initial.A;
            state.B = (byte)test.Initial.B;
            state.C = (byte)test.Initial.C;
            state.D = (byte)test.Initial.D;
            state.E = (byte)test.Initial.E;
            state.F = (byte)test.Initial.F;
            state.H = (byte)test.Initial.H;
            state.L = (byte)test.Initial.L;
            state.I = (byte)test.Initial.I;
            state.R = (byte)test.Initial.R;
            state.IX = (ushort)test.Initial.Ix;
            state.IY = (ushort)test.Initial.Iy;
            state.AF_ = (ushort)test.Initial.Af_;
            state.BC_ = (ushort)test.Initial.Bc_;
            state.DE_ = (ushort)test.Initial.De_;
            state.HL_ = (ushort)test.Initial.Hl_;
            state.WZ = (ushort)test.Initial.Wz;
            state.IM = (byte)test.Initial.Im;
            state.IFF1 = test.Initial.Iff1 != 0;
            state.IFF2 = test.Initial.Iff2 != 0;
            state.EI = (byte)test.Initial.Ei;
            state.P = (byte)test.Initial.P;
            state.Q = (byte)test.Initial.Q;

            for (int cycle = 0; cycle < test.Cycles.Count; cycle++)
            {
                JsonElement c = test.Cycles[cycle];
                int? addr = c[0].ValueKind == JsonValueKind.Null ? null : c[0].GetInt32();
                int? data = c[1].ValueKind == JsonValueKind.Null ? null : c[1].GetInt32();
                string pins = c[2].GetString() ?? "----";
                core.Tick();

                Assert.Equal(addr.HasValue ? (ushort)addr.Value : core.LastAddress, core.LastAddress);
                // compare data
                if (data.HasValue)
                {
                    Assert.Equal((byte)data.Value, core.LastData);
                }
                else
                {
                    Assert.Null(core.LastData);
                }
                Assert.Equal(pins, core.LastPins);
                if (addr.HasValue)
                {
                    Assert.Equal((ushort)addr.Value, core.LastAddress);
                }
            }

            Assert.Equal(0, core.Pending);
            Assert.Equal(test.Final.Pc, core.State.PC);
            Assert.Equal(test.Final.Sp, core.State.SP);
            Assert.Equal(test.Final.A, core.State.A);
            Assert.Equal(test.Final.F, core.State.F);
            Assert.Equal(test.Final.R, core.State.R);
        }
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
