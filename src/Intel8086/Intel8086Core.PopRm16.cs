namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializePopRm16()
    {
        _popRm16PrefixLength = _instructionPrefixLength;
        int modRmIndex = _popRm16PrefixLength + 1;
        if (_state.QueueCount <= modRmIndex)
        {
            throw new InvalidOperationException("POP r/m16 wymaga bajtu ModR/M w kolejce prefetch.");
        }

        _popRm16ModRm = _state.Queue[_state.QueueHead + modRmIndex];
        if (((_popRm16ModRm >> 3) & 7) != 0)
        {
            throw new InvalidOperationException("POP r/m16 obsługuje wyłącznie rozszerzenie ModR/M /0.");
        }

        int mod = _popRm16ModRm >> 6;
        int rm = _popRm16ModRm & 7;
        _popRm16DisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : mod == 2 ? 2 : 0;
        if (_state.QueueCount < modRmIndex + 1 + _popRm16DisplacementBytes)
        {
            throw new InvalidOperationException("POP r/m16 wymaga bajtów displacementu w kolejce prefetch.");
        }

        _popRm16StackAddress = PhysicalAddress(_state.SS, _state.SP);
        _memoryAddress = _popRm16StackAddress;
        _memorySegmentStatus = "SS";
        _memoryWordValue = 0;

        if (mod != 3)
        {
            ushort displacement = _popRm16DisplacementBytes switch
            {
                0 => 0,
                1 => _state.Queue[_state.QueueHead + modRmIndex + 1],
                _ => (ushort)(_state.Queue[_state.QueueHead + modRmIndex + 1] |
                    (_state.Queue[_state.QueueHead + modRmIndex + 2] << 8)),
            };
            Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
                _popRm16ModRm, in _state, displacement, _segmentOverride);
            _popRm16TargetAddress = effectiveAddress.PhysicalAddress;
            _popRm16TargetSegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
        }
        else
        {
            _popRm16TargetAddress = 0;
            _popRm16TargetSegmentStatus = "DS";
        }

        int profileIndex = GetPopRm16ProfileIndex(
            _popRm16ModRm,
            _queueCountAtStart,
            _popRm16PrefixLength != 0,
            (_popRm16StackAddress & 1) != 0,
            mod != 3 && (_popRm16TargetAddress & 1) != 0);
        _popRm16Profile = PopRm16Profiles[profileIndex] ??
            throw new InvalidOperationException("Brak profilu T-state POP r/m16 dla tej kombinacji wejściowej.");
        _instructionCycles = _popRm16Profile.Cycles;
        _popRm16ConsumedBytes = 0;
        _popRm16Transfer = true;
    }

    private void TickPopRm16()
    {
        _instructionTState++;
        bool consumesQueue = (_popRm16Profile.QueuePopMask & (1UL << _instructionTState)) != 0;
        char queueStatus = '-';
        byte queueData = 0;
        if (consumesQueue)
        {
            queueData = _state.Queue[_state.QueueHead];
            queueStatus = _popRm16ConsumedBytes < _popRm16PrefixLength + 1 ? 'F' : 'S';
            _popRm16ConsumedBytes++;
            PopQueue();
            _state.IP++;
        }

        int stackReadPhase = _instructionTState - _popRm16Profile.StackReadStart;
        int stackReadLength = (_popRm16StackAddress & 1) == 0 ? 4 : 8;
        if (stackReadPhase >= 0 && stackReadPhase < stackReadLength)
        {
            LastBusCycle = TickRegisterStackWordTransfer(false, _state.SP, 0, stackReadPhase) with
            {
                QueueStatus = queueStatus,
                QueueData = queueData,
            };
            if (stackReadPhase == stackReadLength - 1)
            {
                _memoryWordResult = _memoryWordValue;
            }
        }
        else if (_popRm16Profile.TargetWriteStart != 0 && _instructionTState >= _popRm16Profile.TargetWriteStart)
        {
            int writePhase = _instructionTState - _popRm16Profile.TargetWriteStart;
            int byteTransferPhaseCount = (_popRm16TargetAddress & 1) == 0 ? 3 : 7;
            LastBusCycle = writePhase < byteTransferPhaseCount
                ? TickPopRm16MemoryWrite(writePhase)
                : IdleCycle(queueStatus, queueData);
            if (LastBusCycle.QueueStatus == '-' && consumesQueue)
            {
                LastBusCycle = LastBusCycle with { QueueStatus = queueStatus, QueueData = queueData };
            }
        }
        else if (TryTickPopRm16Prefetch(queueStatus, queueData, out Intel8086BusCycle prefetchCycle))
        {
            LastBusCycle = prefetchCycle;
        }
        else
        {
            LastBusCycle = IdleCycle(queueStatus, queueData);
        }

        if (_instructionTState == _instructionCycles)
        {
            int register = _popRm16ModRm & 7;
            if ((_popRm16ModRm >> 6) == 3)
            {
                SetWordRegister(register, _memoryWordValue);
            }

            if ((_popRm16ModRm >> 6) != 3 || register != 4)
            {
                _state.SP = unchecked((ushort)(_state.SP + 2));
            }

            while (_state.QueueCount > _popRm16Profile.FinalQueueCount)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle TickPopRm16MemoryWrite(int phase)
    {
        uint originalAddress = _memoryAddress;
        string originalSegment = _memorySegmentStatus;
        _memoryAddress = _popRm16TargetAddress;
        _memorySegmentStatus = _popRm16TargetSegmentStatus;
        Intel8086BusCycle cycle;
        if ((_popRm16TargetAddress & 1) == 0)
        {
            cycle = TickAlignedMemoryWordWrite(phase);
        }
        else
        {
            cycle = phase < 4
                ? TickMemoryWordWrite(phase, 0)
                : TickMemoryWordWrite(phase - 4, 1);
        }

        _memoryAddress = originalAddress;
        _memorySegmentStatus = originalSegment;
        return cycle;
    }

    private bool TryTickPopRm16Prefetch(char queueStatus, byte queueData, out Intel8086BusCycle cycle)
    {
        return TryTickPopRm16PrefetchWindow(_popRm16Profile.Fetch1, queueStatus, queueData, out cycle) ||
            TryTickPopRm16PrefetchWindow(_popRm16Profile.Fetch2, queueStatus, queueData, out cycle) ||
            TryTickPopRm16PrefetchWindow(_popRm16Profile.Fetch3, queueStatus, queueData, out cycle);
    }

    private bool TryTickPopRm16PrefetchWindow(byte start, char queueStatus, byte queueData, out Intel8086BusCycle cycle)
    {
        if (start == 0 || _instructionTState < start || _instructionTState >= start + 4)
        {
            cycle = default;
            return false;
        }

        int phase = _instructionTState - start;
        cycle = phase switch
        {
            0 => FetchCycle("T1", queueStatus, queueData),
            1 => FetchContinuationCycle(queueStatus, queueData),
            2 => FetchDataCycle() with { QueueStatus = queueStatus, QueueData = queueData },
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", queueStatus, queueData),
        };
        if (phase == 2)
        {
            for (int index = 0; index < 2 && _state.QueueCount < _state.Queue.Length; index++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = AdvanceCodePrefetch(_state.CS, _nextFetchAddress, 2);
        }

        return true;
    }
}
