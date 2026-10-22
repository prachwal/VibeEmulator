namespace VibeEmulator.Z80;

/// <summary>
/// Czyste operacje ALU. Flagi, w tym X/Y i reguły DAA/SCF/CCF, porównano z 92 000
/// rekordów SingleStepTests/z80, commit ebe1875d48f374bcfd4b505d8eb8ee751568b5f7.
/// Źródło jest modelem Ares, nie pomiarem krzemu.
/// </summary>
public static class Z80Alu
{
    /// <summary>Grupa 0–7: ADD, ADC, SUB, SBC, AND, XOR, OR, CP.</summary>
    public static (byte Value, byte Flags) Execute(int operation, byte a, byte operand, byte flags)
    {
        if (operation is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        if (operation < 4 || operation == 7)
        {
            bool subtract = operation is 2 or 3 or 7;
            int carry = operation is 1 or 3 ? flags & 1 : 0;
            int sum = subtract ? a - operand - carry : a + operand + carry;
            byte result = (byte)sum;
            int overflow = subtract
                ? (a ^ operand) & (a ^ result) & 0x80
                : ~(a ^ operand) & (a ^ result) & 0x80;
            int f = SignZeroXY(result)
                | (subtract ? 2 : 0)
                | ((a ^ operand ^ result) & 0x10)
                | (sum is < 0 or > 255 ? 1 : 0)
                | (overflow != 0 ? 4 : 0);
            if (operation == 7)
            {
                f = (f & ~0x28) | (operand & 0x28);
                result = a;
            }

            return (result, (byte)f);
        }

        byte value = operation switch
        {
            4 => (byte)(a & operand),
            5 => (byte)(a ^ operand),
            _ => (byte)(a | operand),
        };
        return (value, (byte)(SignZeroXY(value) | Parity(value) | (operation == 4 ? 0x10 : 0)));
    }

    public static (byte Value, byte Flags) Increment(byte value, byte flags, bool decrement)
    {
        byte result = (byte)(value + (decrement ? -1 : 1));
        int f = SignZeroXY(result) | (flags & 1) | (decrement ? 2 : 0)
            | ((value ^ result) & 0x10)
            | (value == (decrement ? 0x80 : 0x7F) ? 4 : 0);
        return (result, (byte)f);
    }

    public static (byte Value, byte Flags) DecimalAdjust(byte a, byte flags)
    {
        int correction = ((flags & 0x10) != 0 || (a & 15) > 9 ? 6 : 0)
            | ((flags & 1) != 0 || a > 0x99 ? 0x60 : 0);
        byte result = (byte)((flags & 2) != 0 ? a - correction : a + correction);
        int f = SignZeroXY(result) | Parity(result) | (flags & 2)
            | ((flags & 1) != 0 || a > 0x99 ? 1 : 0) | ((a ^ result) & 0x10);
        return (result, (byte)f);
    }

    public static (byte Value, byte Flags) Complement(byte a, byte flags)
    {
        byte result = (byte)~a;
        return (result, (byte)((flags & 0xC5) | 0x12 | (result & 0x28)));
    }

    public static byte CarryFlags(byte a, byte flags, byte previousQ, bool complement)
    {
        int xy = (a | (previousQ == 0 ? flags : 0)) & 0x28;
        int carry = complement ? ((flags & 1) << 4) | ((flags & 1) ^ 1) : 1;
        return (byte)((flags & 0xC4) | xy | carry);
    }

    private static int SignZeroXY(byte value) => (value & 0xA8) | (value == 0 ? 0x40 : 0);

    private static int Parity(byte value) =>
        (System.Numerics.BitOperations.PopCount((uint)value) & 1) == 0 ? 4 : 0;
}
