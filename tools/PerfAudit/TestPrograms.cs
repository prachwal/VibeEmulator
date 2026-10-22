using VibeEmulator.Core.C64;

namespace PerfAudit;

internal static class TestPrograms
{
    public const ushort Origin = 0x0200;

    // Źródła: docs/perf-test-program*.asm; opkody i cykle: docs/cpu/opcodes/nmos6502.csv.
    public static ReadOnlySpan<byte> Nop => [0xEA, 0x4C, 0x00, 0x02];

    public static ReadOnlySpan<byte> Full =>
    [
        0x78,             // SEI
        0xAD, 0x00, 0xA0, // LDA $A000
        0x8D, 0x00, 0x04, // STA $0400
        0xAD, 0x00, 0xE0, // LDA $E000
        0x8D, 0x01, 0x04, // STA $0401
        0xAD, 0x00, 0x10, // LDA $1000
        0x8D, 0x02, 0x04, // STA $0402
        0xAD, 0x12, 0xD0, // LDA $D012
        0x8D, 0x00, 0xD4, // STA $D400
        0xAD, 0x00, 0xD4, // LDA $D400
        0xAD, 0x0D, 0xDC, // LDA $DC0D
        0xAD, 0x00, 0xDD, // LDA $DD00
        0xAD, 0x00, 0xDE, // LDA $DE00
        0xEE, 0x00, 0x03, // INC $0300
        0x4C, 0x00, 0x02, // JMP $0200
    ];

    public static C64Machine Create(bool full, byte seed)
    {
        C64Machine machine = C64Machine.CreateCycleExact(
            Rom(0x2000, (byte)(seed ^ 0xA5)),
            Rom(0x2000, (byte)(seed ^ 0x5A)),
            Rom(0x1000, (byte)(seed ^ 0xC3)));
        machine.Reset(Origin);
        machine.Bus.Ram.Fill(seed);
        machine.Bus.LoadRam(Origin, full ? Full : Nop);
        return machine;
    }

    private static byte[] Rom(int length, byte value)
    {
        var rom = new byte[length];
        rom.AsSpan().Fill(value);
        return rom;
    }
}
