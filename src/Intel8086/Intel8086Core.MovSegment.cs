namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeMovSegmentRegisterTransfer(byte opcode)
    {
        int prefixLength = _instructionPrefixLength;
        if (_state.QueueCount < prefixLength + 2)
        {
            throw new InvalidOperationException("MOV segmentowy wymaga bajtu ModR/M w kolejce prefetch.");
        }

        _movSegmentTransfer = true;
        _movSegmentPrefixLength = prefixLength;
        _movSegmentModRm = _state.Queue[_state.QueueHead + prefixLength + 1];
        int mod = _movSegmentModRm >> 6;
        int rm = _movSegmentModRm & 7;
        _movSegmentDisplacementBytes = mod == 3 ? 0 : mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        if (_state.QueueCount < prefixLength + 2 + _movSegmentDisplacementBytes)
        {
            throw new InvalidOperationException("MOV segmentowy wymaga bajtów displacementu w kolejce prefetch.");
        }

        Intel8086SegmentMoveProfile profile;
        if (mod == 3)
        {
            profile = GetMovSegmentProfile(opcode, mod, rm, _queueCountAtStart, prefixLength == 1, false);
            _instructionCycles = profile.Cycles;
        }
        else
        {
            ushort displacement = _movSegmentDisplacementBytes switch
            {
                0 => 0,
                1 => _state.Queue[_state.QueueHead + prefixLength + 2],
                _ => (ushort)(_state.Queue[_state.QueueHead + prefixLength + 2] |
                    (_state.Queue[_state.QueueHead + prefixLength + 3] << 8)),
            };
            Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
                _movSegmentModRm, in _state, displacement, _segmentOverride);
            _movSegmentPhysicalAddress = effectiveAddress.PhysicalAddress;
            _movSegmentStatus = GetSegmentStatus(_segmentOverride == Intel8086SegmentOverride.None
                ? effectiveAddress.Segment == _state.SS ? Intel8086SegmentOverride.Ss : Intel8086SegmentOverride.Ds
                : _segmentOverride);
            profile = GetMovSegmentProfile(opcode, mod, rm, _queueCountAtStart, prefixLength == 1,
                (_movSegmentPhysicalAddress & 1) != 0);
            _movSegmentBusStart = profile.BusStart;
            _instructionCycles = profile.Cycles;
            _movSegmentDisplacementStart = mod == 0 && rm == 6
                ? 4
                : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1;
            _movSegmentDisplacementStart += prefixLength * 2;
        }

        _movSegmentPrefetchBytesRemaining = profile.PrefetchBytes;
        _movSegmentWordValue = opcode == 0x8C ? GetSegmentRegisterValue((_movSegmentModRm >> 3) & 3) : (ushort)0;
        _movSegmentMemoryValue = 0;
    }

    private void TickMovSegmentRegisterTransfer()
    {
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_movSegmentPrefixLength == 1)
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

        int displacementIndex = _instructionTState - _movSegmentDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _movSegmentDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int mod = _movSegmentModRm >> 6;
        if (consumesModRm && mod == 3)
        {
            ExecuteRegisterMovSegmentTransfer();
        }

        int rm = _movSegmentModRm & 7;
        bool oddAddress = (_movSegmentPhysicalAddress & 1) != 0;
        Intel8086SegmentMoveProfile profile = GetMovSegmentProfile(
            Opcode, mod, rm, _queueCountAtStart, _movSegmentPrefixLength == 1, oddAddress);
        int fetchStart = profile.Fetch1 != 0 && IsFetchWindow(_instructionTState, profile.Fetch1)
            ? profile.Fetch1
            : profile.Fetch2 != 0 && IsFetchWindow(_instructionTState, profile.Fetch2)
                ? profile.Fetch2
                : profile.Fetch3 != 0 && IsFetchWindow(_instructionTState, profile.Fetch3) ? profile.Fetch3 : 0;
        bool memoryOperand = mod != 3;
        int transferCount = memoryOperand && oddAddress ? 2 : 1;
        int memoryPhase = _instructionTState - _movSegmentBusStart;
        bool writeTransfer = Opcode == 0x8C;
        bool memoryBusActive = memoryOperand && (writeTransfer
            ? memoryPhase >= 0 && memoryPhase / 4 < transferCount && memoryPhase % 4 <= (transferCount == 2 ? 3 : 2)
            : memoryPhase >= 0 && memoryPhase < transferCount * 4);
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
                    QueueData = consumesModRm ? _movSegmentModRm : displacementByte,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4",
                    consumesDisplacement ? 'S' : '-', displacementByte),
            };
            if (phase == 2 && _state.QueueCount < _state.Queue.Length)
            {
                int bytesToQueue = Math.Min(2, _movSegmentPrefetchBytesRemaining);
                while (bytesToQueue > 0 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                    _movSegmentPrefetchBytesRemaining--;
                    bytesToQueue--;
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (_movSegmentPrefixLength == 1 && consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_movSegmentPrefixLength == 1 && consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _movSegmentModRm);
        }
        else if (consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _movSegmentModRm);
        }
        else if (memoryBusActive)
        {
            LastBusCycle = TickMovSegmentMemoryBus(memoryPhase, transferCount);
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
            if (mod == 3 && _state.QueueCount > 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle TickMovSegmentMemoryBus(int memoryPhase, int transferCount)
    {
        int transfer = memoryPhase / 4;
        int phase = memoryPhase % 4;
        uint address = transferCount == 2 ? (_movSegmentPhysicalAddress + (uint)transfer) & 0xFFFFF : _movSegmentPhysicalAddress;
        byte bhe = transferCount == 1 && (_movSegmentPhysicalAddress & 1) == 0
            ? (byte)0
            : (byte)((address & 1) == 0 ? 1 : 0);
        bool write = Opcode == 0x8C;
        string busStatus = write ? "MEMW" : "MEMR";
        if (phase == 0)
        {
            return new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, busStatus, "T1", '-', 0);
        }

        if (phase == 1)
        {
            return new Intel8086BusCycle(0, address, _movSegmentStatus, write ? "-A-" : "R--", "---", bhe,
                0, busStatus, "T2", '-', 0);
        }

        if (phase == 3 && !write)
        {
            return new Intel8086BusCycle(0, address, _movSegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0);
        }

        if (phase == 3 && write && transferCount == 2 && transfer == 0)
        {
            return new Intel8086BusCycle(0, address, _movSegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0);
        }

        return write
            ? TickMovSegmentMemoryWriteData(transfer, transferCount, address, bhe)
            : TickMovSegmentMemoryReadData(transfer, transferCount, address, bhe);
    }

    private Intel8086BusCycle TickMovSegmentMemoryWriteData(int transfer, int transferCount, uint address, byte bhe)
    {
        ushort data = transferCount == 1
            ? _movSegmentWordValue
            : transfer == 0 ? (ushort)((_movSegmentWordValue & 0x00FF) << 8) : (ushort)(_movSegmentWordValue >> 8);
        if (transferCount == 1)
        {
            _bus.Write(address, (byte)_movSegmentWordValue);
            _bus.Write((address + 1) & 0xFFFFF, (byte)(_movSegmentWordValue >> 8));
        }
        else
        {
            _bus.Write(address, transfer == 0 ? (byte)_movSegmentWordValue : (byte)(_movSegmentWordValue >> 8));
        }

        return new Intel8086BusCycle(0, address, _movSegmentStatus, "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMovSegmentMemoryReadData(int transfer, int transferCount, uint address, byte bhe)
    {
        ushort data;
        if (transferCount == 1)
        {
            byte low = _bus.Read(address);
            byte high = _bus.Read((address + 1) & 0xFFFFF);
            _movSegmentMemoryValue = (ushort)(low | (high << 8));
            data = _movSegmentMemoryValue;
        }
        else
        {
            byte value = _bus.Read(address);
            _movSegmentMemoryValue = transfer == 0
                ? (ushort)((_movSegmentMemoryValue & 0xFF00) | value)
                : (ushort)((_movSegmentMemoryValue & 0x00FF) | (value << 8));
            data = transfer == 0 ? (ushort)(value << 8) : value;
        }

        if (transfer == transferCount - 1)
        {
            SetSegmentRegisterValue((_movSegmentModRm >> 3) & 3, _movSegmentMemoryValue);
        }

        return new Intel8086BusCycle(0, address, _movSegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void ExecuteRegisterMovSegmentTransfer()
    {
        int segment = (_movSegmentModRm >> 3) & 3;
        int register = _movSegmentModRm & 7;
        if (Opcode == 0x8C)
        {
            SetWordRegister(register, GetSegmentRegisterValue(segment));
        }
        else
        {
            SetSegmentRegisterValue(segment, GetWordRegister(register));
        }
    }

    private ushort GetSegmentRegisterValue(int register) => (register & 3) switch
    {
        0 => _state.ES,
        1 => _state.CS,
        2 => _state.SS,
        _ => _state.DS,
    };

    private void SetSegmentRegisterValue(int register, ushort value)
    {
        switch (register & 3)
        {
            case 0: _state.ES = value; break;
            case 1: _state.CS = value; break;
            case 2: _state.SS = value; break;
            case 3: _state.DS = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(register));
        }
    }
}
