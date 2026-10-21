namespace VibeEmulator.Mos6502.Tests;

/// <summary>Testy stosu (etap D) — część <c>partial</c> <see cref="StateMachineCpuCoreTests"/>.</summary>
public partial class StateMachineCpuCoreTests
{
    [Fact]
    public void Pha_TakesThreeCyclesAndPushesAccumulator()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x48]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x42;
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x42, bus.Peek(0x01FD));
        Assert.Equal((ushort)0xFC, cpu.State.SP);
    }

    [Fact]
    public void Php_PushesStatusWithBreakAndUnusedSet()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x08]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.P = VibeEmulator.Mos6502.StatusFlags.Unused | VibeEmulator.Mos6502.StatusFlags.Carry;
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((byte)0x31, bus.Peek(0x01FD));
        Assert.Equal((ushort)0xFC, cpu.State.SP);
    }

    [Fact]
    public void Pla_TakesFourCyclesAndPullsAccumulator()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x68]);
        bus.Load(0x01FE, [0x80]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.Equal((ushort)0xFE, cpu.State.SP);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
    }

    [Fact]
    public void Plp_PullsStatusMaskingBreakBits()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x28]);
        bus.Load(0x01FE, [0xFF]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.SP = 0xFD;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((ushort)0xFE, cpu.State.SP);
        Assert.Equal((VibeEmulator.Mos6502.StatusFlags)0xEF, cpu.State.P);
    }

    [Fact]
    public void Stack_PushPullRoundTrip_PreservesValueAndPointer()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x48, 0x68, 0x08, 0x28]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x5A;
        cpu.State.P = VibeEmulator.Mos6502.StatusFlags.Unused | VibeEmulator.Mos6502.StatusFlags.Zero;
        ushort initialSp = cpu.State.SP;

        RunInstruction(cpu);
        cpu.State.A = 0x00;
        RunInstruction(cpu);
        Assert.Equal((byte)0x5A, cpu.State.A);

        // PLA ustawia N/Z z A, więc P przed PHP ustawiamy jawnie; odczyt po PHP, nie przed.
        cpu.State.P = VibeEmulator.Mos6502.StatusFlags.Unused | VibeEmulator.Mos6502.StatusFlags.Zero;
        RunInstruction(cpu);
        byte pushedP = bus.Peek((ushort)(0x0100 | (initialSp & 0xFF)));
        RunInstruction(cpu);
        Assert.Equal(initialSp, cpu.State.SP);
        Assert.Equal((byte)(pushedP & 0xCF | 0x20), (byte)cpu.State.P);
    }
}
