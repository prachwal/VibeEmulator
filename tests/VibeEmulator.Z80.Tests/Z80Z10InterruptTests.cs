using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public sealed class Z80Z10InterruptTests
{
    [Fact]
    public void Tick_Im0WithDefaultBusByte_ExecutesRst38InThirteenTStates()
    {
        RecordingBus bus = new();
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x4567;
        core.State.SP = 0x9000;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 0;
        core.IrqLevel = true;

        Tick(core, 13);

        Assert.Equal((ushort)0x0038, core.State.PC);
        Assert.Equal((ushort)0x8FFE, core.State.SP);
        Assert.Equal([(int)0x8FFF, (byte)0x45, (int)0x8FFE, (byte)0x67], FlattenWrites(bus.Writes));
        Assert.False(core.State.IFF1);
        Assert.False(core.State.IFF2);
        Assert.Equal((byte)1, core.State.R);
        Assert.Equal(0, core.Pending);
    }

    [Fact]
    public void Tick_Im0ExecutesInjectedNopAfterSixStateAcknowledge()
    {
        RecordingBus bus = new();
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x4567;
        core.State.IFF1 = true;
        core.InterruptDataBus = 0x00;
        core.IrqLevel = true;

        Tick(core, 6);

        Assert.Equal((ushort)0x4567, core.State.PC);
        Assert.Equal((byte)1, core.State.R);
        Assert.Empty(bus.Reads);
        Assert.Equal(0, core.Pending);
    }

    [Fact]
    public void Tick_Im0CbPrefixExecutesSecondAcknowledgeOpcode()
    {
        RecordingBus bus = new();
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x4567;
        core.State.B = 0x80;
        core.State.IFF1 = true;
        core.InterruptDataBus = 0xCB;
        core.IrqLevel = true;

        Tick(core, 6);
        core.InterruptDataBus = 0x00;
        core.Tick();
        Tick(core, 5);

        Assert.Equal((byte)0x01, core.State.B);
        Assert.Equal((byte)2, core.State.R);
        Assert.Equal((ushort)0x4567, core.State.PC);
        Assert.Empty(bus.Reads);
    }

    [Fact]
    public void Tick_Im1_UsesSevenStateAcknowledgeAndThirteenStateResponse()
    {
        RecordingBus bus = new();
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x1234;
        core.State.SP = 0x9000;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 1;
        core.IrqLevel = true;
        List<string> pins = [];

        for (int i = 0; i < 13; i++)
        {
            core.Tick();
            pins.Add(core.LastPins);
        }

        Assert.Equal((ushort)0x0038, core.State.PC);
        Assert.Equal((ushort)0x8FFE, core.State.SP);
        Assert.Equal([(0x8FFF, (byte)0x12), (0x8FFE, (byte)0x34)], bus.Writes);
        Assert.Equal(
            ["----", "----", "----", "----", "r--i", "----", "----", "----", "-wm-", "----", "----", "-wm-", "----"],
            pins);
        Assert.Equal(13, core.Cycles);
    }

    [Fact]
    public void Tick_Im2_UsesOddInterruptVectorByteWithoutMaskingIt()
    {
        RecordingBus bus = new();
        bus.Memory.Write(0x1201, 0xCD);
        bus.Memory.Write(0x1202, 0xAB);
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x3456;
        core.State.SP = 0x8000;
        core.State.I = 0x12;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 2;
        core.InterruptDataBus = 0x01;
        core.IrqLevel = true;

        Tick(core, 19);

        Assert.Equal((ushort)0xABCD, core.State.PC);
        Assert.Equal((ushort)0x7FFE, core.State.SP);
        Assert.Equal([(0x7FFF, (byte)0x34), (0x7FFE, (byte)0x56)], bus.Writes);
        Assert.Equal([(ushort)0x1201, (ushort)0x1202], bus.Reads);
        Assert.Equal((byte)1, core.State.R);
        Assert.Equal(19, core.Cycles);
    }

    [Fact]
    public void Tick_Nmi_TakesPriorityAndLeavesIff2Unchanged()
    {
        RecordingBus bus = new();
        bus.Memory.Write(0x0066, 0x00);
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x2345;
        core.State.SP = 0xA000;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 1;
        core.IrqLevel = true;
        core.NmiLevel = true;

        Tick(core, 11);

        Assert.Equal((ushort)0x0066, core.State.PC);
        Assert.Equal((ushort)0x9FFE, core.State.SP);
        Assert.Equal([(0x9FFF, (byte)0x23), (0x9FFE, (byte)0x45)], bus.Writes);
        Assert.False(core.State.IFF1);
        Assert.True(core.State.IFF2);
        Assert.Equal(11, core.Cycles);

        Tick(core, 4);
        Assert.Equal((ushort)0x0067, core.State.PC);
        Assert.Equal(15, core.Cycles);
    }

    [Fact]
    public void Tick_Ei_DelaysMaskableInterruptUntilAfterFollowingInstruction()
    {
        RecordingBus bus = new();
        bus.Memory.Write(0, 0xFB);
        bus.Memory.Write(1, 0x00);
        Z80Core core = CreateCore(bus);
        core.State.SP = 0x9000;
        core.State.IM = 1;
        core.IrqLevel = true;

        Tick(core, 4);
        Assert.Equal((ushort)1, core.State.PC);
        Assert.Equal((byte)1, core.State.EI);
        Assert.Empty(bus.Writes);

        Tick(core, 4);
        Assert.Equal((ushort)2, core.State.PC);
        Assert.Equal((byte)0, core.State.EI);
        Assert.Empty(bus.Writes);

        Tick(core, 13);
        Assert.Equal((ushort)0x0038, core.State.PC);
        Assert.Equal([(0x8FFF, (byte)0), (0x8FFE, (byte)2)], bus.Writes);
    }

    [Fact]
    public void Tick_Halt_RepeatsFetchAndInterruptReturnsAfterHalt()
    {
        RecordingBus bus = new();
        bus.Memory.Write(0, 0x76);
        Z80Core core = CreateCore(bus);
        core.State.SP = 0x9000;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 1;

        Tick(core, 4);
        Assert.True(core.Halted);
        Assert.Equal((ushort)1, core.State.PC);

        Tick(core, 8);
        Assert.Equal((ushort)1, core.State.PC);
        Assert.Equal(3, bus.Reads.Count);
        Assert.Equal((byte)3, core.State.R);

        core.IrqLevel = true;
        Tick(core, 13);
        Assert.False(core.Halted);
        Assert.Equal((ushort)0x0038, core.State.PC);
        Assert.Equal([(0x8FFF, (byte)0), (0x8FFE, (byte)1)], bus.Writes);
    }

    [Fact]
    public void Tick_InterruptAfterLdAi_ClearsParityFlag()
    {
        RecordingBus bus = new();
        bus.Memory.Write(0, 0xED);
        bus.Memory.Write(1, 0x57);
        Z80Core core = CreateCore(bus);
        core.State.I = 0x80;
        core.State.SP = 0x9000;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.State.IM = 1;

        Tick(core, 9);

        Assert.NotEqual(0, core.State.F & 0x04);
        core.IrqLevel = true;
        core.Tick();
        Assert.Equal(0, core.State.F & 0x04);

        Tick(core, 12);
        Assert.Equal((ushort)0x0038, core.State.PC);
    }

    [Fact]
    public void Tick_ResetLevel_ResetsDocumentedFieldsAfterThreeTStatesAndKeepsOtherRegisters()
    {
        RecordingBus bus = new();
        Z80Core core = CreateCore(bus);
        core.State.PC = 0x4567;
        core.State.SP = 0x9000;
        core.State.A = 0x5A;
        core.State.B = 0x11;
        core.State.I = 0x12;
        core.State.R = 0x34;
        core.State.IM = 2;
        core.State.IFF1 = true;
        core.State.IFF2 = true;
        core.ResetLevel = true;

        Tick(core, 2);
        Assert.Equal((ushort)0x4567, core.State.PC);
        Tick(core, 1);

        Assert.Equal((ushort)0, core.State.PC);
        Assert.Equal((byte)0, core.State.I);
        Assert.Equal((byte)0, core.State.R);
        Assert.Equal((byte)0, core.State.IM);
        Assert.False(core.State.IFF1);
        Assert.False(core.State.IFF2);
        Assert.Equal((byte)0x5A, core.State.A);
        Assert.Equal((byte)0x11, core.State.B);
        Assert.Equal((ushort)0x9000, core.State.SP);
        Assert.Empty(bus.Reads);
    }

    private static Z80Core CreateCore(RecordingBus bus) => new(bus, new FlatPorts());

    private static void Tick(Z80Core core, int count)
    {
        for (int i = 0; i < count; i++)
        {
            core.Tick();
        }
    }

    private static int[] FlattenWrites(List<(int Address, byte Value)> writes) =>
        writes.SelectMany(write => new[] { write.Address, (int)write.Value }).ToArray();

    private sealed class RecordingBus : IMemoryBus
    {
        public FlatMemory Memory { get; } = new();
        public List<ushort> Reads { get; } = [];
        public List<(int Address, byte Value)> Writes { get; } = [];

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
