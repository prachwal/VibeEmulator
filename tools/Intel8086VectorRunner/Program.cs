using System.IO.Compression;
using System.Text.Json;

namespace VibeEmulator.Intel8086VectorRunner;

internal static class Program
{
    private const int MaxTStatesPerVector = 1024;

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
            int mismatched = 0;
            int timeouts = 0;
            int parseErrors = 0;
            int total = 0;
            foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
            {
                string file = entry.GetProperty("file").GetString()!;
                int expectedRecords = entry.GetProperty("records").GetInt32();
                ushort? explicitIgnoredFlagsMask = entry.TryGetProperty("ignoredFlagsMask", out JsonElement ignoredMask)
                    ? ignoredMask.GetUInt16()
                    : null;
                ushort flagsMask = entry.TryGetProperty("flagsMask", out JsonElement mask)
                    ? mask.GetUInt16()
                    : (ushort)~(explicitIgnoredFlagsMask ?? 0);
                ushort ignoredFlagsMask = explicitIgnoredFlagsMask ?? (ushort)~flagsMask;
                int maxTStates = entry.TryGetProperty("maxTStates", out JsonElement maxTStatesElement)
                    ? maxTStatesElement.GetInt32()
                    : MaxTStatesPerVector;
                if (maxTStates <= 0)
                {
                    throw new InvalidDataException($"{file}: maxTStates must be positive.");
                }
                string jsonPath = Path.Combine(vectorDirectory, file);
                string gzipPath = Path.Combine(vectorDirectory, Path.GetFileNameWithoutExtension(file) + ".json.gz");
                string inputPath = File.Exists(jsonPath) ? jsonPath : gzipPath;
                if (!File.Exists(inputPath))
                {
                    parseErrors++;
                    failures.Add($"{file}: no JSON or JSON.GZ file found.");
                    continue;
                }

                int fileRecords = 0;
                bool fileParseError = false;
                await using FileStream fileStream = File.OpenRead(inputPath);
                await using Stream dataStream = inputPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                    ? new GZipStream(fileStream, CompressionMode.Decompress)
                    : fileStream;
                try
                {
                    await foreach (JsonElement vector in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(dataStream))
                    {
                        fileRecords++;
                        total++;
                        int expectedTStates = vector.GetProperty("cycles").GetArrayLength();
                        int vectorMaxTStates = entry.TryGetProperty("maxTStates", out _)
                            ? maxTStates
                            : Math.Max(maxTStates, expectedTStates + 1);
                        VectorRunResult result;
                        try
                        {
                            IReadOnlyDictionary<uint, byte>? ramMasks = IsDivideError(vector)
                                ? GetSavedFlagsRamMasks(vector.GetProperty("initial").GetProperty("regs"), ignoredFlagsMask)
                                : null;
                            IReadOnlyDictionary<int, ushort>? cycleDataMasks = IsDivideError(vector)
                                ? GetSavedFlagsCycleMasks(vector, ignoredFlagsMask)
                                : null;
                            result = Intel8086VectorRunner.Run(
                                vector,
                                vectorMaxTStates,
                                flagsMask,
                                ramMasks,
                                cycleDataMasks);
                        }
                        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or KeyNotFoundException or IndexOutOfRangeException)
                        {
                            VectorRunStatus status = IsUnsupported(exception.Message)
                                ? VectorRunStatus.Skipped
                                : VectorRunStatus.Mismatched;
                            result = new VectorRunResult(status, 0, $"{file}: test_num={vector.GetProperty("test_num").GetInt32()}, hash={vector.GetProperty("test_hash").GetString()}: {exception.Message}");
                        }

                        switch (result.Status)
                        {
                            case VectorRunStatus.Passed:
                                passed++;
                                break;
                            case VectorRunStatus.Skipped:
                                skipped++;
                                failures.Add($"{file}: {result.Difference ?? "unsupported case."}");
                                break;
                            case VectorRunStatus.TimedOut:
                                timeouts++;
                                failures.Add($"{file}: {result.Difference ?? "T-state limit exceeded."}");
                                break;
                            case VectorRunStatus.ParseError:
                                parseErrors++;
                                failures.Add($"{file}: {result.Difference ?? "invalid vector data."}");
                                break;
                            default:
                                mismatched++;
                                failures.Add($"{file}: {result.Difference ?? "unknown mismatch."}");
                                break;
                        }
                    }
                }
                catch (JsonException exception)
                {
                    parseErrors++;
                    fileParseError = true;
                    failures.Add($"{file}: JSON parse error after {fileRecords} records: {exception.Message}");
                }

