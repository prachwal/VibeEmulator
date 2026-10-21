namespace VibeEmulator.Mos6502.Tests;

/// <summary>Testy sterowania (etap E) — część <c>partial</c> <see cref="StateMachineCpuCoreTests"/>.</summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Jsr_TakesSixCyclesPushesReturnAndJumps()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x20, 0x00, 0x04]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x0400, cpu.State.PC);
        Assert.Equal((ushort)0xFB, cpu.State.SP);
        Assert.Equal((byte)0x02, bus.Peek(0x01FD));
        Assert.Equal((byte)0x02, bus.Peek(0x01FC));
    }

    [Fact]
    public void Rts_TakesSixCyclesPullsAndIncrements()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x60]);
        bus.Load(0x01FC, [0x00, 0x04]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFB;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x0401, cpu.State.PC);
        Assert.Equal((ushort)0xFD, cpu.State.SP);
    }

    [Fact]
    public void Rti_TakesSixCyclesPullsStatusAndJumpsWithoutIncrement()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x40]);
        bus.Load(0x01FB, [0x24, 0x00, 0x04]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFA;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x0400, cpu.State.PC);
        Assert.Equal((ushort)0xFD, cpu.State.SP);
        Assert.Equal((VibeEmulator.Mos6502.StatusFlags)0x24, cpu.State.P);
    }

    [Fact]
    public void JmpIndirect_NmosWrapsHighByteOnPageBoundary()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x6C, 0xFF, 0x10]);
        bus.Load(0x10FF, [0x34]);
        bus.Load(0x1000, [0x12]);
        bus.Load(0x1100, [0x99]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((ushort)0x1234, cpu.State.PC);
    }

    [Fact]
    public void JmpIndirect_CmosReadsCorrectHighByteWithExtraCycle()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x6C, 0xFF, 0x10]);
        bus.Load(0x10FF, [0x34]);
        bus.Load(0x1000, [0x12]);
        bus.Load(0x1100, [0x99]);
        var cpu = new StateMachineCpuCore(bus, VibeEmulator.Mos6502.CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal((ushort)0x9934, cpu.State.PC);
    }

    [Fact]
    public void Jsr_Rts_RoundTrip_ReturnsAfterCall()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x20, 0x05, 0x02, 0xEA, 0xEA]);
        bus.Load(0x0205, [0x60]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        ushort initialSp = cpu.State.SP;

        RunInstruction(cpu);
        Assert.Equal((ushort)0x0205, cpu.State.PC);
        RunInstruction(cpu);
        Assert.Equal((ushort)0x0203, cpu.State.PC);
        Assert.Equal(initialSp, cpu.State.SP);
    }
}
