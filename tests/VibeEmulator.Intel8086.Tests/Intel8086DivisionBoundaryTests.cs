using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086DivisionBoundaryTests
{
    [Fact]
    public void TryDivideByte_SignedMinimumNegativeQuotient_ReportsDivideFault()
    {
        bool succeeded = Intel8086SpecialArithmetic.TryDivideByte(
            0xFF80,
            0x01,
            signedOperation: true,
            out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryDivideByte_SignedLargestNegativeQuotient_ReturnsQuotientAndRemainder()
    {
        bool succeeded = Intel8086SpecialArithmetic.TryDivideByte(
            0xFF81,
            0x01,
            signedOperation: true,
            out Intel8086SpecialArithmeticResult result);

        Assert.True(succeeded);
        Assert.Equal((ushort)0x0081, result.AX);
    }

    [Fact]
    public void TryDivideWord_SignedMinimumNegativeQuotient_ReportsDivideFault()
    {
        bool succeeded = Intel8086SpecialArithmetic.TryDivideWord(
            0x8000,
            0xFFFF,
            0x0001,
            signedOperation: true,
            out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryDivideWord_SignedLargestNegativeQuotient_ReturnsQuotientAndRemainder()
    {
        bool succeeded = Intel8086SpecialArithmetic.TryDivideWord(
            0x8001,
            0xFFFF,
            0x0001,
            signedOperation: true,
            out Intel8086DivisionResult result);

        Assert.True(succeeded);
        Assert.Equal((ushort)0x8001, result.AX);
        Assert.Equal((ushort)0x0000, result.DX);
    }
}
