using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086PrefixedCallFarVectorTests
{
    [Fact]
    public void SegmentPrefixedFarCall_MatchesPinnedVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A12PrefixedCallFar");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        int total = 0;
        List<string> failures = [];

        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, Path.GetFileName(entry.GetProperty("file").GetString()!));
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            int expectedRecords = entry.GetProperty("records").GetInt32();
            int fileRecords = 0;

            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector);
                if (!result.Passed)
                {
                    failures.Add(
                        $"{Path.GetFileName(path)}, test_num={vector.GetProperty("test_num").GetInt32()}: {result.Difference}");
                }

                fileRecords++;
                total++;
            }

            Assert.Equal(expectedRecords, fileRecords);
        }

        Assert.Equal(80, total);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
