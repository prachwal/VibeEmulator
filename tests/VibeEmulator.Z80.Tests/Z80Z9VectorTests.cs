namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z9VectorTests
{
    public static IEnumerable<object[]> Stage9Files =>
        Z80VectorManifest.Stage9VectorFiles.Select(static file => new object[] { file });

    [Theory]
    [MemberData(nameof(Stage9Files))]
    public void Vector_Stage9_MatchesReference(string fileName)
    {
        Z80VectorTestRunner.Run(fileName);
    }
}
