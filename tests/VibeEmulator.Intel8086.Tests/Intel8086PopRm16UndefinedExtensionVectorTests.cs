using System.Text.Json;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086PopRm16UndefinedExtensionVectorTests
{
    [Fact]
    public void PopRm16UndefinedModRmExtensions_MatchPinnedMeasurements()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A12_8F_Ignored");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        JsonElement entries = manifest.RootElement.GetProperty("active");
        Assert.Equal(140, entries.GetArrayLength());

        int records = 0;
        var failures = new List<string>();
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            ushort flagsMask = entry.GetProperty("flagsMask").GetUInt16();
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(entry.GetProperty("records").GetInt32(), vectors.RootElement.GetArrayLength());
            records += vectors.RootElement.GetArrayLength();

            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                string context = $"{path}, test_num={vector.GetProperty("test_num").GetInt32()}";
                try
                {
                    VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, flagsMask: flagsMask);
                    if (!result.Passed)
                    {
                        failures.Add($"{context}: {result.Difference}");
                    }
                }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or
                    KeyNotFoundException or IndexOutOfRangeException)
                {
                    failures.Add($"{context}: {exception.Message}");
                }
            }
        }

        Assert.Equal(1461, records);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(20)));
    }
}
