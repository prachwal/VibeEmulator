namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMemoryMov()
    {
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_movMemoryPrefixLength == 1)
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
        else
        {
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
                LastBusCycle = IdleCycle('S', _movMemoryModRm);
                return;
            }
        }

        int displacementIndex = _instructionTState - _movMemoryDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _movMemoryDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        int firstFetchStart = _queueCountAtStart <= 5 ? 3 : _movMemoryPrefixLength == 1 ? 5 : 4;
        int mod = _movMemoryModRm >> 6;
        int rm = _movMemoryModRm & 7;
        (int secondFetchStart, int thirdFetchStart) = _movMemoryPrefixLength == 1
            ? GetPrefixedMovFetchStarts(Opcode, mod, rm, _queueCountAtStart)
            : GetUnprefixedMovFetchStarts(Opcode, mod, rm, firstFetchStart, _queueCountAtStart, _movMemoryBusStart);
        bool queuesSecondByte = _queueCountAtStart == 5 && mod is 1 or 2
            && secondFetchStart != 0 && secondFetchStart + 2 <= _instructionCycles;
        int fetchStart = IsFetchWindow(_instructionTState, firstFetchStart)
            ? firstFetchStart
            : secondFetchStart != 0 && IsFetchWindow(_instructionTState, secondFetchStart)
                ? secondFetchStart
                : thirdFetchStart != 0 && IsFetchWindow(_instructionTState, thirdFetchStart)
                    ? thirdFetchStart
                : 0;
        int memoryPhase = _instructionTState - _movMemoryBusStart;
        bool memoryBusActive = Opcode == 0x88
            ? memoryPhase is >= 0 and <= 2
            : memoryPhase is >= 0 and <= 3;
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
                    QueueStatus = consumesModRm ? 'S' : consumesDisplacement ? 'S' : '-',
                    QueueData = consumesModRm ? _movMemoryModRm : displacementByte,
                },
                2 => FetchDataCycle(consumesDisplacement ? 'S' : '-', displacementByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumesDisplacement ? 'S' : '-', displacementByte),
            };
            bool shouldQueueFetchedByte = fetchStart == firstFetchStart || queuesSecondByte
                || (fetchStart != firstFetchStart && ((_queueCountAtStart == 6 && mod == 2) || (Opcode == 0x88 && mod == 0 && rm == 6)));
            if (phase == 2 && _movMemoryPrefixLength == 1)
            {
                int bytesToQueue = Math.Min(2, _movMemoryPrefetchBytesRemaining);
                while (bytesToQueue > 0 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                    _movMemoryPrefetchBytesRemaining--;
                    bytesToQueue--;
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
            else if (phase == 2 && shouldQueueFetchedByte && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
                if (fetchStart == firstFetchStart && queuesSecondByte && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                if (fetchStart != firstFetchStart && _queueCountAtStart == 6 && mod == 2)
                {
                    PushQueue(0x90);
                }

                if (fetchStart != firstFetchStart && Opcode == 0x88 && mod == 0 && rm == 6)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else
        {
            if (_movMemoryPrefixLength == 1 && consumesOpcode)
            {
                LastBusCycle = IdleCycle('F', Opcode);
            }
            else if (_movMemoryPrefixLength == 1 && consumesModRm)
            {
                LastBusCycle = IdleCycle('S', _movMemoryModRm);
            }
            else if (Opcode == 0x88 && memoryPhase is >= 0 and <= 2)
            {
                LastBusCycle = memoryPhase switch
                {
                    0 => new Intel8086BusCycle(1, _movMemoryPhysicalAddress, "--", "---", "---", _movMemoryBhe, 0, "MEMW", "T1", '-', 0),
                    1 => new Intel8086BusCycle(0, _movMemoryPhysicalAddress, _movMemorySegmentStatus, "-A-", "---", _movMemoryBhe, 0, "MEMW", "T2", '-', 0),
                    _ => TickMemoryMovWriteData(),
                };
            }
            else if (Opcode == 0x8A && memoryPhase is >= 0 and <= 3)
            {
                LastBusCycle = memoryPhase switch
                {
                    0 => new Intel8086BusCycle(1, _movMemoryPhysicalAddress, "--", "---", "---", _movMemoryBhe, 0, "MEMR", "T1", '-', 0),
                    1 => new Intel8086BusCycle(0, _movMemoryPhysicalAddress, _movMemorySegmentStatus, "R--", "---", _movMemoryBhe, 0, "MEMR", "T2", '-', 0),
                    2 => TickMemoryMovReadData(),
                    _ => new Intel8086BusCycle(0, _movMemoryPhysicalAddress, _movMemorySegmentStatus, "---", "---", _movMemoryBhe, 0, "PASV", "T4", '-', 0),
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
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private static bool IsFetchWindow(int state, int fetchStart) => state >= fetchStart && state <= fetchStart + 3;

}
