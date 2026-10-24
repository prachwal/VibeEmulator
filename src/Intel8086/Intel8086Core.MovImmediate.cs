namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMovImmediate()
    {
        int mod = _movImmediateModRm >> 6;
        if (mod == 3)
        {
            TickMovImmediateRegister();
            return;
        }

        bool consumesOpcode = false;
        bool consumesModRm = false;
        bool consumesDisplacement = false;
        bool consumesImmediate = false;
        byte queueByte = 0;
        if (_movImmediatePrefixLength == 1)
        {
            if (_instructionTState == 1)
            {
                queueByte = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', queueByte);
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
            LastBusCycle = IdleCycle('S', _movImmediateModRm);
            return;
        }

        int displacementIndex = _instructionTState - _movImmediateDisplacementStart;
        if (displacementIndex >= 0 && displacementIndex < _movImmediateDisplacementBytes)
        {
            queueByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            consumesDisplacement = true;
        }

        int immediateState = GetMovEffectiveAddressCycles(mod, _movImmediateModRm & 7)
            + (mod == 0 ? 3 : 7) + _movImmediatePrefixLength * 2;
        if (_instructionTState == immediateState)
        {
            queueByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _movImmediateValue = queueByte;
            consumesImmediate = true;
        }

        int rm = _movImmediateModRm & 7;
        (int firstFetch, int secondFetch, int thirdFetch) = GetMovImmediateFetchStarts(
            mod, rm, _queueCountAtStart, _movImmediatePrefixLength == 1);
        int fetchStart = IsFetchWindow(_instructionTState, firstFetch)
            ? firstFetch
            : secondFetch != 0 && IsFetchWindow(_instructionTState, secondFetch)
                ? secondFetch
                : thirdFetch != 0 && IsFetchWindow(_instructionTState, thirdFetch) ? thirdFetch : 0;
        int writePhase = _instructionTState - _movImmediateWriteStart;
        if (writePhase is >= 0 and <= 2)
        {
            fetchStart = 0;
        }

        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesOpcode ? 'F' : consumesDisplacement || consumesImmediate ? 'S' : '-',
                    consumesOpcode ? Opcode : queueByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesModRm || consumesDisplacement || consumesImmediate ? 'S' : '-',
                    QueueData = consumesModRm ? _movImmediateModRm : queueByte,
                },
                2 => FetchMovImmediateDataCycle(consumesDisplacement || consumesImmediate ? 'S' : '-', queueByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4",
                    consumesDisplacement || consumesImmediate ? 'S' : '-', queueByte),
            };
            if (phase == 2)
            {
                QueueMovImmediatePrefetchBytes();
            }
        }
        else if (_movImmediatePrefixLength == 1 && consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_movImmediatePrefixLength == 1 && consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _movImmediateModRm);
        }
        else if (writePhase is >= 0 and <= 2)
        {
            ushort data = _movImmediateBhe == 0 ? (ushort)(_movImmediateValue << 8) : _movImmediateValue;
            LastBusCycle = writePhase switch
            {
                0 => new Intel8086BusCycle(1, _movImmediatePhysicalAddress, "--", "---", "---", _movImmediateBhe, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _movImmediatePhysicalAddress, _movImmediateSegmentStatus, "-A-", "---", _movImmediateBhe, 0, "MEMW", "T2", '-', 0),
                _ => WriteMovImmediateData(data),
            };
        }
        else if (consumesDisplacement || consumesImmediate)
        {
            LastBusCycle = IdleCycle('S', queueByte);
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

    private void QueueMovImmediatePrefetchBytes()
    {
        int bytes = Math.Min(2, _movImmediatePrefetchBytesRemaining);
        if (!_movImmediatePrefetchWordReady)
        {
            throw new InvalidOperationException("Prefetch data cycle was not prepared.");
        }

        int codeBytesAtStart = _movImmediateCodeBytesRemaining;
        int queuedBytes = 0;
        while (bytes > 0 && _state.QueueCount < _state.Queue.Length)
        {
            byte value = queuedBytes < codeBytesAtStart
                ? (byte)(_movImmediatePrefetchWord >> (queuedBytes * 8))
                : (byte)0x90;
            PushQueue(value);
            queuedBytes++;
            if (_movImmediateCodeBytesRemaining > 0)
            {
                _movImmediateCodeBytesRemaining--;
            }
            _movImmediatePrefetchBytesRemaining--;
            bytes--;
        }

        _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        _movImmediatePrefetchWordReady = false;
    }

    private Intel8086BusCycle FetchMovImmediateDataCycle(char queueStatus, byte queueData)
    {
        byte low = _movImmediateCodeBytesRemaining > 0 ? _bus.Read(_nextFetchAddress) : (byte)0x90;
        byte high = _movImmediateCodeBytesRemaining > 1 ? _bus.Read((_nextFetchAddress + 1) & 0xFFFFF) : (byte)0x90;
        _movImmediatePrefetchWord = (ushort)(low | (high << 8));
        _movImmediatePrefetchWordReady = true;
        return new Intel8086BusCycle(0, _nextFetchAddress, "CS", "R--", "---", 0, _movImmediatePrefetchWord, "PASV", "T3", queueStatus, queueData);
    }

    private Intel8086BusCycle WriteMovImmediateData(ushort data)
    {
        _bus.Write(_movImmediatePhysicalAddress, _movImmediateValue);
        return new Intel8086BusCycle(0, _movImmediatePhysicalAddress, _movImmediateSegmentStatus, "-AW", "---", _movImmediateBhe, data, "PASV", "T3", '-', 0);
    }

}
