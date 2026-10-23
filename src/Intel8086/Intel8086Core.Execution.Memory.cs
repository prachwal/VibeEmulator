namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    // Profile T-state/prefetch FE z przypiętego źródła SingleStepTests 8086 (SHA w README wektorów).
    // Indeks: mod * 16 + r/m * 2 + (początkowa kolejka >= 6 ? 1 : 0).
    private static readonly byte[] PrefixedMemoryReadStarts =
    [
        13, 12, 13, 13, 13, 13, 13, 12, 11, 11, 11, 11, 11, 11, 11, 11,
        16, 16, 17, 17, 17, 17, 16, 16, 14, 14, 14, 14, 14, 14, 14, 14,
        16, 16, 18, 17, 18, 17, 16, 16, 15, 14, 15, 14, 15, 14, 15, 14,
    ];

    private static readonly byte[] PrefixedMemorySecondFetchStarts =
    [
        7, 0, 7, 0, 7, 0, 7, 0, 7, 0, 7, 0, 7, 15, 7, 0,
        7, 12, 7, 13, 7, 13, 7, 12, 7, 18, 7, 18, 7, 18, 7, 18,
        7, 12, 7, 13, 7, 13, 7, 12, 7, 18, 7, 18, 7, 18, 7, 18,
    ];

    private static readonly byte[] PrefixedMemoryThirdFetchStarts =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        20, 0, 14, 0, 14, 0, 20, 0, 11, 0, 11, 0, 11, 0, 11, 0,
    ];

    private static int GetPrefixedMemoryReadStart(int mod, int rm, int queueCount) =>
        PrefixedMemoryReadStarts[(mod * 16) + (rm * 2) + (queueCount >= 6 ? 1 : 0)];

    private static bool TryGetSegmentOverride(byte opcode, out Intel8086SegmentOverride segmentOverride)
    {
        segmentOverride = opcode switch
        {
            0x26 => Intel8086SegmentOverride.Es,
            0x2E => Intel8086SegmentOverride.Cs,
            0x36 => Intel8086SegmentOverride.Ss,
            0x3E => Intel8086SegmentOverride.Ds,
            _ => Intel8086SegmentOverride.None,
        };
        return segmentOverride != Intel8086SegmentOverride.None;
    }

    private static string GetMemorySegmentStatus(int rm, int mod, Intel8086SegmentOverride segmentOverride) =>
        segmentOverride switch
        {
            Intel8086SegmentOverride.Es => "ES",
            Intel8086SegmentOverride.Cs => "CS",
            Intel8086SegmentOverride.Ss => "SS",
            Intel8086SegmentOverride.Ds => "DS",
            _ => rm is 2 or 3
                ? "SS"
                : mod != 0 && rm == 6 ? "SS" : "DS",
        };

    private void TickPrefixedRegisterByteIncDec()
    {
        _instructionTState++;
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
        else if (_instructionTState == 4)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                : IdleCycle('S', _memoryModRm);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
            TryTickMemoryPrefetch(out Intel8086BusCycle fetchCycle);
            LastBusCycle = fetchCycle;
            if (_state.QueueCount > 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
            int register = _memoryModRm & 7;
            bool decrement = ((_memoryModRm >> 3) & 7) == 1;
            Intel8086AluResult result = Intel8086Alu.ExecuteIncrementDecrement(
                GetByteRegister(register), _state.Flags, decrement);
            SetByteRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
    }

    private void TickMemoryByteIncDec()
    {
        _instructionTState++;
        if (_instructionPrefixLength != 0)
        {
            TickPrefixedMemoryByteIncDecStart();
            return;
        }

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
        int writePhaseStart = Opcode == 0xD2 ? 15 + (4 * _shiftCount) : 10;
        if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryRead(readPhase);
        }
        else if (readPhase >= writePhaseStart && readPhase <= writePhaseStart + 2)
        {
            LastBusCycle = TickMemoryWrite(readPhase - writePhaseStart);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        FinishMemoryByteIncDecIfNeeded();
    }

    private void TickMemoryWordIncDec()
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
            _ => throw new InvalidOperationException("Invalid FF ModR/M register field."),
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

        int readPhase = _instructionTState - _memoryReadStart;
        bool splitWordTransfer = (_memoryAddress & 1) != 0;
        bool readOnlyWordAlu = IsWordAluModRmOpcode(Opcode) && IsWordAluMemoryReadOnly(Opcode);
        int writePhaseStart = (splitWordTransfer ? 14 : 10) + (Opcode == 0xD3 ? 5 + (4 * _shiftCount) : 0) +
            (IsWordAluModRmOpcode(Opcode) && !readOnlyWordAlu ? 1 : 0);
        bool isWordTransferPhase = !splitWordTransfer && (readPhase is >= 0 and <= 3 || !readOnlyWordAlu && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 2) ||
            splitWordTransfer && (readPhase is >= 0 and <= 7 || !readOnlyWordAlu && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 6);
        if (!isWordTransferPhase && TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle))
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : prefetchCycle;
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        if (!splitWordTransfer && readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickAlignedMemoryWordRead(readPhase);
        }
        else if (splitWordTransfer && readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryWordRead(readPhase, 0);
        }
        else if (splitWordTransfer && readPhase is >= 4 and <= 7)
        {
            LastBusCycle = TickMemoryWordRead(readPhase - 4, 1);
        }
        else if (!readOnlyWordAlu && !splitWordTransfer && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 2)
        {
            LastBusCycle = TickAlignedMemoryWordWrite(readPhase - writePhaseStart);
        }
        else if (!readOnlyWordAlu && splitWordTransfer && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 3)
        {
            LastBusCycle = TickMemoryWordWrite(readPhase - writePhaseStart, 0);
        }
        else if (!readOnlyWordAlu && splitWordTransfer && readPhase >= writePhaseStart + 4 && readPhase <= writePhaseStart + 6)
        {
            LastBusCycle = TickMemoryWordWrite(readPhase - writePhaseStart - 4, 1);
        }
        else
        {
            LastBusCycle = IdleCycle(consumesDisplacement ? 'S' : '-', consumesDisplacement ? displacementByte : (byte)0);
        }

        FinishMemoryByteIncDecIfNeeded();
    }

    private Intel8086BusCycle TickAlignedMemoryWordRead(int phase)
    {
        byte bhe = 0;
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _memoryAddress, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadAlignedMemoryWordData(bhe),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadAlignedMemoryWordData(byte bhe)
    {
        _memoryWordValue = (ushort)(_bus.Read(_memoryAddress) | (_bus.Read((_memoryAddress + 1) & 0xFFFFF) << 8));
        CalculateMemoryWordResult();
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, _memoryWordValue, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickAlignedMemoryWordWrite(int phase)
    {
        byte bhe = 0;
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _memoryAddress, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            _ => WriteAlignedMemoryWordData(bhe),
        };
    }

    private Intel8086BusCycle WriteAlignedMemoryWordData(byte bhe)
    {
        _bus.Write(_memoryAddress, (byte)_memoryWordResult);
        _bus.Write((_memoryAddress + 1) & 0xFFFFF, (byte)(_memoryWordResult >> 8));
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-AW", "---", bhe, _memoryWordResult, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMemoryWordRead(int phase, int byteIndex)
    {
        uint address = _memoryAddress + (uint)byteIndex & 0xFFFFF;
        byte bhe = (byte)((address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadMemoryWordData(address, bhe, byteIndex),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadMemoryWordData(uint address, byte bhe, int byteIndex)
    {
        byte value = _bus.Read(address);
        _memoryWordValue = byteIndex == 0
            ? (ushort)((_memoryWordValue & 0xFF00) | value)
            : (ushort)((_memoryWordValue & 0x00FF) | (value << 8));
        if (byteIndex == 1)
        {
            CalculateMemoryWordResult();
        }

        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMemoryWordWrite(int phase, int byteIndex)
    {
        uint address = (_memoryAddress + (uint)byteIndex) & 0xFFFFF;
        byte bhe = (byte)((address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteMemoryWordData(address, bhe, byteIndex),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteMemoryWordData(uint address, byte bhe, int byteIndex)
    {
        byte value = (byte)(_memoryWordResult >> (byteIndex * 8));
        _bus.Write(address, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void TickPrefixedMemoryByteIncDecStart()
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
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                : IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 4)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                : IdleCycle('S', _memoryModRm);
            return;
        }

        TickMemoryOperandPhase();
    }

    private void TickMemoryOperandPhase()
    {
        int mod = _memoryModRm >> 6;
        int displacementBytes = mod == 0
            ? (_memoryModRm & 7) == 6 ? 2 : 0
            : mod == 1 ? 1 : 2;
        int rm = _memoryModRm & 7;
        int effectiveAddressCycles = rm switch
        {
            0 or 3 => 7,
            1 or 2 => 8,
            4 or 5 or 7 => 5,
            6 => mod == 0 ? 6 : 5,
            _ => throw new InvalidOperationException("Invalid FE ModR/M register field."),
        };
        int displacementStart = mod == 0 && rm == 6 ? 6 : effectiveAddressCycles + 3;
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
        LastBusCycle = readPhase is >= 0 and <= 3
            ? TickMemoryRead(readPhase)
            : readPhase is >= 10 and <= 12
                ? TickMemoryWrite(readPhase - 10)
                : IdleCycle('-', 0);
        FinishMemoryByteIncDecIfNeeded();
    }

    private bool TryTickMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        if ((Opcode is 0xFF or 0xF7 or 0xD1 or 0xD3 || IsWordAluModRmOpcode(Opcode)) && _instructionPrefixLength == 0)
        {
            return TryTickWordMemoryPrefetch(out cycle);
        }

        if (_instructionPrefixLength != 0)
        {
            return TryTickPrefixedMemoryPrefetch(out cycle);
        }
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

    private bool TryTickWordMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int firstFetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        int secondFetchStart = mod switch
        {
            0 when rm == 6 => (_queueCountAtStart <= 5 ? 13 : 14) + ((_memoryAddress & 1) != 0 ? 4 : 0),
            1 or 2 when rm >= 4 => _queueCountAtStart <= 5
                ? 16 + ((_memoryAddress & 1) != 0 ? 4 : 0)
                : mod == 2 ? 17 + ((_memoryAddress & 1) != 0 ? 4 : 0) : 0,
            1 when _queueCountAtStart == 5 => 3 + 7 + (rm is 1 or 2 ? 1 : 0),
            2 => firstFetchStart + 7 + (rm is 1 or 2 ? 1 : 0),
            _ => 0,
        };
        bool secondFetchIsPossible = secondFetchStart != 0;
        bool readOnlyWordAlu = IsWordAluModRmOpcode(Opcode) && IsWordAluMemoryReadOnly(Opcode);
        bool wordAluFetchQueuesThreeBytes = readOnlyWordAlu && rm <= 3 &&
            (mod == 2 || mod == 1 && _queueCountAtStart == 5);
        bool isFirstFetch = _instructionTState >= firstFetchStart && _instructionTState < firstFetchStart + 4;
        int fetchStart = isFirstFetch ? firstFetchStart :
            secondFetchIsPossible && _instructionTState >= secondFetchStart && _instructionTState < secondFetchStart + 4
                ? secondFetchStart
                : -1;
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
            int bytesFetched = readOnlyWordAlu
                ? isFirstFetch
                    ? secondFetchIsPossible ? wordAluFetchQueuesThreeBytes ? 2 : 0 : 1
                    : 1
                : isFirstFetch && secondFetchIsPossible ? 2 : 1;
            for (int index = 0; index < bytesFetched && _state.QueueCount < _state.Queue.Length; index++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }

        return true;
    }

    private bool TryTickPrefixedMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        int mod = _memoryModRm >> 6;
        if (mod == 3)
        {
            int registerFetchStart = _queueCountAtStart <= 5 ? 3 : 5;
            if (_instructionTState < registerFetchStart || _instructionTState > registerFetchStart + 3)
            {
                cycle = default;
                return false;
            }

            int registerFetchPhase = _instructionTState - registerFetchStart;
            cycle = registerFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return true;
        }

        int index = ((_memoryModRm >> 6) * 16) + ((_memoryModRm & 7) * 2) + (_queueCountAtStart >= 6 ? 1 : 0);
        int second = PrefixedMemorySecondFetchStarts[index];
        int third = PrefixedMemoryThirdFetchStarts[index];
        int start;
        int phase;
        int firstFetch = _queueCountAtStart <= 5 ? 3 : 5;
        if (_instructionTState >= firstFetch && _instructionTState <= firstFetch + 3)
        {
            start = firstFetch;
            phase = _instructionTState - start;
        }
        else if (second != 0 && _instructionTState >= second && _instructionTState <= second + 3)
        {
            start = second;
            phase = _instructionTState - start;
        }
        else if (third != 0 && _instructionTState >= third && _instructionTState <= third + 3)
        {
            start = third;
            phase = _instructionTState - start;
        }
        else
        {
            cycle = default;
            return false;
        }

        cycle = phase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle(),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };
        if (phase == 2)
        {
            int lastFetchStart = third != 0 ? third : second != 0 ? second : firstFetch;
            int bytesFetched = start == lastFetchStart ? 1 : 2;
            for (int byteIndex = 0; byteIndex < bytesFetched && _state.QueueCount < _state.Queue.Length; byteIndex++)
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
        if (Opcode == 0xF6)
        {
            CalculateMemoryByteUnaryResult();
        }
        else if (Opcode is 0xD0 or 0xD2)
        {
            byte value = _memoryValue;
            ushort flags = _state.Flags;
            int extension = (_memoryModRm >> 3) & 7;
            int count = Opcode == 0xD0 ? 1 : _shiftCount;
            for (int index = 0; index < count; index++)
            {
                Intel8086AluResult result = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = result.Value;
                flags = result.Flags;
            }

            _memoryResult = value;
            if (count != 0)
            {
                _state.Flags = flags;
            }
        }
        else
        {
            Intel8086AluResult result = Intel8086Alu.ExecuteIncrementDecrement(
                _memoryValue,
                _state.Flags,
                ((_memoryModRm >> 3) & 7) == 1);
            _memoryResult = result.Value;
            _state.Flags = result.Flags;
        }

        ushort data = (ushort)((_memoryAddress & 1) == 0 ? _memoryValue : _memoryValue << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void CalculateMemoryByteUnaryResult()
    {
        Intel8086AluResult result = ((_memoryModRm >> 3) & 7) == 2
            ? Intel8086Alu.ExecuteNot(_memoryValue, _state.Flags)
            : Intel8086Alu.ExecuteNegate(_memoryValue, _state.Flags);
        _memoryResult = result.Value;
        _state.Flags = result.Flags;
    }

    private void CalculateMemoryWordResult()
    {
        if (IsWordAluModRmOpcode(Opcode))
        {
            int register = (_memoryModRm >> 3) & 7;
            bool directionToRegister = Opcode != 0x85 && (Opcode & 0x02) != 0;
            ushort registerValue = GetWordRegister(register);
            ushort left = directionToRegister ? registerValue : _memoryWordValue;
            ushort right = directionToRegister ? _memoryWordValue : registerValue;
            Intel8086AluOperation operation = GetRegisterDirectWordAluOperation(Opcode);
            Intel8086AluWordResult aluResult = Intel8086Alu.Execute(left, right, _state.Flags, operation);
            _memoryWordResult = aluResult.Value;
            _state.Flags = aluResult.Flags;
            if (directionToRegister && Opcode != 0x3B)
            {
                SetWordRegister(register, aluResult.Value);
            }

            return;
        }

        if (Opcode is 0xD1 or 0xD3)
        {
            ushort value = _memoryWordValue;
            ushort flags = _state.Flags;
            int extension = (_memoryModRm >> 3) & 7;
            int count = Opcode == 0xD1 ? 1 : _shiftCount;
            for (int index = 0; index < count; index++)
            {
                Intel8086AluWordResult shiftResult = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = shiftResult.Value;
                flags = shiftResult.Flags;
            }

            _memoryWordResult = value;
            if (count != 0)
            {
                _state.Flags = flags;
            }

            return;
        }

        if (Opcode == 0xF7)
        {
            Intel8086AluWordResult unaryResult = ((_memoryModRm >> 3) & 7) == 2
                ? Intel8086Alu.ExecuteNot(_memoryWordValue, _state.Flags)
                : Intel8086Alu.ExecuteNegate(_memoryWordValue, _state.Flags);
            _memoryWordResult = unaryResult.Value;
            _state.Flags = unaryResult.Flags;
            return;
        }

        Intel8086AluWordResult result = Intel8086Alu.ExecuteIncrementDecrement(
            _memoryWordValue,
            _state.Flags,
            ((_memoryModRm >> 3) & 7) == 1);
        _memoryWordResult = result.Value;
        _state.Flags = result.Flags;
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
