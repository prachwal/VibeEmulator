namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private int _divideErrorPrefetchStart;

    private byte _softwareInterruptVector;
    private ushort _softwareInterruptReturnIp;
    private ushort _softwareInterruptTargetIp;
    private ushort _softwareInterruptTargetCs;
    private ushort _softwareInterruptSavedFlags;
    private ushort _softwareInterruptInitialSp;
    private int _softwareInterruptEntryStart;
    private int _softwareInterruptFirstWriteStart;
    private int _softwareInterruptSecondWriteStart;
    private int _softwareInterruptFlush;
    private int _softwareInterruptThirdWriteStart;
    private int _softwareInterruptEnd;
    private bool _softwareInterruptTaken;
    private bool _softwareInterruptIsDivideError;
    private bool _softwareInterruptIsHardware;

    private static bool IsSoftwareInterruptOpcode(byte opcode) => opcode is 0xCC or 0xCD or 0xCE;

    private void InitializeSoftwareInterrupt(byte opcode)
    {
        _softwareInterruptIsDivideError = false;
        _softwareInterruptTaken = opcode != 0xCE || (_state.Flags & 0x0800) != 0;
        if (!_softwareInterruptTaken)
        {
            _instructionCycles = 4;
            return;
        }

        _softwareInterruptVector = opcode switch
        {
            0xCC => 3,
            0xCE => 4,
            _ => _state.Queue[_state.QueueHead + 1],
        };
        _softwareInterruptReturnIp = unchecked((ushort)(_state.IP + (opcode == 0xCD ? 2 : 1)));
        _softwareInterruptSavedFlags = _state.Flags;
        _softwareInterruptInitialSp = _state.SP;
        _softwareInterruptEntryStart = opcode switch
        {
            0xCD => _queueCountAtStart <= 5 ? 12 : 14,
            0xCC => 12,
            _ => 13,
        };

        uint firstStackAddress = PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2)));
        bool alignedStack = (firstStackAddress & 1) == 0;
        _softwareInterruptFirstWriteStart = _softwareInterruptEntryStart + 15;
        _softwareInterruptSecondWriteStart = _softwareInterruptFirstWriteStart + (alignedStack ? 10 : 14);
        _softwareInterruptFlush = _softwareInterruptSecondWriteStart + (alignedStack ? 7 : 11);
        _softwareInterruptThirdWriteStart = _softwareInterruptFlush + 6;
        _softwareInterruptEnd = _softwareInterruptThirdWriteStart + (alignedStack ? 2 : 6);
        _instructionCycles = _softwareInterruptEnd;
    }

    private void InitializeDivideError(int entryStart = 20)
    {
        _divideErrorPrefetchStart = 0;
        _softwareInterruptIsDivideError = true;
        _softwareInterruptTaken = true;
        _softwareInterruptVector = 0;
        _softwareInterruptReturnIp = unchecked((ushort)(_state.IP + 2 + _instructionPrefixLength));
        _softwareInterruptSavedFlags = _state.Flags;
        _softwareInterruptInitialSp = _state.SP;
        _softwareInterruptEntryStart = entryStart;

        bool alignedStack = (PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2))) & 1) == 0;
        _softwareInterruptFirstWriteStart = _softwareInterruptEntryStart + 15;
        _softwareInterruptSecondWriteStart = _softwareInterruptFirstWriteStart + (alignedStack ? 10 : 14);
        _softwareInterruptFlush = _softwareInterruptSecondWriteStart + (alignedStack ? 7 : 11);
        _softwareInterruptThirdWriteStart = _softwareInterruptFlush + 6;
        _softwareInterruptEnd = _softwareInterruptThirdWriteStart + (alignedStack ? 2 : 6);
        _instructionCycles = _softwareInterruptEnd;
    }

    private void TickSoftwareInterrupt()
    {
        _instructionTState++;
        bool combinedSegmentRepeatDivide = _softwareInterruptIsDivideError &&
            _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2;
        bool startsWithShortPrefetchQueue = combinedSegmentRepeatDivide
            ? _queueCountAtStart > 5
            : _queueCountAtStart <= 5;
        if (_instructionTState == 1)
        {
            if (_softwareInterruptIsHardware)
            {
                LastBusCycle = IdleCycle('-', 0);
            }
            else
            {
                PopQueue();
                _state.IP++;
                byte firstInstructionByte = (byte)(_softwareInterruptIsDivideError &&
                    _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2
                    ? _segmentOverride switch
                    {
                        Intel8086SegmentOverride.Es => 0x26,
                        Intel8086SegmentOverride.Cs => 0x2E,
                        Intel8086SegmentOverride.Ss => 0x36,
                        Intel8086SegmentOverride.Ds => 0x3E,
                        _ => 0,
                    }
                    : _instructionPrefixLength != 0 ? _instructionPrefix : Opcode);
                LastBusCycle = IdleCycle('F', firstInstructionByte);
            }

            return;
        }

        if (_instructionTState == 2)
        {
            if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0)
            {
                LastBusCycle = IdleCycle('-', 0);
                return;
            }

            if (_softwareInterruptIsDivideError)
            {
                if (Opcode == 0xD4)
                {
                    LastBusCycle = IdleCycle('-', 0);
                    return;
                }

                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && _instructionTState == 3)
        {
            byte opcode = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = startsWithShortPrefetchQueue
                ? FetchCycle("T1", 'F', opcode)
                : IdleCycle('F', opcode);
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && _instructionTState == 4)
        {
            if (combinedSegmentRepeatDivide)
            {
                LastBusCycle = startsWithShortPrefetchQueue
                    ? FetchContinuationCycle('-', 0)
                    : IdleCycle('-', 0);
                return;
            }

            byte modRm = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = startsWithShortPrefetchQueue
                ? FetchContinuationCycle('S', modRm)
                : IdleCycle('S', modRm);
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && _instructionTState == 5)
        {
            if (combinedSegmentRepeatDivide)
            {
                byte opcode = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = startsWithShortPrefetchQueue
                    ? FetchDataCycle() with { QueueStatus = 'F', QueueData = opcode }
                    : FetchCycle("T1", 'F', opcode);
                if (!startsWithShortPrefetchQueue)
                {
                    return;
                }

                uint combinedFetchAddress = _nextFetchAddress;
                int combinedBytesFetched = Math.Min((combinedFetchAddress & 1) == 0 ? 2 : 1, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < combinedBytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (combinedFetchAddress + (uint)combinedBytesFetched) & 0xFFFFF;
                return;
            }

            LastBusCycle = startsWithShortPrefetchQueue
                ? FetchDataCycle()
                : FetchCycle("T1", '-', 0);
            if (!startsWithShortPrefetchQueue)
            {
                return;
            }
            uint fetchAddress = _nextFetchAddress;
            int bytesFetched = Math.Min((fetchAddress & 1) == 0 ? 2 : 1, _state.Queue.Length - _state.QueueCount);
            for (int index = 0; index < bytesFetched; index++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && _instructionTState == 6)
        {
            if (combinedSegmentRepeatDivide)
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = startsWithShortPrefetchQueue
                    ? new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", 'S', modRm)
                    : FetchContinuationCycle('S', modRm);
                return;
            }

            LastBusCycle = startsWithShortPrefetchQueue
                ? new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0)
                : FetchContinuationCycle('-', 0);
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && !startsWithShortPrefetchQueue && _instructionTState == 7)
        {
            LastBusCycle = FetchDataCycle();
            uint fetchAddress = _nextFetchAddress;
            int bytesFetched = Math.Min((fetchAddress & 1) == 0 ? 2 : 1, _state.Queue.Length - _state.QueueCount);
            for (int index = 0; index < bytesFetched; index++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            return;
        }

        if (_softwareInterruptIsDivideError && _instructionPrefixLength != 0 && !startsWithShortPrefetchQueue && _instructionTState == 8)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            return;
        }

        if (_softwareInterruptIsDivideError && Opcode == 0xD4 && _instructionTState == 3)
        {
            byte radix = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', radix)
                : IdleCycle('S', radix);
            return;
        }

        if (!_softwareInterruptTaken)
        {
            bool shortQueue = _queueCountAtStart <= 5;
            if (_instructionTState == 3)
            {
                LastBusCycle = shortQueue ? FetchCycle("T1", '-', 0) : IdleCycle('-', 0);
                return;
            }

            LastBusCycle = shortQueue
                ? FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
            return;
        }

        if (Opcode == 0xCD && _instructionTState == 3)
        {
            byte operand = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', operand)
                : IdleCycle('S', operand);
            return;
        }

        int prefetchStart = _softwareInterruptIsDivideError
            ? _divideErrorPrefetchStart != 0 ? _divideErrorPrefetchStart : _instructionPrefixLength != 0 ? startsWithShortPrefetchQueue ? 7 : 9 : startsWithShortPrefetchQueue ? 3 : 4
            : Opcode == 0xCD && _queueCountAtStart > 5 ? 5 : 3;
        bool memoryDivideError = _softwareInterruptIsDivideError && IsMemoryDivideOpcode(Opcode, _memoryModRm);
        int firstVectorRead = _softwareInterruptEntryStart;
        if (memoryDivideError && _instructionTState < firstVectorRead &&
            TryTickMemoryPrefetch(out Intel8086BusCycle dividePrefetchCycle))
        {
            LastBusCycle = dividePrefetchCycle;
            return;
        }

        bool performPrefetch = !memoryDivideError &&
            (_queueCountAtStart <= 5 || _softwareInterruptIsDivideError || Opcode == 0xCD && _instructionTState >= 5);
        int prefetchPhase = _instructionTState - prefetchStart;
        if (performPrefetch && prefetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = prefetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (prefetchPhase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                int bytesFetched = Math.Min((fetchAddress & 1) == 0 ? 2 : 1, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            }

            return;
        }

        if (_instructionTState >= firstVectorRead && _instructionTState < firstVectorRead + 4)
        {
            LastBusCycle = TickInterruptVectorRead(0, _instructionTState - firstVectorRead);
            return;
        }

        int secondVectorRead = firstVectorRead + 7;
        if (_instructionTState >= secondVectorRead && _instructionTState < secondVectorRead + 4)
        {
            LastBusCycle = TickInterruptVectorRead(1, _instructionTState - secondVectorRead);
            return;
        }

        int firstWriteLength = IsSoftwareInterruptStackAligned() ? 4 : 8;
        if (_instructionTState >= _softwareInterruptFirstWriteStart &&
            _instructionTState < _softwareInterruptFirstWriteStart + firstWriteLength)
        {
            LastBusCycle = TickSoftwareInterruptStackWrite(0, _instructionTState - _softwareInterruptFirstWriteStart);
            return;
        }

        if (_instructionTState >= _softwareInterruptSecondWriteStart &&
            _instructionTState < _softwareInterruptSecondWriteStart + firstWriteLength)
        {
            LastBusCycle = TickSoftwareInterruptStackWrite(1, _instructionTState - _softwareInterruptSecondWriteStart);
            return;
        }

        if (_instructionTState == _softwareInterruptFlush)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.IP = _softwareInterruptTargetIp;
            _state.CS = _softwareInterruptTargetCs;
            _state.SP = unchecked((ushort)(_state.SP - 6));
            _state.Flags = (ushort)(_state.Flags & ~0x0300);
            _nextFetchAddress = PhysicalAddress(_state.CS, _state.IP);
            byte queueData = _softwareInterruptIsHardware
                ? (byte)0
                : Opcode == 0xCD
                ? _softwareInterruptVector
                : _softwareInterruptIsDivideError
                    ? IsMemoryDivideOpcode(Opcode, _memoryModRm) ? _memoryDivideQueueData : _memoryModRm
                    : Opcode;
            LastBusCycle = IdleCycle('E', queueData);
            return;
        }

        if (_instructionTState >= _softwareInterruptThirdWriteStart &&
            _instructionTState <= _softwareInterruptEnd)
        {
            LastBusCycle = TickSoftwareInterruptStackWrite(2, _instructionTState - _softwareInterruptThirdWriteStart);
            if (_instructionTState == _softwareInterruptThirdWriteStart + (IsSoftwareInterruptStackAligned() ? 2 : 6))
            {
                if (!_softwareInterruptIsHardware && _state.QueueCount != 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
            }

            return;
        }

        int targetFetchStart = _softwareInterruptFlush + 2;
        int targetFetchPhase = _instructionTState - targetFetchStart;
        if (targetFetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = targetFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => SoftwareInterruptTargetDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (targetFetchPhase == 2)
            {
                int bytesFetched = (_softwareInterruptTargetIp & 1) == 0 ? 2 : 1;
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + (uint)bytesFetched) & 0xFFFFF;
            }

            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private bool IsSoftwareInterruptStackAligned() =>
        (PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP + 4))) & 1) == 0;

    private Intel8086BusCycle TickInterruptVectorRead(int wordIndex, int phase)
    {
        uint address = ((uint)_softwareInterruptVector * 4) + ((uint)wordIndex * 2);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", 0, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, PhysicalAddress(0x2000, (ushort)address), "CS", "R--", "---", 0, 0, "MEMR", "T2", '-', 0),
            2 => ReadSoftwareInterruptVectorWord(address, wordIndex),
            _ => new Intel8086BusCycle(0, PhysicalAddress(0x2000, (ushort)address), "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadSoftwareInterruptVectorWord(uint address, int wordIndex)
    {
        ushort value = (ushort)(_bus.Read(address) | (_bus.Read((address + 1) & 0xFFFFF) << 8));
        if (wordIndex == 0)
        {
            _softwareInterruptTargetIp = value;
        }
        else
        {
            _softwareInterruptTargetCs = value;
        }

        return new Intel8086BusCycle(0, PhysicalAddress(0x2000, (ushort)address), "CS", "R--", "---", 0, value, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickSoftwareInterruptStackWrite(int wordIndex, int phase)
    {
        bool aligned = IsSoftwareInterruptStackAligned();
        uint address = PhysicalAddress(_state.SS, unchecked((ushort)(_softwareInterruptInitialSp - ((wordIndex + 1) * 2))));
        ushort value = wordIndex switch
        {
            0 => _softwareInterruptSavedFlags,
            1 => InstructionCs,
            _ => _softwareInterruptReturnIp,
        };
        if (aligned)
        {
            return phase switch
            {
                0 => new Intel8086BusCycle(1, address, "--", "---", "---", 0, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, 0, "SS", "-A-", "---", 0, 0, "MEMW", "T2", '-', 0),
                2 => WriteSoftwareInterruptStackWord(address, value),
                _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
        }

        int byteIndex = aligned || phase < 4 ? 0 : 1;
        int bytePhase = aligned ? phase : phase < 4 ? phase : phase - 4;
        uint byteAddress = (address + (uint)byteIndex) & 0xFFFFF;
        byte bhe = (byte)((byteAddress & 1) == 0 ? 1 : 0);
        return bytePhase switch
        {
            0 => new Intel8086BusCycle(1, byteAddress, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "SS", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteSoftwareInterruptStackByte(byteAddress, value, byteIndex, bhe),
            _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteSoftwareInterruptStackWord(uint address, ushort value)
    {
        _bus.Write(address, (byte)value);
        _bus.Write((address + 1) & 0xFFFFF, (byte)(value >> 8));
        return new Intel8086BusCycle(0, 0, "SS", "-AW", "---", 0, value, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle WriteSoftwareInterruptStackByte(uint address, ushort value, int byteIndex, byte bhe)
    {
        byte dataByte = (byte)(value >> (byteIndex * 8));
        _bus.Write(address, dataByte);
        ushort data = (ushort)((address & 1) == 0 ? dataByte : dataByte << 8);
        return new Intel8086BusCycle(0, 0, "SS", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle SoftwareInterruptTargetDataCycle() => new(
        0, 0, "CS", "R--", "---", 0,
        (_softwareInterruptTargetIp & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV", "T3", '-', 0);
}
