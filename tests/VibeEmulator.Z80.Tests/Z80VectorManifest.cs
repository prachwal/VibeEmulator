namespace VibeEmulator.Z80.Tests;

/// <summary>Przypięty zakres próbek wektorowych zaimplementowany w testach Z80.</summary>
public static class Z80VectorManifest
{
    public const string SourceCommit = "ebe1875d48f374bcfd4b505d8eb8ee751568b5f7";
    public const int SampleSize = 20;

    public static ReadOnlySpan<byte> Opcodes => [0x00];

    public static string FileName(byte opcode) => $"{opcode:x2}.json";
}
