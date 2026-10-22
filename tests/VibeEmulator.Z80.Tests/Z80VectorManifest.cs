namespace VibeEmulator.Z80.Tests;

/// <summary>Przypięty zakres próbek wektorowych zaimplementowany w testach Z80.</summary>
public static class Z80VectorManifest
{
    public const string SourceCommit = "ebe1875d48f374bcfd4b505d8eb8ee751568b5f7";
    public const int SampleSize = 20;

    private static readonly byte[] Stage2 =
    [
        0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
        0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
        0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
        0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
        0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
        0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
        0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x77, 0x78,
        0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
        0x06, 0x0E, 0x16, 0x1E, 0x26, 0x2E, 0x36, 0x3E,
        0x0A, 0x1A, 0x3A,
        0x02, 0x12, 0x32,
    ];

    private static readonly byte[] Stage3 =
    [
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
        0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
        0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7,
        0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
        0xC6, 0xCE, 0xD6, 0xDE, 0xE6, 0xEE, 0xF6, 0xFE,
        0x04, 0x0C, 0x14, 0x1C, 0x24, 0x2C, 0x34, 0x3C,
        0x05, 0x0D, 0x15, 0x1D, 0x25, 0x2D, 0x35, 0x3D,
        0x27, 0x2F, 0x37, 0x3F
    ];

    private static readonly byte[] Stage4 =
    [
        0x01, 0x11, 0x21, 0x31, 0x22, 0x2A, 0xF9, 0x03,
        0x13, 0x23, 0x33, 0x0B, 0x1B, 0x2B, 0x3B, 0x09,
        0x19, 0x29, 0x39, 0xC5, 0xD5, 0xE5, 0xF5, 0xC1,
        0xD1, 0xE1, 0xF1, 0xE3, 0xEB, 0x08, 0xD9,
    ];

    private static readonly byte[] Stage5 =
    [
        0xC3, 0xC2, 0xCA, 0xD2, 0xDA, 0xE2, 0xEA, 0xF2,
        0xFA, 0xE9, 0x18, 0x20, 0x28, 0x30, 0x38, 0x10,
        0xCD, 0xC4, 0xCC, 0xD4, 0xDC, 0xE4, 0xEC, 0xF4,
        0xFC, 0xC9, 0xC0, 0xC8, 0xD0, 0xD8, 0xE0, 0xE8,
        0xF0, 0xF8, 0xC7, 0xCF, 0xD7, 0xDF, 0xE7, 0xEF,
        0xF7, 0xFF, 0x76, 0xF3, 0xFB,
    ];

    private static readonly byte[] AccumulatorRotations = [0x07, 0x0F, 0x17, 0x1F];
    private static readonly byte[] AllOpcodes = [0x00, .. Stage2, .. Stage3, .. Stage4, .. Stage5, .. AccumulatorRotations];
    private static readonly string[] ExistingFiles = Array.ConvertAll(AllOpcodes, FileName);

