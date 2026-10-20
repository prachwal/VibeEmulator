using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.Tests;

public class AddressingModeTests
{
    [Fact]
    public void Immediate_ReturnsOperandByteAndAdvancesPc()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0x42);
        var state = CpuState.Reset();
        state.PC = 0x0200;

        Operand operand = Addressing.Resolve(AddressingMode.Immediate, ref state, memory);

        Assert.Equal((ushort)0x0200, operand.Address);
        Assert.Equal((byte)0x42, operand.Value);
        Assert.Equal((ushort)0x0201, state.PC);
    }

    [Fact]
    public void ZeroPage_ReadsSingleByteAddress()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0x10);
        memory.Write(0x0010, 0xAB);
        var state = CpuState.Reset();
        state.PC = 0x0200;

        Operand operand = Addressing.Resolve(AddressingMode.ZeroPage, ref state, memory);

        Assert.Equal((ushort)0x0010, operand.Address);
        Assert.Equal((byte)0xAB, operand.Value);
        Assert.Equal((ushort)0x0201, state.PC);
    }

    [Fact]
    public void ZeroPageX_WrapsWithinZeroPage()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0xFE);
        memory.Write(0x0003, 0x77); // 0xFE + 5 = 0x103 → 0x03
        var state = CpuState.Reset();
        state.PC = 0x0200;
        state.X = 0x05;

        Operand operand = Addressing.Resolve(AddressingMode.ZeroPageX, ref state, memory);

        Assert.Equal((ushort)0x0003, operand.Address);
        Assert.Equal((byte)0x77, operand.Value);
    }

    [Fact]
    public void Absolute_ReadsLittleEndianWord()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0x34);
        memory.Write(0x0201, 0x12);
        memory.Write(0x1234, 0x99);
        var state = CpuState.Reset();
        state.PC = 0x0200;

        Operand operand = Addressing.Resolve(AddressingMode.Absolute, ref state, memory);

        Assert.Equal((ushort)0x1234, operand.Address);
        Assert.Equal((byte)0x99, operand.Value);
        Assert.Equal((ushort)0x0202, state.PC);
    }

    [Fact]
    public void AbsoluteX_AddsIndexRegister()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0x34);
        memory.Write(0x0201, 0x12);
        memory.Write(0x1236, 0x01);
        var state = CpuState.Reset();
        state.PC = 0x0200;
        state.X = 0x02;

        Operand operand = Addressing.Resolve(AddressingMode.AbsoluteX, ref state, memory);

        Assert.Equal((ushort)0x1236, operand.Address);
        Assert.Equal((byte)0x01, operand.Value);
    }

    [Fact]
    public void Implied_DoesNotAdvancePcNorReadMemory()
    {
        var memory = new FlatMemory();
        var state = CpuState.Reset();
        state.PC = 0x0200;

        Operand operand = Addressing.Resolve(AddressingMode.Implied, ref state, memory);

        Assert.Equal((ushort)0, operand.Address);
        Assert.Equal((byte)0, operand.Value);
        Assert.Equal((ushort)0x0200, state.PC);
    }

    [Fact]
    public void Indirect_DereferencesPointerFromMemory()
    {
        var memory = new FlatMemory();
        memory.Write(0x0200, 0x00);
        memory.Write(0x0201, 0x03); // wskaźnik = $0300
        memory.Write(0x0300, 0x10);
        memory.Write(0x0301, 0x02); // cel = $0210
        memory.Write(0x0210, 0x55);
        var state = CpuState.Reset();
        state.PC = 0x0200;

        Operand operand = Addressing.Resolve(AddressingMode.Indirect, ref state, memory);

        Assert.Equal((ushort)0x0210, operand.Address);
        Assert.Equal((byte)0x55, operand.Value);
        Assert.Equal((ushort)0x0202, state.PC);
    }

    [Fact]
    public void Resolve_NullBus_Throws()
    {
        var state = CpuState.Reset();

        Assert.Throws<ArgumentNullException>(
            () => Addressing.Resolve(AddressingMode.Implied, ref state, null!));
    }

    [Fact]
    public void Resolve_UnknownMode_Throws()
    {
        var memory = new FlatMemory();
        var state = CpuState.Reset();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Addressing.Resolve((AddressingMode)99, ref state, memory));
    }
}
