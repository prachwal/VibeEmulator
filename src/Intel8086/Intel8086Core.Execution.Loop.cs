namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private sbyte _loopDisplacement;
    private ushort _loopTarget;
    private bool _loopTaken;

    private static bool IsLoopOpcode(byte opcode) => opcode is >= 0xE0 and <= 0xE3;

    private void InitializeLoop(byte opcode)
    {
        if (_state.QueueCount < 2)
        {
            throw new InvalidOperationException("LOOP/JCXZ requires the displacement byte in the prefetch queue.");
        }

        _loopDisplacement = unchecked((sbyte)_state.Queue[_state.QueueHead + 1]);
        _loopTarget = unchecked((ushort)(_state.IP + 2 + _loopDisplacement));
        if (opcode != 0xE3)
        {
            _state.CX--;
        }

        _loopTaken = opcode switch
        {
            0xE0 => _state.CX != 0 && (_state.Flags & 0x0040) == 0,
            0xE1 => _state.CX != 0 && (_state.Flags & 0x0040) != 0,
            0xE2 => _state.CX != 0,
            _ => _state.CX == 0,
        };
        bool shortQueue = _queueCountAtStart <= 5;
        _instructionCycles = !_loopTaken ? 6 : opcode switch
        {
            0xE2 => 17,
            0xE3 => 18,
            _ => shortQueue ? 18 : 21,
        };
    }

    private void TickLoop()
    {
        _instructionTState++;
        bool shortQueue = _queueCountAtStart <= 5;
        int speculativeFetchStart = shortQueue ? 3 : Opcode == 0xE2 ? int.MaxValue : 7;
        int branchDecisionTState = Opcode switch
        {
            0xE2 => 11,
            0xE3 => 12,
            _ => shortQueue ? 12 : 15,
        };
        int targetFetchStart = branchDecisionTState + 2;
        int completionFetchTState = _instructionCycles;

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionTState is 3 or 4)
        {
            LastBusCycle = shortQueue
                ? _instructionTState == 3 ? FetchCycle("T1", '-', 0) : FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
            return;
        }

        if (_instructionTState == 5)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = shortQueue
                ? FetchDataCycle() with { QueueStatus = 'S', QueueData = unchecked((byte)_loopDisplacement) }
                : IdleCycle('S', unchecked((byte)_loopDisplacement));
            if (!_loopTaken && shortQueue)
            {
                uint fetchAddress = _nextFetchAddress;
                int bytesFetched = (fetchAddress & 1) == 0 ? 2 : 1;
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            }

            return;
        }

        if (!_loopTaken)
        {
            if (_instructionTState == 6)
            {
                LastBusCycle = shortQueue
                    ? new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0)
                    : IdleCycle('-', 0);
                if (_state.QueueCount != 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
                return;
            }

            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int speculativeFetchPhase = _instructionTState - speculativeFetchStart;
        if (speculativeFetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = speculativeFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (speculativeFetchPhase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                PushQueue(_bus.Read(fetchAddress));
                PushQueue(_bus.Read((fetchAddress + 1) & 0xFFFFF));
                _nextFetchAddress = (fetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

        if (_instructionTState == branchDecisionTState)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.IP = _loopTarget;
            _nextFetchAddress = PhysicalAddress(InstructionCs, _loopTarget);
            LastBusCycle = IdleCycle('E', unchecked((byte)_loopDisplacement));
            return;
        }

        if (_instructionTState == branchDecisionTState + 1)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int targetFetchPhase = _instructionTState - targetFetchStart;
        if (targetFetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = targetFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => LoopTargetDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (targetFetchPhase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                int bytesFetched = (_loopTarget & 1) == 0 ? 2 : 1;
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            }

            return;
        }

        if (_instructionTState == completionFetchTState)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle LoopTargetDataCycle() => new(
        0, 0, "CS", "R--", "---", 0,
        (_loopTarget & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV", "T3", '-', 0);
}
