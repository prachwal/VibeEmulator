using System.Numerics;

namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool _memoryDivideFaultPending;
    private int _memoryDivideFaultEntryStart;
    private byte _memoryDivideQueueData;

    private static bool IsRegisterDirectMultiplyOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) is 4 or 5;

    private static bool IsRegisterDirectUnsignedDivideOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) == 6;

    private static bool IsRegisterDirectSignedDivideOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) == 7;

    private static bool IsRegisterDirectDivideOpcode(byte opcode, byte modRm) =>
        IsRegisterDirectUnsignedDivideOpcode(opcode, modRm) || IsRegisterDirectSignedDivideOpcode(opcode, modRm);

    private static bool IsMemoryDivideOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) != 3 && ((modRm >> 3) & 7) is 6 or 7;

    private static bool IsMemoryMultiplyOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) != 3 && ((modRm >> 3) & 7) is 4 or 5;

    private void PrepareMemoryDivide(ushort divisor)
    {
        _memoryDivideFaultPending = false;
        bool byteOperation = Opcode == 0xF6;
        bool signedOperation = ((_memoryModRm >> 3) & 7) == 7;
        int divideCycles = signedOperation
            ? GetSignedDivideCycles(Opcode, divisor)
            : GetUnsignedDivideCycles(Opcode, divisor);
        if (_memoryDivideFaultPending)
        {
            return;
        }

        int memoryTransferCycles = byteOperation ? 3 : (_memoryAddress & 1) == 0 ? 3 : 7;
        int combinedSegmentRepeatCycles = _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2 ? -2 : 0;
        _instructionCycles = _memoryReadStart - 1 + memoryTransferCycles + divideCycles + combinedSegmentRepeatCycles;
    }

    private void CompleteMemoryDivideFaultIfNeeded()
    {
        if (!_memoryDivideFaultPending)
        {
            return;
        }

        InitializeDivideError(_memoryDivideFaultEntryStart);
        int mod = _memoryModRm >> 6;
        bool hasDirectAddressDisplacement = mod == 0 && (_memoryModRm & 7) == 6;
        bool prefetchAfterOperand = Opcode == 0xF7 || ((_memoryModRm >> 3) & 7) == 7 || hasDirectAddressDisplacement;
        _divideErrorPrefetchStart = prefetchAfterOperand ? _instructionTState + 1 : int.MaxValue;
        _softwareInterruptReturnIp = _state.IP;
        _memoryDivideFaultPending = false;
    }

    private void CompleteMemoryDivideIfNeeded()
    {
        if (!IsMemoryDivideOpcode(Opcode, _memoryModRm) ||
            _instructionTState != _instructionCycles ||
            _softwareInterruptIsDivideError)
        {
            return;
        }

        bool byteOperation = Opcode == 0xF6;
        bool signedOperation = ((_memoryModRm >> 3) & 7) == 7;
        Intel8086SpecialArithmeticResult byteResult = default;
        Intel8086DivisionResult wordResult = default;
        bool succeeded;
        if (byteOperation)
        {
            succeeded = Intel8086SpecialArithmetic.TryDivideByte(
                _state.AX,
                (byte)_memoryValue,
                signedOperation,
                out byteResult);
        }
        else
        {
            succeeded = Intel8086SpecialArithmetic.TryDivideWord(
                _state.AX,
                _state.DX,
                _memoryWordValue,
                signedOperation,
                out wordResult);
        }
        if (!succeeded)
        {
            throw new InvalidOperationException("Memory divide entered its result phase after a detected divide fault.");
        }

        if (byteOperation)
        {
            _state.AX = byteResult.AX;
        }
        else
        {
            _state.AX = wordResult.AX;
            _state.DX = wordResult.DX;
        }

        InvertRepeatPrefixedIdivQuotientIfRequired();
    }

    private void InvertRepeatPrefixedIdivQuotientIfRequired()
    {
        if (_instructionPrefixLength == 0 ||
            _instructionPrefix is not (0xF2 or 0xF3) ||
            ((_memoryModRm >> 3) & 7) != 7)
        {
            return;
        }

        if (Opcode == 0xF6)
        {
            byte quotient = (byte)_state.AX;
            byte invertedQuotient = unchecked((byte)-(sbyte)quotient);
            _state.AX = (ushort)((_state.AX & 0xFF00) | invertedQuotient);
        }
        else
        {
            _state.AX = unchecked((ushort)-(short)_state.AX);
        }
    }

    private void PrepareMemoryMultiply(ushort operand)
    {
        int multiplyCycles = GetMultiplyCycles(Opcode, _memoryModRm, operand);
        if (Opcode == 0xF6 && ((_memoryModRm >> 3) & 7) == 5 && (byte)operand == 0x80 && (sbyte)_state.AX >= 0)
        {
            multiplyCycles--;
        }

        int transferCycles = Opcode == 0xF6 ? 3 : (_memoryAddress & 1) == 0 ? 3 : 7;
        _instructionCycles = _memoryReadStart - 1 + transferCycles + multiplyCycles;
    }

    private void CompleteMemoryMultiplyIfNeeded()
    {
        if (!IsMemoryMultiplyOpcode(Opcode, _memoryModRm) || _instructionTState != _instructionCycles)
        {
            return;
        }

        bool signedOperation = ((_memoryModRm >> 3) & 7) == 5;
        if (Opcode == 0xF6)
        {
            Intel8086SpecialArithmeticResult result = Intel8086SpecialArithmetic.MultiplyByte(
                _state.AX,
                (byte)_memoryValue,
                _state.Flags,
                signedOperation);
            _state.AX = result.AX;
            _state.Flags = result.Flags;
        }
        else
        {
            Intel8086WordMultiplyResult result = Intel8086SpecialArithmetic.MultiplyWord(
                _state.AX,
                _memoryWordValue,
                _state.Flags,
                signedOperation);
            _state.AX = result.AX;
            _state.DX = result.DX;
            _state.Flags = result.Flags;
        }
    }

    private int GetRegisterDirectUnsignedDivideCycles(byte opcode, byte modRm)
    {
        bool byteOperation = opcode == 0xF6;
        int register = modRm & 7;
        ushort divisor = byteOperation ? GetByteRegister(register) : GetWordRegister(register);
        return GetUnsignedDivideCycles(opcode, divisor);
    }

    private int GetUnsignedDivideCycles(byte opcode, ushort divisor)
    {
        bool byteOperation = opcode == 0xF6;
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
            UpdateDivideErrorFlags(divisor);
            if (IsMemoryDivideOpcode(Opcode, _memoryModRm))
            {
                _memoryDivideFaultPending = true;
                _memoryDivideFaultEntryStart = GetMemoryDivideEntryStart(20);
                return 0;
            }

            InitializeDivideError(20 + (_instructionPrefixLength == 0 ? 0 : 2));
            if (_instructionPrefixLength != 0 && _queueCountAtStart > 5)
            {
                _divideErrorPrefetchStart = int.MaxValue;
            }

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
        ushort divisor = byteOperation ? GetByteRegister(register) : GetWordRegister(register);
        return GetSignedDivideCycles(opcode, divisor);
    }

    private int GetSignedDivideCycles(byte opcode, ushort divisor)
    {
        bool byteOperation = opcode == 0xF6;
        int width = byteOperation ? 8 : 16;
        int signedDivisor = byteOperation
            ? (sbyte)(byte)divisor
            : (short)divisor;
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
            UpdateDivideErrorFlags(unchecked((ushort)signedDivisor));
            int exceptionStart = signedDivisor == 0 || quotientMagnitude > ((1u << width) - 1)
                ? 30 + (signedDividend < 0 ? 4 : 0) - (signedDivisor < 0 ? 1 : 0)
                : cycles;
            bool combinedSegmentRepeatPrefix =
                _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2;
            int prefixCycles = combinedSegmentRepeatPrefix
                ? IsMemoryDivideOpcode(Opcode, _memoryModRm) ? 0 : 2
                : _instructionPrefixLength == 0 ? 0 : 2;

            if (IsMemoryDivideOpcode(Opcode, _memoryModRm))
            {
                exceptionStart += combinedSegmentRepeatPrefix ? prefixCycles : 0;
                exceptionStart = GetMemoryDivideEntryStart(exceptionStart);
                _memoryDivideFaultPending = true;
                _memoryDivideFaultEntryStart = exceptionStart;
                return 0;
            }

            exceptionStart += prefixCycles;
            InitializeDivideError(exceptionStart);
            if (_instructionPrefixLength != 0 && _queueCountAtStart > 5)
            {
                _divideErrorPrefetchStart = int.MaxValue;
            }

            return _instructionCycles;
        }

        bool hasCombinedSegmentRepeatPrefix =
            _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2;
        return cycles + (hasCombinedSegmentRepeatPrefix && IsMemoryDivideOpcode(opcode, _memoryModRm) ? 2 : 0);
    }

    private void UpdateDivideErrorFlags(ushort divisor)
    {
        bool byteOperation = Opcode == 0xF6;
        bool signedOperation = ((_memoryModRm >> 3) & 7) == 7;
        int width = byteOperation ? 8 : 16;
        ulong dividend = byteOperation
            ? _state.AX
            : ((ulong)_state.DX << 16) | _state.AX;
        uint divisorMagnitude = divisor;

        if (signedOperation)
        {
            long signedDividend = byteOperation
                ? (short)_state.AX
                : unchecked((int)(((uint)_state.DX << 16) | _state.AX));
            long signedDivisor = byteOperation
                ? (sbyte)(byte)divisor
                : (short)divisor;
            dividend = (ulong)Math.Abs(signedDividend);
            divisorMagnitude = (uint)Math.Abs(signedDivisor);
        }

        uint mask = (1u << width) - 1;
        uint upperDividend = (uint)(dividend >> width) & mask;
        _state.Flags = byteOperation
            ? Intel8086Alu.Execute(
                (byte)upperDividend,
                (byte)divisorMagnitude,
                _state.Flags,
                Intel8086AluOperation.Compare).Flags
            : Intel8086Alu.Execute(
                (ushort)upperDividend,
                (ushort)divisorMagnitude,
                _state.Flags,
                Intel8086AluOperation.Compare).Flags;
    }

    private int GetMemoryDivideEntryStart(int baseEntryStart)
    {
        int transferCycles = Opcode == 0xF6
            ? 3
            : (_memoryAddress & 1) == 0 ? 3 : 7;
        return baseEntryStart + _memoryReadStart - 1 + transferCycles;
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
        ushort operand = opcode == 0xF6 ? GetByteRegister(register) : GetWordRegister(register);
        int cycles = GetMultiplyCycles(opcode, modRm, operand);
        if (_segmentOverride != Intel8086SegmentOverride.None &&
            opcode == 0xF6 &&
            ((modRm >> 3) & 7) == 5 &&
            (byte)operand == 0x80 &&
            (sbyte)_state.AX >= 0)
        {
            cycles--;
        }

        return cycles;
    }

    private int GetMultiplyCycles(byte opcode, byte modRm, ushort operand)
    {
        if (opcode == 0xF6)
        {
            byte accumulator = (byte)_state.AX;
            byte byteOperand = (byte)operand;
            bool signedOperation = ((modRm >> 3) & 7) == 5;
            int multiplier = signedOperation ? Math.Abs((int)(sbyte)accumulator) : accumulator;
            int cycles = Math.Max(70, 69 + BitOperations.PopCount((uint)multiplier));
            if (!signedOperation &&
                (byteOperand == 0 && accumulator != 0 || accumulator * byteOperand is > 0 and < 0x100))
            {
                cycles++;
            }

            if (!signedOperation)
            {
                return cycles;
            }

            int signedProduct = (sbyte)accumulator * (sbyte)byteOperand;
            bool fitsByte = signedProduct is >= sbyte.MinValue and <= sbyte.MaxValue;
            return cycles + GetSignedMultiplyOverhead((sbyte)accumulator < 0, (sbyte)byteOperand < 0) +
                (fitsByte ? 1 : 0) - (accumulator == 0 ? 1 : 0);
        }

        ushort wordAccumulator = _state.AX;
        ushort wordOperand = operand;
        bool signedWordOperation = ((modRm >> 3) & 7) == 5;
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
        return wordCycles + GetSignedMultiplyOverhead((short)wordAccumulator < 0, (short)wordOperand < 0) +
            (fitsWord ? 1 : 0) - (wordAccumulator == 0 ? 1 : 0);
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
        if (TryGetSegmentOverride(_instructionPrefix, out _) && _instructionTState <= 4)
        {
            if (_instructionTState == 1)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
            }
            else if (_instructionTState == 2)
            {
                LastBusCycle = IdleCycle('-', 0);
            }
            else if (_instructionTState == 3)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                    ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                    : IdleCycle('F', Opcode);
            }
            else
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                    ? modRmFetchCycle with { QueueStatus = 'S', QueueData = modRm }
                    : IdleCycle('S', modRm);
            }

            return;
        }

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

        int fetchStart = _instructionPrefixLength != 0
            ? _queueCountAtStart <= 5 ? 3 : 5
            : _queueCountAtStart <= 5 ? 3 : 4;
        if (_instructionPrefixLength != 0 && _queueCountAtStart <= 5 && _instructionTState is >= 7 and <= 10)
        {
            int additionalFetchPhase = _instructionTState - 7;
            LastBusCycle = additionalFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (additionalFetchPhase == 2)
            {
                int bytesFetched = Math.Min(2, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

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
            if (_instructionPrefixLength != 0)
            {
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
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
        if (_segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 2 &&
            !_combinedDividePrefixPreludeComplete)
        {
            TickCombinedSegmentRepeatDividePrefix();
            return;
        }

        _instructionTState++;
        if (_instructionPrefixLength != 0)
        {
            if (_instructionTState == 1)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
                return;
            }

            if (_instructionTState == 2)
            {
                LastBusCycle = IdleCycle('-', 0);
                return;
            }

            if (_instructionTState == 3)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                    ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                    : IdleCycle('F', Opcode);
                return;
            }

            if (_instructionTState == 4)
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                    ? modRmFetchCycle with { QueueStatus = 'S', QueueData = modRm }
                    : IdleCycle('S', modRm);
                return;
            }
        }

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

        int fetchStart = _instructionPrefixLength != 0
            ? _queueCountAtStart <= 5 ? 3 : 5
            : _queueCountAtStart <= 5 ? 3 : 4;
        if (_instructionPrefixLength != 0 && _queueCountAtStart <= 5 &&
            IsRegisterDirectDivideOpcode(Opcode, _memoryModRm) && _instructionTState is >= 7 and <= 10)
        {
            int additionalFetchPhase = _instructionTState - 7;
            LastBusCycle = additionalFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (additionalFetchPhase == 2)
            {
                int bytesFetched = Math.Min(2, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

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
            if (_instructionPrefixLength != 0)
            {
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
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

            InvertRepeatPrefixedIdivQuotientIfRequired();
            _instructionTState = 0;
        }
    }

    private void TickCombinedSegmentRepeatDividePrefix()
    {
        _instructionTState++;
        byte segmentPrefix = _segmentOverride switch
        {
            Intel8086SegmentOverride.Es => 0x26,
            Intel8086SegmentOverride.Cs => 0x2E,
            Intel8086SegmentOverride.Ss => 0x36,
            Intel8086SegmentOverride.Ds => 0x3E,
            _ => 0,
        };
        bool shortQueue = _queueCountAtStart > 5;
        switch (_instructionTState)
        {
            case 1:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', segmentPrefix);
                break;
            case 2:
                LastBusCycle = IdleCycle('-', 0);
                break;
            case 3:
                PopQueue();
                _state.IP++;
                LastBusCycle = shortQueue
                    ? FetchCycle("T1", 'F', _instructionPrefix)
                    : IdleCycle('F', _instructionPrefix);
                break;
            case 4:
                LastBusCycle = shortQueue
                    ? FetchContinuationCycle('-', 0)
                    : IdleCycle('-', 0);
                break;
            case 5:
                PopQueue();
                _state.IP++;
                LastBusCycle = shortQueue
                    ? FetchPrefixedCodeDataCycle() with { QueueStatus = 'F', QueueData = Opcode }
                    : FetchCycle("T1", 'F', Opcode);
                if (shortQueue)
                {
                    PushQueue(ReadPrefixedCodeByte(_nextFetchAddress));
                    PushQueue(ReadPrefixedCodeByte((_nextFetchAddress + 1) & 0xFFFFF));
                    _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
                }

                break;
            default:
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = shortQueue
                    ? new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", 'S', modRm)
                    : FetchContinuationCycle('S', modRm);
                _instructionTState = 4;
                if (_softwareInterruptIsDivideError)
                {
                    _instructionCycles -= 2;
                }

                _combinedDividePrefixPreludeComplete = true;
                break;
        }
    }

}
