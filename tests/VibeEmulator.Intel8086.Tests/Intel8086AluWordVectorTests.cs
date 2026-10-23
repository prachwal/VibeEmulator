using System.Text.Json;
using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086AluWordVectorTests
{
    public static IEnumerable<object[]> AccumulatorImmediateWordVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A6Alu");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            Intel8086AluOperation operation = Enum.Parse<Intel8086AluOperation>(entry.GetProperty("operation").GetString()!);
            ushort ignoredFlagsMask = entry.GetProperty("ignoredFlagsMask").GetUInt16();
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                yield return [vector.Clone(), operation, ignoredFlagsMask];
            }
        }
    }

    [Theory]
    [MemberData(nameof(AccumulatorImmediateWordVectors))]
    public void AccumulatorImmediateWordVector_MatchesSixteenBitResultAndDefinedFlags(
        JsonElement vector,
        Intel8086AluOperation operation,
        ushort ignoredFlagsMask)
    {
        JsonElement initialRegisters = vector.GetProperty("initial").GetProperty("regs");
        ushort left = initialRegisters.GetProperty("ax").GetUInt16();
        ushort initialFlags = initialRegisters.GetProperty("flags").GetUInt16();
        JsonElement instructionBytes = vector.GetProperty("bytes");
        int length = instructionBytes.GetArrayLength();
        ushort right = (ushort)(instructionBytes[length - 2].GetByte() | (instructionBytes[length - 1].GetByte() << 8));
        Intel8086AluWordResult result = Intel8086Alu.Execute(left, right, initialFlags, operation);
        JsonElement finalRegisters = vector.GetProperty("final").GetProperty("regs");
        ushort expectedFlags = finalRegisters.TryGetProperty("flags", out JsonElement finalFlags)
            ? finalFlags.GetUInt16()
            : initialFlags;
        ushort expectedAx = finalRegisters.TryGetProperty("ax", out JsonElement finalAx)
            ? finalAx.GetUInt16()
            : left;

        Assert.Equal((ushort)(expectedFlags & ~ignoredFlagsMask), (ushort)(result.Flags & ~ignoredFlagsMask));
        if (operation == Intel8086AluOperation.Compare)
        {
            Assert.Equal(left, expectedAx);
        }
        else
        {
            Assert.Equal(expectedAx, result.Value);
        }
    }
}
