namespace VibeEmulator.Z80.Tests;

public class Z80Z2VectorTests
{
    public static IEnumerable<object[]> Stage2OpcodeData()
    {
        foreach (byte opcode in Z80VectorManifest.Stage2Opcodes)
        {
            yield return [opcode];
        }
    }

    [Theory]
    [MemberData(nameof(Stage2OpcodeData))]
    public void Vector_Z2_MatchesReference(byte opcode)
    {
        Z80VectorTestRunner.Run(Z80VectorManifest.FileName(opcode));
    }
}
