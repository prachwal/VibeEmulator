using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086AluBoundaryTests
{
    [Fact]
    public void Add_UnsignedCarryToZero_SetsCarryAuxiliaryCarryParityAndZero()
    {
        Intel8086AluResult result = Intel8086Alu.Execute(0xFF, 0x01, 0x0002, Intel8086AluOperation.Add);

        Assert.Equal((byte)0, result.Value);
        Assert.Equal((ushort)0x0055, (ushort)(result.Flags & 0x00D5));
    }

    [Fact]
    public void Add_SignedPositiveOverflow_SetsSignAuxiliaryCarryAndOverflow()
    {
        Intel8086AluResult result = Intel8086Alu.Execute(0x7F, 0x01, 0x0002, Intel8086AluOperation.Add);

        Assert.Equal((byte)0x80, result.Value);
        Assert.Equal((ushort)0x0890, (ushort)(result.Flags & 0x0895));
    }

    [Fact]
    public void Adc_UsesCarryInFromFlags()
    {
        Intel8086AluResult result = Intel8086Alu.Execute(0x01, 0x01, 0x0003, Intel8086AluOperation.Adc);

        Assert.Equal((byte)0x03, result.Value);
        Assert.Equal((ushort)0x0004, (ushort)(result.Flags & 0x00D5));
    }

    [Fact]
    public void Sub_BorrowFromZero_SetsCarryAuxiliaryCarrySignAndParity()
    {
        Intel8086AluResult result = Intel8086Alu.Execute(0x00, 0x01, 0x0002, Intel8086AluOperation.Sub);

        Assert.Equal((byte)0xFF, result.Value);
        Assert.Equal((ushort)0x0095, (ushort)(result.Flags & 0x00D5));
    }

    [Fact]
    public void Sbb_UsesCarryInForBorrow()
    {
        Intel8086AluResult result = Intel8086Alu.Execute(0x01, 0x00, 0x0003, Intel8086AluOperation.Sbb);

        Assert.Equal((byte)0, result.Value);
        Assert.Equal((ushort)0x0044, (ushort)(result.Flags & 0x00D5));
    }

    [Fact]
    public void Compare_EqualOperandsSetZeroFlagWithoutChangingOperand()
    {
        ushort initialFlags = 0x0A02;

        Intel8086AluResult result = Intel8086Alu.Execute(0x10, 0x10, initialFlags, Intel8086AluOperation.Compare);

        Assert.Equal((byte)0, result.Value);
        Assert.NotEqual(initialFlags, result.Flags);
        Assert.Equal(0x0040, result.Flags & 0x0040);
    }
}
