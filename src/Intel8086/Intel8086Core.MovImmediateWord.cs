namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeMovImmediateWord()
    {
        int modRmIndex = _state.QueueHead + _movImmediateWordPrefixLength + 1;
        if (_state.QueueCount < _movImmediateWordPrefixLength + 2)
        {
            throw new InvalidOperationException("C7 wymaga bajtu ModR/M w kolejce prefetch.");
        }

        _movImmediateWordModRm = _state.Queue[modRmIndex];
        _memoryModRm = _movImmediateWordModRm;
        int mod = _movImmediateWordModRm >> 6;
        int rm = _movImmediateWordModRm & 7;
        _movImmediateWordDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : mod == 2 ? 2 : 0;
        if (_state.QueueCount < _movImmediateWordPrefixLength + 2 + _movImmediateWordDisplacementBytes)
        {
            throw new InvalidOperationException("C7 wymaga bajtów displacementu w kolejce prefetch.");
        }

        if (mod != 3)
        {
            int displacementIndex = _state.QueueHead + _movImmediateWordPrefixLength + 2;
            ushort displacement = _movImmediateWordDisplacementBytes switch
            {
                0 => 0,
                1 => _state.Queue[displacementIndex],
                _ => (ushort)(_state.Queue[displacementIndex] | (_state.Queue[displacementIndex + 1] << 8)),
            };
            Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
                _movImmediateWordModRm, in _state, displacement, _movImmediateWordSegmentOverride);
            _movImmediateWordPhysicalAddress = effectiveAddress.PhysicalAddress;
            _movImmediateWordSegment = effectiveAddress.Segment;
            _movImmediateWordSegmentStatus = GetMemorySegmentStatus(rm, mod, _movImmediateWordSegmentOverride);
            _memoryAddress = _movImmediateWordPhysicalAddress;
            _memorySegmentStatus = _movImmediateWordSegmentStatus;
        }

        int profileIndex = GetMovImmediateWordProfileIndex(
            (byte)(_movImmediateWordModRm & 0xC7),
            _queueCountAtStart,
            _movImmediateWordPrefixLength != 0,
            mod != 3 && (_movImmediateWordPhysicalAddress & 1) != 0);
        _movImmediateWordProfile = MovImmediateWordProfiles[profileIndex] ??
            throw new InvalidOperationException("Brak profilu T-state MOV r/m16,imm16 dla tej kombinacji wejściowej.");
        _movImmediateWordCodeBytesRemaining = Math.Max(
            0,
            _movImmediateWordPrefixLength + 4 + _movImmediateWordDisplacementBytes - _queueCountAtStart);
        _movImmediateWordConsumedBytes = 0;
        _movImmediateWordValue = 0;
        _instructionCycles = _movImmediateWordProfile.Cycles;
        _movImmediateWordTransfer = true;
    }

    private void TickMovImmediateWord()
    {
        bool consumesQueue = (_movImmediateWordProfile.QueuePopMask & (1UL << _instructionTState)) != 0;
        char queueStatus = '-';
        byte queueData = 0;
        if (consumesQueue)
        {
            queueData = _state.Queue[_state.QueueHead];
            queueStatus = _movImmediateWordConsumedBytes < _movImmediateWordPrefixLength + 1 ? 'F' : 'S';
            int instructionByteIndex = _movImmediateWordConsumedBytes - _movImmediateWordPrefixLength - 2 - _movImmediateWordDisplacementBytes;
            if (instructionByteIndex == 0)
            {
                _movImmediateWordLowByte = queueData;
                _movImmediateWordValue = (ushort)((_movImmediateWordValue & 0xFF00) | queueData);
            }
            else if (instructionByteIndex == 1)
            {
                _movImmediateWordValue = (ushort)((queueData << 8) | _movImmediateWordLowByte);
                _memoryWordResult = _movImmediateWordValue;
            }

            _movImmediateWordConsumedBytes++;
            PopQueue();
            _state.IP++;
        }

        int mod = _movImmediateWordModRm >> 6;
        int writePhase = _instructionTState - _movImmediateWordProfile.Write1;
        int secondWritePhase = _movImmediateWordProfile.Write2 == 0
            ? -1
            : _instructionTState - _movImmediateWordProfile.Write2;
        int writeLength = (_movImmediateWordPhysicalAddress & 1) == 0 ? 3 : 4;
        bool isWriteCycle = mod != 3 &&
            (writePhase >= 0 && writePhase < writeLength || secondWritePhase >= 0 && secondWritePhase < writeLength);
        if (isWriteCycle)
        {
            bool firstByte = writePhase >= 0 && writePhase < writeLength;
            int phase = firstByte ? writePhase : secondWritePhase;
            LastBusCycle = (_movImmediateWordPhysicalAddress & 1) == 0
                ? TickAlignedMemoryWordWrite(phase) with { QueueStatus = queueStatus, QueueData = queueData }
                : TickMemoryWordWrite(phase, firstByte ? 0 : 1) with
                {
                    QueueStatus = queueStatus,
                    QueueData = queueData,
                };
        }
        else if (TryTickMovImmediateWordPrefetch(_movImmediateWordProfile.Fetch1, queueStatus, queueData, out Intel8086BusCycle cycle) ||
            TryTickMovImmediateWordPrefetch(_movImmediateWordProfile.Fetch2, queueStatus, queueData, out cycle) ||
            TryTickMovImmediateWordPrefetch(_movImmediateWordProfile.Fetch3, queueStatus, queueData, out cycle))
        {
            LastBusCycle = cycle;
        }
        else
        {
            LastBusCycle = IdleCycle(queueStatus, queueData);
        }

        if (_instructionTState == _instructionCycles)
        {
            if (mod == 3)
            {
                SetWordRegister(_movImmediateWordModRm & 7, _movImmediateWordValue);
            }

            while (_state.QueueCount > _movImmediateWordProfile.FinalQueueCount)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private bool TryTickMovImmediateWordPrefetch(byte start, char queueStatus, byte queueData, out Intel8086BusCycle cycle)
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
            2 => ReadMovImmediateWordPrefetch(queueStatus, queueData),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", queueStatus, queueData),
        };
        return true;
    }

    private Intel8086BusCycle ReadMovImmediateWordPrefetch(char queueStatus, byte queueData)
    {
        byte low = _movImmediateWordCodeBytesRemaining > 0 ? _bus.Read(_nextFetchAddress) : (byte)0x90;
        uint highAddress = AdvanceCodePrefetch(InstructionCs, _nextFetchAddress, 1);
        byte high = _movImmediateWordCodeBytesRemaining > 1 ? _bus.Read(highAddress) : (byte)0x90;
        ushort data = (ushort)(low | (high << 8));
        for (int index = 0; index < 2 && _state.QueueCount < _state.Queue.Length; index++)
        {
            PushQueue(index == 0 ? low : high);
            if (_movImmediateWordCodeBytesRemaining > 0)
            {
                _movImmediateWordCodeBytesRemaining--;
            }
        }

        uint address = _nextFetchAddress;
        _nextFetchAddress = AdvanceInstructionPrefetchWord(_nextFetchAddress);
        return new Intel8086BusCycle(0, address, "CS", "R--", "---", 0, data, "PASV", "T3", queueStatus, queueData);
    }
}
