namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy wariantu Ricoh 2A03 (etap P): ta sama tabela co NMOS 6502, ale bez trybu dziesiętnego.
/// Zachowanie wyprowadzone z wektorów <c>nes6502</c> (pełne 10 000 na opkod: 2 560 000 testów,
/// 0 niezgodności) — tutaj przypadki, które pokazują **różnicę** względem NMOS.
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Nes2A03_Adc_WithDecimalFlag_ComputesBinary()
    {
        var nmosBus = new TestBus();
        nmosBus.Load(0x0200, [0x69, 0x01]); // ADC #$01
        var nmos = new StateMachineCpuCore(nmosBus, CpuVariants.Nmos6502);
        nmos.Reset(0x0200);
        nmos.State.A = 0x09;
        nmos.State.P = nmos.State.P.With(StatusFlags.Decimal, true);

        var nesBus = new TestBus();
        nesBus.Load(0x0200, [0x69, 0x01]);
        var nes = new StateMachineCpuCore(nesBus, CpuVariants.Nes2A03);
        nes.Reset(0x0200);
        nes.State.A = 0x09;
        nes.State.P = nes.State.P.With(StatusFlags.Decimal, true);

        Assert.Equal(2, RunInstruction(nmos));
        Assert.Equal(2, RunInstruction(nes));

        // NMOS: BCD 09 + 01 = 10. 2A03: binarnie 09 + 01 = 0A.
        Assert.Equal((byte)0x10, nmos.State.A);
        Assert.Equal((byte)0x0A, nes.State.A);
    }

    [Fact]
    public void Nes2A03_Sbc_WithDecimalFlag_ComputesBinary()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xE9, 0x01]); // SBC #$01
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nes2A03);
        cpu.Reset(0x0200);
        cpu.State.A = 0x20;
        cpu.State.P = cpu.State.P.With(StatusFlags.Decimal, true).With(StatusFlags.Carry, true);

        Assert.Equal(2, RunInstruction(cpu));

        // Binarnie 0x20 - 0x01 = 0x1F; w BCD byłoby 0x19.
        Assert.Equal((byte)0x1F, cpu.State.A);
    }

    [Fact]
    public void Nes2A03_KeepsDecimalFlag_ButItDoesNotAffectArithmetic()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xF8, 0x69, 0x01]); // SED ; ADC #$01
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nes2A03);
        cpu.Reset(0x0200);
        cpu.State.A = 0x09;

        RunInstruction(cpu);

        // SED działa — flaga D jest ustawiona (2A03 ma rejestr stanu bez zmian).
        Assert.True(cpu.State.P.Has(StatusFlags.Decimal));

        RunInstruction(cpu);

        Assert.Equal((byte)0x0A, cpu.State.A);
    }

    [Fact]
    public void Nes2A03_HasSameOpcodeTableAsNmos()
    {
        Assert.Equal(256, CpuVariants.Nes2A03.Opcodes.Count);
        Assert.False(CpuVariants.Nes2A03.HasDecimalMode);
        Assert.True(CpuVariants.Nmos6502.HasDecimalMode);
        Assert.Equal("KIL", CpuVariants.Nes2A03.Describe(0x02)?.Mnemonic);
        Assert.Equal("ISC", CpuVariants.Nes2A03.Describe(0xFF)?.Mnemonic);
        Assert.Equal(RmwStyle.DoubleWrite, CpuVariants.Nes2A03.Rmw);
    }

    [Fact]
    public void Nes2A03_Arr_WithDecimalFlag_ComputesBinary()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x6B, 0x66]); // ARR #$66
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nes2A03);
        cpu.Reset(0x0200);
        cpu.State.A = 0x66;
        cpu.State.P = cpu.State.P.With(StatusFlags.Decimal, true);

        Assert.Equal(2, RunInstruction(cpu));

        // Binarnie: and = 0x66, ROR = 0x33; wariant dziesiętny dałby 0x99 (etap J).
        Assert.Equal((byte)0x33, cpu.State.A);
    }
}