    private static readonly string[] CbFiles =
    [
                "cb 00.json", "cb 01.json", "cb 02.json", "cb 03.json", "cb 04.json", "cb 05.json", "cb 06.json", "cb 07.json",
        "cb 08.json", "cb 09.json", "cb 0a.json", "cb 0b.json", "cb 0c.json", "cb 0d.json", "cb 0e.json", "cb 0f.json",
        "cb 10.json", "cb 11.json", "cb 12.json", "cb 13.json", "cb 14.json", "cb 15.json", "cb 16.json", "cb 17.json",
        "cb 18.json", "cb 19.json", "cb 1a.json", "cb 1b.json", "cb 1c.json", "cb 1d.json", "cb 1e.json", "cb 1f.json",
        "cb 20.json", "cb 21.json", "cb 22.json", "cb 23.json", "cb 24.json", "cb 25.json", "cb 26.json", "cb 27.json",
        "cb 28.json", "cb 29.json", "cb 2a.json", "cb 2b.json", "cb 2c.json", "cb 2d.json", "cb 2e.json", "cb 2f.json",
        "cb 30.json", "cb 31.json", "cb 32.json", "cb 33.json", "cb 34.json", "cb 35.json", "cb 36.json", "cb 37.json",
        "cb 38.json", "cb 39.json", "cb 3a.json", "cb 3b.json", "cb 3c.json", "cb 3d.json", "cb 3e.json", "cb 3f.json",
        "cb 40.json", "cb 41.json", "cb 42.json", "cb 43.json", "cb 44.json", "cb 45.json", "cb 46.json", "cb 47.json",
        "cb 48.json", "cb 49.json", "cb 4a.json", "cb 4b.json", "cb 4c.json", "cb 4d.json", "cb 4e.json", "cb 4f.json",
        "cb 50.json", "cb 51.json", "cb 52.json", "cb 53.json", "cb 54.json", "cb 55.json", "cb 56.json", "cb 57.json",
        "cb 58.json", "cb 59.json", "cb 5a.json", "cb 5b.json", "cb 5c.json", "cb 5d.json", "cb 5e.json", "cb 5f.json",
        "cb 60.json", "cb 61.json", "cb 62.json", "cb 63.json", "cb 64.json", "cb 65.json", "cb 66.json", "cb 67.json",
        "cb 68.json", "cb 69.json", "cb 6a.json", "cb 6b.json", "cb 6c.json", "cb 6d.json", "cb 6e.json", "cb 6f.json",
        "cb 70.json", "cb 71.json", "cb 72.json", "cb 73.json", "cb 74.json", "cb 75.json", "cb 76.json", "cb 77.json",
        "cb 78.json", "cb 79.json", "cb 7a.json", "cb 7b.json", "cb 7c.json", "cb 7d.json", "cb 7e.json", "cb 7f.json",
        "cb 80.json", "cb 81.json", "cb 82.json", "cb 83.json", "cb 84.json", "cb 85.json", "cb 86.json", "cb 87.json",
        "cb 88.json", "cb 89.json", "cb 8a.json", "cb 8b.json", "cb 8c.json", "cb 8d.json", "cb 8e.json", "cb 8f.json",
        "cb 90.json", "cb 91.json", "cb 92.json", "cb 93.json", "cb 94.json", "cb 95.json", "cb 96.json", "cb 97.json",
        "cb 98.json", "cb 99.json", "cb 9a.json", "cb 9b.json", "cb 9c.json", "cb 9d.json", "cb 9e.json", "cb 9f.json",
        "cb a0.json", "cb a1.json", "cb a2.json", "cb a3.json", "cb a4.json", "cb a5.json", "cb a6.json", "cb a7.json",
        "cb a8.json", "cb a9.json", "cb aa.json", "cb ab.json", "cb ac.json", "cb ad.json", "cb ae.json", "cb af.json",
        "cb b0.json", "cb b1.json", "cb b2.json", "cb b3.json", "cb b4.json", "cb b5.json", "cb b6.json", "cb b7.json",
        "cb b8.json", "cb b9.json", "cb ba.json", "cb bb.json", "cb bc.json", "cb bd.json", "cb be.json", "cb bf.json",
        "cb c0.json", "cb c1.json", "cb c2.json", "cb c3.json", "cb c4.json", "cb c5.json", "cb c6.json", "cb c7.json",
        "cb c8.json", "cb c9.json", "cb ca.json", "cb cb.json", "cb cc.json", "cb cd.json", "cb ce.json", "cb cf.json",
        "cb d0.json", "cb d1.json", "cb d2.json", "cb d3.json", "cb d4.json", "cb d5.json", "cb d6.json", "cb d7.json",
        "cb d8.json", "cb d9.json", "cb da.json", "cb db.json", "cb dc.json", "cb dd.json", "cb de.json", "cb df.json",
        "cb e0.json", "cb e1.json", "cb e2.json", "cb e3.json", "cb e4.json", "cb e5.json", "cb e6.json", "cb e7.json",
        "cb e8.json", "cb e9.json", "cb ea.json", "cb eb.json", "cb ec.json", "cb ed.json", "cb ee.json", "cb ef.json",
        "cb f0.json", "cb f1.json", "cb f2.json", "cb f3.json", "cb f4.json", "cb f5.json", "cb f6.json", "cb f7.json",
        "cb f8.json", "cb f9.json", "cb fa.json", "cb fb.json", "cb fc.json", "cb fd.json", "cb fe.json", "cb ff.json",
    ];

    private static readonly string[] Stage6Files = [.. CbFiles, .. Array.ConvertAll(AccumulatorRotations, FileName)];
    private static readonly string[] EdFiles = CreateEdFiles();
    private static readonly string[] Stage7Files = [.. EdFiles, FileName(0xDB), FileName(0xD3)];
    private static readonly string[] Stage8Files = CreateIndexFiles();
    private static readonly string[] Stage9Files = CreateIndexCbFiles();
    private static readonly string[] AllFiles = [.. ExistingFiles, .. Stage6Files, .. Stage7Files, .. Stage8Files, .. Stage9Files];

    public static IReadOnlyList<string> Stage6VectorFiles => Stage6Files;

    public static IReadOnlyList<string> Stage7VectorFiles => Stage7Files;

    public static IReadOnlyList<string> Stage8VectorFiles => Stage8Files;

    public static IReadOnlyList<string> Stage9VectorFiles => Stage9Files;

    public static IReadOnlyList<string> VectorFiles => AllFiles;

    public static IReadOnlyList<byte> Stage5Opcodes => Stage5;

    public static IReadOnlyList<byte> Stage4Opcodes => Stage4;

    public static IReadOnlyList<byte> Stage3Opcodes => Stage3;

    public static IReadOnlyList<byte> Stage2Opcodes => Stage2;

    public static ReadOnlySpan<byte> Opcodes => AllOpcodes;

    public static string FileName(byte opcode) => $"{opcode:x2}.json";

    private static string[] CreateEdFiles()
    {
        var files = new List<string>(80);
        for (int opcode = 0x40; opcode <= 0x7F; opcode++)
        {
            files.Add($"ed {opcode:x2}.json");
        }

        foreach (int opcode in new[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA8, 0xA9, 0xAA, 0xAB, 0xB0, 0xB1, 0xB2, 0xB3, 0xB8, 0xB9, 0xBA, 0xBB })
        {
            files.Add($"ed {opcode:x2}.json");
        }

        return files.ToArray();
    }

    private static string[] CreateIndexFiles()
    {
        var files = new List<string>(504);
        foreach (string prefix in new[] { "dd", "fd" })
        {
            for (int opcode = 0; opcode <= 0xFF; opcode++)
            {
                if (opcode is not (0xCB or 0xDD or 0xED or 0xFD))
                {
                    files.Add($"{prefix} {opcode:x2}.json");
                }
            }
        }

        return files.ToArray();
    }

    private static string[] CreateIndexCbFiles()
    {
        var files = new List<string>(512);
        foreach (string prefix in new[] { "dd", "fd" })
        {
            for (int opcode = 0; opcode <= 0xFF; opcode++)
            {
                files.Add($"{prefix} cb __ {opcode:x2}.json");
            }
        }

        return files.ToArray();
    }
}
