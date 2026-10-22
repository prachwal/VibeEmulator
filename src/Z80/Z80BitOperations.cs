namespace VibeEmulator.Z80;

/// <summary>
/// Rotacje, przesunięcia i test bitu. Flagowanie porównano z pełnymi wektorami Z6
/// z przypiętego zestawu SingleStepTests/z80 (model Ares, nie test krzemowy).
/// </summary>
public static class Z80BitOperations
{
    /// <summary>Operacja 0–7: RLC, RRC, RL, RR, SLA, SRA, SLL, SRL.</summary>
    public static (byte Value, byte Flags) RotateShift(byte operation, byte value, byte flags)
    {
        int carryIn = flags & 1;
        int carry;
        byte result;
        switch (operation)
        {
            case 0: carry = value >> 7; result = (byte)((value << 1) | carry); break;
            case 1: carry = value & 1; result = (byte)((value >> 1) | (carry << 7)); break;
            case 2: carry = value >> 7; result = (byte)((value << 1) | carryIn); break;
            case 3: carry = value & 1; result = (byte)((value >> 1) | (carryIn << 7)); break;
            case 4: carry = value >> 7; result = (byte)(value << 1); break;
            case 5: carry = value & 1; result = (byte)((value >> 1) | (value & 0x80)); break;
            case 6: carry = value >> 7; result = (byte)((value << 1) | 1); break;
            case 7: carry = value & 1; result = (byte)(value >> 1); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }

        byte resultFlags = (byte)(SignZeroXY(result) | Parity(result) | carry);
        return (result, resultFlags);
    }

    /// <summary>BIT: XY pochodzi z xySource; zachowuje C, ustawia H, a Z i PV są zgodne.</summary>
    public static byte TestBit(byte bit, byte value, byte flags, byte xySource)
    {
        if (bit > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(bit));
        }

        bool set = (value & (1 << bit)) != 0;
        int result = (flags & 1) | 0x10 | (xySource & 0x28);
        if (!set)
        {
            result |= 0x44;
        }
        else if (bit == 7)
        {
            result |= 0x80;
        }

        return (byte)result;
    }

    /// <summary>RLCA, RRCA, RLA, RRA zachowują S/Z/PV.</summary>
    public static (byte Value, byte Flags) RotateAccumulator(byte operation, byte value, byte flags)
    {
        int carryIn = flags & 1;
        int carry;
        byte result;
        switch (operation)
        {
            case 0: carry = value >> 7; result = (byte)((value << 1) | carry); break;
            case 1: carry = value & 1; result = (byte)((value >> 1) | (carry << 7)); break;
            case 2: carry = value >> 7; result = (byte)((value << 1) | carryIn); break;
            case 3: carry = value & 1; result = (byte)((value >> 1) | (carryIn << 7)); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }

        int resultFlags = (flags & 0xC4) | (result & 0x28) | carry;
        return (result, (byte)resultFlags);
    }

    private static int SignZeroXY(byte value) =>
        (value & 0xA8) | (value == 0 ? 0x40 : 0);

    private static int Parity(byte value) =>
        (System.Numerics.BitOperations.PopCount((uint)value) & 1) == 0 ? 4 : 0;
}
