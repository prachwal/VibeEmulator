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
                if (operation is "Aam" or "Aad")
                {
                    VectorRunResult asciiAdjustCpuResult = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                        vector,
                        flagsMask: (ushort)~ignoredFlagsMask);
                    Assert.True(asciiAdjustCpuResult.Passed, asciiAdjustCpuResult.Difference);
                }

                break;
            case "Daa":
            case "Das":
            case "Aaa":
            case "Aas":
                ushort adjustFlags = initialRegisters.GetProperty("flags").GetUInt16();
                ushort expectedAdjustFlags = finalRegisters.TryGetProperty("flags", out JsonElement finalAdjustFlags)
                    ? finalAdjustFlags.GetUInt16()
                    : adjustFlags;
                Intel8086SpecialArithmeticResult adjustResult = operation switch
                {
                    "Daa" => Intel8086SpecialArithmetic.Daa(initialAx, adjustFlags),
                    "Das" => Intel8086SpecialArithmetic.Das(initialAx, adjustFlags),
                    "Aaa" => Intel8086SpecialArithmetic.Aaa(initialAx, adjustFlags),
                    _ => Intel8086SpecialArithmetic.Aas(initialAx, adjustFlags),
                };
                Assert.Equal(expectedAx, adjustResult.AX);
                Assert.Equal((ushort)(expectedAdjustFlags & ~ignoredFlagsMask), (ushort)(adjustResult.Flags & ~ignoredFlagsMask));
                VectorRunResult adjustCpuResult = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask);
                Assert.True(adjustCpuResult.Passed, adjustCpuResult.Difference);
                break;
            case "MulByte":
            case "ImulByte":
                byte byteRm = GetByteRegister(initialRegisters, vector.GetProperty("bytes")[1].GetByte() & 7);
                ushort byteFlags = initialRegisters.GetProperty("flags").GetUInt16();
                ushort byteExpectedFlags = finalRegisters.TryGetProperty("flags", out JsonElement byteFinalFlags)
                    ? byteFinalFlags.GetUInt16()
                    : byteFlags;
                Intel8086SpecialArithmeticResult byteProduct = Intel8086SpecialArithmetic.MultiplyByte(
                    initialAx,
                    byteRm,
                    byteFlags,
                    operation == "ImulByte");
                Assert.Equal(expectedAx, byteProduct.AX);
                Assert.Equal((ushort)(byteExpectedFlags & ~ignoredFlagsMask), (ushort)(byteProduct.Flags & ~ignoredFlagsMask));
                VectorRunResult byteMultiplyCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask);
                Assert.True(byteMultiplyCpu.Passed, byteMultiplyCpu.Difference);

                break;
            case "MulWord":
            case "ImulWord":
                int wordRmIndex = vector.GetProperty("bytes")[1].GetByte() & 7;
                ushort wordRm = GetWordRegister(initialRegisters, wordRmIndex);
                ushort wordFlags = initialRegisters.GetProperty("flags").GetUInt16();
                ushort wordExpectedFlags = finalRegisters.TryGetProperty("flags", out JsonElement wordFinalFlags)
                    ? wordFinalFlags.GetUInt16()
                    : wordFlags;
                ushort multiplyExpectedDx = finalRegisters.TryGetProperty("dx", out JsonElement multiplyFinalDx)
                    ? multiplyFinalDx.GetUInt16()
                    : initialRegisters.GetProperty("dx").GetUInt16();
                Intel8086WordMultiplyResult wordProduct = Intel8086SpecialArithmetic.MultiplyWord(
                    initialAx,
                    wordRm,
                    wordFlags,
                    operation == "ImulWord");
                Assert.Equal(expectedAx, wordProduct.AX);
                Assert.Equal(multiplyExpectedDx, wordProduct.DX);
                Assert.Equal((ushort)(wordExpectedFlags & ~ignoredFlagsMask), (ushort)(wordProduct.Flags & ~ignoredFlagsMask));
                VectorRunResult wordMultiplyCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask);
                Assert.True(wordMultiplyCpu.Passed, wordMultiplyCpu.Difference);

                break;
            case "DivByte":
            case "IdivByte":
                byte byteDivisor = GetByteRegister(initialRegisters, vector.GetProperty("bytes")[1].GetByte() & 7);
                bool byteDivisionSucceeded = Intel8086SpecialArithmetic.TryDivideByte(
                    initialAx,
                    byteDivisor,
                    operation == "IdivByte",
                    out Intel8086SpecialArithmeticResult byteDivision);
                bool byteExpectedFault = IsDivideFault(finalRegisters);
                Assert.Equal(!byteExpectedFault, byteDivisionSucceeded);
                if (byteDivisionSucceeded)
                {
                    Assert.Equal(expectedAx, byteDivision.AX);
                    if (operation == "DivByte")
                    {
                        VectorRunResult byteDivisionCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                            vector,
                            flagsMask: (ushort)~ignoredFlagsMask);
                        Assert.True(byteDivisionCpu.Passed, byteDivisionCpu.Difference);
                    }
                }

                break;
            case "DivWord":
            case "IdivWord":
                ushort wordDivisor = GetWordRegister(initialRegisters, vector.GetProperty("bytes")[1].GetByte() & 7);
                bool wordDivisionSucceeded = Intel8086SpecialArithmetic.TryDivideWord(
                    initialAx,
                    initialRegisters.GetProperty("dx").GetUInt16(),
                    wordDivisor,
                    operation == "IdivWord",
                    out Intel8086DivisionResult wordDivision);
                bool wordExpectedFault = IsDivideFault(finalRegisters);
                Assert.Equal(!wordExpectedFault, wordDivisionSucceeded);
                if (wordDivisionSucceeded)
                {
                    Assert.Equal(expectedAx, wordDivision.AX);
                    ushort expectedDivisionDx = finalRegisters.TryGetProperty("dx", out JsonElement finalDivisionDx)
                        ? finalDivisionDx.GetUInt16()
                        : initialRegisters.GetProperty("dx").GetUInt16();
                    Assert.Equal(expectedDivisionDx, wordDivision.DX);
                    if (operation == "DivWord")
                    {
                        VectorRunResult wordDivisionCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                            vector,
                            flagsMask: (ushort)~ignoredFlagsMask);
                        Assert.True(wordDivisionCpu.Passed, wordDivisionCpu.Difference);
                    }
                }

                break;
            default:
                throw new InvalidOperationException($"Unknown operation '{operation}'.");
        }
    }

    private static byte GetByteRegister(JsonElement registers, int index)
    {
        string[] names = ["ax", "cx", "dx", "bx"];
        ushort value = registers.GetProperty(names[index & 3]).GetUInt16();
        return index < 4 ? (byte)value : (byte)(value >> 8);
    }

    private static ushort GetWordRegister(JsonElement registers, int index)
    {
        string[] names = ["ax", "cx", "dx", "bx", "sp", "bp", "si", "di"];
        return registers.GetProperty(names[index]).GetUInt16();
    }

    private static bool IsDivideFault(JsonElement finalRegisters) =>
        finalRegisters.TryGetProperty("cs", out JsonElement cs) && cs.GetUInt16() == 0 &&
        finalRegisters.TryGetProperty("ip", out JsonElement ip) && ip.GetUInt16() == 0x0400;
}
