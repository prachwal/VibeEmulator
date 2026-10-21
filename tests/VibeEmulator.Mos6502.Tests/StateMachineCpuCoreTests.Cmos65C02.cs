namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy nowych opkodów 65C02 (etap H). Sekwencje dostępów i flagi z wektorów wdc65c02
/// (próbka + pełne 530 000 testów); WAI/STP bez wektorów (pliki źródłowe puste) — z CSV/datasheetu.
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Bra_AlwaysTakesThreeCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x80, 0x04]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((ushort)0x0206, cpu.State.PC);
    }

    [Fact]
    public void Lda_IndirectZeroPage_TakesFiveCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xB2, 0x10]);
        bus.Load(0x0010, [0x00, 0x03]);
        bus.Load(0x0300, [0x42]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x42, cpu.State.A);
    }

    [Fact]
    public void Sta_IndirectZeroPage_WritesThroughPointer()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x92, 0x10]);
        bus.Load(0x0010, [0x00, 0x03]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x55;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x55, bus.Peek(0x0300));
    }

    [Fact]
    public void Rmb_ClearsBitAndKeepsFlags()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x17, 0x30]); // RMB1 $30
        bus.Load(0x0030, [0x86]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.P = (VibeEmulator.Mos6502.StatusFlags)0xEB;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x84, bus.Peek(0x0030));
        Assert.Equal((VibeEmulator.Mos6502.StatusFlags)0xEB, cpu.State.P);
    }

    [Fact]
    public void Smb_SetsBitWithoutChangingFlags()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x97, 0x30]); // SMB1 $30
        bus.Load(0x0030, [0x84]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        RunInstruction(cpu);

        Assert.Equal((byte)0x86, bus.Peek(0x0030));
    }

    [Fact]
    public void Tsb_SetsBitsFromAccumulator_ZeroFromAnd()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x04, 0x30]);
        bus.Load(0x0030, [0x02]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x04;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x06, bus.Peek(0x0030));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero)); // A & M = 0x04 & 0x02 = 0
    }

    [Fact]
    public void Trb_ClearsBitsFromAccumulator_ZeroFromAnd()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x14, 0x30]);
        bus.Load(0x0030, [0x07]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x03;

        RunInstruction(cpu);

        Assert.Equal((byte)0x04, bus.Peek(0x0030));
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Zero));
    }

    [Fact]
    public void Bbr_TakesFiveCyclesWhenBitClear_AndJumpsWhenSet()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x1F, 0x30, 0x04]); // BBR1 $30, +4
        bus.Load(0x0030, [0x02]); // bit 1 ustawiony → bez skoku
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((ushort)0x0203, cpu.State.PC);

        bus.Load(0x0030, [0x00]); // bit 1 wyczyszczony → skok
        cpu.Reset(0x0200);

        cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x0207, cpu.State.PC);
    }

    [Fact]
    public void Phx_Plx_RoundTrip_RestoresXAndStack()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xDA, 0xFA]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.X = 0x7B;
        ushort initialSp = cpu.State.SP;

        RunInstruction(cpu);
        cpu.State.X = 0x00;
        RunInstruction(cpu);

        Assert.Equal((byte)0x7B, cpu.State.X);
        Assert.Equal(initialSp, cpu.State.SP);
    }

    [Fact]
    public void Jmp_IndirectAbsoluteX_TakesSixCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x7C, 0x00, 0x03]);
        bus.Load(0x0302, [0x34, 0x12]); // wskaźnik pod $0300+X
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.X = 0x02;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x1234, cpu.State.PC);
    }

    [Fact]
    public void Wai_TakesThreeCyclesThenWaitsForIrq()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xCB, 0xEA]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        Assert.Equal(3, RunInstruction(cpu));
        Assert.Equal((ushort)0x0201, cpu.State.PC);

        cpu.Tick();
        Assert.Equal((ushort)0x0201, cpu.State.PC); // dalej czeka, NOP niewykonany

        cpu.IrqLevel = true;
        Assert.Equal(2, RunInstruction(cpu)); // IRQ budzi — NOP się wykonuje
        Assert.Equal((ushort)0x0202, cpu.State.PC);
    }

    [Fact]
    public void Stp_TakesThreeCyclesAndHalts()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xDB, 0xEA]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        cpu.Tick();
        cpu.Tick();
        cpu.Tick();

        Assert.Equal(3, cpu.Cycles);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
        Assert.Equal(1, cpu.Pending);

        long cycles = cpu.Cycles;
        cpu.Tick();
        cpu.Tick();

        Assert.Equal(cycles + 2, cpu.Cycles);
        Assert.Equal((ushort)0x0201, cpu.State.PC); // NOP nigdy się nie wykona
    }

    [Fact]
    public void Bit_Immediate_Cmos_SetsOnlyZeroFlag()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x89, 0x9E]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x6D;
        cpu.State.P = (VibeEmulator.Mos6502.StatusFlags)0xA3;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((VibeEmulator.Mos6502.StatusFlags)0xA1, cpu.State.P);
    }
}
