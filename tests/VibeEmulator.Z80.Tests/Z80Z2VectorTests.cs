using System.Text.Json;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80Z2VectorTests
{
    private static void Run(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", file);
        string json = File.ReadAllText(path);
        var tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(json)!;
        foreach (var test in tests)
        {
            var bus = new FlatMemory(0x10000);
            foreach (int[] e in test.Initial.Ram) bus.Write((ushort)e[0], (byte)e[1]);
            var core = new Z80Core(bus, new FlatPorts());
            ref var s = ref core.State;
            s.PC = (ushort)test.Initial.Pc; s.SP = (ushort)test.Initial.Sp; s.A = (byte)test.Initial.A; s.B = (byte)test.Initial.B;
            s.C = (byte)test.Initial.C; s.D = (byte)test.Initial.D; s.E = (byte)test.Initial.E; s.F = (byte)test.Initial.F;
            s.H = (byte)test.Initial.H; s.L = (byte)test.Initial.L; s.I = (byte)test.Initial.I; s.R = (byte)test.Initial.R;
            s.IX = (ushort)test.Initial.Ix; s.IY = (ushort)test.Initial.Iy;
            for (int ci = 0; ci < test.Cycles.Count; ci++)
            {
                var c = test.Cycles[ci];
                int? addr = c[0].ValueKind == JsonValueKind.Null ? null : c[0].GetInt32();
                int? data = c[1].ValueKind == JsonValueKind.Null ? null : c[1].GetInt32();
                string pins = c[2].GetString() ?? "----";
                core.Tick();
                if (addr.HasValue) Assert.True((ushort)addr.Value == core.LastAddress, $"{file} cycle {ci}: exp addr {addr.Value} got {core.LastAddress} pins {pins}");
                if (data.HasValue) Assert.True(core.LastData.HasValue && (byte)data.Value == core.LastData.Value, $"{file} cycle {ci}: exp data {data} got {core.LastData} pins {pins}");
                else Assert.True(core.LastData == null, $"{file} cycle {ci}: exp null got {core.LastData} pins {pins}");
                Assert.True(pins == core.LastPins, $"{file} cycle {ci}: exp pins {pins} got {core.LastPins}");
            }
            Assert.Equal(0, core.Pending);
            Assert.Equal(test.Final.Pc, core.State.PC);
        }
    }

    [Theory]
    [InlineData("40.json")]
    [InlineData("70.json")]
    [InlineData("06.json")]
    [InlineData("3a.json")]
    [InlineData("32.json")]
    [InlineData("02.json")]
    [InlineData("0a.json")]
    public void Vector_Z2_MatchesReference(string file) => Run(file);
}
