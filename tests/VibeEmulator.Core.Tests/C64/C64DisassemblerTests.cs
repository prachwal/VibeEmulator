using VibeEmulator.Core.C64;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.Tests;

/// <summary>Deasembler przez prawdziwą magistralę C64 (bankowanie ROM-ów), więc zostaje w projekcie Core.</summary>
public class C64DisassemblerTests
{
    [Fact]
    public void Decode_BusReadsKernalAtE000()
    {
        byte[] kernal = new byte[0x2000];
        kernal[0] = 0xA9;
        kernal[1] = 0x55;
        C64Machine machine = C64Machine.CreateCycleExact(null, kernal, null);

        string text = Disassembler.Decode(machine.Bus, 0xE000, out int length);

        Assert.Equal("LDA #$55", text);
        Assert.Equal(2, length);
    }
}
