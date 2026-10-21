namespace VibeEmulator.Mos6502.Tests;

public class StateMachineCpuCoreTests
{
    [Fact]
    public void Lda_Immediate_TakesTwoCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xA9, 0x42]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x42, cpu.State.A);
        Assert.Equal((ushort)0x0202, cpu.State.PC);
    }

    [Fact]
    public void Lda_Absolute_TakesFourCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xAD, 0x00, 0x03]);
        bus.Load(0x0300, [0x77]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((byte)0x77, cpu.State.A);
    }

    [Fact]
    public void Lda_AbsoluteX_WithoutPageCross_TakesFourCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xBD, 0x00, 0x03]);
        bus.Load(0x0305, [0x55]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.X = 0x05;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal((byte)0x55, cpu.State.A);
    }

    [Fact]
    public void Lda_AbsoluteX_WithPageCross_TakesFiveCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xBD, 0xFF, 0x30]); // $30FF + 1 = $3100
        bus.Load(0x3100, [0x99]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.X = 0x01;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0x99, cpu.State.A);
    }

    [Fact]
    public void Sta_AbsoluteX_AlwaysTakesFiveCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x9D, 0x00, 0x03]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0xAB;
        cpu.State.X = 0x02;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal((byte)0xAB, bus.Peek(0x0302));
    }

    [Fact]
    public void Inc_Absolute_WritesOldAndNewValue()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xEE, 0x00, 0x03]);
        bus.Load(0x0300, [0x41]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        bus.Writes.Clear();
        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal([(0x0300, (byte)0x41), (0x0300, (byte)0x42)], bus.Writes);
        Assert.Equal((byte)0x42, bus.Peek(0x0300));
    }

    [Fact]
    public void Inx_TakesTwoCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xE8]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x01, cpu.State.X);
    }

    [Fact]
    public void Jmp_Absolute_TakesThreeCycles()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x4C, 0x00, 0x04]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(3, cycles);
        Assert.Equal((ushort)0x0400, cpu.State.PC);
    }

    [Fact]
    public void Adc_SetsCarryAndOverflow()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0x69, 0x01]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);
        cpu.State.A = 0x7F;

        RunInstruction(cpu);

        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Overflow));
        Assert.True(cpu.State.P.Has(VibeEmulator.Mos6502.StatusFlags.Negative));
    }

    [Fact]
    public void UnknownOpcode_JamsCpu()
    {
        var bus = new TestBus();
        bus.Load(0x0200, [0xFF]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        cpu.Tick();

        Assert.True(cpu.Jammed);

        long before = cpu.Cycles;
        cpu.Tick();

        Assert.Equal(before + 1, cpu.Cycles);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
    }

    [Fact]
    public void Stall_DoesNotCountAsCpuCycle()
    {
        var bus = new TestBus();
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        cpu.Stall();
        cpu.Stall();

        Assert.Equal(0, cpu.Cycles);
    }

    private static int RunInstruction(StateMachineCpuCore cpu)
    {
        int cycles = 0;

        do
        {
            cpu.Tick();
            cycles++;
        }
        while (cpu.Pending != 0 && cycles < 64);

        return cycles;
    }

    private sealed class TestBus : VibeEmulator.Memory.IMemoryBus
    {
        private readonly byte[] _memory = new byte[0x10000];

        public List<(int Address, byte Value)> Writes { get; } = [];

        public byte Read(ushort address) => _memory[address];

        public void Write(ushort address, byte value)
        {
            Writes.Add((address, value));
            _memory[address] = value;
        }

        public void Load(ushort address, ReadOnlySpan<byte> data) => data.CopyTo(_memory.AsSpan(address));

        public byte Peek(ushort address) => _memory[address];
    }
}
