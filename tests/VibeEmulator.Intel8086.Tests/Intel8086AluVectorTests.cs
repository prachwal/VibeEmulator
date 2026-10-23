using System.Text.Json;
using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086AluVectorTests
{
    public static IEnumerable<object[]> AccumulatorImmediateVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A4Alu");
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
    [MemberData(nameof(AccumulatorImmediateVectors))]
    public void AccumulatorImmediateVector_MatchesEightBitResultAndDefinedFlags(
        JsonElement vector,
        Intel8086AluOperation operation,
        ushort ignoredFlagsMask)
    {
        byte left = (byte)(vector.GetProperty("initial").GetProperty("regs").GetProperty("ax").GetUInt16() & 0x00FF);
        ushort initialFlags = vector.GetProperty("initial").GetProperty("regs").GetProperty("flags").GetUInt16();
        byte right = vector.GetProperty("bytes").EnumerateArray().Last().GetByte();
        Intel8086AluResult result = Intel8086Alu.Execute(left, right, initialFlags, operation);
        ushort initialAx = vector.GetProperty("initial").GetProperty("regs").GetProperty("ax").GetUInt16();
        JsonElement finalRegisters = vector.GetProperty("final").GetProperty("regs");
        ushort expectedFlags = finalRegisters.TryGetProperty("flags", out JsonElement finalFlags)
            ? finalFlags.GetUInt16()
            : initialFlags;
        ushort expectedAx = finalRegisters.TryGetProperty("ax", out JsonElement finalAx)
            ? finalAx.GetUInt16()
            : initialAx;

        Assert.Equal((ushort)(expectedFlags & ~ignoredFlagsMask), (ushort)(result.Flags & ~ignoredFlagsMask));
        if (operation == Intel8086AluOperation.Compare)
        {
            Assert.Equal((byte)(initialAx & 0x00FF), (byte)(expectedAx & 0x00FF));
        }
        else
        {
            Assert.Equal((byte)(expectedAx & 0x00FF), result.Value);
        }
    }
}
