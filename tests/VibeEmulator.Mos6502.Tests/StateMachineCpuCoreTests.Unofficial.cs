namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy 93 nieoficjalnych opkodów NMOS (etap J). Oczekiwania pochodzą z wektorów SingleStepTests 65x02
/// (pełne 10 000 na opkod: 930 000 testów, 0 niezgodności); przypadki brzegowe ARR w trybie dziesiętnym
/// i niestabilne zapisy (SHX/SHY/AHX/TAS) opisano w <c>docs/cpu/opcodes.md</c>.
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Slo_ZeroPage_ShiftsMemoryThenOresAccumulator()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x07, 0x10]); // SLO $10
        bus.Load(0x0010, [0x81]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x0F;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x02, bus.Peek(0x0010));
        Assert.Equal((byte)0x0F, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Rla_AbsoluteX_TakesSevenCyclesAndRotatesMemory()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x3F, 0x00, 0x03]); // RLA $0300,X
        bus.Load(0x0305, [0x80]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.X = 0x05;
        cpu.State.A = 0xFF;

        int cycles = RunInstruction(cpu);

        Assert.Equal(7, cycles);
        Assert.Equal((byte)0x00, bus.Peek(0x0305));
        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void Sre_ZeroPage_ShiftsRightThenEorsAccumulator()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x47, 0x10]); // SRE $10
        bus.Load(0x0010, [0x03]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x01;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x01, bus.Peek(0x0010));
        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Rra_ZeroPage_UsesCarryFromRotateForAdc()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x67, 0x10]); // RRA $10
        bus.Load(0x0010, [0x01]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x10;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x80, bus.Peek(0x0010)); // ROR z C=1
        Assert.Equal((byte)0x91, cpu.State.A);        // 0x10 + 0x80 + C z rotacji (1)
        Assert.False(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Dcp_ZeroPage_DecrementsMemoryThenCompares()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xC7, 0x10]); // DCP $10
        bus.Load(0x0010, [0x42]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x41;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x41, bus.Peek(0x0010));
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void Isc_ZeroPage_IncrementsMemoryThenSubtracts()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xE7, 0x10]); // ISC $10
        bus.Load(0x0010, [0x09]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x15;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x0A, bus.Peek(0x0010));
        Assert.Equal((byte)0x0B, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Lax_ZeroPage_LoadsBothRegistersFromValue()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xA7, 0x10]); // LAX $10
        bus.Load(0x0010, [0x80]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.Equal((byte)0x80, cpu.State.X);
        // LAX ustawia N/Z z załadowanej wartości (wektory A7.json).
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
        Assert.False(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void Lax_Immediate_UsesStableNmosMagicMask()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xAB, 0x12]); // LAX #$12
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x21;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        // (A | 0xEE) & 0x12 & (0xEE | (A & 0x11)) = 0x12 & 0xEE = 0x02
        Assert.Equal((byte)0x02, cpu.State.A);
        Assert.Equal((byte)0x02, cpu.State.X);
    }

    [Fact]
    public void Sax_ZeroPage_WritesAndOfAccumulatorAndX()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x87, 0x10]); // SAX $10
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0xF0;
        cpu.State.X = 0x3C;

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x30, bus.Peek(0x0010));
    }

    [Fact]
    public void Las_AbsoluteY_MasksAccumulatorWithStackPointer()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xBB, 0x00, 0x03]); // LAS $0300,Y
        bus.Load(0x0300, [0xCF]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0x0F;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((byte)0x0F, cpu.State.A);
        Assert.Equal((byte)0x0F, cpu.State.X);
        Assert.Equal((byte)0x0F, cpu.State.SP);
    }

    [Fact]
    public void Anc_Immediate_AndsAndLoadsCarryFromBit7()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x0B, 0x80]); // ANC #$80
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0xFF;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Alr_Immediate_AndsThenShiftsRight()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x4B, 0xFF]); // ALR #$FF
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x03;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x01, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Arr_Immediate_BinaryMode_SetsCarryFromBit6()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x6B, 0xFF]); // ARR #$FF
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x40;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x20, cpu.State.A);
        Assert.False(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Overflow)); // V = bit6 ^ bit5 wyniku = 0 ^ 1
    }

    [Fact]
    public void Arr_Immediate_DecimalMode_AppliesDigitCorrection()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x6B, 0x66]); // ARR #$66 w trybie dziesiętnym
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x66;
        cpu.State.P = cpu.State.P.With(StatusFlags.Decimal, true);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        // and = 0x66, rotated = 0x33; obie cyfry `and` >= 5, więc korekta:
        // niska (3+6)&0x0F = 9, wysoka ((0x30 + 0x60) & 0xF0) = 0x90 → 0x99.
        Assert.Equal((byte)0x99, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Decimal));
        Assert.False(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void Axs_Immediate_SubtractsFromAAndXWithoutBorrow()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xCB, 0x10]); // AXS #$10
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0xFF;
        cpu.State.X = 0x3F;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x2F, cpu.State.X);
        Assert.Equal((byte)0xFF, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Xaa_Immediate_UsesStableNmosMagicMask()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x8B, 0xFF]); // XAA #$FF
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x03;
        cpu.State.X = 0xFF;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        // (A | 0xEE) & X & imm = 0xEF & 0xFF & 0xFF = 0xEF
        Assert.Equal((byte)0xEF, cpu.State.A);
    }

    [Fact]
    public void Shx_AbsoluteY_StoresRegisterMaskedByHighBytePlusOne()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x9E, 0x00, 0x03]); // SHX $0300,Y
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.X = 0xFF;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x04, bus.Peek(0x0300)); // 0xFF & (0x03 + 1)
    }

    [Fact]
    public void Tas_AbsoluteY_SetsStackPointerAndUsesUnstableHighByte()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x9B, 0x00, 0x03]); // TAS $0300,Y
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0xFF;
        cpu.State.X = 0x0F;

        int cycles = RunInstruction(cpu);

        // bez przekroczenia strony: zapis pod bazowy adres, SP = A & X.
        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x0F, cpu.State.SP);
        // value = (A & X) & (high+1) = 0x0F & 0x04 = 0x04
        Assert.Equal((byte)0x04, bus.Peek(0x0300));
    }

    [Fact]
    public void Nop_Absolute_ReadsEffectiveAddressNotPcMinusOne()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x0C, 0x35, 0x3C]); // NOP $3C35
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        // NMOS-owy NOP czyta adres operandu (wektory 0C.json), inaczej niż 65C02 NOP abs.
        Assert.Contains(0x3C35, bus.Reads);
    }

    [Fact]
    public void Nop_Implied_TakesTwoCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x1A]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
    }

    [Fact]
    public void Nop_AbsoluteX_TakesFourCyclesWithoutPageCross()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x1C, 0x00, 0x03]); // NOP $0300,X
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.X = 0x04;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((ushort)0x0203, cpu.State.PC);
    }
}
