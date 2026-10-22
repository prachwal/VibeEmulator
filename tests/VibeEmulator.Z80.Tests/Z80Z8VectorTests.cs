namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z8VectorTests
{
    public static IEnumerable<object[]> Stage8Files =>
        Z80VectorManifest.Stage8VectorFiles.Select(static file => new object[] { file });

    [Theory]
    [MemberData(nameof(Stage8Files))]
    public void Vector_Stage8_MatchesReference(string fileName)
    {
        Z80VectorTestRunner.Run(fileName);
    }
}
