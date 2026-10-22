namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy 12 opkodów KIL/JAM (etap K). Sekwencja dostępów i liczba cykli pochodzą z wektorów
/// SingleStepTests 65x02 (pełne 10 000 na opkod: 120 000 testów, 0 niezgodności): fetch, dummy read
/// spod PC, potem <c>$FFFF, $FFFE, $FFFE, $FFFF×6</c> — razem 11 cykli; PC rośnie o 1, reszta stanu
/// bez zmian, a rdzeń zatrzymuje się bez wyjątku.
/// </summary>
public partial class StateMachineCpuCoreTests
{
    /// <summary>Wszystkie 12 bajtów KIL w wariancie NMOS (w 65C02 część z nich to prawdziwe instrukcje).</summary>
    public static TheoryData<int> KilOpcodes => [0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x92, 0xB2, 0xD2, 0xF2];

    [Theory]
    [MemberData(nameof(KilOpcodes))]
    public void Kil_TakesElevenCyclesAndJamsWithoutChangingState(int opcode)
    {
        var bus = new TestBus();
        bus.Load(0x0200, [(byte)opcode]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x5A;
        cpu.State.X = 0xA5;
        cpu.State.Y = 0x3C;
        cpu.State.SP = 0xF0;
        CpuState before = cpu.State;

        for (int cycle = 0; cycle < 10; cycle++)
        {
            cpu.Tick();
        }

        Assert.False(cpu.Jammed);

        cpu.Tick();

        Assert.True(cpu.Jammed);
        Assert.Equal(11, cpu.Cycles);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
        Assert.Equal(before.A, cpu.State.A);
        Assert.Equal(before.X, cpu.State.X);
        Assert.Equal(before.Y, cpu.State.Y);
        Assert.Equal(before.SP, cpu.State.SP);
        Assert.Equal(before.P, cpu.State.P);
        Assert.Empty(bus.Writes);
    }

    [Fact]
    public void Kil_ReadsVectorSequenceFromResetVectorArea()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x02]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        for (int cycle = 0; cycle < 11; cycle++)
        {
            cpu.Tick();
        }

        // Wektory 02.json: fetch, dummy spod PC, $FFFF, $FFFE, $FFFE i sześć odczytów $FFFF.
        int[] expected =
        [
            0x0200, 0x0201,
            0xFFFF, 0xFFFE, 0xFFFE,
            0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF,
        ];
        Assert.Equal(expected, bus.Reads);
    }

    [Fact]
    public void Kil_AfterJam_CountsCyclesWithoutBusAccess()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xF2]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        for (int cycle = 0; cycle < 11; cycle++)
        {
            cpu.Tick();
        }

        int readsAfterJam = bus.Reads.Count;
        long cyclesAfterJam = cpu.Cycles;

        cpu.Tick();
        cpu.Tick();

        // Zatrzymany rdzeń nie wystawia adresów, ale czas płynie (VIC-II nadal kradnie cykle).
        Assert.Equal(cyclesAfterJam + 2, cpu.Cycles);
        Assert.Equal(readsAfterJam, bus.Reads.Count);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
    }

    [Fact]
    public void Kil_IsExplicitCatalogEntry_NotUnknownByte()
    {
        CpuOpcode? kil = StateMachineCpuCore.Describe(0x02);

        Assert.NotNull(kil);
        Assert.Equal("KIL", kil.Value.Mnemonic);
        Assert.Equal(OpKind.Kil, kil.Value.Kind);
        Assert.Equal(CpuAction.Kil, kil.Value.Action);
    }

    [Fact]
    public void Kil_DoesNotExistInCmos65C02()
    {
        // Te same bajty w 65C02 to prawdziwe instrukcje (np. $92 to STA (zp)), a STP ($DB) jest osobną
        // instrukcją — KIL jest wyłącznie wpisem wariantu NMOS.
        Assert.Equal("NOP", CpuVariants.Cmos65C02.Describe(0x02)?.Mnemonic);
        Assert.Equal(AddressingMode.Immediate, CpuVariants.Cmos65C02.Describe(0x02)?.Mode);
        Assert.Equal("STA", CpuVariants.Cmos65C02.Describe(0x92)?.Mnemonic);
        Assert.Equal("KIL", CpuVariants.Nmos6502.Describe(0x92)?.Mnemonic);
    }

    [Fact]
    public void Kil_DecodesToMnemonicWithoutOperand()
    {
        byte[] memory = [0x02, 0x34, 0x12];

        string text = Disassembler.Decode(CpuVariants.Nmos6502, address => memory[address], 0, out int length);

        Assert.Equal("KIL", text);
        Assert.Equal(1, length);
    }
}
