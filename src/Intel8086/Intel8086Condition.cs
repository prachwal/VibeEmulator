namespace VibeEmulator.Intel8086;

/// <summary>Ocena czterobitowego warunku Jcc dla flag 8086.</summary>
public static class Intel8086Condition
{
    private const ushort Carry = 0x0001;
    private const ushort Parity = 0x0004;
    private const ushort Zero = 0x0040;
    private const ushort Sign = 0x0080;
    private const ushort Overflow = 0x0800;

    public static bool IsSatisfied(byte condition, ushort flags)
    {
        bool carry = (flags & Carry) != 0;
        bool parity = (flags & Parity) != 0;
        bool zero = (flags & Zero) != 0;
        bool sign = (flags & Sign) != 0;
        bool overflow = (flags & Overflow) != 0;

        return (condition & 0x0F) switch
        {
            0x0 => overflow,
            0x1 => !overflow,
            0x2 => carry,
            0x3 => !carry,
            0x4 => zero,
            0x5 => !zero,
            0x6 => carry || zero,
            0x7 => !carry && !zero,
            0x8 => sign,
            0x9 => !sign,
            0xA => parity,
            0xB => !parity,
            0xC => sign != overflow,
            0xD => sign == overflow,
            0xE => zero || sign != overflow,
            0xF => !zero && sign == overflow,
            _ => throw new ArgumentOutOfRangeException(nameof(condition)),
        };
    }
}
