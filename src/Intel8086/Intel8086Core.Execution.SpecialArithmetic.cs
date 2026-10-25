using System.Numerics;

namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickEscRegisterDirect()
    {
        _instructionTState++;
        if (_instructionPrefixLength != 0 && _instructionTState <= 4)
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
                LastBusCycle = _queueCountAtStart < 5 && TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                    ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                    : IdleCycle('F', Opcode);
            }
            else
            {
                PopQueue();
                if (_state.QueueCount != 0)
                {
                    PopQueue();
                }

                _state.IP++;
                LastBusCycle = _queueCountAtStart < 5 && TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                    ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                    : IdleCycle('S', _memoryModRm);
                _instructionTState = 0;
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

        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        PopQueue();
        _state.IP++;
        LastBusCycle = IdleCycle('S', modRm);
        _instructionTState = 0;
    }

    private static int GetAamCycles(ushort ax, byte radix)
    {
        if (radix == 0)
        {
            return 18;
        }

        int quotient = (byte)ax / radix;
        return 77 + BitOperations.PopCount((uint)quotient) + ((quotient & 1) * 2);
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

    private void TickSalc()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', consumed);
            return;
        }

        if (_instructionPrefixLength == 1 && _instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionPrefixLength == 0 && _instructionTState == 2)
        {
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            SetByteRegister(0, (_state.Flags & 0x0001) != 0 ? byte.MaxValue : (byte)0);
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionPrefixLength == 1 && _instructionTState == 3)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'F', consumed)
                : IdleCycle('F', consumed);
            return;
        }

        if (_instructionTState == 3 && _instructionPrefixLength == 0)
        {
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", '-', 0)
                : IdleCycle('-', 0);
        }
        else if (_instructionPrefixLength == 1 && _instructionTState == 4)
        {
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
        }
        else if (_instructionPrefixLength == 1 && _instructionTState == 5)
        {
            bool carrySet = (_state.Flags & 0x0001) != 0;
            if (_queueCountAtStart <= 5)
            {
                if (carrySet)
                {
                    LastBusCycle = FetchDataCycle('-', 0);
                    PushQueue(0x90);
                    _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
                }
                else
                {
                    if (_state.QueueCount != 0)
                    {
                        PopQueue();
                    }

                    LastBusCycle = FetchDataCycle('-', 0);
                }
            }
            else
            {
                if (_state.QueueCount != 0)
                {
                    PopQueue();
                }

                LastBusCycle = FetchCycle("T1", '-', 0);
            }

            SetByteRegister(0, (_state.Flags & 0x0001) != 0 ? byte.MaxValue : (byte)0);
        }
        else if (_instructionPrefixLength == 1 && _instructionTState == 6)
        {
            LastBusCycle = _queueCountAtStart <= 5
                ? new Intel8086BusCycle(0, _nextFetchAddress, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0)
                : FetchContinuationCycle('-', 0);
            _instructionTState = 0;
        }
        else if (_instructionPrefixLength == 0 && _instructionTState == 4)
        {
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
            _instructionTState = 0;
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private void TickAsciiAdjust(bool multiplyAdjust)
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
            Intel8086SpecialArithmeticResult result = multiplyAdjust
                ? Intel8086SpecialArithmetic.Aam(_state.AX, radix, _state.Flags)
                : Intel8086SpecialArithmetic.Aad(_state.AX, radix, _state.Flags);
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
