namespace VibeEmulator.Core.Cpu;

/// <summary>Rejestr statusu — bity zgodne z układem 6502.</summary>
[Flags]
public enum StatusFlags : byte
{
    None = 0,
    Carry = 0b0000_0001,
    Zero = 0b0000_0010,
    InterruptDisable = 0b0000_0100,
    Decimal = 0b0000_1000,
    Break = 0b0001_0000,
    Unused = 0b0010_0000,
    Overflow = 0b0100_0000,
    Negative = 0b1000_0000,
}

public static class StatusFlagsExtensions
{
    public static bool Has(this StatusFlags flags, StatusFlags bit) => (flags & bit) == bit;

    public static StatusFlags With(this StatusFlags flags, StatusFlags bit, bool value) =>
        value ? flags | bit : flags & ~bit;
}
