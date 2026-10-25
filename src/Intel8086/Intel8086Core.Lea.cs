namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    // Profile pobrań T1 dla segment-prefiksowanego LEA z przypiętych śladów SST 8D.
    // Indeks: mod * 16 + r/m * 2 + (początkowa kolejka >= 6 ? 1 : 0).
    private static readonly byte[] PrefixedLeaFirstFetchStarts =
    [
        1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3,
        1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3,
        1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3,
    ];

    private static readonly byte[] PrefixedLeaSecondFetchStarts =
    [
        5, 0, 5, 0, 5, 0, 5, 0, 5, 0, 5, 0, 5, 7, 5, 0,
        5, 10, 5, 11, 5, 11, 5, 10, 5, 10, 5, 10, 5, 10, 5, 10,
        5, 10, 5, 11, 5, 11, 5, 10, 5, 10, 5, 10, 5, 10, 5, 10,
    ];

    private static readonly byte[] PrefixedLeaThirdFetchStarts =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        12, 0, 12, 0, 12, 0, 12, 0, 9, 0, 9, 0, 9, 0, 9, 0,
    ];

    private static readonly byte[] PrefixedLeaQueueRefillCounts =
    [
        3, 1, 3, 1, 3, 1, 3, 1, 1, 1, 1, 1, 3, 1, 1, 1,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 1, 3, 1, 3, 1, 3, 1,
        3, 3, 3, 3, 3, 3, 3, 3, 3, 1, 3, 1, 3, 1, 3, 1,
    ];

    private void TickLea()
    {
        int prefixCycles = _instructionPrefixLength == 0 ? 0 : 2;
        if (prefixCycles != 0 && _instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
            return;
        }

        if (prefixCycles != 0 && _instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int leaTState = _instructionTState - prefixCycles;
        int mod = _memoryModRm >> 6;
        int displacementStart = mod switch
        {
            0 when (_memoryModRm & 7) == 6 => 4,
            0 => int.MaxValue,
            1 or 2 => _leaEffectiveAddressCycles + 1,
            _ => throw new InvalidOperationException("LEA nie obsługuje adresowania rejestrowego."),
        };
        int displacementIndex = leaTState - displacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _leaDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        if (leaTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = prefixCycles == 0 || _queueCountAtStart >= 6
                ? IdleCycle('F', Opcode)
                : FetchCycle("T1", 'F', Opcode);
            return;
        }

        if (leaTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = prefixCycles == 0 || _queueCountAtStart >= 6
                ? IdleCycle('S', _memoryModRm)
                : FetchContinuationCycle('S', _memoryModRm);
            return;
        }

        int firstFetchStart;
        int secondFetchStart;
        int thirdFetchStart = 0;
        if (prefixCycles != 0)
        {
            int profileIndex = (mod * 16) + ((_memoryModRm & 7) * 2) + (_queueCountAtStart >= 6 ? 1 : 0);
            firstFetchStart = PrefixedLeaFirstFetchStarts[profileIndex];
            secondFetchStart = PrefixedLeaSecondFetchStarts[profileIndex];
            thirdFetchStart = PrefixedLeaThirdFetchStarts[profileIndex];
        }
        else
        {
            firstFetchStart = _queueCountAtStart <= 5 ? 3 : 4;
            secondFetchStart = GetLeaSecondFetchStart(mod, _memoryModRm & 7, firstFetchStart);
        }
        int fetchStart = leaTState >= firstFetchStart && leaTState <= firstFetchStart + 3
            ? firstFetchStart
            : secondFetchStart != 0 && leaTState >= secondFetchStart && leaTState <= secondFetchStart + 3
                ? secondFetchStart
                : thirdFetchStart != 0 && leaTState >= thirdFetchStart && leaTState <= thirdFetchStart + 3
                    ? thirdFetchStart
                    : 0;
        if (fetchStart != 0)
        {
            int phase = leaTState - fetchStart;
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
            int bytesToQueue;
            if (prefixCycles != 0)
            {
                int profileIndex = (mod * 16) + ((_memoryModRm & 7) * 2) + (_queueCountAtStart >= 6 ? 1 : 0);
                int refillCount = PrefixedLeaQueueRefillCounts[profileIndex];
                bytesToQueue = fetchStart == firstFetchStart
                    ? Math.Min(refillCount, 2)
                    : fetchStart == secondFetchStart && refillCount == 3 ? 1 : 0;
            }
            else
            {
                bool queuesSecondByte = _queueCountAtStart == 5 && mod is 1 or 2 &&
                    secondFetchStart != 0 && secondFetchStart + 2 <= _instructionCycles;
                bytesToQueue = fetchStart == firstFetchStart
                    ? queuesSecondByte ? 2 : 1
                    : queuesSecondByte && fetchStart == secondFetchStart ? 1 : 0;
            }

            if (phase == 2 && bytesToQueue > 0)
            {
                for (int index = 0; index < bytesToQueue && _state.QueueCount < _state.Queue.Length; index++)
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
