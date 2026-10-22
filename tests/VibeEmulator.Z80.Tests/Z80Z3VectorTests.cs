namespace VibeEmulator.Z80.Tests;

public class Z80Z3VectorTests
{
    public static IEnumerable<object[]> Stage3OpcodeData()
    {
        foreach (byte opcode in Z80VectorManifest.Stage3Opcodes)
        {
            yield return [opcode];
        }
    }

    [Theory]
    [MemberData(nameof(Stage3OpcodeData))]
    public void Vector_Z3_MatchesReference(byte opcode)
    {
        Z80VectorTestRunner.Run(Z80VectorManifest.FileName(opcode));
    }
}
