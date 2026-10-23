using System.Text.Json;
using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086ConditionVectorTests
{
    public static IEnumerable<object[]> BranchVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors", "A7Branch");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            byte condition = entry.GetProperty("condition").GetByte();
            using var vectors = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in vectors.RootElement.EnumerateArray())
            {
                JsonElement bytes = vector.GetProperty("bytes");
                ushort initialIp = vector.GetProperty("initial").GetProperty("regs").GetProperty("ip").GetUInt16();
                ushort finalIp = vector.GetProperty("final").GetProperty("regs").GetProperty("ip").GetUInt16();
                int instructionLength = bytes.GetArrayLength();
                int displacementByte = bytes[instructionLength - 1].GetByte();
                int displacement = displacementByte < 0x80 ? displacementByte : displacementByte - 0x100;
                ushort fallThroughIp = (ushort)(initialIp + instructionLength);
                ushort targetIp = (ushort)(fallThroughIp + displacement);
                if (targetIp != fallThroughIp)
                {
                    yield return [vector.Clone(), condition, finalIp == targetIp];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(BranchVectors))]
    public void ConditionalBranchVector_MatchesFlagPredicate(JsonElement vector, byte condition, bool expected)
    {
        ushort flags = vector.GetProperty("initial").GetProperty("regs").GetProperty("flags").GetUInt16();

        Assert.Equal(expected, Intel8086Condition.IsSatisfied(condition, flags));
    }
}