                if (!fileParseError && fileRecords != expectedRecords)
                {
                    parseErrors++;
                    failures.Add($"{file}: expected {expectedRecords} records, found {fileRecords}.");
                }
            }

            var report = new
            {
                sourceSha,
                total,
                passed,
                skipped,
                mismatched,
                timeouts,
                parseErrors,
                failures,
            };
            string reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (options.TryGetValue("--report", out string? reportPath))
            {
                await File.WriteAllTextAsync(reportPath, reportJson);
            }

            Console.WriteLine($"SST 8086 {sourceSha}: total {total}, passed {passed}, skipped {skipped}, mismatched {mismatched}, timeouts {timeouts}, parse errors {parseErrors}.");
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

    private static bool IsUnsupported(string message) =>
        message.Contains("not implemented", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("Only FF /0", StringComparison.Ordinal) ||
        message.Contains("Only F6/F7", StringComparison.Ordinal) ||
        message.Contains("Only D0-D3", StringComparison.Ordinal) ||
        message.Contains("POP r/m16 obsługuje wyłącznie rozszerzenie ModR/M /0", StringComparison.Ordinal) ||
        message.Contains("C7 obsługuje wyłącznie kodowanie /0", StringComparison.Ordinal) ||
        message.Contains("Segment-prefixed C7 requires", StringComparison.Ordinal) ||
        message.Contains("Nadpisanie segmentu jest obsługiwane tylko", StringComparison.Ordinal) ||
        message.Contains("W tym etapie prefiks segmentowy jest obsługiwany", StringComparison.Ordinal) ||
        message.Contains("Segment-prefixed C6 requires", StringComparison.Ordinal) ||
        message.Contains("C6 obsługuje wyłącznie kodowanie /0", StringComparison.Ordinal) ||
        message.Contains("F6/F7 memory form requires its displacement bytes", StringComparison.Ordinal);

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

    private static bool IsDivideError(JsonElement vector)
    {
        JsonElement registers = vector.GetProperty("final").GetProperty("regs");
        return registers.TryGetProperty("cs", out JsonElement cs) && cs.GetUInt16() == 0 &&
            registers.TryGetProperty("ip", out JsonElement ip) && ip.GetUInt16() == 0x0400;
    }

    private static Dictionary<uint, byte> GetSavedFlagsRamMasks(JsonElement registers, ushort ignoredFlagsMask)
    {
        uint address = (((uint)registers.GetProperty("ss").GetUInt16() << 4) +
            unchecked((ushort)(registers.GetProperty("sp").GetUInt16() - 2))) & 0xFFFFF;
        byte[] masks = [(byte)~ignoredFlagsMask, (byte)~(ignoredFlagsMask >> 8)];
        return new Dictionary<uint, byte>
        {
            [address] = masks[0],
            [(address + 1) & 0xFFFFF] = masks[1],
        };
    }

    private static Dictionary<int, ushort> GetSavedFlagsCycleMasks(JsonElement vector, ushort ignoredFlagsMask)
    {
        JsonElement registers = vector.GetProperty("initial").GetProperty("regs");
        uint firstAddress = (((uint)registers.GetProperty("ss").GetUInt16() << 4) +
            unchecked((ushort)(registers.GetProperty("sp").GetUInt16() - 2))) & 0xFFFFF;
        byte[] masks = [(byte)~ignoredFlagsMask, (byte)~(ignoredFlagsMask >> 8)];
        var cycleMasks = new Dictionary<int, ushort>();
        JsonElement cycles = vector.GetProperty("cycles");
        for (int index = 0; index < cycles.GetArrayLength(); index++)
        {
            JsonElement cycle = cycles[index];
            if (cycle[0].GetByte() == 0 || cycle[1].GetUInt32() != firstAddress ||
                cycle[7].GetString() != "MEMW" || cycle[8].GetString() != "T1")
            {
                continue;
            }

            if ((firstAddress & 1) == 0)
            {
                cycleMasks[index + 2] = (ushort)~ignoredFlagsMask;
            }
            else
            {
                for (int byteIndex = 0; byteIndex < 2; byteIndex++)
                {
                    int dataCycleIndex = index + (byteIndex * 4) + 2;
                    JsonElement writeCycle = cycles[dataCycleIndex];
                    ushort laneMask = writeCycle[5].GetByte() == 1
                        ? masks[byteIndex]
                        : (ushort)(masks[byteIndex] << 8);
                    cycleMasks[dataCycleIndex] = laneMask;
                }
            }

            break;
        }

        return cycleMasks;
    }

    private static void PrintUsage() => Console.Error.WriteLine(
        "Usage: VibeEmulator.Intel8086VectorRunner --vectors <directory> --manifest <manifest.json> --source-sha <sha> [--report <path>]");
}
