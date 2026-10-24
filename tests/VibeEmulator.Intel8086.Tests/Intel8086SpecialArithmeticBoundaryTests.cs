using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086SpecialArithmeticBoundaryTests
{
    [Fact]
    public void CbwAndCwd_SignBoundariesExtendIntoHighRegister()
    {
        Assert.Equal((ushort)0xFF80, Intel8086SpecialArithmetic.Cbw(0xAB80));
        Assert.Equal((ushort)0x007F, Intel8086SpecialArithmetic.Cbw(0xFF7F));
        Assert.Equal((ushort)0xFFFF, Intel8086SpecialArithmetic.Cwd(0x8000));
        Assert.Equal((ushort)0x0000, Intel8086SpecialArithmetic.Cwd(0x7FFF));
    }

    [Fact]
    public void MultiplyByte_ExtremeUnsignedAndSignedProductsSetCarryAndOverflow()
    {
        Intel8086SpecialArithmeticResult unsigned = Intel8086SpecialArithmetic.MultiplyByte(0x00FF, 0xFF, 0, signedOperation: false);
        Intel8086SpecialArithmeticResult signedInRange = Intel8086SpecialArithmetic.MultiplyByte(0x0080, 0x01, 0, signedOperation: true);
        Intel8086SpecialArithmeticResult signedOverflow = Intel8086SpecialArithmetic.MultiplyByte(0x0080, 0xFF, 0, signedOperation: true);

        Assert.Equal((ushort)0xFE01, unsigned.AX);
        Assert.Equal((ushort)0x0801, (ushort)(unsigned.Flags & 0x0801));
        Assert.Equal((ushort)0xFF80, signedInRange.AX);
        Assert.Equal((ushort)0x0000, (ushort)(signedInRange.Flags & 0x0801));
        Assert.Equal((ushort)0x0080, signedOverflow.AX);
        Assert.Equal((ushort)0x0801, (ushort)(signedOverflow.Flags & 0x0801));
    }

    [Fact]
    public void MultiplyWord_ExtremeUnsignedAndSignedProductsSetCarryAndOverflow()
    {
        Intel8086WordMultiplyResult unsigned = Intel8086SpecialArithmetic.MultiplyWord(0xFFFF, 0xFFFF, 0, signedOperation: false);
        Intel8086WordMultiplyResult signedInRange = Intel8086SpecialArithmetic.MultiplyWord(0x8000, 0x0001, 0, signedOperation: true);
        Intel8086WordMultiplyResult signedOverflow = Intel8086SpecialArithmetic.MultiplyWord(0x8000, 0xFFFF, 0, signedOperation: true);

        Assert.Equal((ushort)0x0001, unsigned.AX);
        Assert.Equal((ushort)0xFFFE, unsigned.DX);
        Assert.Equal((ushort)0x0801, (ushort)(unsigned.Flags & 0x0801));
        Assert.Equal((ushort)0x8000, signedInRange.AX);
        Assert.Equal((ushort)0xFFFF, signedInRange.DX);
        Assert.Equal((ushort)0x0000, (ushort)(signedInRange.Flags & 0x0801));
        Assert.Equal((ushort)0x8000, signedOverflow.AX);
        Assert.Equal((ushort)0x0000, signedOverflow.DX);
        Assert.Equal((ushort)0x0801, (ushort)(signedOverflow.Flags & 0x0801));
    }

    [Fact]
    public void UnsignedDivide_ZeroAndQuotientWidthBoundariesAreClassified()
    {
        Assert.False(Intel8086SpecialArithmetic.TryDivideByte(0x0001, 0, signedOperation: false, out _));
        Assert.True(Intel8086SpecialArithmetic.TryDivideByte(0x00FF, 1, signedOperation: false, out Intel8086SpecialArithmeticResult byteMax));
        Assert.Equal((ushort)0x00FF, byteMax.AX);
        Assert.False(Intel8086SpecialArithmetic.TryDivideByte(0x0100, 1, signedOperation: false, out _));

        Assert.False(Intel8086SpecialArithmetic.TryDivideWord(0x0001, 0, 0, signedOperation: false, out _));
        Assert.True(Intel8086SpecialArithmetic.TryDivideWord(0xFFFF, 0, 1, signedOperation: false, out Intel8086DivisionResult wordMax));
        Assert.Equal((ushort)0xFFFF, wordMax.AX);
        Assert.False(Intel8086SpecialArithmetic.TryDivideWord(0x0000, 1, 1, signedOperation: false, out _));
    }

    [Fact]
    public void AsciiAdjust_ExtremeNibbleCarryWrapsAccumulator()
    {
        Intel8086SpecialArithmeticResult add = Intel8086SpecialArithmetic.Aaa(0xFF0A, 0);
        Intel8086SpecialArithmeticResult subtract = Intel8086SpecialArithmetic.Aas(0x000A, 0);

        Assert.Equal((ushort)0x0000, add.AX);
        Assert.Equal((ushort)0x0011, (ushort)(add.Flags & 0x0011));
        Assert.Equal((ushort)0xFF04, subtract.AX);
        Assert.Equal((ushort)0x0011, (ushort)(subtract.Flags & 0x0011));
    }

    [Fact]
    public void PackedDecimalAdjust_NibbleAndCarryBoundariesAreApplied()
    {
        Intel8086SpecialArithmeticResult addLow = Intel8086SpecialArithmetic.Daa(0x000A, 0);
        Intel8086SpecialArithmeticResult addHigh = Intel8086SpecialArithmetic.Daa(0x009A, 0);
        Intel8086SpecialArithmeticResult subtractLow = Intel8086SpecialArithmetic.Das(0x000A, 0);
        Intel8086SpecialArithmeticResult subtractHigh = Intel8086SpecialArithmetic.Das(0x009A, 0);

        Assert.Equal((ushort)0x0010, addLow.AX);
        Assert.Equal((ushort)0x0010, (ushort)(addLow.Flags & 0x0011));
        Assert.Equal((ushort)0x0000, addHigh.AX);
        Assert.Equal((ushort)0x0011, (ushort)(addHigh.Flags & 0x0011));
        Assert.Equal((ushort)0x0004, subtractLow.AX);
        Assert.Equal((ushort)0x0010, (ushort)(subtractLow.Flags & 0x0011));
        Assert.Equal((ushort)0x0034, subtractHigh.AX);
        Assert.Equal((ushort)0x0011, (ushort)(subtractHigh.Flags & 0x0011));
    }

    [Fact]
    public void AsciiMultiplyAndDivideAdjust_RadixBoundariesProduceByteResults()
    {
        Intel8086SpecialArithmeticResult multiplyBaseTen = Intel8086SpecialArithmetic.Aam(0x00FF, 10, 0);
        Intel8086SpecialArithmeticResult multiplyBaseOne = Intel8086SpecialArithmetic.Aam(0x00FF, 1, 0);
        Intel8086SpecialArithmeticResult divideBaseTen = Intel8086SpecialArithmetic.Aad(0x0102, 10, 0);
        Intel8086SpecialArithmeticResult divideBaseZero = Intel8086SpecialArithmetic.Aad(0x0102, 0, 0);

        Assert.Equal((ushort)0x1905, multiplyBaseTen.AX);
        Assert.Equal((ushort)0xFF00, multiplyBaseOne.AX);
        Assert.Equal((ushort)0x000C, divideBaseTen.AX);
        Assert.Equal((ushort)0x0002, divideBaseZero.AX);
        Assert.Throws<DivideByZeroException>(() => Intel8086SpecialArithmetic.Aam(0x00FF, 0, 0));
    }
}
