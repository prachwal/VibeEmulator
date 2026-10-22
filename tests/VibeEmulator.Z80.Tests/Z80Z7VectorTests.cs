namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z7VectorTests
{
    public static IEnumerable<object[]> Stage7Files =>
        Z80VectorManifest.Stage7VectorFiles.Select(static file => new object[] { file });

    [Theory]
    [MemberData(nameof(Stage7Files))]
    public void Vector_Stage7_MatchesReference(string fileName)
    {
        Z80VectorTestRunner.Run(fileName);
    }
}
