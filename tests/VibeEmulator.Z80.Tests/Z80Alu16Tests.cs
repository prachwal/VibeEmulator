using VibeEmulator.Z80;

namespace VibeEmulator.Z80.Tests;

public class Z80Alu16Tests
{
    // Reguły ADD HL,rr porównane z 4000 pełnych rekordów 09/19/29/39.
    [Theory]
    [InlineData(0x0FFF, 1, 0, 0x1000, 0x10)]
    [InlineData(0xFFFF, 1, 0, 0, 0x11)]
    [InlineData(0x7FFF, 1, 0xC4, 0x8000, 0xD4)]
    [InlineData(0x1400, 0x1400, 0, 0x2800, 0x28)]
    [InlineData(0xFFFF, 1, 0xFF, 0, 0xD5)]
    public void Add16_BoundaryValues_PreservesSignZeroParityAndComputesCarry(
        ushort left, ushort right, byte flags, ushort expectedValue, byte expectedFlags)
    {
        (ushort value, byte actualFlags) = Z80Alu.Add16(left, right, flags);

        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedFlags, actualFlags);
    }
}
