namespace VibeEmulator.Mos6502.Tests;

public partial class CpuVariantTests
{
    [Fact]
    public void Implied_Nop_ReadsPcInSecondCycle()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xEA, 0x55]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal(
            new List<BusAccess>
            {
                new(0x0200, 0xEA, IsWrite: false),
                new(0x0201, 0x55, IsWrite: false),
            },
            bus.Accesses);
        Assert.Equal((ushort)0x0201, cpu.State.PC);
    }

    [Fact]
    public void Indexed_Nmos_PageCrossReadUsesGhostByte()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xBD, 0xFF, 0x02]);
        bus.Poke(0x0300, 0x99);
        bus.Poke(0x0200, 0xBD);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        cpu.State.X = 0x01;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal(new BusAccess(0x0200, 0xBD, IsWrite: false), bus.Accesses[3]);
        Assert.Equal(new BusAccess(0x0300, 0x99, IsWrite: false), bus.Accesses[4]);
    }

    [Fact]
    public void Indexed_Cmos_PageCrossReadUsesOperandHighByte()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xBD, 0xFF, 0x02]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.X = 0x01;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal(new BusAccess(0x0202, 0x02, IsWrite: false), bus.Accesses[3]);
        Assert.Equal(new BusAccess(0x0300, 0x00, IsWrite: false), bus.Accesses[4]);
    }

    [Fact]
    public void Indexed_Cmos_ReadWithoutPageCrossHasNoDummyRead()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xBD, 0x10, 0x03]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.X = 0x01;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal(new BusAccess(0x0311, 0x00, IsWrite: false), bus.Accesses[^1]);
    }

    [Fact]
    public void Indexed_Cmos_StoreAlwaysHasOperandHighDummyRead()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x9D, 0x10, 0x03]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.X = 0x01;
        cpu.State.A = 0x5A;

        int cycles = RunInstruction(cpu);

        Assert.Equal(5, cycles);
        Assert.Equal(new BusAccess(0x0202, 0x03, IsWrite: false), bus.Accesses[3]);
        Assert.Equal(new BusAccess(0x0311, 0x5A, IsWrite: true), bus.Accesses[4]);
    }

    [Fact]
    public void ZeroPageY_Ldx_WrapsInsideZeroPage()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xB6, 0xF0]);
        bus.Poke(0x0010, 0x6C);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        cpu.State.Y = 0x20;

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal(new BusAccess(0x00F0, 0x00, IsWrite: false), bus.Accesses[2]);
        Assert.Equal((byte)0x6C, cpu.State.X);
    }

    [Fact]
    public void IndexedIndirect_Lda_ReadsPointerWithZeroPageWrap()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xA1, 0xFE]);
        bus.Poke(0x0000, 0x34);
        bus.Poke(0x0001, 0x12);
        bus.Poke(0x1234, 0x77);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        cpu.State.X = 0x02;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal(new BusAccess(0x0000, 0x34, IsWrite: false), bus.Accesses[3]);
        Assert.Equal(new BusAccess(0x0001, 0x12, IsWrite: false), bus.Accesses[4]);
        Assert.Equal((byte)0x77, cpu.State.A);
    }

    [Fact]
    public void IndirectIndexed_Sta_PageCrossWritesAfterGhostRead()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x91, 0x10]);
        bus.Poke(0x0010, 0xF0);
        bus.Poke(0x0011, 0x12);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        cpu.State.Y = 0x20;
        cpu.State.A = 0x3C;

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal(new BusAccess(0x1210, 0x00, IsWrite: false), bus.Accesses[4]);
        Assert.Equal(new BusAccess(0x1310, 0x3C, IsWrite: true), bus.Accesses[5]);
    }

    [Theory]
    [InlineData(0x18, StatusFlags.Carry, false)]
    [InlineData(0x38, StatusFlags.Carry, true)]
    [InlineData(0x58, StatusFlags.InterruptDisable, false)]
    [InlineData(0x78, StatusFlags.InterruptDisable, true)]
    [InlineData(0xB8, StatusFlags.Overflow, false)]
    [InlineData(0xD8, StatusFlags.Decimal, false)]
    [InlineData(0xF8, StatusFlags.Decimal, true)]
    public void Flag_Opcode_SetsOnlyItsBit(int opcode, StatusFlags flag, bool value)
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [(byte)opcode]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        StatusFlags before = (StatusFlags)0xFF;
        cpu.State.P = before;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal(before.With(flag, value), cpu.State.P);
    }

    [Fact]
    public void Tsx_CopiesStackPointerAndSetsFlags_Txs_LeavesFlags()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xBA, 0x9A]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);
        cpu.State.SP = 0x80;

        RunInstruction(cpu);
        Assert.Equal((byte)0x80, cpu.State.X);
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));

        cpu.State.X = 0x10;
        cpu.State.P = StatusFlags.Unused;
        RunInstruction(cpu);
        Assert.Equal((ushort)0x10, cpu.State.SP);
        Assert.Equal(StatusFlags.Unused, cpu.State.P);
    }

    private static void LoadProgram(RecordingBus bus, ushort address, byte[] program)
    {
        for (int i = 0; i < program.Length; i++)
        {
            bus.Poke((ushort)(address + i), program[i]);
        }
    }
}
