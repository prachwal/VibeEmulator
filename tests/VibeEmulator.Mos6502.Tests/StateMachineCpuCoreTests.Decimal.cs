namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy trybu dziesiętnego (etap G): ADC/SBC z D=1. Wartości flag zgodne z modelem potwierdzonym
/// pełnymi wektorami SingleStepTests (16 form ADC/SBC × 2 warianty, 320 000 testów bez pominięć).
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Adc_Decimal_Nmos_AddsBcdAndSetsBinaryFlags()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x79;
        cpu.State.P = cpu.State.P
            .With(VibeEmulator.Mos6502.StatusFlags.Decimal, true)
            .With(VibeEmulator.Mos6502.StatusFlags.Carry, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x81, cpu.State.A);
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Overflow));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }

    [Fact]
    public void Adc_Decimal_Cmos_TakesExtraCycle()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x79;
        cpu.State.P = cpu.State.P
            .With(VibeEmulator.Mos6502.StatusFlags.Decimal, true)
            .With(VibeEmulator.Mos6502.StatusFlags.Carry, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x81, cpu.State.A);
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Overflow));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }

    [Fact]
    public void Adc_Binary_Cmos_TakesNoExtraCycle()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x79;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x7A, cpu.State.A);
    }

    [Fact]
    public void Sbc_Decimal_Nmos_SubtractsBcd()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xE9, 0x01]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x20;
        cpu.State.P = cpu.State.P
            .With(VibeEmulator.Mos6502.StatusFlags.Decimal, true)
            .With(VibeEmulator.Mos6502.StatusFlags.Carry, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x19, cpu.State.A);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }

    [Fact]
    public void Sbc_Decimal_Cmos_WrapsOnBorrow()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xE9, 0x02]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x02;
        cpu.State.P = cpu.State.P.With(VibeEmulator.Mos6502.StatusFlags.Decimal, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        // Wektor wdc65c02/e9 "e9 8": A=2, operand=2, C=0 → A=$99, P=$BC (N=1, V=0, Z=0, C=0).
        Assert.Equal((byte)0x99, cpu.State.A);
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
    }

    [Fact]
    public void Adc_Decimal_CarryAcrossNibble_ProducesBcdCarry()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x99;
        cpu.State.P = cpu.State.P.With(VibeEmulator.Mos6502.StatusFlags.Decimal, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        // NMOS: N z sumy bez korekty $A0 (r2 = $A0 → N=1), Z z wyniku binarnego ($9A ≠ 0).
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }

    [Fact]
    public void Adc_Decimal_Cmos_ZeroFlagFromBcdResult()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x99;
        cpu.State.P = cpu.State.P.With(VibeEmulator.Mos6502.StatusFlags.Decimal, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Carry));
        // 65C02: N i Z z wyniku BCD (0x00 → N=0, Z=1).
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }
}
