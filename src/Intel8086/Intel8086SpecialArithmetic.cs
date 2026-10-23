using System.Numerics;

namespace VibeEmulator.Intel8086;

public readonly record struct Intel8086SpecialArithmeticResult(ushort AX, ushort Flags);

/// <summary>Konwersje i operacje AAM/AAD zweryfikowane próbkami SingleStepTests.</summary>
public static class Intel8086SpecialArithmetic
{
    private const ushort Carry = 0x0001;
    private const ushort AuxiliaryCarry = 0x0010;
    private const ushort Parity = 0x0004;
    private const ushort Zero = 0x0040;
    private const ushort Sign = 0x0080;
    private const ushort StatusFlags = Parity | Zero | Sign;

    public static ushort Cbw(ushort ax) => (ushort)((ax & 0x00FF) | ((ax & 0x0080) == 0 ? 0 : 0xFF00));

    public static ushort Cwd(ushort ax) => (ax & 0x8000) == 0 ? (ushort)0 : ushort.MaxValue;

    public static Intel8086SpecialArithmeticResult Aam(ushort ax, byte radix, ushort flags)
    {
        if (radix == 0)
        {
            throw new DivideByZeroException("AAM with radix 0 raises divide error on the 8086.");
        }

        byte al = (byte)ax;
        byte quotient = (byte)(al / radix);
        byte remainder = (byte)(al % radix);
        ushort result = (ushort)((quotient << 8) | remainder);
        return new Intel8086SpecialArithmeticResult(result, UpdateStatusFlags(flags, remainder));
    }

    public static Intel8086SpecialArithmeticResult Aad(ushort ax, byte radix, ushort flags)
    {
        byte al = (byte)ax;
        byte ah = (byte)(ax >> 8);
        byte result = (byte)((ah * radix) + al);
        return new Intel8086SpecialArithmeticResult(result, UpdateStatusFlags(flags, result));
    }

    public static Intel8086SpecialArithmeticResult Daa(ushort ax, ushort flags) => AdjustPackedBcd(ax, flags, subtract: false);

    public static Intel8086SpecialArithmeticResult Das(ushort ax, ushort flags) => AdjustPackedBcd(ax, flags, subtract: true);

    public static Intel8086SpecialArithmeticResult Aaa(ushort ax, ushort flags) => AdjustAscii(ax, flags, subtract: false);

    public static Intel8086SpecialArithmeticResult Aas(ushort ax, ushort flags) => AdjustAscii(ax, flags, subtract: true);

    private static Intel8086SpecialArithmeticResult AdjustPackedBcd(ushort ax, ushort flags, bool subtract)
    {
        byte oldAl = (byte)ax;
        byte al = oldAl;
        bool oldCarry = (flags & Carry) != 0;
        bool adjustLow = (oldAl & 0x0F) > 9 || (flags & AuxiliaryCarry) != 0;
        bool adjustHigh = oldAl > 0x99 || oldCarry;

        if (adjustLow)
        {
            al = unchecked((byte)(al + (subtract ? -6 : 6)));
        }

        if (adjustHigh)
        {
            al = unchecked((byte)(al + (subtract ? -0x60 : 0x60)));
        }

        ushort resultFlags = (ushort)(flags & ~(Carry | AuxiliaryCarry));
        if (adjustHigh)
        {
            resultFlags |= Carry;
        }

        if (adjustLow)
        {
            resultFlags |= AuxiliaryCarry;
        }

        resultFlags = UpdateStatusFlags(resultFlags, al);
        return new Intel8086SpecialArithmeticResult((ushort)((ax & 0xFF00) | al), resultFlags);
    }

    private static Intel8086SpecialArithmeticResult AdjustAscii(ushort ax, ushort flags, bool subtract)
    {
        byte al = (byte)ax;
        byte ah = (byte)(ax >> 8);
        bool adjust = (al & 0x0F) > 9 || (flags & AuxiliaryCarry) != 0;
        ushort resultFlags = (ushort)(flags & ~(Carry | AuxiliaryCarry));
        if (adjust)
        {
            ah = unchecked((byte)(ah + (subtract ? -1 : 1)));
            al = unchecked((byte)(al + (subtract ? -6 : 6)));
            resultFlags |= Carry | AuxiliaryCarry;
        }

        al &= 0x0F;
        return new Intel8086SpecialArithmeticResult((ushort)((ah << 8) | al), resultFlags);
    }

    private static ushort UpdateStatusFlags(ushort flags, byte value)
    {
        ushort result = (ushort)(flags & ~StatusFlags);
        if (value == 0)
        {
            result |= Zero;
        }

        if ((value & 0x80) != 0)
        {
            result |= Sign;
        }

        if ((BitOperations.PopCount((uint)value) & 1) == 0)
        {
            result |= Parity;
        }

        return result;
    }
}
