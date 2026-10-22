namespace VibeEmulator.Z80.Tests;

public class Z80Z4VectorTests
{
    public static IEnumerable<object[]> Stage4OpcodeData()
    {
        foreach (byte opcode in Z80VectorManifest.Stage4Opcodes)
        {
            yield return [opcode];
        }
    }

    [Theory]
    [MemberData(nameof(Stage4OpcodeData))]
    public void Vector_Z4_MatchesReference(byte opcode)
    {
        Z80VectorTestRunner.Run(Z80VectorManifest.FileName(opcode));
    }
}
