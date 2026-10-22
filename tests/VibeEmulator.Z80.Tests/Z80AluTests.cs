using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80AluTests
{
    // Przypadki graniczne reguł porównanych z pełnymi wektorami Z3 (pin w manifeście).
    [Theory]
    [InlineData(0, 0x0F, 1, 0, 0x10, 0x10)]
    [InlineData(0, 0x7F, 1, 0, 0x80, 0x94)]
    [InlineData(0, 0xFF, 1, 0, 0, 0x51)]
    [InlineData(1, 0x7F, 0, 1, 0x80, 0x94)]
    [InlineData(2, 0x10, 1, 0, 0x0F, 0x1A)]
    [InlineData(2, 0x80, 1, 0, 0x7F, 0x3E)]
    [InlineData(3, 0, 0, 1, 0xFF, 0xBB)]
    [InlineData(7, 0, 0x28, 0, 0, 0xBB)]
    [InlineData(4, 0x80, 0xFF, 0, 0x80, 0x90)]
    [InlineData(5, 0xFF, 0xFF, 0, 0, 0x44)]
    [InlineData(6, 0, 3, 0, 3, 4)]
    public void Execute_BoundaryValues_ReturnsResultAndAllFlags(
        int operation, byte a, byte operand, byte flags, byte expectedValue, byte expectedFlags)
    {
        (byte value, byte actualFlags) = Z80Alu.Execute(operation, a, operand, flags);

        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedFlags, actualFlags);
    }

    [Theory]
    [InlineData(0x7F, 1, false, 0x80, 0x95)]
    [InlineData(0xFF, 0, false, 0, 0x50)]
    [InlineData(0x80, 1, true, 0x7F, 0x3F)]
    [InlineData(0, 0, true, 0xFF, 0xBA)]
    public void Increment_BoundaryValues_PreservesCarry(
        byte input, byte flags, bool decrement, byte expectedValue, byte expectedFlags)
    {
        (byte value, byte actualFlags) = Z80Alu.Increment(input, flags, decrement);

        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedFlags, actualFlags);
    }

    // 27 0000: odejmowanie z niekanonicznym wejściem BCD w modelu referencyjnym.
    [Theory]
    [InlineData(243, 78, 147, 135)]
    public void DecimalAdjust_ReferenceCase_ReturnsResultAndFlags(
        byte a, byte flags, byte expectedValue, byte expectedFlags)
    {
        Assert.Equal((expectedValue, expectedFlags), Z80Alu.DecimalAdjust(a, flags));
    }

    [Theory]
    [InlineData(0, 0x28, 0, false, 0x29)]
    [InlineData(0, 0x28, 0x28, false, 1)]
    [InlineData(0, 0x29, 0, true, 0x38)]
    [InlineData(0, 0x29, 0x29, true, 0x10)]
    public void CarryFlags_PreviousQ_SelectsUndocumentedFlags(
        byte a, byte flags, byte q, bool complement, byte expectedFlags)
    {
        Assert.Equal(expectedFlags, Z80Alu.CarryFlags(a, flags, q, complement));
    }

    [Fact]
    public void Complement_ReferenceCase_PreservesSignZeroParityCarry()
    {
        Assert.Equal(((byte)0xFF, (byte)0xFF), Z80Alu.Complement(0, 0xC5));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void Execute_InvalidOperation_Throws(int operation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Z80Alu.Execute(operation, 0, 0, 0));
    }
}
