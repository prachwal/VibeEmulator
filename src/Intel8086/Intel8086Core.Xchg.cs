namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMemoryXchg()
    {
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_xchgPrefixLength == 1)
        {
            if (_instructionTState == 1)
            {
                byte prefix = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', prefix);
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
                consumesOpcode = true;
            }

            if (_instructionTState == 4)
            {
                PopQueue();
                _state.IP++;
                consumesModRm = true;
            }
        }
        else if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }
        else if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _xchgModRm);
            return;
        }

        int displacementIndex = _instructionTState - _xchgDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _xchgDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int mod = _xchgModRm >> 6;
        int rm = _xchgModRm & 7;
        (int firstFetch, int secondFetch, int thirdFetch) = GetXchgFetchStarts(mod, rm, _queueCountAtStart, _xchgPrefixLength == 1);
        int fetchStart = IsFetchWindow(_instructionTState, firstFetch)
            ? firstFetch
            : secondFetch != 0 && IsFetchWindow(_instructionTState, secondFetch)
                ? secondFetch
                : thirdFetch != 0 && IsFetchWindow(_instructionTState, thirdFetch) ? thirdFetch : 0;
        int readPhase = _instructionTState - _xchgMemoryReadStart;
        int writeStart = _xchgMemoryReadStart + 12;
        int writePhase = _instructionTState - writeStart;
        bool memoryBusActive = readPhase is >= 0 and <= 3 || writePhase is >= 0 and <= 2;
        if (memoryBusActive)
        {
            fetchStart = 0;
        }

        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesOpcode ? 'F' : consumesDisplacement ? 'S' : '-', consumesOpcode ? Opcode : displacementByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesModRm || consumesDisplacement ? 'S' : '-',
                    QueueData = consumesModRm ? _xchgModRm : displacementByte,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumesDisplacement ? 'S' : '-', displacementByte),
            };

            if (phase == 2 && _state.QueueCount < _state.Queue.Length)
            {
                int bytesToQueue = Math.Min(2, _xchgPrefetchBytesRemaining);
                while (bytesToQueue > 0 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                    _xchgPrefetchBytesRemaining--;
                    bytesToQueue--;
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (_xchgPrefixLength == 1 && consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_xchgPrefixLength == 1 && consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _xchgModRm);
        }
        else if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = readPhase switch
            {
                0 => new Intel8086BusCycle(1, _xchgPhysicalAddress, "--", "---", "---", _xchgBhe, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _xchgPhysicalAddress, _xchgSegmentStatus, "R--", "---", _xchgBhe, 0, "MEMR", "T2", '-', 0),
                2 => TickMemoryXchgReadData(),
                _ => new Intel8086BusCycle(0, _xchgPhysicalAddress, _xchgSegmentStatus, "---", "---", _xchgBhe, 0, "PASV", "T4", '-', 0),
            };
        }
        else if (writePhase is >= 0 and <= 2)
        {
            ushort data = _xchgBhe == 0 ? (ushort)(_xchgRegisterValue << 8) : _xchgRegisterValue;
            LastBusCycle = writePhase switch
            {
                0 => new Intel8086BusCycle(1, _xchgPhysicalAddress, "--", "---", "---", _xchgBhe, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _xchgPhysicalAddress, _xchgSegmentStatus, "-A-", "---", _xchgBhe, 0, "MEMW", "T2", '-', 0),
                _ => TickMemoryXchgWriteData(data),
            };
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
            _instructionTState = 0;
        }
    }

    private static (int First, int Second, int Third) GetXchgFetchStarts(int mod, int rm, int queueCount, bool prefixed)
    {
        if (prefixed)
        {
            if (queueCount == 5)
            {
                int third = mod == 0 && rm == 6 ? 15
                    : mod == 2 && (rm is 0 or 3) ? 20
                        : mod == 2 && (rm is 1 or 2) ? 14
                            : mod == 2 && rm >= 4 ? 11 : 0;
                return (3, 7, third);
            }

            int second = mod == 0 && rm == 6 ? 15
                : mod >= 1 && (rm is 0 or 3) ? 12
                    : mod >= 1 && (rm is 1 or 2) ? 13
                        : mod >= 1 ? 18 : 0;
            return (5, second, 0);
        }

        int first = queueCount == 5 ? 3 : 4;
        int unprefixedSecond = mod == 0 && rm == 6 ? queueCount == 5 ? 13 : 14
            : mod == 1 && queueCount == 6 ? 0
                : mod == 2 && queueCount == 6 ? rm < 4 ? rm is 1 or 2 ? 12 : 11 : 17
                    : mod >= 1 ? rm < 4 ? rm is 1 or 2 ? 11 : 10 : 16 : 0;
        return (first, unprefixedSecond, 0);
    }

    private static int GetXchgMemoryReadStart(int mod, int rm, int queueCount, bool prefixed)
    {
        int eaCycles = GetMovEffectiveAddressCycles(mod, rm);
        if (!prefixed)
        {
            return mod switch
            {
                0 when rm <= 3 => eaCycles + 3,
                0 when rm == 6 => queueCount == 5 ? 9 : 10,
                0 => queueCount == 5 ? 9 : 8,
                1 => eaCycles + 7,
                2 => eaCycles + 7 + (queueCount == 6 ? 1 : 0),
                _ => throw new ArgumentOutOfRangeException(nameof(mod)),
            };
        }

        return mod switch
        {
            0 when rm is 0 or 3 => eaCycles + (queueCount == 5 ? 6 : 5),
            0 when rm is 1 or 2 => eaCycles + 5,
            0 when rm >= 4 => 11,
            1 => eaCycles + 9,
            2 when queueCount == 5 => eaCycles + (rm is 0 or 3 ? 9 : 10),
            2 => eaCycles + 9,
            _ => throw new ArgumentOutOfRangeException(nameof(mod)),
        };
    }

    private static int GetXchgPrefetchBytes(int queueCount, int mod, int prefixLength, int displacementBytes)
    {
        int finalQueueCount = prefixLength == 1
            ? queueCount == 5 ? mod == 1 ? 4 : 5 : mod == 1 ? 5 : 4
            : queueCount == 5 ? mod == 1 ? 5 : 4 : mod == 1 ? 4 : 5;
        int instructionLength = prefixLength + 2 + displacementBytes;
        return Math.Max(0, finalQueueCount - (queueCount - instructionLength));
    }

    private Intel8086BusCycle TickMemoryXchgReadData()
    {
        byte value = _bus.Read(_xchgPhysicalAddress);
        SetByteRegister((_xchgModRm >> 3) & 7, value);
        ushort data = _xchgBhe == 0 ? (ushort)(value << 8) : value;
        return new Intel8086BusCycle(0, _xchgPhysicalAddress, _xchgSegmentStatus, "R--", "---", _xchgBhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMemoryXchgWriteData(ushort data)
    {
        _bus.Write(_xchgPhysicalAddress, _xchgRegisterValue);
        return new Intel8086BusCycle(0, _xchgPhysicalAddress, _xchgSegmentStatus, "-AW", "---", _xchgBhe, data, "PASV", "T3", '-', 0);
    }

}
