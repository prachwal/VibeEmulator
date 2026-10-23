using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086SpecialArithmeticVectorTests
{
    public static IEnumerable<object[]> SpecialArithmeticVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A8Arithmetic");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            string operation = entry.GetProperty("operation").GetString()!;
            ushort ignoredFlagsMask = entry.GetProperty("ignoredFlagsMask").GetUInt16();
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                yield return [vector.Clone(), operation, ignoredFlagsMask];
            }
        }
    }

    [Theory]
    [MemberData(nameof(SpecialArithmeticVectors))]
    public void SpecialArithmeticVector_MatchesConversionOrAsciiBcdResult(
        JsonElement vector,
        string operation,
        ushort ignoredFlagsMask)
    {
        JsonElement initialRegisters = vector.GetProperty("initial").GetProperty("regs");
        JsonElement finalRegisters = vector.GetProperty("final").GetProperty("regs");
        ushort initialAx = initialRegisters.GetProperty("ax").GetUInt16();
        ushort expectedAx = finalRegisters.TryGetProperty("ax", out JsonElement finalAx) ? finalAx.GetUInt16() : initialAx;

        switch (operation)
        {
            case "Cbw":
                Assert.Equal(expectedAx, Intel8086SpecialArithmetic.Cbw(initialAx));
                VectorRunResult cbwResult = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(vector);
                Assert.True(cbwResult.Passed, cbwResult.Difference);
                break;
            case "Cwd":
                ushort initialDx = initialRegisters.GetProperty("dx").GetUInt16();
                ushort expectedDx = finalRegisters.TryGetProperty("dx", out JsonElement finalDx) ? finalDx.GetUInt16() : initialDx;
                Assert.Equal(expectedDx, Intel8086SpecialArithmetic.Cwd(initialAx));
                VectorRunResult cwdResult = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(vector);
                Assert.True(cwdResult.Passed, cwdResult.Difference);
                break;
            case "Aam":
            case "Aad":
                byte radix = vector.GetProperty("bytes").EnumerateArray().Last().GetByte();
                ushort initialFlags = initialRegisters.GetProperty("flags").GetUInt16();
                ushort expectedFlags = finalRegisters.TryGetProperty("flags", out JsonElement finalFlags) ? finalFlags.GetUInt16() : initialFlags;
                Intel8086SpecialArithmeticResult result = operation == "Aam"
                    ? Intel8086SpecialArithmetic.Aam(initialAx, radix, initialFlags)
                    : Intel8086SpecialArithmetic.Aad(initialAx, radix, initialFlags);
                Assert.Equal(expectedAx, result.AX);
                Assert.Equal((ushort)(expectedFlags & ~ignoredFlagsMask), (ushort)(result.Flags & ~ignoredFlagsMask));
                break;
            default:
                throw new InvalidOperationException($"Unknown operation '{operation}'.");
        }
    }
}
