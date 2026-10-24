using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;

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
}
