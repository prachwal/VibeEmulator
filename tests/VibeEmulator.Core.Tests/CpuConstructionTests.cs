using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.Tests;

public class CpuConstructionTests
{
    [Fact]
    public void Constructor_NullBus_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Cpu<IMemoryBus>(null!, Cpu6502.CreateTable()));
    }

    [Fact]
    public void Constructor_NullTable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Cpu<FlatMemory>(new FlatMemory(), null!));
    }

    [Fact]
    public void Constructor_ExposesBusAndTable()
    {
        var memory = new FlatMemory();
        var table = Cpu6502.CreateTable();

        var cpu = new Cpu<FlatMemory>(memory, table);

        Assert.Same(memory, cpu.Bus);
        Assert.Same(table, cpu.Table);
    }

    [Fact]
    public void Reset_SetsInitialState()
    {
        var cpu = Cpu6502.Create(new FlatMemory());
        cpu.State.A = 0x42;

        cpu.Reset(0x0200);

        Assert.Equal((byte)0, cpu.State.A);
        Assert.Equal((byte)0, cpu.State.X);
        Assert.Equal((byte)0, cpu.State.Y);
        Assert.Equal((ushort)0x0200, cpu.State.PC);
        Assert.Equal((ushort)0xFD, cpu.State.SP);
        Assert.True(cpu.State.P.Has(StatusFlags.InterruptDisable));
        Assert.False(cpu.State.P.Has(StatusFlags.Break));
    }

    [Fact]
    public void State_AllowsDirectRegisterManipulation()
    {
        var cpu = Cpu6502.Create(new FlatMemory());

        cpu.State.A = 0x12;
        cpu.State.X = 0x34;
        cpu.State.Y = 0x56;
        cpu.State.SP = 0x01;
        cpu.State.PC = 0xBEEF;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, true);

        Assert.Equal((byte)0x12, cpu.State.A);
        Assert.Equal((byte)0x34, cpu.State.X);
        Assert.Equal((byte)0x56, cpu.State.Y);
        Assert.Equal((ushort)0x0001, cpu.State.SP);
        Assert.Equal((ushort)0xBEEF, cpu.State.PC);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Step_ExecutesInstructionFromTable()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0xA9, 0x7F]); // LDA #$7F
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);

        Instruction instruction = cpu.Step();

        Assert.Equal("LDA", instruction.Mnemonic);
        Assert.Equal(AddressingMode.Immediate, instruction.Mode);
        Assert.Equal((byte)0x7F, cpu.State.A);
        Assert.Equal((ushort)0x0202, cpu.State.PC);
    }

    [Fact]
    public void Step_UndefinedOpcode_Throws()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0xFF]);
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);

        Assert.Throws<InvalidOperationException>(() => cpu.Step());
    }

    [Fact]
    public void Run_StopsOnUndefinedOpcode()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0xEA, 0xEA, 0xFF]); // NOP, NOP, nieznany
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);

        int executed = cpu.Run();

        Assert.Equal(2, executed);
        Assert.Equal((ushort)0x0202, cpu.State.PC);
    }

    [Fact]
    public void Run_StopsOnBreakInstruction()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0xEA, 0x00]); // NOP, BRK
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);

        int executed = cpu.Run();

        Assert.Equal(2, executed);
        Assert.True(cpu.State.P.Has(StatusFlags.Break));
    }

    [Fact]
    public void Run_RespectsMaxSteps()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0x4C, 0x00, 0x02]); // JMP $0200
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);

        int executed = cpu.Run(maxSteps: 100);

        Assert.Equal(100, executed);
    }

    [Fact]
    public void Run_NegativeMaxSteps_Throws()
    {
        var cpu = Cpu6502.Create(new FlatMemory());

        Assert.Throws<ArgumentOutOfRangeException>(() => cpu.Run(-1));
    }
}
