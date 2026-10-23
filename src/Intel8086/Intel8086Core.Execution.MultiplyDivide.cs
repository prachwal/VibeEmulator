using System.Numerics;

namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsRegisterDirectMultiplyOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) is 4 or 5;

    private static bool IsRegisterDirectUnsignedDivideOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) == 6;

    private static bool IsRegisterDirectSignedDivideOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) == 7;

    private static bool IsRegisterDirectDivideOpcode(byte opcode, byte modRm) =>
        IsRegisterDirectUnsignedDivideOpcode(opcode, modRm) || IsRegisterDirectSignedDivideOpcode(opcode, modRm);

    private int GetRegisterDirectUnsignedDivideCycles(byte opcode, byte modRm)
    {
        bool byteOperation = opcode == 0xF6;
        int register = modRm & 7;
        ushort divisor = byteOperation ? GetByteRegister(register) : GetWordRegister(register);
        uint dividend = byteOperation
            ? _state.AX
            : ((uint)_state.DX << 16) | _state.AX;
        bool succeeded;
        uint quotient;
        if (byteOperation)
        {
            succeeded = Intel8086SpecialArithmetic.TryDivideByte(
                _state.AX,
                (byte)divisor,
                signedOperation: false,
                out Intel8086SpecialArithmeticResult byteResult);
            quotient = (byte)byteResult.AX;
        }
        else
        {
            succeeded = Intel8086SpecialArithmetic.TryDivideWord(
                _state.AX,
                _state.DX,
                divisor,
                signedOperation: false,
                out Intel8086DivisionResult wordResult);
            quotient = wordResult.AX;
        }

        if (!succeeded)
        {
            InitializeDivideError();
            return _instructionCycles;
        }

        int width = byteOperation ? 8 : 16;
        uint mask = (1u << width) - 1;
        uint upper = (dividend >> width) & mask;
        uint lower = dividend & mask;
        int upperCarryCount = 0;
        for (int index = 0; index < width; index++)
        {
            bool upperCarry = (upper & (1u << (width - 1))) != 0;
            upper = ((upper << 1) | (lower >> (width - 1))) & mask;
            lower = (lower << 1) & mask;
            if (upperCarry)
            {
                upperCarryCount++;
                upper = unchecked(upper - divisor) & mask;
                lower |= 1;
            }
            else if (upper >= divisor)
            {
                upper -= divisor;
                lower |= 1;
            }
        }

        return (byteOperation ? 80 : 144) +
            BitOperations.PopCount(quotient) - upperCarryCount +
            ((quotient & 1) != 0 ? 2 : 0);
    }

    private int GetRegisterDirectSignedDivideCycles(byte opcode, byte modRm)
    {
        bool byteOperation = opcode == 0xF6;
        int register = modRm & 7;
        int width = byteOperation ? 8 : 16;
        int signedDivisor = byteOperation
            ? (sbyte)GetByteRegister(register)
            : (short)GetWordRegister(register);
        long signedDividend = byteOperation
            ? (short)_state.AX
            : unchecked((int)(((uint)_state.DX << 16) | _state.AX));
        bool succeeded = byteOperation
            ? Intel8086SpecialArithmetic.TryDivideByte(
                _state.AX,
                unchecked((byte)signedDivisor),
                signedOperation: true,
                out _)
            : Intel8086SpecialArithmetic.TryDivideWord(
                _state.AX,
                _state.DX,
                unchecked((ushort)signedDivisor),
                signedOperation: true,
                out _);

        ulong dividendMagnitude = (ulong)Math.Abs(signedDividend);
        uint divisorMagnitude = (uint)Math.Abs(signedDivisor);
        ulong quotientMagnitude = signedDivisor == 0 ? 0 : dividendMagnitude / divisorMagnitude;
        int cycles = GetCordDivideCycles(width, (uint)dividendMagnitude, divisorMagnitude, (uint)quotientMagnitude) +
            GetSignedDivideOverhead(signedDividend < 0, signedDivisor < 0);

        if (!succeeded)
        {
            int exceptionStart = signedDivisor == 0 || quotientMagnitude > ((1u << width) - 1)
                ? 30 + (signedDividend < 0 ? 4 : 0) - (signedDivisor < 0 ? 1 : 0)
                : cycles;
            InitializeDivideError(exceptionStart);
            return _instructionCycles;
        }

        return cycles;
    }

    private static int GetCordDivideCycles(int width, uint dividend, uint divisor, uint quotient)
    {
        uint mask = (1u << width) - 1;
        uint upper = (dividend >> width) & mask;
        uint lower = dividend & mask;
        int upperCarryCount = 0;
        for (int index = 0; index < width; index++)
        {
            bool upperCarry = (upper & (1u << (width - 1))) != 0;
            upper = ((upper << 1) | (lower >> (width - 1))) & mask;
            lower = (lower << 1) & mask;
            if (upperCarry)
            {
                upperCarryCount++;
                upper = unchecked(upper - divisor) & mask;
                lower |= 1;
            }
            else if (upper >= divisor)
            {
                upper -= divisor;
                lower |= 1;
            }
        }

        return (width == 8 ? 80 : 144) +
            BitOperations.PopCount(quotient) - upperCarryCount +
            ((quotient & 1) != 0 ? 2 : 0);
    }

    private static int GetSignedDivideOverhead(bool dividendNegative, bool divisorNegative) =>
        (dividendNegative, divisorNegative) switch
        {
            (false, false) => 21,
            (false, true) => 20,
            (true, false) => 25,
            (true, true) => 24,
        };

    private int GetRegisterDirectMultiplyCycles(byte opcode, byte modRm)
    {
        int register = modRm & 7;
        if (opcode == 0xF6)
        {
            byte accumulator = (byte)_state.AX;
            byte operand = GetByteRegister(register);
            bool signedOperation = ((_memoryModRm >> 3) & 7) == 5;
            int multiplier = signedOperation ? Math.Abs((int)(sbyte)accumulator) : accumulator;
            int cycles = Math.Max(70, 69 + BitOperations.PopCount((uint)multiplier));
            if (!signedOperation && operand == 0 && accumulator != 0)
            {
                cycles++;
            }

            if (!signedOperation)
            {
                return cycles;
            }

            int signedProduct = (sbyte)accumulator * (sbyte)operand;
            bool fitsByte = signedProduct is >= sbyte.MinValue and <= sbyte.MaxValue;
            return cycles + GetSignedMultiplyOverhead((sbyte)accumulator < 0, (sbyte)operand < 0) + (fitsByte ? 1 : 0);
        }

        ushort wordAccumulator = _state.AX;
        ushort wordOperand = GetWordRegister(register);
        bool signedWordOperation = ((_memoryModRm >> 3) & 7) == 5;
        int wordMultiplier = signedWordOperation ? Math.Abs((int)(short)wordAccumulator) : wordAccumulator;
        int wordCycles = Math.Max(118, 117 + BitOperations.PopCount((uint)wordMultiplier));
        if (!signedWordOperation && wordOperand == 0 && wordAccumulator != 0)
        {
            wordCycles++;
        }

        if (!signedWordOperation)
        {
            return wordCycles;
        }

        long signedWordProduct = (short)wordAccumulator * (long)(short)wordOperand;
        bool fitsWord = signedWordProduct is >= short.MinValue and <= short.MaxValue;
        return wordCycles + GetSignedMultiplyOverhead((short)wordAccumulator < 0, (short)wordOperand < 0) + (fitsWord ? 1 : 0);
    }

    private static int GetSignedMultiplyOverhead(bool accumulatorNegative, bool operandNegative) =>
        (accumulatorNegative, operandNegative) switch
        {
            (false, false) => 10,
            (false, true) => 21,
            (true, false) => 24,
            (true, true) => 11,
        };

    private void TickRegisterDirectMultiply()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            byte modRm = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', modRm);
            return;
        }

        int fetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        int fetchPhase = _instructionTState - fetchStart;
        LastBusCycle = fetchPhase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle(),
            3 => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            _ => IdleCycle('-', 0),
        };
        if (fetchPhase == 2)
        {
            PushQueue(0x90);
        }

        if (_instructionTState == _instructionCycles)
        {
            int register = _memoryModRm & 7;
            if (Opcode == 0xF6)
            {
                Intel8086SpecialArithmeticResult result = Intel8086SpecialArithmetic.MultiplyByte(
                    _state.AX,
                    GetByteRegister(register),
                    _state.Flags,
                    signedOperation: ((_memoryModRm >> 3) & 7) == 5);
                _state.AX = result.AX;
                _state.Flags = result.Flags;
            }
            else
            {
                Intel8086WordMultiplyResult result = Intel8086SpecialArithmetic.MultiplyWord(
                    _state.AX,
                    GetWordRegister(register),
                    _state.Flags,
                    signedOperation: ((_memoryModRm >> 3) & 7) == 5);
                _state.AX = result.AX;
                _state.DX = result.DX;
                _state.Flags = result.Flags;
            }

            _instructionTState = 0;
        }
    }

    private void TickRegisterDirectDivide()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            byte modRm = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', modRm);
            return;
        }

        int fetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        int fetchPhase = _instructionTState - fetchStart;
        LastBusCycle = fetchPhase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle(),
            3 => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            _ => IdleCycle('-', 0),
        };
        if (fetchPhase == 2)
        {
            PushQueue(0x90);
        }

        if (_instructionTState == _instructionCycles)
        {
            int register = _memoryModRm & 7;
            if (IsRegisterDirectUnsignedDivideOpcode(Opcode, _memoryModRm) && Opcode == 0xF6)
            {
                if (!Intel8086SpecialArithmetic.TryDivideByte(
                    _state.AX,
                    GetByteRegister(register),
                    signedOperation: false,
                    out Intel8086SpecialArithmeticResult result))
                {
                    throw new InvalidOperationException("DIV byte entered its result phase after a detected divide fault.");
                }

                _state.AX = result.AX;
            }
            else if (IsRegisterDirectUnsignedDivideOpcode(Opcode, _memoryModRm))
            {
                if (!Intel8086SpecialArithmetic.TryDivideWord(
                    _state.AX,
                    _state.DX,
                    GetWordRegister(register),
                    signedOperation: false,
                    out Intel8086DivisionResult result))
                {
                    throw new InvalidOperationException("DIV word entered its result phase after a detected divide fault.");
                }

                _state.AX = result.AX;
                _state.DX = result.DX;
            }
            else if (Opcode == 0xF6)
            {
                if (!Intel8086SpecialArithmetic.TryDivideByte(
                    _state.AX,
                    GetByteRegister(register),
                    signedOperation: true,
                    out Intel8086SpecialArithmeticResult result))
                {
                    throw new InvalidOperationException("IDIV byte entered its result phase after a detected divide fault.");
                }

                _state.AX = result.AX;
            }
            else
            {
                if (!Intel8086SpecialArithmetic.TryDivideWord(
                    _state.AX,
                    _state.DX,
                    GetWordRegister(register),
                    signedOperation: true,
                    out Intel8086DivisionResult result))
                {
                    throw new InvalidOperationException("IDIV word entered its result phase after a detected divide fault.");
                }

                _state.AX = result.AX;
                _state.DX = result.DX;
            }

            _instructionTState = 0;
        }
    }

}
