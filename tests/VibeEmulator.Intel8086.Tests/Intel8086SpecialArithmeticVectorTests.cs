using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Intel8086VectorRunner;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086SpecialArithmeticVectorTests
{
    [Fact]
    public void VectorRunner_ReportsPerCaseTStateLimit()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A8Arithmetic");
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "D8.json")));
        VectorRunResult result = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
            vectors.RootElement[0],
            maxTStates: 1);

        Assert.False(result.Passed);
        Assert.Contains("test_num=0", result.Difference, StringComparison.Ordinal);
        Assert.Contains("exceeded 1 T-states", result.Difference, StringComparison.Ordinal);
    }

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
                if (operation == "Aam" && radix == 0)
                {
                    Assert.True(IsDivideFault(finalRegisters), "AAM with radix 0 must enter Type 0.");
                    IReadOnlyDictionary<uint, byte> aamRamMasks = GetDivideErrorSavedFlagsMasks(initialRegisters, ignoredFlagsMask);
                    IReadOnlyDictionary<int, ushort> aamCycleMasks = GetDivideErrorSavedFlagsCycleMasks(vector, initialRegisters, ignoredFlagsMask);
                    VectorRunResult aamDivideErrorCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                        vector,
                        flagsMask: (ushort)~ignoredFlagsMask,
                        ramMasks: aamRamMasks,
                        cycleDataMasks: aamCycleMasks);
                    Assert.True(aamDivideErrorCpu.Passed, aamDivideErrorCpu.Difference);
                    break;
                }

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
                byte byteModRm = vector.GetProperty("bytes")[1].GetByte();
                byte byteRm = (byteModRm >> 6) == 3
                    ? GetByteRegister(initialRegisters, byteModRm & 7)
                    : (byte)GetMemoryArithmeticOperand(vector, byteOperation: true);
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
                byte wordModRm = vector.GetProperty("bytes")[1].GetByte();
                ushort wordRm = (wordModRm >> 6) == 3
                    ? GetWordRegister(initialRegisters, wordModRm & 7)
                    : GetMemoryArithmeticOperand(vector, byteOperation: false);
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
                if (operation == "IdivByte" && IsRepeatPrefixed(vector))
                {
                    bool repeatByteFault = IsDivideFault(finalRegisters);
                    IReadOnlyDictionary<uint, byte>? repeatByteRamMasks = repeatByteFault
                        ? GetDivideErrorSavedFlagsMasks(initialRegisters, ignoredFlagsMask)
                        : null;
                    IReadOnlyDictionary<int, ushort>? repeatByteCycleMasks = repeatByteFault
                        ? GetDivideErrorSavedFlagsCycleMasks(vector, initialRegisters, ignoredFlagsMask)
                        : null;
                    VectorRunResult repeatByteCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                        vector,
                        flagsMask: (ushort)~ignoredFlagsMask,
                        ramMasks: repeatByteRamMasks,
                        cycleDataMasks: repeatByteCycleMasks);
                    Assert.True(repeatByteCpu.Passed, repeatByteCpu.Difference);
                    break;
                }

                byte modRmByte = vector.GetProperty("bytes")[1].GetByte();
                byte byteDivisor = (modRmByte >> 6) == 3
                    ? GetByteRegister(initialRegisters, modRmByte & 7)
                    : (byte)GetMemoryArithmeticOperand(vector, byteOperation: true);
                bool byteDivisionSucceeded = Intel8086SpecialArithmetic.TryDivideByte(
                    initialAx,
                    byteDivisor,
                    operation == "IdivByte",
                    out Intel8086SpecialArithmeticResult byteDivision);
                bool byteExpectedFault = IsDivideFault(finalRegisters);
                Assert.Equal(!byteExpectedFault, byteDivisionSucceeded);
                IReadOnlyDictionary<uint, byte>? divideRamMasks = byteExpectedFault
                    ? GetDivideErrorSavedFlagsMasks(initialRegisters, ignoredFlagsMask)
                    : null;
                IReadOnlyDictionary<int, ushort>? divideCycleMasks = byteExpectedFault
                    ? GetDivideErrorSavedFlagsCycleMasks(vector, initialRegisters, ignoredFlagsMask)
                    : null;
                VectorRunResult byteDivisionCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask,
                    ramMasks: divideRamMasks,
                    cycleDataMasks: divideCycleMasks);
                Assert.True(byteDivisionCpu.Passed, byteDivisionCpu.Difference);
                if (byteDivisionSucceeded)
                {
                    Assert.Equal(expectedAx, byteDivision.AX);
                }

                break;
            case "DivWord":
            case "IdivWord":
                if (operation == "IdivWord" && IsRepeatPrefixed(vector))
                {
                    bool repeatWordFault = IsDivideFault(finalRegisters);
                    IReadOnlyDictionary<uint, byte>? repeatWordRamMasks = repeatWordFault
                        ? GetDivideErrorSavedFlagsMasks(initialRegisters, ignoredFlagsMask)
                        : null;
                    IReadOnlyDictionary<int, ushort>? repeatWordCycleMasks = repeatWordFault
                        ? GetDivideErrorSavedFlagsCycleMasks(vector, initialRegisters, ignoredFlagsMask)
                        : null;
                    VectorRunResult repeatWordCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                        vector,
                        flagsMask: (ushort)~ignoredFlagsMask,
                        ramMasks: repeatWordRamMasks,
                        cycleDataMasks: repeatWordCycleMasks);
                    Assert.True(repeatWordCpu.Passed, repeatWordCpu.Difference);
                    break;
                }

                byte modRmWord = vector.GetProperty("bytes")[1].GetByte();
                ushort wordDivisor = (modRmWord >> 6) == 3
                    ? GetWordRegister(initialRegisters, modRmWord & 7)
                    : GetMemoryArithmeticOperand(vector, byteOperation: false);
                bool wordDivisionSucceeded = Intel8086SpecialArithmetic.TryDivideWord(
                    initialAx,
                    initialRegisters.GetProperty("dx").GetUInt16(),
                    wordDivisor,
                    operation == "IdivWord",
                    out Intel8086DivisionResult wordDivision);
                bool wordExpectedFault = IsDivideFault(finalRegisters);
                Assert.Equal(!wordExpectedFault, wordDivisionSucceeded);
                IReadOnlyDictionary<uint, byte>? wordDivideRamMasks = wordExpectedFault
                    ? GetDivideErrorSavedFlagsMasks(initialRegisters, ignoredFlagsMask)
                    : null;
                IReadOnlyDictionary<int, ushort>? wordDivideCycleMasks = wordExpectedFault
                    ? GetDivideErrorSavedFlagsCycleMasks(vector, initialRegisters, ignoredFlagsMask)
                    : null;
                VectorRunResult wordDivisionCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask,
                    ramMasks: wordDivideRamMasks,
                    cycleDataMasks: wordDivideCycleMasks);
                Assert.True(wordDivisionCpu.Passed, wordDivisionCpu.Difference);
                if (wordDivisionSucceeded)
                {
                    Assert.Equal(expectedAx, wordDivision.AX);
                    ushort expectedDivisionDx = finalRegisters.TryGetProperty("dx", out JsonElement finalDivisionDx)
                        ? finalDivisionDx.GetUInt16()
                        : initialRegisters.GetProperty("dx").GetUInt16();
                    Assert.Equal(expectedDivisionDx, wordDivision.DX);
                }

                break;
            case "Esc":
                VectorRunResult escCpu = VibeEmulator.Intel8086VectorRunner.Intel8086VectorRunner.Run(
                    vector,
                    flagsMask: (ushort)~ignoredFlagsMask);
                Assert.True(escCpu.Passed, escCpu.Difference);
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

    private static ushort GetMemoryArithmeticOperand(JsonElement vector, bool byteOperation)
    {
        uint address = 0;
        bool foundRead = false;
        foreach (JsonElement cycle in vector.GetProperty("cycles").EnumerateArray())
        {
            if (cycle[7].GetString() == "MEMR" && cycle[8].GetString() == "T1")
            {
                address = cycle[1].GetUInt32();
                foundRead = true;
                break;
            }
        }

        Assert.True(foundRead, "Memory divide vector must contain a memory read cycle.");
        var initialRam = new Dictionary<uint, byte>();
        foreach (JsonElement entry in vector.GetProperty("initial").GetProperty("ram").EnumerateArray())
        {
            initialRam[entry[0].GetUInt32()] = entry[1].GetByte();
        }

        byte low = initialRam.TryGetValue(address, out byte lowByte) ? lowByte : (byte)0;
        if (byteOperation)
        {
            return low;
        }

        uint highAddress = (address + 1) & 0xFFFFF;
        byte high = initialRam.TryGetValue(highAddress, out byte highByte) ? highByte : (byte)0;
        return (ushort)(low | (high << 8));
    }

    private static bool IsDivideFault(JsonElement finalRegisters) =>
        finalRegisters.TryGetProperty("cs", out JsonElement cs) && cs.GetUInt16() == 0 &&
        finalRegisters.TryGetProperty("ip", out JsonElement ip) && ip.GetUInt16() == 0x0400;

    private static bool IsRepeatPrefixed(JsonElement vector) =>
        vector.GetProperty("bytes")[0].GetByte() is 0xF2 or 0xF3;

    private static Dictionary<uint, byte> GetDivideErrorSavedFlagsMasks(
        JsonElement registers,
        ushort ignoredFlagsMask)
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

    private static Dictionary<int, ushort> GetDivideErrorSavedFlagsCycleMasks(
        JsonElement vector,
        JsonElement registers,
        ushort ignoredFlagsMask)
    {
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
                    ushort laneMask = writeCycle[5].GetByte() == 1 ? masks[byteIndex] : (ushort)(masks[byteIndex] << 8);
                    cycleMasks[dataCycleIndex] = laneMask;
                }
            }

            break;
        }

        return cycleMasks;
    }
}
