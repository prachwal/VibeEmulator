namespace VibeEmulator.Z80.Tests;

public class Z80Z5VectorTests
{
    public static IEnumerable<object[]> Stage5OpcodeData()
    {
        foreach (byte opcode in Z80VectorManifest.Stage5Opcodes)
        {
            yield return [opcode];
        }
    }

    [Theory]
    [MemberData(nameof(Stage5OpcodeData))]
    public void Vector_Z5_MatchesReference(byte opcode)
    {
        Z80VectorTestRunner.Run(Z80VectorManifest.FileName(opcode));
    }
}
