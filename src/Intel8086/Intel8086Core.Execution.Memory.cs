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

    private static readonly byte[] PrefixedUnalignedWordDivideErrorFetchStarts =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 19, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 22, 0, 22, 0, 22, 0, 22,
        24, 0, 0, 0, 0, 0, 24, 0, 0, 0, 0, 0, 0, 0, 0, 22,
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

    private void TickPrefixedRegisterWordIncDec()
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
            Intel8086AluWordResult result = Intel8086Alu.ExecuteIncrementDecrement(
                GetWordRegister(register), _state.Flags, decrement);
            SetWordRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
    }

    private void TickPrefixedRegisterUnary()
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
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle fetchCycle)
                ? fetchCycle
                : IdleCycle('-', 0);
            if (_state.QueueCount > 0)
            {
                PopQueue();
            }

            int register = _memoryModRm & 7;
            bool negate = ((_memoryModRm >> 3) & 7) == 3;
            if (Opcode == 0xF6)
            {
                Intel8086AluResult result = negate
                    ? Intel8086Alu.ExecuteNegate(GetByteRegister(register), _state.Flags)
                    : Intel8086Alu.ExecuteNot(GetByteRegister(register), _state.Flags);
                SetByteRegister(register, result.Value);
                _state.Flags = result.Flags;
            }
            else
            {
                Intel8086AluWordResult result = negate
                    ? Intel8086Alu.ExecuteNegate(GetWordRegister(register), _state.Flags)
                    : Intel8086Alu.ExecuteNot(GetWordRegister(register), _state.Flags);
                SetWordRegister(register, result.Value);
                _state.Flags = result.Flags;
            }

            _instructionTState = 0;
        }
    }

    private void TickPrefixedWordImmediateRegister()
    {
        _instructionTState++;
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

        bool hasPrefetch = TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle);
        if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = hasPrefetch
                ? prefetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                : IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 4)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = hasPrefetch
                ? prefetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                : IdleCycle('S', _memoryModRm);
        }
        else
        {
            bool consumesImmediate = _instructionTState == 5 || Opcode == 0x81 && _instructionTState == 6;
            byte immediate = 0;
            if (consumesImmediate)
            {
                immediate = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                if (Opcode == 0x83)
                {
                    _immediateWord = unchecked((ushort)(short)(sbyte)immediate);
                }
                else if (_instructionTState == 5)
                {
                    _immediateWord = immediate;
                }
                else
                {
                    _immediateWord = (ushort)(_immediateWord | (immediate << 8));
                }

                if (Opcode == 0x83 || _instructionTState == 6)
                {
                    Intel8086AluOperation operation = GetWordAluImmediateGroupOperation((_memoryModRm >> 3) & 7);
                    Intel8086AluWordResult result = Intel8086Alu.Execute(
                        GetWordRegister(_memoryModRm & 7), _immediateWord, _state.Flags, operation);
                    if (operation != Intel8086AluOperation.Compare)
                    {
                        SetWordRegister(_memoryModRm & 7, result.Value);
                    }

                    _state.Flags = result.Flags;
                }
            }

            LastBusCycle = hasPrefetch
                ? prefetchCycle with { QueueStatus = consumesImmediate ? 'S' : '-', QueueData = consumesImmediate ? immediate : (byte)0 }
                : consumesImmediate ? IdleCycle('S', immediate) : IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5 && _state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private void TickMemoryByteIncDec()
    {
        if (_segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 2 &&
            Opcode is 0xF6 or 0xF7 &&
            ((_memoryModRm >> 3) & 7) == 7 &&
            !_combinedDividePrefixPreludeComplete)
        {
            TickCombinedSegmentRepeatDividePrefix();
            return;
        }

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

        bool testImmediate = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
        int immediateStart = _memoryReadStart + 6;
        bool consumesImmediate = testImmediate && _instructionTState == immediateStart;
        byte immediateByte = 0;
        if (consumesImmediate)
        {
            immediateByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _immediateWord = immediateByte;
            CalculateMemoryTestImmediate();
        }

        int pendingReadPhase = _instructionTState - _memoryReadStart;
        bool memorySpecialReadCycle = (IsMemoryDivideOpcode(Opcode, _memoryModRm) || IsMemoryMultiplyOpcode(Opcode, _memoryModRm)) && pendingReadPhase is >= 0 and <= 3;
        if (!memorySpecialReadCycle && TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle))
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : consumesImmediate
                    ? prefetchCycle with { QueueStatus = 'S', QueueData = immediateByte }
                : prefetchCycle;
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        if (consumesDisplacement || consumesImmediate)
        {
            LastBusCycle = IdleCycle('S', consumesDisplacement ? displacementByte : immediateByte);
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        int readPhase = _instructionTState - _memoryReadStart;
        int writePhaseStart = IsMemoryDivideOpcode(Opcode, _memoryModRm) || IsMemoryMultiplyOpcode(Opcode, _memoryModRm)
            ? int.MaxValue
            : Opcode == 0xD2 ? 15 + (4 * _shiftCount) : 10;
        if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryRead(readPhase);
            if (readPhase == 3)
            {
                CompleteMemoryDivideFaultIfNeeded();
            }
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
        if (_segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 2 &&
            Opcode == 0xF7 &&
            ((_memoryModRm >> 3) & 7) == 7 &&
            !_combinedDividePrefixPreludeComplete)
        {
            TickCombinedSegmentRepeatDividePrefix();
            return;
        }

        _instructionTState++;
        bool memoryPush = IsGroupPushRm16(Opcode, _memoryModRm);
        uint pushStackAddress = PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2)));
        int pushStackWriteStart = _memoryReadStart + ((_memoryAddress & 1) == 0 ? 11 : 15);
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
                LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                    ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                    : IdleCycle('F', Opcode);
            }
            else
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                    ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                    : IdleCycle('S', _memoryModRm);
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
        int displacementStart = _instructionPrefixLength != 0
            ? mod == 0 && rm == 6 ? 6 : effectiveAddressCycles + 3
            : mod == 0 && rm == 6 ? 4 : effectiveAddressCycles + 1;
        int displacementIndex = _instructionTState - displacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < displacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        bool testImmediate = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
        int immediateByteCount = testImmediate ? Opcode == 0xF7 ? 2 : 1 : Opcode == 0x81 ? 2 : 1;
        int immediateStart = _memoryReadStart + ((_memoryAddress & 1) != 0 ? 10 : 6);
        int immediateIndex = _instructionTState - immediateStart;
        bool consumesImmediate = (IsWordAluImmediateGroupOpcode(Opcode) || testImmediate) && immediateIndex >= 0 && immediateIndex < immediateByteCount;
        byte immediateByte = 0;
        if (consumesImmediate)
        {
            immediateByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            if (testImmediate && Opcode == 0xF6)
            {
                _immediateWord = immediateByte;
                CalculateMemoryTestImmediate();
            }
            else if (immediateIndex == 0)
            {
                _immediateWord = Opcode == 0x83
                    ? unchecked((ushort)(short)(sbyte)immediateByte)
                    : immediateByte;
            }
            else
            {
                _immediateWord = (ushort)(_immediateWord | (immediateByte << 8));
            }

            if (immediateIndex == immediateByteCount - 1)
            {
                if (testImmediate)
                {
                    CalculateMemoryTestImmediate();
                }
                else
                {
                    CalculateMemoryImmediateWordResult();
                }
            }
        }

        int readPhase = _instructionTState - _memoryReadStart;
        bool splitWordTransfer = (_memoryAddress & 1) != 0;
        bool readOnlyWordAlu = IsMemoryDivideOpcode(Opcode, _memoryModRm) ||
            IsMemoryMultiplyOpcode(Opcode, _memoryModRm) ||
            IsGroupPushRm16(Opcode, _memoryModRm) ||
            IsEscOpcode(Opcode) ||
            IsWordAluModRmOpcode(Opcode) && IsWordAluMemoryReadOnly(Opcode) ||
            IsWordAluImmediateGroupOpcode(Opcode) && ((_memoryModRm >> 3) & 7) == 7 ||
            testImmediate && Opcode == 0xF7;
        bool wordImmediateWriteQueueAdjustment = Opcode == 0x81 &&
            (mod == 0 && _queueCountAtStart == 6 || mod == 1 && _queueCountAtStart == 5 || mod == 2 && _queueCountAtStart == 6);
        int writePhaseStart = (splitWordTransfer ? 14 : 10) + (Opcode == 0xD3 ? 5 + (4 * _shiftCount) : 0) +
            (IsWordAluModRmOpcode(Opcode) && !readOnlyWordAlu ? 1 : 0) +
            (IsWordAluImmediateGroupOpcode(Opcode) && !readOnlyWordAlu ? 2 + (wordImmediateWriteQueueAdjustment ? 1 : 0) : 0);
        if (_instructionPrefixLength != 0 && IsWordAluImmediateGroupOpcode(Opcode) && !readOnlyWordAlu)
        {
            writePhaseStart = GetPrefixedWordImmediateWriteStart(0) - _memoryReadStart;
        }

        bool isStackWritePhase = memoryPush && _instructionTState >= pushStackWriteStart && _instructionTState <= _instructionCycles;
        bool isWordTransferPhase = isStackWritePhase ||
            !splitWordTransfer && (readPhase is >= 0 and <= 3 || !readOnlyWordAlu && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 2) ||
            splitWordTransfer && (readPhase is >= 0 and <= 7 || !readOnlyWordAlu && readPhase >= writePhaseStart && readPhase <= writePhaseStart + 6);
        Intel8086BusCycle prefetchCycle = default;
        bool hasPrefetchCycle = !isWordTransferPhase && TryTickMemoryPrefetch(out prefetchCycle);
        bool preservePrefetchDuringImmediate = (_memoryAddress & 1) != 0 || Opcode == 0x81 || testImmediate;
        if (hasPrefetchCycle && (!consumesImmediate || prefetchCycle.TState == "T3" || preservePrefetchDuringImmediate))
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : consumesImmediate
                    ? prefetchCycle with { QueueStatus = 'S', QueueData = immediateByte }
                : prefetchCycle;
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        if (!splitWordTransfer && readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickAlignedMemoryWordRead(readPhase);
            if (readPhase == 3)
            {
                CompleteMemoryDivideFaultIfNeeded();
            }
        }
        else if (splitWordTransfer && readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryWordRead(readPhase, 0);
        }
        else if (splitWordTransfer && readPhase is >= 4 and <= 7)
        {
            LastBusCycle = TickMemoryWordRead(readPhase - 4, 1);
            if (readPhase == 7)
            {
                CompleteMemoryDivideFaultIfNeeded();
            }
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
        else if (isStackWritePhase)
        {
            LastBusCycle = TickMemoryPushStackWrite(_instructionTState - pushStackWriteStart, pushStackAddress);
        }
        else
        {
            LastBusCycle = IdleCycle(consumesDisplacement ? 'S' : '-', consumesDisplacement ? displacementByte : (byte)0);
        }

        if (consumesImmediate)
        {
            LastBusCycle = IdleCycle('S', immediateByte);
        }

        if (memoryPush)
        {
            if (_instructionTState == _instructionCycles)
            {
                _state.SP = unchecked((ushort)(_state.SP - 2));
                _instructionTState = 0;
            }
        }
        else
        {
            FinishMemoryByteIncDecIfNeeded();
        }
    }

    private Intel8086BusCycle TickMemoryPushStackWrite(int phase, uint stackAddress)
    {
        ushort stackOffset = unchecked((ushort)(_state.SP - 2));
        if ((stackAddress & 1) == 0)
        {
            return phase switch
            {
                0 => new Intel8086BusCycle(1, stackAddress, "--", "---", "---", 0, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, 0, "SS", "-A-", "---", 0, 0, "MEMW", "T2", '-', 0),
                _ => WriteMemoryPushStackWord(stackAddress),
            };
        }

        int byteIndex = phase / 4;
        int busPhase = phase % 4;
        uint address = PhysicalAddress(_state.SS, unchecked((ushort)(stackOffset + byteIndex)));
        byte lane = (byte)((address & 1) == 0 ? 1 : 0);
        return busPhase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", lane, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "SS", "-A-", "---", lane, 0, "MEMW", "T2", '-', 0),
            2 => WriteMemoryPushStackByte(address, byteIndex, lane),
            _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", lane, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteMemoryPushStackWord(uint address)
    {
        _bus.Write(address, (byte)_memoryWordValue);
        _bus.Write((address + 1) & 0xFFFFF, (byte)(_memoryWordValue >> 8));
        return new Intel8086BusCycle(0, 0, "SS", "-AW", "---", 0, _memoryWordValue, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle WriteMemoryPushStackByte(uint address, int byteIndex, byte lane)
    {
        byte value = (byte)(_memoryWordValue >> (byteIndex * 8));
        _bus.Write(address, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "SS", "-AW", "---", lane, data, "PASV", "T3", '-', 0);
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
        uint address = GetMemoryWordByteAddress(byteIndex);
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
        uint address = GetMemoryWordByteAddress(byteIndex);
        byte bhe = (byte)((address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteMemoryWordData(address, bhe, byteIndex),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private uint GetMemoryWordByteAddress(int byteIndex)
    {
        if (!IsTestImmediateGroupOpcode(Opcode, _memoryModRm))
        {
            return (_memoryAddress + (uint)byteIndex) & 0xFFFFF;
        }

        ushort segment = _memorySegmentStatus switch
        {
            "ES" => _state.ES,
            "CS" => _state.CS,
            "SS" => _state.SS,
            "DS" => _state.DS,
            _ => throw new InvalidOperationException("Nieznany segment transferu słowa."),
        };
        uint segmentBase = (uint)segment << 4;
        ushort offset = unchecked((ushort)(_memoryAddress - segmentBase));
        return PhysicalAddress(segment, unchecked((ushort)(offset + byteIndex)));
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

        bool testImmediate = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
        int immediateByteCount = testImmediate && Opcode == 0xF7 ? 2 : 1;
        int immediateStart = _memoryReadStart + ((_memoryAddress & 1) != 0 && Opcode == 0xF7 ? 10 : 6);
        int immediateIndex = _instructionTState - immediateStart;
        bool consumesImmediate = testImmediate && immediateIndex >= 0 && immediateIndex < immediateByteCount;
        byte immediateByte = 0;
        if (consumesImmediate)
        {
            immediateByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _immediateWord = immediateIndex == 0
                ? immediateByte
                : (ushort)(_immediateWord | (immediateByte << 8));
            if (immediateIndex == immediateByteCount - 1)
            {
                CalculateMemoryTestImmediate();
            }
        }

        if (TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle))
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : consumesImmediate
                    ? prefetchCycle with { QueueStatus = 'S', QueueData = immediateByte }
                : prefetchCycle;
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        if (consumesDisplacement || consumesImmediate)
        {
            LastBusCycle = IdleCycle('S', consumesDisplacement ? displacementByte : immediateByte);
            FinishMemoryByteIncDecIfNeeded();
            return;
        }

        int readPhase = _instructionTState - _memoryReadStart;
        int writePhaseStart = IsMemoryDivideOpcode(Opcode, _memoryModRm) || IsMemoryMultiplyOpcode(Opcode, _memoryModRm)
            ? int.MaxValue
            : 10;
        if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = TickMemoryRead(readPhase);
            if (readPhase == 3)
            {
                CompleteMemoryDivideFaultIfNeeded();
            }
        }
        else
        {
            LastBusCycle = readPhase >= writePhaseStart && readPhase <= writePhaseStart + 2
                ? TickMemoryWrite(readPhase - writePhaseStart)
                : IdleCycle('-', 0);
        }

        FinishMemoryByteIncDecIfNeeded();
    }

    private bool TryTickMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        if (IsTestImmediateGroupOpcode(Opcode, _memoryModRm))
        {
            return TryTickTestImmediatePrefetch(out cycle);
        }

        if ((Opcode is 0xFF or 0xF7 or 0xD1 or 0xD3 || IsAnyWordAluModRmOpcode(Opcode) || IsEscOpcode(Opcode)) && _instructionPrefixLength == 0)
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
        bool testImmediate = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
        bool readOnlyWordAlu = IsWordAluModRmOpcode(Opcode) && IsWordAluMemoryReadOnly(Opcode) ||
            IsWordAluImmediateGroupOpcode(Opcode) && ((_memoryModRm >> 3) & 7) == 7 ||
            testImmediate && Opcode == 0xF7;
        bool wordAluFetchQueuesThreeBytes = readOnlyWordAlu && rm <= 3 &&
            (mod == 2 || mod == 1 && _queueCountAtStart == 5);
        bool wordImmediateByteMissing = Opcode == 0x81 && _queueCountAtStart == 5 &&
            (mod == 2 || mod == 0 && rm == 6);
        bool wordImmediateDirectDisplacementNeedsPrefetch = IsWordAluImmediateGroupOpcode(Opcode) &&
            Opcode == 0x83 && mod == 0 && rm == 6 && _queueCountAtStart <= 6;
        bool wordImmediateDirectWordReadOnlyFullQueue = IsWordAluImmediateGroupOpcode(Opcode) &&
            Opcode == 0x81 && readOnlyWordAlu && mod == 0 && rm == 6 && _queueCountAtStart == 6;
        bool wordImmediateReadOnlyShortQueue = IsWordAluImmediateGroupOpcode(Opcode) && readOnlyWordAlu &&
            (mod == 2 && rm >= 4 && _queueCountAtStart == 6 ||
                Opcode == 0x83 && mod == 2 && rm >= 4 && _queueCountAtStart == 5);
        bool wordImmediateReadOnlyModOneShortQueue = IsWordAluImmediateGroupOpcode(Opcode) && readOnlyWordAlu &&
            mod == 1 && rm >= 4 && _queueCountAtStart == 5;
        int additionalFetchStart = GetWordImmediateAdditionalFetchStart(mod, rm);
        bool isFirstFetch = _instructionTState >= firstFetchStart && _instructionTState < firstFetchStart + 4;
        bool isSecondFetch = !isFirstFetch && secondFetchIsPossible &&
            _instructionTState >= secondFetchStart && _instructionTState < secondFetchStart + 4;
        bool isAdditionalFetch = !isFirstFetch && !isSecondFetch && additionalFetchStart != 0 &&
            _instructionTState >= additionalFetchStart && _instructionTState < additionalFetchStart + 4;
        int fetchStart = isFirstFetch ? firstFetchStart : isSecondFetch ? secondFetchStart : isAdditionalFetch ? additionalFetchStart : -1;
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
            2 => IsWordAluImmediateGroupOpcode(Opcode) || testImmediate && Opcode == 0xF7
                ? FetchWordImmediateDataCycle()
                : FetchDataCycle(),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };
        if (fetchPhase == 2)
        {
            int bytesFetched = IsEscOpcode(Opcode) && isFirstFetch && (_memoryModRm & 7) >= 4
                ? 1
                : IsWordAluImmediateGroupOpcode(Opcode) && !readOnlyWordAlu
                ? isFirstFetch
                    ? Opcode == 0x83 && mod == 0 && rm != 6 && _queueCountAtStart == 6 ? 1 : 2
                    : isAdditionalFetch &&
                        ((mod is 1 or 2) && _queueCountAtStart == 5 || mod == 0 && rm == 6 &&
                            _queueCountAtStart == 5 || Opcode == 0x81 && mod == 2 &&
                            _queueCountAtStart == 6 && rm <= 3)
                            ? 2
                            : 1
                : readOnlyWordAlu
                    ? isFirstFetch
                        ? secondFetchIsPossible
                            ? wordAluFetchQueuesThreeBytes || wordImmediateDirectWordReadOnlyFullQueue ? 2 : (wordImmediateByteMissing ||
                                wordImmediateReadOnlyModOneShortQueue ||
                                wordImmediateDirectDisplacementNeedsPrefetch || wordImmediateReadOnlyShortQueue) ? 1 : 0
                            : 1
                        : (wordImmediateDirectDisplacementNeedsPrefetch && !isFirstFetch ||
                            Opcode == 0x81 && mod == 0 && rm == 6 && _queueCountAtStart == 5 &&
                                wordImmediateByteMissing && !isFirstFetch ||
                            Opcode == 0x81 && mod == 0 && rm == 6 && _queueCountAtStart == 5 && isAdditionalFetch ||
                            Opcode == 0x81 && mod == 2 && _queueCountAtStart == 5 &&
                                (rm >= 4 && isSecondFetch ||
                                    (_memoryAddress & 1) == 0 && isAdditionalFetch) ||
                            wordImmediateReadOnlyShortQueue || wordImmediateReadOnlyModOneShortQueue)
                            ? 2
                            : 1
                    : isFirstFetch && secondFetchIsPossible ? 2 : 1;
            for (int index = 0; index < bytesFetched && _state.QueueCount < _state.Queue.Length; index++)
            {
                byte value = IsWordAluImmediateGroupOpcode(Opcode) || testImmediate && Opcode == 0xF7
                    ? ReadWordImmediatePrefetchByte((_nextFetchAddress + (uint)index) & 0xFFFFF)
                    : (byte)0x90;
                PushQueue(value);
            }

            _nextFetchAddress = AdvanceInstructionPrefetchWord(_nextFetchAddress);
        }

        return true;
    }

    private Intel8086BusCycle FetchWordImmediateDataCycle()
    {
        byte low = ReadWordImmediatePrefetchByte(_nextFetchAddress);
        byte high = ReadWordImmediatePrefetchByte((_nextFetchAddress + 1) & 0xFFFFF);
        ushort data = (ushort)(low | (high << 8));
        return new Intel8086BusCycle(0, 0, "CS", "R--", "---", 0, data, "PASV", "T3", '-', 0);
    }

    private byte ReadWordImmediatePrefetchByte(uint address)
    {
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int displacementBytes = mod == 3 ? 0 : mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        int immediateByteCount = IsTestImmediateGroupOpcode(Opcode, _memoryModRm)
            ? Opcode == 0xF7 ? 2 : 1
            : Opcode == 0x81 ? 2 : 1;
        int instructionLength = _instructionPrefixLength + 2 + displacementBytes + immediateByteCount;
        uint instructionStart = PhysicalAddress(InstructionCs, InstructionPc);
        uint offset = (address - instructionStart) & 0xFFFFF;
        return offset < instructionLength ? _bus.Read(address) : (byte)0x90;
    }

    private uint AdvanceInstructionPrefetchWord(uint physicalAddress) =>
        AdvanceCodePrefetch(InstructionCs, physicalAddress, 2);

    private static uint AdvanceCodePrefetch(ushort segment, uint physicalAddress, uint byteCount)
    {
        uint segmentBase = (uint)segment << 4;
        ushort offset = (ushort)((physicalAddress - segmentBase) & 0xFFFF);
        return PhysicalAddress(segment, unchecked((ushort)(offset + byteCount)));
    }

    private int GetWordImmediateAdditionalFetchStart(int mod, int rm)
    {
        if (!IsWordAluImmediateGroupOpcode(Opcode))
        {
            return 0;
        }

        bool queueHasFiveBytes = _queueCountAtStart <= 5;
        bool needsAdditionalFetch = mod switch
        {
            0 => queueHasFiveBytes || Opcode == 0x81 && rm != 6,
            1 => Opcode == 0x81 ? queueHasFiveBytes && rm <= 3 || !queueHasFiveBytes : !queueHasFiveBytes,
            2 => queueHasFiveBytes || Opcode == 0x81 && rm <= 3,
            _ => false,
        };
        if (!needsAdditionalFetch)
        {
            return 0;
        }

        int queueTimingAdjustment = Opcode == 0x81 &&
            (mod == 0 && !queueHasFiveBytes || mod == 1 && queueHasFiveBytes || mod == 2 && !queueHasFiveBytes)
                ? 1
                : 0;
        int readStart = _memoryReadStart;
        return readStart + 8 + ((_memoryAddress & 1) != 0 ? 4 : 0) + queueTimingAdjustment;
    }

    private bool TryTickPrefixedMemoryPrefetch(out Intel8086BusCycle cycle)
    {
        int mod = _memoryModRm >> 6;
        if (mod == 3)
        {
            int registerFetchStart = _queueCountAtStart <= 5 ? 3 : 5;
            bool testImmediate = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
            int registerSecondFetchStart = IsGroupPushRm16(Opcode, _memoryModRm) && _queueCountAtStart <= 5
                ? 7
                : testImmediate && _queueCountAtStart <= 5
                ? 7
                : IsWordAluImmediateGroupOpcode(Opcode)
                ? GetPrefixedWordImmediateRegisterSecondFetchStart()
                : 0;
            int fetchStart = _instructionTState >= registerFetchStart && _instructionTState <= registerFetchStart + 3
                ? registerFetchStart
                : registerSecondFetchStart != 0 &&
                    _instructionTState >= registerSecondFetchStart && _instructionTState <= registerSecondFetchStart + 3
                    ? registerSecondFetchStart
                    : 0;
            if (fetchStart == 0)
            {
                cycle = default;
                return false;
            }

            int registerFetchPhase = _instructionTState - fetchStart;
            cycle = registerFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => IsWordAluImmediateGroupOpcode(Opcode) || testImmediate && Opcode == 0xF7
                    ? FetchWordImmediateDataCycle()
                    : FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };

            bool groupPushRegister = IsGroupPushRm16(Opcode, _memoryModRm);
            if (registerFetchPhase == 2 && (IsWordAluImmediateGroupOpcode(Opcode) || groupPushRegister || testImmediate))
            {
                int lastCompletedFetchStart = groupPushRegister
                    ? registerSecondFetchStart != 0 ? registerSecondFetchStart : registerFetchStart
                    : registerFetchStart + 2 <= _instructionCycles ? registerFetchStart : 0;
                if (!groupPushRegister && registerSecondFetchStart != 0 && registerSecondFetchStart + 2 <= _instructionCycles)
                {
                    lastCompletedFetchStart = registerSecondFetchStart;
                }

                int bytesFetched = fetchStart == lastCompletedFetchStart ? 1 : 2;
                for (int byteIndex = 0; byteIndex < bytesFetched && _state.QueueCount < _state.Queue.Length; byteIndex++)
                {
                    byte fetchedByte = (byte)(IsWordAluImmediateGroupOpcode(Opcode) || testImmediate && Opcode == 0xF7
                        ? ReadWordImmediatePrefetchByte((_nextFetchAddress + (uint)byteIndex) & 0xFFFFF)
                        : 0x90);
                    PushQueue(fetchedByte);
                }

                _nextFetchAddress = IsWordAluImmediateGroupOpcode(Opcode) || testImmediate && Opcode == 0xF7
                    ? AdvanceInstructionPrefetchWord(_nextFetchAddress)
                    : (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return true;
        }

        int firstFetch = _queueCountAtStart <= 5 ? 3 : 5;
        bool isPrefixedWordImmediate = IsWordAluImmediateGroupOpcode(Opcode);
        int index = ((_memoryModRm >> 6) * 16) + ((_memoryModRm & 7) * 2) + (_queueCountAtStart >= 6 ? 1 : 0);
        int second = isPrefixedWordImmediate
            ? GetPrefixedWordImmediateFetchStart(0)
            : PrefixedMemorySecondFetchStarts[index];
        int third = isPrefixedWordImmediate
            ? GetPrefixedWordImmediateFetchStart(1)
            : PrefixedMemoryThirdFetchStarts[index];
        int fourth = isPrefixedWordImmediate ? GetPrefixedWordImmediateFetchStart(2) : 0;
        bool isTestImmediateGroup = IsTestImmediateGroupOpcode(Opcode, _memoryModRm);
        bool isWordMemoryRead =
            Opcode == 0xFF && ((_memoryModRm >> 3) & 7) is 0 or 1 or 6 or 7 ||
            Opcode == 0xF7 && ((_memoryModRm >> 3) & 7) is >= 2 and <= 7 ||
            isTestImmediateGroup && Opcode == 0xF7;
        if (isWordMemoryRead && second != 0)
        {
            int readTransferEnd = _memoryReadStart + ((_memoryAddress & 1) == 0 ? 4 : 8);
            if (second >= _memoryReadStart && second < readTransferEnd)
            {
                second = readTransferEnd;
            }

            if (third >= _memoryReadStart && third < readTransferEnd)
            {
                third = readTransferEnd;
            }
        }


        if (_softwareInterruptIsDivideError && Opcode == 0xF7 && (_memoryAddress & 1) != 0)
        {
            int divideErrorFetchStart = PrefixedUnalignedWordDivideErrorFetchStarts[index];
            if (divideErrorFetchStart != 0)
            {
                third = divideErrorFetchStart;
            }
        }

        int start;
        int phase;
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
        else if (fourth != 0 && _instructionTState >= fourth && _instructionTState <= fourth + 3)
        {
            start = fourth;
            phase = _instructionTState - start;
        }
        else
        {
            cycle = default;
            return false;
        }

        bool isCombinedSegmentRepeatDivide =
            _segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 2 && Opcode is 0xF6 or 0xF7;
        bool isPrefixedByteAluImmediate = _instructionPrefixLength == 1 && Opcode is 0x80 or 0x82;
        bool isPrefixedTestImmediate = _instructionPrefixLength == 1 && isTestImmediateGroup;
        cycle = phase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => isCombinedSegmentRepeatDivide || isPrefixedByteAluImmediate || isPrefixedTestImmediate && Opcode == 0xF6
                ? FetchPrefixedCodeDataCycle()
                : isPrefixedWordImmediate || isPrefixedTestImmediate && Opcode == 0xF7
                    ? FetchWordImmediateDataCycle()
                    : FetchDataCycle(),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };
        if (phase == 2)
        {
            int lastFetchStart;
            if (isPrefixedWordImmediate)
            {
                lastFetchStart = firstFetch + 2 <= _instructionCycles ? firstFetch : 0;
                if (second != 0 && second + 2 <= _instructionCycles)
                {
                    lastFetchStart = second;
                }

                if (third != 0 && third + 2 <= _instructionCycles)
                {
                    lastFetchStart = third;
                }

                if (fourth != 0 && fourth + 2 <= _instructionCycles)
                {
                    lastFetchStart = fourth;
                }
            }
            else
            {
                lastFetchStart = fourth != 0 ? fourth : third != 0 ? third : second != 0 ? second : firstFetch;
            }

            int bytesFetched = start == lastFetchStart ? 1 : 2;
            for (int byteIndex = 0; byteIndex < bytesFetched && _state.QueueCount < _state.Queue.Length; byteIndex++)
            {
                byte fetchedByte = isCombinedSegmentRepeatDivide || isPrefixedByteAluImmediate || isPrefixedTestImmediate && Opcode == 0xF6
                    ? ReadPrefixedCodeByte((_nextFetchAddress + (uint)byteIndex) & 0xFFFFF)
                    : isPrefixedWordImmediate || isPrefixedTestImmediate && Opcode == 0xF7
                        ? ReadWordImmediatePrefetchByte((_nextFetchAddress + (uint)byteIndex) & 0xFFFFF)
                    : (byte)0x90;
                PushQueue(fetchedByte);
            }

            _nextFetchAddress = isPrefixedWordImmediate || isPrefixedTestImmediate && Opcode == 0xF7
                ? AdvanceInstructionPrefetchWord(_nextFetchAddress)
                : (_nextFetchAddress + 2) & 0xFFFFF;
        }

        return true;
    }

    private Intel8086BusCycle FetchPrefixedCodeDataCycle()
    {
        byte low = ReadPrefixedCodeByte(_nextFetchAddress);
        byte high = ReadPrefixedCodeByte((_nextFetchAddress + 1) & 0xFFFFF);
        ushort data = (ushort)(low | (high << 8));
        return new Intel8086BusCycle(0, 0, "CS", "R--", "---", 0, data, "PASV", "T3", '-', 0);
    }

    private byte ReadPrefixedCodeByte(uint address)
    {
        int mod = _memoryModRm >> 6;
        int rm = _memoryModRm & 7;
        int displacementBytes = mod == 3 ? 0 : mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        int instructionLength = _instructionPrefixLength + 2 + displacementBytes +
            (Opcode is 0x80 or 0x82 || IsTestImmediateGroupOpcode(Opcode, _memoryModRm) ? 1 : 0);
        uint instructionStart = PhysicalAddress(InstructionCs, InstructionPc);
        uint offset = (address - instructionStart) & 0xFFFFF;
        return offset < instructionLength ? _bus.Read(address) : (byte)0x90;
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
            if (IsMemoryDivideOpcode(Opcode, _memoryModRm))
            {
                PrepareMemoryDivide(_memoryValue);
            }
            else if (IsMemoryMultiplyOpcode(Opcode, _memoryModRm))
            {
                PrepareMemoryMultiply(_memoryValue);
            }
            else if (IsTestImmediateGroupOpcode(Opcode, _memoryModRm))
            {
                _memoryResult = _memoryValue;
            }
            else
            {
                CalculateMemoryByteUnaryResult();
            }
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
        if (IsGroupPushRm16(Opcode, _memoryModRm))
        {
            _memoryWordResult = _memoryWordValue;
            return;
        }

        if (IsEscOpcode(Opcode))
        {
            return;
        }

        if (IsWordAluImmediateGroupOpcode(Opcode))
        {
            return;
        }

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
            if (IsTestImmediateGroupOpcode(Opcode, _memoryModRm))
            {
                _memoryWordResult = _memoryWordValue;
                return;
            }

            if (IsMemoryDivideOpcode(Opcode, _memoryModRm))
            {
                PrepareMemoryDivide(_memoryWordValue);
                return;
            }

            if (IsMemoryMultiplyOpcode(Opcode, _memoryModRm))
            {
                PrepareMemoryMultiply(_memoryWordValue);
                return;
            }

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
            CompleteMemoryDivideIfNeeded();
            CompleteMemoryMultiplyIfNeeded();
            if (IsTestImmediateGroupOpcode(Opcode, _memoryModRm))
            {
                ApplyTestImmediateQueueProfile();
            }

            _instructionTState = 0;
        }
    }

    private void CalculateMemoryImmediateWordResult()
    {
        Intel8086AluOperation operation = GetWordAluImmediateGroupOperation((_memoryModRm >> 3) & 7);
        Intel8086AluWordResult result = Intel8086Alu.Execute(_memoryWordValue, _immediateWord, _state.Flags, operation);
        _memoryWordResult = result.Value;
        _state.Flags = result.Flags;
    }
}
