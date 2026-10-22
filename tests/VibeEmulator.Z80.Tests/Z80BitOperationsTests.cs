using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80BitOperationsTests
{
    [Theory]
    [InlineData(0, 0x81, 0, 0x03, 0x05)]
    [InlineData(1, 0x01, 0, 0x80, 0x81)]
    [InlineData(2, 0x80, 1, 0x01, 0x01)]
    [InlineData(3, 0x01, 1, 0x80, 0x81)]
    [InlineData(4, 0x80, 0, 0x00, 0x45)]
    [InlineData(5, 0x81, 0, 0xC0, 0x85)]
    [InlineData(6, 0x80, 0, 0x01, 0x01)]
    [InlineData(7, 0x01, 0, 0x00, 0x45)]
    public void RotateShift_BoundaryValues_ReturnsResultAndFlags(
        byte operation, byte value, byte flags, byte expectedValue, byte expectedFlags)
    {
        Assert.Equal((expectedValue, expectedFlags), Z80BitOperations.RotateShift(operation, value, flags));
    }

    [Theory]
    [InlineData(0, 0x01, 0, 0x10)]
    [InlineData(0, 0x00, 1, 0x55)]
    [InlineData(7, 0x80, 1, 0x91)]
    [InlineData(7, 0x00, 0x28, 0x54)]
    public void TestBit_SetsSignZeroParityHalfCarryAndXY(byte bit, byte value, byte flags, byte expected)
    {
        Assert.Equal(expected, Z80BitOperations.TestBit(bit, value, flags, value));
    }

    [Theory]
    [InlineData(0, 0x81, 0xC4, 0x03, 0xC5)]
    [InlineData(1, 0x01, 0x00, 0x80, 0x01)]
    [InlineData(2, 0x80, 0x01, 0x01, 0x01)]
    [InlineData(3, 0x01, 0x01, 0x80, 0x01)]
    public void RotateAccumulator_PreservesSzPv(byte operation, byte value, byte flags, byte expectedValue, byte expectedFlags)
    {
        Assert.Equal((expectedValue, expectedFlags), Z80BitOperations.RotateAccumulator(operation, value, flags));
    }

    [Fact]
    public void TestBit_InvalidBit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Z80BitOperations.TestBit(8, 0, 0, 0));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(byte.MaxValue)]
    public void RotateShift_InvalidOperation_Throws(byte operation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Z80BitOperations.RotateShift(operation, 0, 0));
    }
}
