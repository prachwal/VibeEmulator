namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMemoryByteIncDec()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        int mod = _memoryModRm >> 6;
        int displacementBytes = mod == 0
            ? (_memoryModRm & 7) == 6 ? 2 : 0
            : mod == 1 ? 1 : 2;
        if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _memoryModRm);
            return;
        }

        int rm = _memoryModRm & 7;
        int effectiveAddressCycles = rm switch
        {
            0 or 3 => 7,
            1 or 2 => 8,
            4 or 5 or 7 => 5,
            6 => mod == 0 ? 6 : 5,
            _ => throw new InvalidOperationException("Invalid FE ModR/M register field."),
        };
        int displacementStart = mod == 0 && rm == 6 ? 4 : effectiveAddressCycles + 1;
        int displacementIndex = _instructionTState - displacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < displacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        if (TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle))
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : prefetchCycle;
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        if (consumesDisplacement)
        {
            LastBusCycle = IdleCycle('S', displacementByte);
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        int readPhase = _instructionTState - _memoryReadStart;
        if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryRead(readPhase);
        }
        else if (readPhase is >= 10 and <= 12)
        {
            LastBusCycle = TickMemoryWrite(readPhase - 10);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        FinishMemoryByteIncDecIfNeeded();
    }

    private bool TryTickMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        int firstFetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int effectiveAddressCycles = rm switch
        {
            0 or 3 => 7,
            1 or 2 => 8,
            4 or 5 or 7 => 5,
            6 => mod == 0 ? 6 : 5,
            _ => throw new InvalidOperationException("Invalid FE ModR/M register field."),
        };
        bool secondFetchIsPossible = mod switch
        {
            0 => rm == 6,
            1 => _queueCountAtStart == 5,
            _ => true,
        };
        int secondFetchStart = firstFetchStart + (mod == 0 ? 10 : 7 + (effectiveAddressCycles >= 8 ? 1 : 0));
        if (secondFetchStart <= _memoryReadStart + 3 && secondFetchStart + 3 >= _memoryReadStart)
        {
            secondFetchStart = _memoryReadStart + 4;
        }
        int fetchStart = -1;
        bool isFirstFetch = false;
        if (_instructionTState >= firstFetchStart && _instructionTState < firstFetchStart + 4)
        {
            fetchStart = firstFetchStart;
            isFirstFetch = true;
        }
        else if (secondFetchIsPossible &&
                 _instructionTState >= secondFetchStart && _instructionTState < secondFetchStart + 4)
        {
            fetchStart = secondFetchStart;
        }

        if (fetchStart < 0)
        {
            cycle = default;
            return false;
        }

        int fetchPhase = _instructionTState - fetchStart;
        cycle = fetchPhase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle(),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };
        if (fetchPhase == 2)
        {
            int bytesFetched = isFirstFetch && secondFetchIsPossible ? 2 : 1;
            for (int index = 0; index < bytesFetched && _state.QueueCount < _state.Queue.Length; index++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }

        return true;
    }

    private Intel8086BusCycle TickMemoryRead(int phase)
    {
        byte bhe = (byte)((_memoryAddress & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _memoryAddress, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadMemoryData(bhe),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadMemoryData(byte bhe)
    {
        _memoryValue = _bus.Read(_memoryAddress);
        Intel8086AluResult result = Intel8086Alu.ExecuteIncrementDecrement(
            _memoryValue,
            _state.Flags,
            ((_memoryModRm >> 3) & 7) == 1);
        _memoryResult = result.Value;
        _state.Flags = result.Flags;
        ushort data = (ushort)((_memoryAddress & 1) == 0 ? _memoryValue : _memoryValue << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMemoryWrite(int phase)
    {
        byte bhe = (byte)((_memoryAddress & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _memoryAddress, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            _ => WriteMemoryData(bhe),
        };
    }

    private Intel8086BusCycle WriteMemoryData(byte bhe)
    {
        _bus.Write(_memoryAddress, _memoryResult);
        ushort data = (ushort)((_memoryAddress & 1) == 0 ? _memoryResult : _memoryResult << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void FinishMemoryByteIncDecIfNeeded()
    {
        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }
}
