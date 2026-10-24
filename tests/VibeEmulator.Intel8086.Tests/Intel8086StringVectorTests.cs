using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086StringVectorTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(19)]
    public void Movsb_Unprefixed_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "A4.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Fact]
    public void Movsb_DirectionFlagClear_MatchesTwentyFilteredPinnedVectors()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "A4.df0.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.Equal(20, document.RootElement.GetArrayLength());
        foreach (JsonElement vector in document.RootElement.EnumerateArray())
        {
            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

            Assert.True(result.Passed, result.Difference);
            Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
        }
    }

    [Theory]
    [InlineData(8)]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(17)]
    public void Movsw_Unprefixed_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "A5.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("AA", 0)]
    [InlineData("AB", 0)]
    [InlineData("AB", 16)]
    [InlineData("AB", 19)]
    public void Stos_Unprefixed_MatchesPinnedVector(string opcode, int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("AC", 6)]
    [InlineData("AC", 10)]
    [InlineData("AC", 13)]
    [InlineData("AC", 15)]
    [InlineData("AD", 1)]
    [InlineData("AD", 3)]
    [InlineData("AD", 12)]
    [InlineData("AD", 14)]
    [InlineData("AD", 16)]
    [InlineData("AD", 18)]
    public void Lods_Unprefixed_MatchesPinnedVector(string opcode, int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("AE", 11)]
    [InlineData("AF", 4)]
    [InlineData("AF", 17)]
    public void Scas_Unprefixed_MatchesPinnedVector(string opcode, int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("A6", 2)]
    [InlineData("A6", 7)]
    [InlineData("A6", 9)]
    [InlineData("A6", 12)]
    [InlineData("A6", 16)]
    [InlineData("A6", 17)]
    [InlineData("A6", 19)]
    [InlineData("A7", 0)]
    public void Cmps_Unprefixed_MatchesPinnedVector(string opcode, int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("A4", 13)]
    [InlineData("AE", 15)]
    public void RepString_WithZeroCount_MatchesPinnedVector(string opcode, int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Fact]
    public void RepneScasb_WithFourIterations_MatchesPinnedVector()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AE.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == 17);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 128);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData("E4")]
    [InlineData("EC")]
    public void InputByte_Unprefixed_MatchesAllPinnedSamples(string opcode)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(20, document.RootElement.GetArrayLength());

        foreach (JsonElement vector in document.RootElement.EnumerateArray())
        {
            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);
            Assert.True(result.Passed, result.Difference);
            Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
        }
    }

    [Fact]
    public void ByteInput_UsesInjectedPortBusAtSelectedPort()
    {
        foreach ((string opcode, int testNumber, ushort expectedPort) in new[]
        {
            ("E4", 0, (ushort)128),
            ("EC", 0, (ushort)0x3277),
        })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);
            var ports = new RecordingPorts();

            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64, portBus: ports);

            Assert.True(result.Passed, result.Difference);
            Assert.Equal(new[] { expectedPort }, ports.InputPorts);
            Assert.Empty(ports.Outputs);
        }
    }

    [Theory]
    [InlineData("E6")]
    [InlineData("EE")]
    public void OutputByte_Unprefixed_MatchesAllPinnedSamples(string opcode)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(20, document.RootElement.GetArrayLength());

        foreach (JsonElement vector in document.RootElement.EnumerateArray())
        {
            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);
            Assert.True(result.Passed, result.Difference);
            Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
        }
    }

    [Fact]
    public void ByteOutput_UsesInjectedPortBusAtSelectedPortAndValue()
    {
        foreach ((string opcode, int testNumber, ushort expectedPort, byte expectedValue) in new[]
        {
            ("E6", 0, (ushort)98, (byte)0x31),
            ("EE", 0, (ushort)0x7F2E, (byte)0xD5),
        })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);
            var ports = new RecordingPorts();

            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64, portBus: ports);

            Assert.True(result.Passed, result.Difference);
            Assert.Equal(new[] { (expectedPort, expectedValue) }, ports.Outputs);
            Assert.Empty(ports.InputPorts);
        }
    }

    [Theory]
    [InlineData("E5")]
    [InlineData("ED")]
    public void InputWord_Unprefixed_MatchesAllPinnedSamples(string opcode)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(20, document.RootElement.GetArrayLength());

        foreach (JsonElement vector in document.RootElement.EnumerateArray())
        {
            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);
            Assert.True(result.Passed, result.Difference);
            Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
        }
    }

    [Fact]
    public void WordInput_UsesAdjacentPortsInAscendingOrder()
    {
        foreach ((string opcode, int testNumber, ushort firstPort) in new[]
        {
            ("E5", 0, (ushort)103),
            ("ED", 0, (ushort)0x5773),
        })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);
            var ports = new RecordingPorts();

            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64, portBus: ports);

            Assert.True(result.Passed, result.Difference);
            Assert.Equal(new[] { firstPort, unchecked((ushort)(firstPort + 1)) }, ports.InputPorts);
            Assert.Empty(ports.Outputs);
        }
    }

    [Theory]
    [InlineData("E7")]
    [InlineData("EF")]
    public void OutputWord_Unprefixed_MatchesAllPinnedSamples(string opcode)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(20, document.RootElement.GetArrayLength());

        foreach (JsonElement vector in document.RootElement.EnumerateArray())
        {
            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64);
            Assert.True(result.Passed, result.Difference);
            Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
        }
    }

    [Fact]
    public void WordOutput_UsesAdjacentPortsInAscendingOrder()
    {
        foreach ((string opcode, int testNumber, ushort firstPort, ushort ax) in new[]
        {
            ("E7", 0, (ushort)161, (ushort)0xB4FF),
            ("EF", 0, (ushort)0xB153, (ushort)0x0000),
        })
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9Io", $"{opcode}.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);
            var ports = new RecordingPorts();

            VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 64, portBus: ports);

            Assert.True(result.Passed, result.Difference);
            Assert.Equal(new[] { (firstPort, (byte)ax), (unchecked((ushort)(firstPort + 1)), (byte)(ax >> 8)) }, ports.Outputs);
            Assert.Empty(ports.InputPorts);
        }
    }

    private sealed class RecordingPorts : IPortBus
    {
        public List<ushort> InputPorts { get; } = [];

        public List<(ushort Port, byte Value)> Outputs { get; } = [];

        public byte Input(ushort port)
        {
            InputPorts.Add(port);
            return byte.MaxValue;
        }

        public void Output(ushort port, byte value) => Outputs.Add((port, value));
    }
}
