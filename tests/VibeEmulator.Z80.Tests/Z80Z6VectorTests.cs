namespace VibeEmulator.Z80.Tests;

public class Z80Z6VectorTests
{
    public static IEnumerable<object[]> Stage6VectorFileData()
    {
        foreach (string fileName in Z80VectorManifest.Stage6VectorFiles)
        {
            yield return [fileName];
        }
    }

    [Theory]
    [MemberData(nameof(Stage6VectorFileData))]
    public void Vector_Z6_MatchesReference(string fileName)
    {
        Z80VectorTestRunner.Run(fileName);
    }
}
