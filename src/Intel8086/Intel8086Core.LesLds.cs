namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeLesLds(byte opcode)
    {
        _lesLdsPrefixLength = _instructionPrefixLength;
        if (_state.QueueCount < _lesLdsPrefixLength + 2)
        {
            throw new InvalidOperationException("LES/LDS wymaga bajtu ModR/M w kolejce prefetch.");
        }

        _lesLdsModRm = _state.Queue[_state.QueueHead + _lesLdsPrefixLength + 1];
        int mod = _lesLdsModRm >> 6;
        int rm = _lesLdsModRm & 7;
        if (mod == 3)
        {
            throw new InvalidOperationException("LES/LDS wymaga pamięciowego operandu ModR/M.");
        }

        _lesLdsDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        if (_state.QueueCount < _lesLdsPrefixLength + 2 + _lesLdsDisplacementBytes)
        {
            throw new InvalidOperationException("LES/LDS wymaga bajtów displacementu w kolejce prefetch.");
        }

        ushort displacement = _lesLdsDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + _lesLdsPrefixLength + 2],
            _ => (ushort)(_state.Queue[_state.QueueHead + _lesLdsPrefixLength + 2] |
                (_state.Queue[_state.QueueHead + _lesLdsPrefixLength + 3] << 8)),
        };
        Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
            _lesLdsModRm, in _state, displacement, _segmentOverride);
        _lesLdsPhysicalAddress = effectiveAddress.PhysicalAddress;
        _lesLdsSegmentStatus = GetSegmentStatus(_segmentOverride == Intel8086SegmentOverride.None
            ? effectiveAddress.Segment == _state.SS ? Intel8086SegmentOverride.Ss : Intel8086SegmentOverride.Ds
            : _segmentOverride);

        Intel8086LesLdsProfile profile = GetLesLdsProfile(
            opcode, mod, rm, _queueCountAtStart, _lesLdsPrefixLength == 1, (_lesLdsPhysicalAddress & 1) != 0);
        _lesLdsBusStart = profile.BusStart;
        _instructionCycles = profile.Cycles;
        _lesLdsPrefetchBytesRemaining = profile.PrefetchBytes;
        _lesLdsDisplacementStart = mod == 0 && rm == 6
            ? 4
            : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1;
        _lesLdsDisplacementStart += _lesLdsPrefixLength * 2;
        _lesLdsPointerValue = 0;
        _lesLdsTransfer = true;
    }

    private void TickLesLds()
    {
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_lesLdsPrefixLength == 1)
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
            consumesOpcode = true;
        }
        else if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            consumesModRm = true;
        }

        int displacementIndex = _instructionTState - _lesLdsDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _lesLdsDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int mod = _lesLdsModRm >> 6;
        int rm = _lesLdsModRm & 7;
        Intel8086LesLdsProfile profile = GetLesLdsProfile(
            Opcode, mod, rm, _queueCountAtStart, _lesLdsPrefixLength == 1, (_lesLdsPhysicalAddress & 1) != 0);
        int fetchStart = profile.Fetch1 != 0 && IsFetchWindow(_instructionTState, profile.Fetch1)
            ? profile.Fetch1
            : profile.Fetch2 != 0 && IsFetchWindow(_instructionTState, profile.Fetch2)
                ? profile.Fetch2
                : profile.Fetch3 != 0 && IsFetchWindow(_instructionTState, profile.Fetch3)
                    ? profile.Fetch3
                    : 0;
        int transferCount = (_lesLdsPhysicalAddress & 1) == 0 ? 2 : 4;
        int memoryPhase = _instructionTState - _lesLdsBusStart;
        bool memoryBusActive = TryGetLesLdsBusTransfer(memoryPhase, transferCount, out int transfer, out int transferPhase);
        if (memoryBusActive)
        {
            fetchStart = 0;
        }

        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesOpcode ? 'F' : consumesDisplacement ? 'S' : '-',
                    consumesOpcode ? Opcode : displacementByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesModRm || consumesDisplacement ? 'S' : '-',
                    QueueData = consumesModRm ? _lesLdsModRm : displacementByte,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4",
                    consumesDisplacement ? 'S' : '-', displacementByte),
            };
            if (phase == 2 && _state.QueueCount < _state.Queue.Length)
            {
                int bytesToQueue = Math.Min(2, _lesLdsPrefetchBytesRemaining);
                while (bytesToQueue > 0 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                    _lesLdsPrefetchBytesRemaining--;
                    bytesToQueue--;
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (_lesLdsPrefixLength == 1 && consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_lesLdsPrefixLength == 1 && consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _lesLdsModRm);
        }
        else if (consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _lesLdsModRm);
        }
        else if (memoryBusActive)
        {
            LastBusCycle = TickLesLdsMemoryBus(transfer, transferPhase, transferCount);
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

    private static bool TryGetLesLdsBusTransfer(int memoryPhase, int transferCount, out int transfer, out int phase)
    {
        bool splitBytes = transferCount == 4;
        if (memoryPhase is >= 0 and <= 3)
        {
            transfer = 0;
            phase = memoryPhase;
            return true;
        }

        if (splitBytes && memoryPhase is >= 4 and <= 7)
        {
            transfer = 1;
            phase = memoryPhase - 4;
            return true;
        }

        int secondWordStart = splitBytes ? 14 : 10;
        if (memoryPhase >= secondWordStart && memoryPhase < secondWordStart + 4)
        {
            transfer = splitBytes ? 2 : 1;
            phase = memoryPhase - secondWordStart;
            return true;
        }

        if (splitBytes && memoryPhase is >= 18 and <= 21)
        {
            transfer = 3;
            phase = memoryPhase - 18;
            return true;
        }

        transfer = 0;
        phase = 0;
        return false;
    }

    private Intel8086BusCycle TickLesLdsMemoryBus(int transfer, int phase, int transferCount)
    {
        bool splitBytes = transferCount == 4;
        uint address = (_lesLdsPhysicalAddress + (uint)(transfer * (splitBytes ? 1 : 2))) & 0xFFFFF;
        byte bhe = splitBytes ? (byte)((address & 1) == 0 ? 1 : 0) : (byte)0;
        if (phase == 0)
        {
            return new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0);
        }

        if (phase == 1)
        {
            return new Intel8086BusCycle(0, address, _lesLdsSegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0);
        }

        if (phase == 3)
        {
            return new Intel8086BusCycle(0, address, _lesLdsSegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0);
        }

        ushort data;
        if (splitBytes)
        {
            byte value = _bus.Read(address);
            int shift = transfer * 8;
            _lesLdsPointerValue = (_lesLdsPointerValue & ~(0xFFu << shift)) | ((uint)value << shift);
            data = (ushort)((address & 1) != 0 ? value << 8 : value);
        }
        else
        {
            byte low = _bus.Read(address);
            byte high = _bus.Read((address + 1) & 0xFFFFF);
            int shift = transfer * 16;
            ushort word = (ushort)(low | (high << 8));
            _lesLdsPointerValue = (_lesLdsPointerValue & ~(0xFFFFu << shift)) | ((uint)word << shift);
            data = word;
        }

        if (transfer == transferCount - 1)
        {
            SetWordRegister((_lesLdsModRm >> 3) & 7, (ushort)_lesLdsPointerValue);
            if (Opcode == 0xC4)
            {
                _state.ES = (ushort)(_lesLdsPointerValue >> 16);
            }
            else
            {
                _state.DS = (ushort)(_lesLdsPointerValue >> 16);
            }
        }

        return new Intel8086BusCycle(0, address, _lesLdsSegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }
}
