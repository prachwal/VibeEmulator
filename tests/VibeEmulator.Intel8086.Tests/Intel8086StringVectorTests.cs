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
}
