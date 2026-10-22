using System.Text.Json;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

internal static class Z80VectorTestRunner
{
    public static void Run(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", fileName);
        List<Z80VectorTest> tests = JsonSerializer.Deserialize<List<Z80VectorTest>>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Pusty plik wektorów: {path}");
        Assert.Equal(Z80VectorManifest.SampleSize, tests.Count);

        foreach (Z80VectorTest test in tests)
        {
            RunTest(test);
        }
    }

    private static void RunTest(Z80VectorTest test)
    {
        string context = $"{test.Name}";
        var memory = new RecordingMemoryBus();
        byte[] expectedRam = new byte[0x10000];
        LoadRam(test.Initial.Ram, expectedRam, memory);

        var core = new Z80Core(memory, new FlatPorts());
        SetState(ref core.State, test.Initial);

        for (int tState = 0; tState < test.Cycles.Count; tState++)
        {
            JsonElement cycle = test.Cycles[tState];
            ushort? address = ReadNullableUShort(cycle[0]);
            byte? data = ReadNullableByte(cycle[1]);
            string pins = cycle[2].GetString() ?? "----";

            int accessCount = memory.Accesses.Count;
            memory.CurrentTState = tState;
            core.Tick();

            if (address.HasValue)
            {
                Equal(context, $"T{tState} address", address.Value, core.LastAddress);
            }

            Equal(context, $"T{tState} data", data, core.LastData);
            Equal(context, $"T{tState} pins", pins, core.LastPins);
            int expectedPending = tState == 0 ? 3 : test.Cycles.Count - tState - 1;
            Equal(context, $"T{tState} Pending", expectedPending, core.Pending);

            bool memoryRead = pins.Length >= 3 && pins[0] == 'r' && pins[2] == 'm';
            bool memoryWrite = pins.Length >= 3 && pins[1] == 'w' && pins[2] == 'm';
            int expectedAccesses = memoryRead || memoryWrite ? 1 : 0;
            Equal(context, $"T{tState} memory access count", expectedAccesses, memory.Accesses.Count - accessCount);

            if (expectedAccesses == 1)
            {
                MemoryAccess actual = memory.Accesses[^1];
                Equal(context, $"T{tState} memory access T-state", tState, actual.TState);
                Equal(context, $"T{tState} memory access address", address, (ushort?)actual.Address);
                Equal(context, $"T{tState} memory access direction", memoryWrite, actual.IsWrite);
                if (memoryWrite && data.HasValue)
                {
                    Equal(context, $"T{tState} memory write data", data.Value, actual.Value);
                }
            }
        }

        Equal(context, "Pending after instruction", 0, core.Pending);
        AssertState(context, test.Final, core.State);
        LoadRam(test.Final.Ram, expectedRam, null);

        ReadOnlySpan<byte> actualRam = memory.Memory.Span;
        for (int address = 0; address < expectedRam.Length; address++)
        {
            if (expectedRam[address] != actualRam[address])
            {
                Assert.Fail($"{context}: RAM ${address:X4}: oczekiwano ${expectedRam[address]:X2}, otrzymano ${actualRam[address]:X2}.");
            }
        }
    }

    private static void SetState(ref Z80State state, Z80VectorState vector)
    {
        state.PC = (ushort)vector.Pc;
        state.SP = (ushort)vector.Sp;
        state.A = (byte)vector.A;
        state.B = (byte)vector.B;
        state.C = (byte)vector.C;
        state.D = (byte)vector.D;
        state.E = (byte)vector.E;
        state.F = (byte)vector.F;
        state.H = (byte)vector.H;
        state.L = (byte)vector.L;
        state.I = (byte)vector.I;
        state.R = (byte)vector.R;
        state.IX = (ushort)vector.Ix;
        state.IY = (ushort)vector.Iy;
        state.AF_ = (ushort)vector.Af_;
        state.BC_ = (ushort)vector.Bc_;
        state.DE_ = (ushort)vector.De_;
        state.HL_ = (ushort)vector.Hl_;
        state.WZ = (ushort)vector.Wz;
        state.IM = (byte)vector.Im;
        state.IFF1 = vector.Iff1 != 0;
        state.IFF2 = vector.Iff2 != 0;
        state.EI = (byte)vector.Ei;
        state.P = (byte)vector.P;
        state.Q = (byte)vector.Q;
    }

    private static void AssertState(string context, Z80VectorState expected, Z80State actual)
    {
        Equal(context, "PC", expected.Pc, actual.PC);
        Equal(context, "SP", expected.Sp, actual.SP);
        Equal(context, "A", expected.A, actual.A);
        Equal(context, "B", expected.B, actual.B);
        Equal(context, "C", expected.C, actual.C);
        Equal(context, "D", expected.D, actual.D);
        Equal(context, "E", expected.E, actual.E);
        Equal(context, "F", expected.F, actual.F);
        Equal(context, "H", expected.H, actual.H);
        Equal(context, "L", expected.L, actual.L);
        Equal(context, "I", expected.I, actual.I);
        Equal(context, "R", expected.R, actual.R);
        Equal(context, "IX", expected.Ix, actual.IX);
        Equal(context, "IY", expected.Iy, actual.IY);
        Equal(context, "AF_", expected.Af_, actual.AF_);
        Equal(context, "BC_", expected.Bc_, actual.BC_);
        Equal(context, "DE_", expected.De_, actual.DE_);
        Equal(context, "HL_", expected.Hl_, actual.HL_);
        Equal(context, "WZ", expected.Wz, actual.WZ);
        Equal(context, "IM", expected.Im, actual.IM);
        Equal(context, "IFF1", expected.Iff1 != 0, actual.IFF1);
        Equal(context, "IFF2", expected.Iff2 != 0, actual.IFF2);
        Equal(context, "EI", expected.Ei, actual.EI);
        Equal(context, "P", expected.P, actual.P);
        Equal(context, "Q", expected.Q, actual.Q);
    }

    private static void LoadRam(List<int[]> entries, byte[] expected, RecordingMemoryBus? target)
    {
        foreach (int[] entry in entries)
        {
            ushort address = (ushort)entry[0];
            byte value = (byte)entry[1];
            expected[address] = value;
            target?.Initialize(address, value);
        }
    }

    private static ushort? ReadNullableUShort(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : (ushort)element.GetInt32();

    private static byte? ReadNullableByte(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : (byte)element.GetInt32();

    private static void Equal<T>(string context, string field, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Assert.Fail($"{context}: {field}: expected {expected}, actual {actual}.");
        }
    }

    private sealed class RecordingMemoryBus : IMemoryBus
    {
        private readonly FlatMemory _memory = new();

        public List<MemoryAccess> Accesses { get; } = new();

        public int CurrentTState { get; set; }

        public FlatMemory Memory => _memory;

        public byte Read(ushort address)
        {
            byte value = _memory.Read(address);
            Accesses.Add(new MemoryAccess(CurrentTState, address, value, false));
            return value;
        }

        public void Write(ushort address, byte value)
        {
            Accesses.Add(new MemoryAccess(CurrentTState, address, value, true));
            _memory.Write(address, value);
        }

        public void Initialize(ushort address, byte value) => _memory.Write(address, value);
    }

    private readonly record struct MemoryAccess(int TState, ushort Address, byte Value, bool IsWrite);
}
