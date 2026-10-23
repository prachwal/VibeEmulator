using System.Numerics;

namespace VibeEmulator.Intel8086;

public enum Intel8086AluOperation
{
    Add,
    Adc,
    Sub,
    Sbb,
    Compare,
    And,
    Or,
    Xor,
    Test,
}

public readonly record struct Intel8086AluResult(byte Value, ushort Flags);

public readonly record struct Intel8086AluWordResult(ushort Value, ushort Flags);

/// <summary>Ośmiobitowe funkcje ALU; kody operacji wynikają z wektorów A4.</summary>
public static partial class Intel8086Alu
{
    private const ushort CarryFlag = 0x0001;
    private const ushort ParityFlag = 0x0004;
    private const ushort AuxiliaryCarryFlag = 0x0010;
    private const ushort ZeroFlag = 0x0040;
    private const ushort SignFlag = 0x0080;
    private const ushort OverflowFlag = 0x0800;
    private const ushort ArithmeticFlags = CarryFlag | ParityFlag | AuxiliaryCarryFlag | ZeroFlag | SignFlag | OverflowFlag;

    public static Intel8086AluResult Execute(byte left, byte right, ushort flags, Intel8086AluOperation operation)
    {
        int carryIn = operation is Intel8086AluOperation.Adc or Intel8086AluOperation.Sbb
            ? flags & CarryFlag
            : 0;
        int result;
        ushort changedFlags = (ushort)(flags & ~ArithmeticFlags);

        switch (operation)
        {
            case Intel8086AluOperation.Add:
            case Intel8086AluOperation.Adc:
                result = left + right + carryIn;
                if (result > byte.MaxValue)
                {
                    changedFlags |= CarryFlag;
                }

                if (((left & 0x0F) + (right & 0x0F) + carryIn) > 0x0F)
                {
                    changedFlags |= AuxiliaryCarryFlag;
                }

                if (((~(left ^ right) & (left ^ result)) & 0x80) != 0)
                {
                    changedFlags |= OverflowFlag;
                }

                break;
            case Intel8086AluOperation.Sub:
            case Intel8086AluOperation.Sbb:
            case Intel8086AluOperation.Compare:
                result = left - right - carryIn;
                if (left < right + carryIn)
                {
                    changedFlags |= CarryFlag;
                }

                if ((left & 0x0F) < ((right & 0x0F) + carryIn))
                {
                    changedFlags |= AuxiliaryCarryFlag;
                }

                if ((((left ^ right) & (left ^ result)) & 0x80) != 0)
                {
                    changedFlags |= OverflowFlag;
                }

                break;
            case Intel8086AluOperation.And:
            case Intel8086AluOperation.Test:
                result = left & right;
                break;
            case Intel8086AluOperation.Or:
                result = left | right;
                break;
            case Intel8086AluOperation.Xor:
                result = left ^ right;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        byte value = (byte)result;
        if (value == 0)
        {
            changedFlags |= ZeroFlag;
        }

        if ((value & 0x80) != 0)
        {
            changedFlags |= SignFlag;
        }

        if ((BitOperations.PopCount((uint)value) & 1) == 0)
        {
            changedFlags |= ParityFlag;
        }

        return new Intel8086AluResult(value, changedFlags);
    }

    public static Intel8086AluWordResult Execute(ushort left, ushort right, ushort flags, Intel8086AluOperation operation)
    {
        int carryIn = operation is Intel8086AluOperation.Adc or Intel8086AluOperation.Sbb
            ? flags & CarryFlag
            : 0;
        int result;
        ushort changedFlags = (ushort)(flags & ~ArithmeticFlags);

        switch (operation)
        {
            case Intel8086AluOperation.Add:
            case Intel8086AluOperation.Adc:
                result = left + right + carryIn;
                if (result > ushort.MaxValue)
                {
                    changedFlags |= CarryFlag;
                }

                if (((left & 0x0F) + (right & 0x0F) + carryIn) > 0x0F)
                {
                    changedFlags |= AuxiliaryCarryFlag;
                }

                if (((~(left ^ right) & (left ^ result)) & 0x8000) != 0)
                {
                    changedFlags |= OverflowFlag;
                }

                break;
            case Intel8086AluOperation.Sub:
            case Intel8086AluOperation.Sbb:
            case Intel8086AluOperation.Compare:
                result = left - right - carryIn;
                if (left < right + carryIn)
                {
                    changedFlags |= CarryFlag;
                }

                if ((left & 0x0F) < ((right & 0x0F) + carryIn))
                {
                    changedFlags |= AuxiliaryCarryFlag;
                }

                if ((((left ^ right) & (left ^ result)) & 0x8000) != 0)
                {
                    changedFlags |= OverflowFlag;
                }

                break;
            case Intel8086AluOperation.And:
            case Intel8086AluOperation.Test:
                result = left & right;
                break;
            case Intel8086AluOperation.Or:
                result = left | right;
                break;
            case Intel8086AluOperation.Xor:
                result = left ^ right;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        ushort value = (ushort)result;
        if (value == 0)
        {
            changedFlags |= ZeroFlag;
        }

        if ((value & 0x8000) != 0)
        {
            changedFlags |= SignFlag;
        }

        if ((BitOperations.PopCount((uint)(value & 0x00FF)) & 1) == 0)
        {
            changedFlags |= ParityFlag;
        }

        return new Intel8086AluWordResult(value, changedFlags);
    }

    public static Intel8086AluWordResult ExecuteIncrementDecrement(ushort value, ushort flags, bool decrement)
    {
        const ushort carryFlag = 0x0001;
        Intel8086AluWordResult arithmetic = Execute(
            value,
            1,
            flags,
            decrement ? Intel8086AluOperation.Sub : Intel8086AluOperation.Add);
        ushort resultFlags = (ushort)((arithmetic.Flags & ~carryFlag) | (flags & carryFlag));
        return new Intel8086AluWordResult(arithmetic.Value, resultFlags);
    }

    public static Intel8086AluResult ExecuteIncrementDecrement(byte value, ushort flags, bool decrement)
    {
        const ushort carryFlag = 0x0001;
        Intel8086AluResult arithmetic = Execute(
            value,
            1,
            flags,
            decrement ? Intel8086AluOperation.Sub : Intel8086AluOperation.Add);
        ushort resultFlags = (ushort)((arithmetic.Flags & ~carryFlag) | (flags & carryFlag));
        return new Intel8086AluResult(arithmetic.Value, resultFlags);
    }

    public static Intel8086AluResult ExecuteNot(byte value, ushort flags) => new((byte)~value, flags);

    public static Intel8086AluResult ExecuteNegate(byte value, ushort flags) =>
        Execute(0, value, flags, Intel8086AluOperation.Sub);

    public static Intel8086AluWordResult ExecuteNot(ushort value, ushort flags) => new((ushort)~value, flags);

    public static Intel8086AluWordResult ExecuteNegate(ushort value, ushort flags) =>
        Execute(0, value, flags, Intel8086AluOperation.Sub);

}
