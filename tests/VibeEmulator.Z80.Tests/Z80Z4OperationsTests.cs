using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80Z4OperationsTests
{
    [Theory]
    [InlineData(0x08)]
    [InlineData(0xD9)]
    [InlineData(0xEB)]
    public void Tick_ExchangeTwice_RestoresRegisters(byte opcode)
    {
        var memory = new FlatMemory();
        memory.Write(0, opcode);
        memory.Write(1, opcode);
        var core = new Z80Core(memory, new FlatPorts());
        core.State.A = 0x12;
        core.State.F = 0xA9;
        core.State.B = 0x34;
        core.State.C = 0x56;
        core.State.D = 0x78;
        core.State.E = 0x9A;
        core.State.H = 0xBC;
        core.State.L = 0xDE;
        core.State.AF_ = 0x1128;
        core.State.BC_ = 0x2233;
        core.State.DE_ = 0x4455;
        core.State.HL_ = 0x6677;
        core.State.WZ = 0xCAFE;
        core.State.Q = 0xA9;
        Z80State initial = core.State;

        RunInstruction(core, 4);

        if (opcode == 0x08)
        {
            Assert.Equal((byte)0x11, core.State.A);
            Assert.Equal((byte)0x28, core.State.F);
            Assert.Equal((ushort)0x12A9, core.State.AF_);
        }
        else if (opcode == 0xD9)
        {
            Assert.Equal(((byte)0x22, (byte)0x33), (core.State.B, core.State.C));
            Assert.Equal(((byte)0x44, (byte)0x55), (core.State.D, core.State.E));
            Assert.Equal(((byte)0x66, (byte)0x77), (core.State.H, core.State.L));
            Assert.Equal((ushort)0x3456, core.State.BC_);
            Assert.Equal((ushort)0x789A, core.State.DE_);
            Assert.Equal((ushort)0xBCDE, core.State.HL_);
        }
        else
        {
            Assert.Equal(((byte)0xBC, (byte)0xDE), (core.State.D, core.State.E));
            Assert.Equal(((byte)0x78, (byte)0x9A), (core.State.H, core.State.L));
        }

        Assert.Equal((byte)0, core.State.Q);
        RunInstruction(core, 4);
        Assert.Equal((initial.A, initial.F, initial.AF_), (core.State.A, core.State.F, core.State.AF_));
        Assert.Equal((initial.B, initial.C, initial.BC_), (core.State.B, core.State.C, core.State.BC_));
        Assert.Equal((initial.D, initial.E, initial.DE_), (core.State.D, core.State.E, core.State.DE_));
        Assert.Equal((initial.H, initial.L, initial.HL_), (core.State.H, core.State.L, core.State.HL_));
        Assert.Equal(initial.WZ, core.State.WZ);
        Assert.Equal(8, core.Cycles);
    }

    [Fact]
    public void Tick_PushPopAfAtStackBoundary_PreservesFullFlagsAndWriteOrder()
    {
        var memory = new WriteRecordingBus();
        memory.Memory.Write(0x100, 0xF5);
        memory.Memory.Write(0x101, 0xF1);
        var core = new Z80Core(memory, new FlatPorts());
        core.Reset(0x100);
        core.State.SP = 1;
        core.State.A = 0xAB;
        core.State.F = 0xFF;

        RunInstruction(core, 11);

        Assert.Equal((ushort)0xFFFF, core.State.SP);
        Assert.Equal([(0, (byte)0xAB), (0xFFFF, (byte)0xFF)], memory.Writes);
        core.State.A = 0;
        core.State.F = 0;
        core.State.Q = 0x10;
        RunInstruction(core, 10);

        Assert.Equal((ushort)1, core.State.SP);
        Assert.Equal((byte)0xAB, core.State.A);
        Assert.Equal((byte)0xFF, core.State.F);
        Assert.Equal((byte)0, core.State.Q);
    }

    [Fact]
    public void Tick_ExchangeStackAtBoundary_UsesHighThenLowWritesAndSetsWz()
    {
        var memory = new WriteRecordingBus();
        memory.Memory.Write(0x100, 0xE3);
        memory.Memory.Write(0xFFFF, 0x78);
        memory.Memory.Write(0, 0x56);
        var core = new Z80Core(memory, new FlatPorts());
        core.Reset(0x100);
        core.State.SP = 0xFFFF;
        core.State.H = 0x12;
        core.State.L = 0x34;

        RunInstruction(core, 19);

        Assert.Equal([(0, (byte)0x12), (0xFFFF, (byte)0x34)], memory.Writes);
        Assert.Equal(((byte)0x56, (byte)0x78), (core.State.H, core.State.L));
        Assert.Equal((ushort)0x5678, core.State.WZ);
        Assert.Equal((ushort)0xFFFF, core.State.SP);
    }

    [Theory]
    [InlineData(0x22)]
    [InlineData(0x2A)]
    public void Tick_WordTransferAtAddressBoundary_WrapsHighByteAndWz(byte opcode)
    {
        var memory = new WriteRecordingBus();
        memory.Memory.Write(0x100, opcode);
        memory.Memory.Write(0x101, 0xFF);
        memory.Memory.Write(0x102, 0xFF);
        memory.Memory.Write(0xFFFF, 0x78);
        memory.Memory.Write(0, 0x56);
        var core = new Z80Core(memory, new FlatPorts());
        core.Reset(0x100);
        core.State.H = 0x12;
        core.State.L = 0x34;

        RunInstruction(core, 16);

        if (opcode == 0x22)
        {
            Assert.Equal([(0xFFFF, (byte)0x34), (0, (byte)0x12)], memory.Writes);
        }
        else
        {
            Assert.Equal(((byte)0x56, (byte)0x78), (core.State.H, core.State.L));
            Assert.Empty(memory.Writes);
        }

        Assert.Equal((ushort)0, core.State.WZ);
        Assert.Equal((ushort)0x103, core.State.PC);
    }

    [Fact]
    public void Tick_LoadPairAcrossPcBoundary_UsesLittleEndianAndWrapsPc()
    {
        var memory = new FlatMemory();
        memory.Write(0xFFFE, 0x01);
        memory.Write(0xFFFF, 0x34);
        memory.Write(0, 0x12);
        var core = new Z80Core(memory, new FlatPorts());
        core.Reset(0xFFFE);

        RunInstruction(core, 10);

        Assert.Equal(((byte)0x12, (byte)0x34), (core.State.B, core.State.C));
        Assert.Equal((ushort)1, core.State.PC);
    }

    private static void RunInstruction(Z80Core core, int expectedTStates)
    {
        int cycles = 0;
        do
        {
            core.Tick();
            cycles++;
        }
        while (core.Pending != 0 && cycles < 64);

        Assert.Equal(expectedTStates, cycles);
        Assert.Equal(0, core.Pending);
    }

    private sealed class WriteRecordingBus : IMemoryBus
    {
        public FlatMemory Memory { get; } = new();
        public List<(int Address, byte Value)> Writes { get; } = new();

        public byte Read(ushort address) => Memory.Read(address);

        public void Write(ushort address, byte value)
        {
            Writes.Add((address, value));
            Memory.Write(address, value);
        }
    }
}
