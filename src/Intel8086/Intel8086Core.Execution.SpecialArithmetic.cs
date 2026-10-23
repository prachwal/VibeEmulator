using System.Numerics;

namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsRegisterDirectMultiplyOpcode(byte opcode, byte modRm) =>
        opcode is 0xF6 or 0xF7 && (modRm >> 6) == 3 && ((modRm >> 3) & 7) is 4 or 5;

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

    private void TickCbw()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        _state.AX = Intel8086SpecialArithmetic.Cbw(_state.AX);
        if (_state.QueueCount != 0)
        {
            PopQueue();
        }

        LastBusCycle = IdleCycle('-', 0);
        _instructionTState = 0;
    }

    private void TickAad()
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
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        byte radix = (byte)_immediateWord;
        if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', radix)
                : IdleCycle('S', radix);
            return;
        }

        int fetchStart = _queueCountAtStart <= 5 ? 3 : 5;
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
            Intel8086SpecialArithmeticResult result = Intel8086SpecialArithmetic.Aad(_state.AX, radix, _state.Flags);
            _state.AX = result.AX;
            _state.Flags = result.Flags;
            _instructionTState = 0;
        }
    }

    private void TickCwd()
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
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        bool prefetchAvailable = _queueCountAtStart <= 5;
        LastBusCycle = prefetchAvailable
            ? _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            }
            : IdleCycle('-', 0);

        if (_instructionTState == _instructionCycles)
        {
            _state.DX = Intel8086SpecialArithmetic.Cwd(_state.AX);
            if (prefetchAvailable && _state.AX >= 0x8000)
            {
                PushQueue(0x90);
                PushQueue(0x90);
            }

            _instructionTState = 0;
        }
    }

    private void TickDecimalAdjust()
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
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        bool fetchAvailable = _queueCountAtStart <= 5;
        LastBusCycle = fetchAvailable && _instructionTState <= (Opcode is 0x27 or 0x2F ? 4 : 6)
            ? _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            }
            : IdleCycle('-', 0);

        if (fetchAvailable && Opcode is 0x37 or 0x3F && _instructionTState == 5)
        {
            PushQueue(0x90);
            PushQueue(0x90);
        }

        if (_instructionTState == _instructionCycles)
        {
            Intel8086SpecialArithmeticResult result = Opcode switch
            {
                0x27 => Intel8086SpecialArithmetic.Daa(_state.AX, _state.Flags),
                0x2F => Intel8086SpecialArithmetic.Das(_state.AX, _state.Flags),
                0x37 => Intel8086SpecialArithmetic.Aaa(_state.AX, _state.Flags),
                0x3F => Intel8086SpecialArithmetic.Aas(_state.AX, _state.Flags),
                _ => throw new InvalidOperationException($"Unexpected decimal-adjust opcode {Opcode:X2}."),
            };
            _state.AX = result.AX;
            _state.Flags = result.Flags;
            _instructionTState = 0;
        }
    }
}
