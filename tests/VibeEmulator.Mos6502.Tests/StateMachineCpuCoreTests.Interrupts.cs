namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy przerwań (etap F): BRK, IRQ, NMI i opóźnienia po CLI/SEI/PLP.
/// Część <c>partial</c> <see cref="StateMachineCpuCoreTests"/>; sekwencje zgodne z
/// <c>docs/cpu/interrupts-and-reset.md</c> i wektorami SingleStepTests (BRK: próbka 00.json).
/// </summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Brk_TakesSevenCyclesPushesAndJumpsThroughVector()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x00, 0xAA]); // BRK + bajt sygnatury (odrzucony)
        bus.Load(0xFFFE, [0x00, 0x04]); // wektor IRQ/BRK → $0400
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(7, cycles);
        Assert.Equal((ushort)0x0400, cpu.State.PC);
        Assert.Equal((ushort)0xFA, cpu.State.SP);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.InterruptDisable));
        Assert.Equal(
            [(0x01FD, (byte)0x02), (0x01FC, (byte)0x02), (0x01FB, (byte)0x34)],
            bus.Writes);
    }

    [Fact]
    public void Brk_Rti_RoundTrip_ReturnsAfterSignatureByte()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x00, 0xAA]);
        bus.Load(0xFFFE, [0x00, 0x04]);
        bus.Load(0x0400, [0x40]); // RTI w procedurze obsługi
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFD;

        RunInstruction(cpu);
        Assert.Equal((ushort)0x0400, cpu.State.PC);

        RunInstruction(cpu);

        Assert.Equal((ushort)0x0202, cpu.State.PC);
        Assert.Equal((ushort)0xFD, cpu.State.SP);
    }

    [Fact]
    public void Irq_TakesSevenCyclesAndPushesUnchangedPc()
    {
        var bus = new TestBus();
        bus.Load(0xFFFE, [0x34, 0x12]); // wektor → $1234
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.P &= ~VibeEmulator.Mos6502.StatusFlags.InterruptDisable;
        cpu.State.SP = 0xFD;
        cpu.IrqLevel = true;

        int cycles = RunInstruction(cpu);

        Assert.Equal(7, cycles);
        Assert.Equal((ushort)0x1234, cpu.State.PC);
        Assert.Equal((ushort)0xFA, cpu.State.SP);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.InterruptDisable));
        Assert.Equal(
            [(0x01FD, (byte)0x02), (0x01FC, (byte)0x00), (0x01FB, (byte)0x20)],
            bus.Writes);
    }

    [Fact]
    public void Irq_MaskedByInterruptDisable_ExecutesInstruction()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xA9, 0x42]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200); // P = Unused | InterruptDisable
        cpu.IrqLevel = true;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x42, cpu.State.A);
        Assert.Equal((ushort)0x0202, cpu.State.PC);
    }

    [Fact]
    public void Nmi_ServicedRegardlessOfInterruptDisable()
    {
        var bus = new TestBus();
        bus.Load(0xFFFA, [0x34, 0x12]); // wektor NMI → $1234
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200); // I = 1
        cpu.State.SP = 0xFD;
        cpu.NmiLevel = true; // zbocze opadające złapane w pierwszym Tick

        int cycles = RunInstruction(cpu);

        Assert.Equal(7, cycles);
        Assert.Equal((ushort)0x1234, cpu.State.PC);
        Assert.Equal((ushort)0xFA, cpu.State.SP);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.InterruptDisable));
    }

    [Fact]
    public void Cli_DefersPendingIrqByOneInstruction()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x58, 0xEA]); // CLI; NOP
        bus.Load(0xFFFE, [0x00, 0x04]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200); // I = 1
        cpu.IrqLevel = true;

        Assert.Equal(2, RunInstruction(cpu));
        Assert.Equal((ushort)0x0201, cpu.State.PC);

        Assert.Equal(2, RunInstruction(cpu));
        Assert.Equal((ushort)0x0202, cpu.State.PC);

        Assert.Equal(7, RunInstruction(cpu));
        Assert.Equal((ushort)0x0400, cpu.State.PC);
    }

    [Fact]
    public void Rti_ServicesIrqImmediately()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x40]); // RTI
        bus.Load(0x01FB, [0x20, 0x00, 0x04]); // P (I=0), PCL, PCH → powrót pod $0400
        bus.Load(0xFFFE, [0x00, 0x05]); // wektor IRQ → $0500
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFA;
        cpu.IrqLevel = true;

        Assert.Equal(6, RunInstruction(cpu));
        Assert.Equal((ushort)0x0400, cpu.State.PC);
        Assert.False(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.InterruptDisable));

        Assert.Equal(7, RunInstruction(cpu));
        Assert.Equal((ushort)0x0500, cpu.State.PC);
    }
}
