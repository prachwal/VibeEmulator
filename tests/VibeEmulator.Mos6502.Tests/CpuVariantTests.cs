namespace VibeEmulator.Mos6502.Tests;

public partial class CpuVariantTests
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
            [
                new(0x0200, 0xEE, IsWrite: false),
                new(0x0201, 0x00, IsWrite: false),
                new(0x0202, 0x03, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x41, IsWrite: true),
                new(0x0300, 0x42, IsWrite: true),
            ],
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
            [
                new(0x0200, 0xEE, IsWrite: false),
                new(0x0201, 0x00, IsWrite: false),
                new(0x0202, 0x03, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x41, IsWrite: false),
                new(0x0300, 0x42, IsWrite: true),
            ],
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
    public void DefaultConstructor_UsesNmos6502_JamsOnUndefinedOpcode()
    {
        // Etap J: 0x1A jest w NMOS nieoficjalnym NOP-em, więc jam sprawdzamy na KIL (0x02).
        var bus = new RecordingBus();
        LoadProgram(bus, 0x0200, [0x02]);
        var cpu = new StateMachineCpuCore(bus);
        cpu.Reset(0x0200);

        cpu.Tick();

        Assert.Null(StateMachineCpuCore.Describe(0x02));
        Assert.True(cpu.Jammed);
    }

    [Fact]
    public void CpuVariant_DuplicateOpcode_ThrowsArgumentException()
    {
        CpuOpcode nop = new(0xEA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        Assert.Throws<ArgumentException>(() => new CpuVariant("zduplikowany", [nop, nop], RmwStyle.DoubleWrite, IndexedDummyRead.GhostByte));
    }

    [Fact]
    public void Cmos65C02_Table_IsNmosPlusAllAdditions()
    {
        // Etap J: baza NMOS ma 244 opkody, a 65C02 definiuje część z nich inaczej — dodatki 65C02
        // **nadpisują** bazę, więc tabela 65C02 to pełne 256 bajtów, nie suma.
        Assert.Equal(256, CpuVariants.Cmos65C02.Opcodes.Count);
        Assert.Equal(244, CpuVariants.Nmos6502.Opcodes.Count);
        Assert.Equal("STZ", CpuVariants.Cmos65C02.Describe(0x9E)?.Mnemonic);
        Assert.Equal("SHX", CpuVariants.Nmos6502.Describe(0x9E)?.Mnemonic);
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
        // Etap J: 0x9E jest w NMOS opkodems SHX abs,Y — nieużywany w NMOS jest tylko KIL (0x02).
        byte[] memory = [0x02, 0x34, 0x12];

        string text = Disassembler.Decode(CpuVariants.Nmos6502, address => memory[address], 0, out int length);

        Assert.Equal("???", text);
        Assert.Equal(1, length);
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
