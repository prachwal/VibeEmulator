using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public partial class Z80Z5ControlTests
{
    [Theory]
    [InlineData(0x00FD, 1, 0x0100)]
    [InlineData(0x0100, 0xFC, 0x00FE)]
    [InlineData(0xFFFE, 1, 1)]
    [InlineData(0, 0xFD, 0xFFFF)]
    public void Tick_JrAcrossBoundary_AddsSignedOffsetAndSetsWz(ushort pc, byte offset, ushort expectedPc)
    {
        var bus = new RecordingBus();
        bus.Memory.Write(pc, 0x18);
        bus.Memory.Write((ushort)(pc + 1), offset);
        var core = new Z80Core(bus, new FlatPorts());
        core.Reset(pc);

        RunInstruction(core, 12);

        Assert.Equal(expectedPc, core.State.PC);
        Assert.Equal(expectedPc, core.State.WZ);
        Assert.Equal(2, bus.Reads.Count);
    }

    [Theory]
    [InlineData(1, 0, 8, 0x102, 0xBEEF)]
    [InlineData(0, 255, 13, 0x100, 0x100)]
    [InlineData(2, 1, 13, 0x100, 0x100)]
    public void Tick_DjnzBoundaryValues_UsesDecrementedBAndPreservesFlags(
        byte b, byte expectedB, int tStates, ushort expectedPc, ushort expectedWz)
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0x100, 0x10);
        bus.Memory.Write(0x101, 0xFE);
        var core = new Z80Core(bus, new FlatPorts());
        core.Reset(0x100);
        core.State.B = b;
        core.State.F = 0xFF;
        core.State.WZ = 0xBEEF;

        RunInstruction(core, tStates);

        Assert.Equal(expectedB, core.State.B);
        Assert.Equal(expectedPc, core.State.PC);
        Assert.Equal(expectedWz, core.State.WZ);
        Assert.Equal((byte)0xFF, core.State.F);
        Assert.Equal((byte)0, core.State.Q);
    }

    [Fact]
    public void Tick_JpHl_ReadsOnlyOpcodeAndPreservesWz()
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, 0xE9);
        var core = new Z80Core(bus, new FlatPorts());
        core.State.H = 0xAB;
        core.State.L = 0xCD;
        core.State.WZ = 0x1234;

        RunInstruction(core, 4);

        Assert.Equal([(ushort)0], bus.Reads);
        Assert.Equal((ushort)0xABCD, core.State.PC);
        Assert.Equal((ushort)0x1234, core.State.WZ);
    }

    [Theory]
    [InlineData(0xC2)]
    [InlineData(0xC4)]
    public void Tick_FalseAbsoluteCondition_ReadsTargetAndSetsWzWithoutStackAccess(byte opcode)
    {
        var bus = new RecordingBus();
        bus.Memory.Write(0, opcode);
        bus.Memory.Write(1, 0x34);
        bus.Memory.Write(2, 0x12);
        var core = new Z80Core(bus, new FlatPorts());
        core.State.F = 0x40;
        core.State.SP = 0x8000;

        RunInstruction(core, 10);

        Assert.Equal((ushort)3, core.State.PC);
        Assert.Equal((ushort)0x1234, core.State.WZ);
        Assert.Equal((ushort)0x8000, core.State.SP);
        Assert.Equal([(ushort)0, (ushort)1, (ushort)2], bus.Reads);
        Assert.Empty(bus.Writes);
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

    private sealed class RecordingBus : IMemoryBus
    {
        public FlatMemory Memory { get; } = new();
        public List<ushort> Reads { get; } = new();
        public List<(int Address, byte Value)> Writes { get; } = new();

        public byte Read(ushort address)
        {
            Reads.Add(address);
            return Memory.Read(address);
        }

        public void Write(ushort address, byte value)
        {
            Writes.Add((address, value));
            Memory.Write(address, value);
        }
    }
}
