using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086StringCountTests
{
    [Theory]
    [InlineData(0xA4, 1, 1)]
    [InlineData(0xA5, 2, 2)]
    [InlineData(0xA6, 1, 1)]
    [InlineData(0xA7, 2, 2)]
    [InlineData(0xAA, 0, 1)]
    [InlineData(0xAB, 0, 2)]
    [InlineData(0xAC, 1, 0)]
    [InlineData(0xAD, 2, 0)]
    [InlineData(0xAE, 0, 1)]
    [InlineData(0xAF, 0, 2)]
    public void RepString_WithCountOne_ExecutesExactlyOneElement(byte opcode, int sourceAdvance, int destinationAdvance)
    {
        var memory = new FlatAddressMemory(0x100000);
        const ushort sourceSegment = 0x1000;
        const ushort destinationSegment = 0x2000;
        const ushort sourceOffset = 0x0100;
        const ushort destinationOffset = 0x0200;
        uint sourceAddress = PhysicalAddress(sourceSegment, sourceOffset);
        uint destinationAddress = PhysicalAddress(destinationSegment, destinationOffset);
        memory.Write(sourceAddress, 0x34);
        memory.Write(sourceAddress + 1, 0x12);
        memory.Write(destinationAddress, 0x34);
        memory.Write(destinationAddress + 1, 0x12);

        var cpu = new Intel8086Core(memory);
        var state = Intel8086State.Initial();
        state.DS = sourceSegment;
        state.ES = destinationSegment;
        state.SI = sourceOffset;
        state.DI = destinationOffset;
        state.CX = 1;
        state.AX = 0x1234;
        state.Queue = [0xF3, opcode, 0x90, 0x90, 0x90, 0x90];
        state.QueueCount = state.Queue.Length;
        cpu.State = state;

        int tStates = 0;
        do
        {
            cpu.Tick();
            tStates++;
        }
        while (cpu.Pending != 0 && tStates < 64);

        Assert.InRange(tStates, 1, 63);
        Assert.Equal((ushort)0, cpu.State.CX);
        Assert.Equal(unchecked((ushort)(sourceOffset + sourceAdvance)), cpu.State.SI);
        Assert.Equal(unchecked((ushort)(destinationOffset + destinationAdvance)), cpu.State.DI);

        if (opcode is 0xA4 or 0xA5)
        {
            Assert.Equal(memory.Read(sourceAddress), memory.Read(destinationAddress));
            Assert.Equal(memory.Read(sourceAddress + 1), memory.Read(destinationAddress + 1));
        }

        if (opcode is 0xAA or 0xAB)
        {
            Assert.Equal((byte)cpu.State.AX, memory.Read(destinationAddress));
            Assert.Equal((byte)(cpu.State.AX >> 8), memory.Read(destinationAddress + 1));
        }

        if (opcode is 0xAC or 0xAD)
        {
            Assert.Equal((ushort)0x1234, cpu.State.AX);
        }

        if (opcode is 0xA6 or 0xA7 or 0xAE or 0xAF)
        {
            Assert.NotEqual(0, cpu.State.Flags & 0x0040);
        }
    }

    private static uint PhysicalAddress(ushort segment, ushort offset) =>
        (uint)(((segment << 4) + offset) & 0xFFFFF);
}
