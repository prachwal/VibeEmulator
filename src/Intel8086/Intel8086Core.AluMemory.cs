namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeAluMemoryTransfer()
    {
        int prefixLength = _instructionPrefixLength;
        _aluMemoryModRm = _state.Queue[_state.QueueHead + prefixLength + 1];
        _memoryModRm = _aluMemoryModRm;
        int mod = _aluMemoryModRm >> 6;
        if (mod == 3)
        {
            return;
        }

        int rm = _aluMemoryModRm & 7;
        _aluMemoryDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        if (_state.QueueCount < prefixLength + 2 + _aluMemoryDisplacementBytes)
        {
            throw new InvalidOperationException($"Opcode {Opcode:X2} requires displacement bytes in the prefetch queue.");
        }

        ushort displacement = _aluMemoryDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + prefixLength + 2],
            _ => (ushort)(_state.Queue[_state.QueueHead + prefixLength + 2] | (_state.Queue[_state.QueueHead + prefixLength + 3] << 8)),
        };
        Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
            _aluMemoryModRm, in _state, displacement, _segmentOverride);
        _aluMemoryPhysicalAddress = effectiveAddress.PhysicalAddress;
        _aluMemorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
        _memoryAddress = _aluMemoryPhysicalAddress;
        _memorySegmentStatus = _aluMemorySegmentStatus;
        _aluMemoryBhe = (byte)((_aluMemoryPhysicalAddress & 1) == 0 ? 1 : 0);
        _aluMemoryDisplacementStart = mod == 0 && rm == 6
            ? prefixLength == 0 ? 4 : 6
            : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1 + (prefixLength * 2);
        _aluMemoryReadStart = prefixLength == 0
            ? GetMovMemoryBusStart(0x8A, mod, rm, _aluMemoryDisplacementBytes, _queueCountAtStart)
            : GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart);
        _memoryReadStart = _aluMemoryReadStart;
        Intel8086AluOperation operation = GetRegisterDirectAluOperation(Opcode);
        bool directionToRegister = (Opcode & 0x02) != 0;
        _aluMemoryWrites = !directionToRegister
            && operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test;
        _aluMemoryTransfer = true;
        _instructionCycles = _aluMemoryReadStart + (_aluMemoryWrites ? 13 : 6);
    }

    private void TickMemoryAlu()
    {
        if (_instructionPrefixLength != 0)
        {
            TickPrefixedMemoryAlu();
            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _aluMemoryModRm);
            return;
        }

        int displacementIndex = _instructionTState - _aluMemoryDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _aluMemoryDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int mod = _aluMemoryModRm >> 6;
        int rm = _aluMemoryModRm & 7;
        int firstFetchStart = _queueCountAtStart <= 5 ? 3 : 4;
        (int secondFetchStart, int thirdFetchStart) = GetUnprefixedMovFetchStarts(
            0x8A, mod, rm, firstFetchStart, _queueCountAtStart, _aluMemoryReadStart);
        bool queuesSecondByte = _queueCountAtStart == 5 && mod is 1 or 2
            && secondFetchStart != 0 && secondFetchStart + 2 <= _instructionCycles;
        int fetchStart = IsFetchWindow(_instructionTState, firstFetchStart)
            ? firstFetchStart
            : secondFetchStart != 0 && IsFetchWindow(_instructionTState, secondFetchStart)
                ? secondFetchStart
                : thirdFetchStart != 0 && IsFetchWindow(_instructionTState, thirdFetchStart)
                    ? thirdFetchStart
                    : 0;
        int readPhase = _instructionTState - _aluMemoryReadStart;
        int writeStart = _aluMemoryReadStart + 11;
        int writePhase = _instructionTState - writeStart;
        bool memoryBusActive = readPhase is >= 0 and <= 3
            || _aluMemoryWrites && writePhase is >= 0 and <= 2;
        bool suppressExtraReadOnlyPrefetch = !_aluMemoryWrites && mod is 1 or 2 && rm >= 4;
        if (memoryBusActive)
        {
            fetchStart = 0;
        }

        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesDisplacement ? 'S' : '-', displacementByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesDisplacement ? 'S' : '-',
                    QueueData = displacementByte,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumesDisplacement ? 'S' : '-', displacementByte),
            };

            bool shouldQueueFetchedByte = fetchStart == firstFetchStart
                || (!suppressExtraReadOnlyPrefetch && queuesSecondByte)
                || (!suppressExtraReadOnlyPrefetch && fetchStart != firstFetchStart && _queueCountAtStart == 6 && mod == 2)
                || (_aluMemoryWrites && fetchStart != firstFetchStart && mod == 0 && rm == 6);
            if (phase == 2 && shouldQueueFetchedByte && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
                if (!suppressExtraReadOnlyPrefetch && fetchStart == firstFetchStart && queuesSecondByte && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                if (!suppressExtraReadOnlyPrefetch && fetchStart != firstFetchStart && _queueCountAtStart == 6 && mod == 2 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                if (_aluMemoryWrites && fetchStart != firstFetchStart && mod == 0 && rm == 6 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = readPhase switch
            {
                0 => new Intel8086BusCycle(1, _aluMemoryPhysicalAddress, "--", "---", "---", _aluMemoryBhe, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "R--", "---", _aluMemoryBhe, 0, "MEMR", "T2", '-', 0),
                2 => TickAluMemoryReadData(),
                _ => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "---", "---", _aluMemoryBhe, 0, "PASV", "T4", '-', 0),
            };
        }
        else if (_aluMemoryWrites && writePhase is >= 0 and <= 2)
        {
            ushort data = _aluMemoryBhe == 0 ? (ushort)(_aluMemoryResult << 8) : _aluMemoryResult;
            LastBusCycle = writePhase switch
            {
                0 => new Intel8086BusCycle(1, _aluMemoryPhysicalAddress, "--", "---", "---", _aluMemoryBhe, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "-A-", "---", _aluMemoryBhe, 0, "MEMW", "T2", '-', 0),
                _ => WriteAluMemoryResult(data),
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

    private void TickPrefixedMemoryAlu()
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
                ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _aluMemoryModRm }
                : IdleCycle('S', _aluMemoryModRm);
            return;
        }

        _ = _aluMemoryModRm >> 6;
        _ = _aluMemoryModRm & 7;
        int displacementIndex = _instructionTState - _aluMemoryDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _aluMemoryDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int readPhase = _instructionTState - _aluMemoryReadStart;
        int writeStart = _aluMemoryReadStart + 11;
        int writePhase = _instructionTState - writeStart;
        bool memoryBusActive = readPhase is >= 0 and <= 3 ||
            _aluMemoryWrites && writePhase is >= 0 and <= 2;
        Intel8086BusCycle prefetchCycle = default;
        bool hasPrefetch = !memoryBusActive && TryTickMemoryPrefetch(out prefetchCycle);
        if (hasPrefetch)
        {
            LastBusCycle = consumesDisplacement
                ? prefetchCycle with { QueueStatus = 'S', QueueData = displacementByte }
                : prefetchCycle;
        }
        else if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = readPhase switch
            {
                0 => new Intel8086BusCycle(1, _aluMemoryPhysicalAddress, "--", "---", "---", _aluMemoryBhe, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "R--", "---", _aluMemoryBhe, 0, "MEMR", "T2", '-', 0),
                2 => TickAluMemoryReadData(),
                _ => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "---", "---", _aluMemoryBhe, 0, "PASV", "T4", '-', 0),
            };
        }
        else if (_aluMemoryWrites && writePhase is >= 0 and <= 2)
        {
            ushort data = _aluMemoryBhe == 0 ? (ushort)(_aluMemoryResult << 8) : _aluMemoryResult;
            LastBusCycle = writePhase switch
            {
                0 => new Intel8086BusCycle(1, _aluMemoryPhysicalAddress, "--", "---", "---", _aluMemoryBhe, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "-A-", "---", _aluMemoryBhe, 0, "MEMW", "T2", '-', 0),
                _ => WriteAluMemoryResult(data),
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

    private Intel8086BusCycle TickAluMemoryReadData()
    {
        byte memoryValue = _bus.Read(_aluMemoryPhysicalAddress);
        int register = (_aluMemoryModRm >> 3) & 7;
        byte registerValue = GetByteRegister(register);
        bool directionToRegister = (Opcode & 0x02) != 0;
        Intel8086AluOperation operation = GetRegisterDirectAluOperation(Opcode);
        byte left = directionToRegister ? registerValue : memoryValue;
        byte right = directionToRegister ? memoryValue : registerValue;
        Intel8086AluResult result = Intel8086Alu.Execute(left, right, _state.Flags, operation);
        if (operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
        {
            if (directionToRegister)
            {
                SetByteRegister(register, result.Value);
            }
            else
            {
                _aluMemoryResult = result.Value;
            }
        }

        _state.Flags = result.Flags;
        ushort data = _aluMemoryBhe == 0 ? (ushort)(memoryValue << 8) : memoryValue;
        return new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "R--", "---", _aluMemoryBhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle WriteAluMemoryResult(ushort data)
    {
        _bus.Write(_aluMemoryPhysicalAddress, _aluMemoryResult);
        return new Intel8086BusCycle(0, _aluMemoryPhysicalAddress, _aluMemorySegmentStatus, "-AW", "---", _aluMemoryBhe, data, "PASV", "T3", '-', 0);
    }
}
