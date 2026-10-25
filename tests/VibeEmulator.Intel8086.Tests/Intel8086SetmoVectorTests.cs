using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086SetmoVectorTests
{
    [Fact]
    public void SetmoD0ThroughD3_MatchesPinnedVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A12Setmo");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            ushort flagsMask = entry.GetProperty("flagsMask").GetUInt16();
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, flagsMask: flagsMask);

                Assert.True(result.Passed, $"{path}, test_num={vector.GetProperty("test_num").GetInt32()}: {result.Difference}");
            }
        }
    }
}
