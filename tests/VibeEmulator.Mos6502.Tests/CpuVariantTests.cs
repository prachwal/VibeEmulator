namespace VibeEmulator.Mos6502.Tests;

public class CpuVariantTests
{
    [Fact]
    public void Rmw_IncAbsolute_Nmos_WritesOldThenNewValue()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xEE, 0x00, 0x03]);
        bus.Poke(0x0300, 0x41);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nmos6502);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal(
            new List<BusAccess>
            {
                new(0x0200, 0xEE, IsWrite: false),
                new(0x0201, 0x00, IsWrite: false),
                new(0x0202, 0x03, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x41, IsWrite: true),
                new(0x0300, 0x42, IsWrite: true),
            },
            bus.Accesses);
    }

    [Fact]
    public void Rmw_IncAbsolute_Cmos_ReadsTwiceThenWritesNewValue()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0xEE, 0x00, 0x03]);
        bus.Poke(0x0300, 0x41);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(6, cycles);
        Assert.Equal(
            new List<BusAccess>
            {
                new(0x0200, 0xEE, IsWrite: false),
                new(0x0201, 0x00, IsWrite: false),
                new(0x0202, 0x03, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x42, IsWrite: true),
            },
            bus.Accesses);
        Assert.Equal((byte)0x42, bus.Peek(0x0300));
    }

    [Fact]
    public void Stz_Absolute_WritesZeroToMemory()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x9C, 0x00, 0x03]);
        bus.Poke(0x0300, 0x55);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);

        int cycles = RunInstruction(cpu);

        Assert.Equal(4, cycles);
        Assert.Equal(new BusAccess(0x0300, 0x00, IsWrite: true), bus.Accesses[^1]);
        Assert.Equal((byte)0x00, bus.Peek(0x0300));
    }

    [Fact]
    public void IncA_WrapsFfToZeroAndSetsZero()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x1A]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0xFF;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Zero));
        Assert.False(cpu.State.P.Has(StatusFlags.Negative));
        Assert.Equal((ushort)0x0201, cpu.State.PC);
    }

    [Fact]
    public void IncA_From7FSetsNegative()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x1A]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x7F;

        RunInstruction(cpu);

        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
        Assert.False(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void DecA_WrapsZeroToFfAndSetsNegative()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x3A]);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Cmos65C02);
        cpu.Reset(0x0200);
        cpu.State.A = 0x00;

        int cycles = RunInstruction(cpu);

        Assert.Equal(2, cycles);
        Assert.Equal((byte)0xFF, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
        Assert.False(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void DefaultConstructor_UsesNmos6502_JamsOnCmosOnlyOpcode()
    {
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x1A]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        cpu.Tick();

        Assert.Null(StateMachineCpuCore.Describe(0x1A));
        Assert.True(cpu.Jammed);
    }

    [Fact]
    public void CpuVariant_DuplicateOpcode_ThrowsArgumentException()
    {
        CpuOpcode nop = new(0xEA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        Assert.Throws<ArgumentException>(() => new CpuVariant("zduplikowany", [nop, nop], RmwStyle.DoubleWrite, IndexedDummyRead.GhostByte));
    }

    [Fact]
    public void Cmos65C02_Table_IsNmosPlusSixAdditions()
    {
        Assert.Equal(CpuVariants.Nmos6502.Opcodes.Count + 6, CpuVariants.Cmos65C02.Opcodes.Count);
        Assert.Equal("STZ", CpuVariants.Cmos65C02.Describe(0x9E)?.Mnemonic);
        Assert.Null(CpuVariants.Nmos6502.Describe(0x9E));
    }

    [Fact]
    public void AtomicCmos65C02_Table_AddsSixOpcodesToStandard()
    {
        Assert.Equal(
            Cpu6502.CreateTable().DefinedCount + 6,
            Cpu65C02.CreateTable().DefinedCount);
        Assert.True(Cpu65C02.CreateTable().IsDefined(0x1A));
        Assert.False(Cpu6502.CreateTable().IsDefined(0x1A));
    }

    [Theory]
    [InlineData(0x1A, 0x00, 0x00, "INC A", 1)]
    [InlineData(0x3A, 0x00, 0x00, "DEC A", 1)]
    [InlineData(0x64, 0x10, 0x00, "STZ zp $10", 2)]
    [InlineData(0x74, 0x10, 0x00, "STZ zp,X $10", 2)]
    [InlineData(0x9C, 0x34, 0x12, "STZ abs $1234", 3)]
    [InlineData(0x9E, 0x34, 0x12, "STZ abs,X $1234", 3)]
    public void Decode_Cmos65C02Opcode_MatchesReferenceFormat(int opcode, int operandLow, int operandHigh, string expected, int expectedLength)
    {
        byte[] memory = [(byte)opcode, (byte)operandLow, (byte)operandHigh];

        string text = Disassembler.Decode(CpuVariants.Cmos65C02, address => memory[address], 0, out int length);

        Assert.Equal(expected, text);
        Assert.Equal(expectedLength, length);
    }

    [Fact]
    public void Decode_Cmos65C02Opcode_NmosVariantReturnsUnknown()
    {
        byte[] memory = [0x9E, 0x34, 0x12];

        string text = Disassembler.Decode(CpuVariants.Nmos6502, address => memory[address], 0, out int length);

        Assert.Equal("???", text);
        Assert.Equal(1, length);
    }

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
}
