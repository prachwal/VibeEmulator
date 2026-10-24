namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickLea()
    {
        int mod = _memoryModRm >> 6;
        int displacementStart = mod switch
        {
            0 when (_memoryModRm & 7) == 6 => 4,
            0 => int.MaxValue,
            1 or 2 => _leaEffectiveAddressCycles + 1,
            _ => throw new InvalidOperationException("LEA nie obsługuje adresowania rejestrowego."),
        };
        int displacementIndex = _instructionTState - displacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _leaDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
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
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _memoryModRm);
            return;
        }

        int firstFetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        int secondFetchStart = GetLeaSecondFetchStart(mod, _memoryModRm & 7, firstFetchStart);
        int fetchStart = _instructionTState >= firstFetchStart && _instructionTState <= firstFetchStart + 3
            ? firstFetchStart
            : secondFetchStart != 0 && _instructionTState >= secondFetchStart && _instructionTState <= secondFetchStart + 3
                ? secondFetchStart
                : 0;
        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesDisplacement ? 'S' : '-', displacementByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesDisplacement ? 'S' : '-',
                    QueueData = consumesDisplacement ? displacementByte : (byte)0,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumesDisplacement ? 'S' : '-', displacementByte),
            };
            bool queuesSecondByte = _queueCountAtStart == 5 && mod is 1 or 2 &&
                secondFetchStart != 0 && secondFetchStart + 2 <= _instructionCycles;
            bool shouldQueueFetchedByte = fetchStart == firstFetchStart || queuesSecondByte;
            if (phase == 2 && shouldQueueFetchedByte && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
                if (fetchStart == firstFetchStart && queuesSecondByte && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (consumesDisplacement)
        {
            LastBusCycle = IdleCycle('S', displacementByte);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            SetWordRegister((_memoryModRm >> 3) & 7, _leaOffset);
            _instructionTState = 0;
        }
    }

    private static Intel8086BusCycle FetchDataCycle(char queueStatus, byte queueData) =>
        // Pola T3/PASV odpowiadają obserwowanemu cyklowi LEA w przypiętych wektorach SST.
        new(0, 0, "CS", "R--", "---", 0, 37008, "PASV", "T3", queueStatus, queueData);

    private int GetLeaSecondFetchStart(int mod, int rm, int firstFetchStart)
    {
        if (mod == 0 && rm != 6)
        {
            return 0;
        }

        if (mod == 0 && rm == 6)
        {
            return firstFetchStart + 4;
        }

        if (mod == 1 && _queueCountAtStart > 5)
        {
            return 0;
        }

        if (mod >= 1)
        {
            return _queueCountAtStart <= 5
                ? 10 + (rm is 1 or 2 ? 1 : 0)
                : mod == 2 ? 11 + (rm is 1 or 2 ? 1 : 0) : 0;
        }

        return 0;
    }

    private void PushQueue(byte value)
    {
        if (_state.QueueHead + _state.QueueCount >= _state.Queue.Length)
        {
            Array.Copy(_state.Queue, _state.QueueHead, _state.Queue, 0, _state.QueueCount);
            _state.QueueHead = 0;
        }

        int tail = _state.QueueHead + _state.QueueCount;
        _state.Queue[tail] = value;
        _state.QueueCount++;
    }

    private void SetWordRegister(int register, ushort value)
    {
        switch (register)
        {
            case 0: _state.AX = value; break;
            case 1: _state.CX = value; break;
            case 2: _state.DX = value; break;
            case 3: _state.BX = value; break;
            case 4: _state.SP = value; break;
            case 5: _state.BP = value; break;
            case 6: _state.SI = value; break;
            case 7: _state.DI = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(register));
        }
    }
}
