using System.IO.Compression;
using System.Text.Json;

namespace VibeEmulator.Intel8086VectorRunner;

internal static class Program
{
    private const int MaxTStatesPerVector = 256;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            PrintUsage();
            return 0;
        }

        try
        {
            Dictionary<string, string> options = ParseArguments(args);
            string sourceSha = options["--source-sha"];
            string vectorDirectory = Path.GetFullPath(options["--vectors"]);
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(options["--manifest"]));
            string manifestSha = manifest.RootElement.GetProperty("sourceSha").GetString()!;
            if (!string.Equals(sourceSha, manifestSha, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Source SHA mismatch: argument {sourceSha}, manifest {manifestSha}.");
            }

            var failures = new List<string>();
            int passed = 0;
            int skipped = 0;
            int total = 0;
            foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
            {
                string file = entry.GetProperty("file").GetString()!;
                int expectedRecords = entry.GetProperty("records").GetInt32();
                string jsonPath = Path.Combine(vectorDirectory, file);
                string gzipPath = Path.Combine(vectorDirectory, Path.GetFileNameWithoutExtension(file) + ".json.gz");
                string inputPath = File.Exists(jsonPath) ? jsonPath : gzipPath;
                if (!File.Exists(inputPath))
                {
                    failures.Add($"{file}: no JSON or JSON.GZ file found.");
                    continue;
                }

                int fileRecords = 0;
                await using FileStream fileStream = File.OpenRead(inputPath);
                await using Stream dataStream = inputPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                    ? new GZipStream(fileStream, CompressionMode.Decompress)
                    : fileStream;
                await foreach (JsonElement vector in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(dataStream))
                {
                    fileRecords++;
                    total++;
                    VectorRunResult result;
                    try
                    {
                        result = Intel8086VectorRunner.Run(vector, MaxTStatesPerVector);
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or KeyNotFoundException or IndexOutOfRangeException)
                    {
                        result = new VectorRunResult(false, 0, $"{file}: test_num={vector.GetProperty("test_num").GetInt32()}, hash={vector.GetProperty("test_hash").GetString()}: {exception.Message}");
                    }

                    if (result.Passed)
                    {
                        passed++;
                    }
                    else
                    {
                        failures.Add(result.Difference ?? $"{file}: unknown mismatch.");
                    }
                }

                if (fileRecords != expectedRecords)
                {
                    failures.Add($"{file}: expected {expectedRecords} records, found {fileRecords}.");
                }
            }

            var report = new
            {
                sourceSha,
                total,
                passed,
                skipped,
                mismatched = failures.Count,
                failures,
            };
            string reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (options.TryGetValue("--report", out string? reportPath))
            {
                await File.WriteAllTextAsync(reportPath, reportJson);
            }

            Console.WriteLine($"SST 8086 {sourceSha}: passed {passed}, skipped {skipped}, mismatched {failures.Count}.");
            if (failures.Count > 0)
            {
                Console.Error.WriteLine(reportJson);
                return 1;
            }

            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or JsonException or KeyNotFoundException)
        {
            Console.Error.WriteLine(exception.Message);
            PrintUsage();
            return 2;
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] is not ("--vectors" or "--manifest" or "--source-sha" or "--report"))
            {
                throw new ArgumentException($"Invalid argument near '{args[index]}'.");
            }

            options.Add(args[index], args[index + 1]);
        }

        foreach (string required in new[] { "--vectors", "--manifest", "--source-sha" })
        {
            if (!options.ContainsKey(required))
            {
                throw new ArgumentException($"Required argument {required} is missing.");
            }
        }

        return options;
    }

    private static void PrintUsage() => Console.Error.WriteLine(
        "Usage: VibeEmulator.Intel8086VectorRunner --vectors <directory> --manifest <manifest.json> --source-sha <sha> [--report <path>]");
}
