using System.Text.Json;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086A9FixtureTests
{
    private const string SourceSha = "e71c68d215a6bb8c356bd4cb3842de3bef345ca9";

    [Theory]
    [InlineData("A9String", 11)]
    [InlineData("A9Io", 8)]
    public void PreparedFixtures_HavePinnedSourceAndDeclaredTStateLimits(string group, int expectedFiles)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", group);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        JsonElement root = manifest.RootElement;

        Assert.Equal(SourceSha, root.GetProperty("sourceSha").GetString());
        Assert.Equal(20, root.GetProperty("sampleRecordsPerFile").GetInt32());
        string expectedExecution = group == "A9String" ? "partially-active" : "prepared-not-active";
        Assert.Equal(expectedExecution, root.GetProperty("execution").GetString());
        JsonElement active = root.GetProperty("active");
        Assert.Equal(group == "A9String" ? 11 : 0, active.GetArrayLength());
        foreach (JsonElement entry in active.EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            int maxTStates = entry.GetProperty("maxTStates").GetInt32();
            using var sample = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, file)));
            JsonElement vectors = sample.RootElement;
            if (entry.TryGetProperty("testNumbers", out JsonElement testNumbers))
            {
                foreach (JsonElement number in testNumbers.EnumerateArray())
                {
                    JsonElement vector = vectors.EnumerateArray().Single(item => item.GetProperty("test_num").GetInt32() == number.GetInt32());
                    Assert.True(vector.GetProperty("cycles").GetArrayLength() < maxTStates);
                }
            }
            else
            {
                Assert.Equal(entry.GetProperty("records").GetInt32(), vectors.GetArrayLength());
                Assert.All(vectors.EnumerateArray(), vector => Assert.True(vector.GetProperty("cycles").GetArrayLength() < maxTStates));
            }
        }

        JsonElement prepared = root.GetProperty("prepared");
        Assert.Equal(expectedFiles, prepared.GetArrayLength());

        foreach (JsonElement entry in prepared.EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            int expectedRecords = entry.GetProperty("records").GetInt32();
            int maxTStates = entry.GetProperty("maxTStates").GetInt32();
            using var sample = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, file)));
            JsonElement vectors = sample.RootElement;

            Assert.Equal(20, expectedRecords);
            Assert.Equal(expectedRecords, vectors.GetArrayLength());
            Assert.All(vectors.EnumerateArray(), vector =>
                Assert.True(vector.GetProperty("cycles").GetArrayLength() < maxTStates));
        }
    }
}
