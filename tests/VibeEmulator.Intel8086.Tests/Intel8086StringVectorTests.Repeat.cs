using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed partial class Intel8086StringVectorTests
{
    [Theory]
    [InlineData("A4", 13)]
    [InlineData("A4", 17)]
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
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(11)]
    public void RepMovsb_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "A4.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(17)]
    [InlineData(18)]
    public void RepStosb_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AA.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(16)]
    [InlineData(19)]
    public void RepLodsb_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AC.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(10)]
    public void RepLodsw_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AD.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(9)]
    public void RepStosw_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AB.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    public void RepScasw_MatchesPinnedVector(int testNumber)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "A9String", "AF.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == testNumber);

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, maxTStates: 4096);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }
}
