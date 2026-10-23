using System.Numerics;

namespace VibeEmulator.Intel8086;

public static partial class Intel8086Alu
{
    public static Intel8086AluResult ExecuteShiftRotate(byte value, ushort flags, int extension) =>
        ToByteResult(ExecuteShiftRotateCore(value, flags, extension, 8));

    public static Intel8086AluWordResult ExecuteShiftRotate(ushort value, ushort flags, int extension) =>
        ExecuteShiftRotateCore(value, flags, extension, 16);

    private static Intel8086AluResult ToByteResult(Intel8086AluWordResult result) =>
        new((byte)result.Value, result.Flags);

    private static Intel8086AluWordResult ExecuteShiftRotateCore(ushort value, ushort flags, int extension, int width)
    {
        int mask = (1 << width) - 1;
        int sign = 1 << (width - 1);
        int carryIn = flags & CarryFlag;
        int carryOut;
        int result;
        bool isShift = extension >= 4;

        switch (extension)
        {
            case 0:
                carryOut = value >> (width - 1);
                result = ((value << 1) | carryOut) & mask;
                break;
            case 1:
                carryOut = value & 1;
                result = (value >> 1) | (carryOut << (width - 1));
                break;
            case 2:
                carryOut = value >> (width - 1);
                result = ((value << 1) | carryIn) & mask;
                break;
            case 3:
                carryOut = value & 1;
                result = (value >> 1) | (carryIn << (width - 1));
                break;
            case 4:
            case 6:
                carryOut = value >> (width - 1);
                result = (value << 1) & mask;
                break;
            case 5:
                carryOut = value & 1;
                result = value >> 1;
                break;
            case 7:
                carryOut = value & 1;
                result = (value >> 1) | (value & sign);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(extension));
        }

        ushort changedFlags = (ushort)(flags & ~(CarryFlag | OverflowFlag | (isShift ? (ushort)(SignFlag | ZeroFlag | ParityFlag) : (ushort)0)));
        if (carryOut != 0)
        {
            changedFlags |= CarryFlag;
        }

        bool overflow = extension switch
        {
            0 or 2 or 4 or 6 => ((result & sign) != 0) != (carryOut != 0),
            1 or 3 => ((result & sign) != 0) != ((result & (sign >> 1)) != 0),
            5 => (value & sign) != 0,
            7 => false,
            _ => false,
        };
        if (overflow)
        {
            changedFlags |= OverflowFlag;
        }

        if (isShift)
        {
            if (result == 0)
            {
                changedFlags |= ZeroFlag;
            }

            if ((result & sign) != 0)
            {
                changedFlags |= SignFlag;
            }

            if ((BitOperations.PopCount((uint)(result & 0x00FF)) & 1) == 0)
            {
                changedFlags |= ParityFlag;
            }
        }

        return new Intel8086AluWordResult((ushort)result, changedFlags);
    }
}
