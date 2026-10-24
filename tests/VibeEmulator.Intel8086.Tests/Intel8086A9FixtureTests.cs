using System.Text.Json;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086A9FixtureTests
{
    private const string SourceSha = "e71c68d215a6bb8c356bd4cb3842de3bef345ca9";

    [Theory]
    [InlineData("A9String", 10)]
    [InlineData("A9Io", 8)]
    public void PreparedFixtures_HavePinnedSourceAndDeclaredTStateLimits(string group, int expectedFiles)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", group);
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        JsonElement root = manifest.RootElement;

        Assert.Equal(SourceSha, root.GetProperty("sourceSha").GetString());
        Assert.Equal(20, root.GetProperty("sampleRecordsPerFile").GetInt32());
        Assert.Equal("prepared-not-active", root.GetProperty("execution").GetString());
        Assert.Empty(root.GetProperty("active").EnumerateArray());
        JsonElement prepared = root.GetProperty("prepared");
        Assert.Equal(expectedFiles, prepared.GetArrayLength());

        foreach (JsonElement entry in prepared.EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            int expectedRecords = entry.GetProperty("records").GetInt32();
            int maxTStates = entry.GetProperty("maxTStates").GetInt32();
            using JsonDocument sample = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, file)));
            JsonElement vectors = sample.RootElement;

            Assert.Equal(20, expectedRecords);
            Assert.Equal(expectedRecords, vectors.GetArrayLength());
            Assert.All(vectors.EnumerateArray(), vector =>
                Assert.True(vector.GetProperty("cycles").GetArrayLength() < maxTStates));
        }
    }
}
