namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private sbyte _conditionalBranchDisplacement;
    private ushort _conditionalBranchTarget;
    private bool _conditionalBranchTaken;

    // Na P80C86A-2 bajty 60h–6Fh są aliasami Jcc 70h–7Fh. Warunek zależy
    // od dolnych czterech bitów, więc ten sam dekoder obsługuje oba zakresy.
    private static bool IsConditionalBranchOpcode(byte opcode) => opcode is >= 0x60 and <= 0x7F;

    private void InitializeConditionalBranch(byte opcode)
    {
        if (_state.QueueCount < 2)
        {
            throw new InvalidOperationException("Jcc requires the displacement byte in the prefetch queue.");
        }

        int prefixLength = _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 1 ? 1 : 0;
        _conditionalBranchDisplacement = unchecked((sbyte)_state.Queue[_state.QueueHead + prefixLength + 1]);
        _conditionalBranchTarget = unchecked((ushort)(_state.IP + 2 + prefixLength + _conditionalBranchDisplacement));
        _conditionalBranchTaken = Intel8086Condition.IsSatisfied((byte)(opcode & 0x0F), _state.Flags);
        int segmentPrefixCycles = prefixLength == 0 ? 0 : 2;
        _instructionCycles = (!_conditionalBranchTaken ? 4 : _queueCountAtStart <= 5 ? 17 : 19) + segmentPrefixCycles;
    }

    private void ResetConditionalBranchState()
    {
        _conditionalBranchDisplacement = 0;
        _conditionalBranchTarget = 0;
        _conditionalBranchTaken = false;
    }

    private void TickConditionalBranch()
    {
        _instructionTState++;
        int branchTState = _instructionTState;
        bool hasSegmentPrefix = _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 1;
        if (hasSegmentPrefix && _instructionTState <= 2)
        {
            if (_instructionTState == 1)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
            }
            else
            {
                LastBusCycle = IdleCycle('-', 0);
            }

            return;
        }

        if (hasSegmentPrefix)
        {
            branchTState -= 2;
        }

        bool shortQueue = _queueCountAtStart <= 5;
        int speculativeFetchStart = shortQueue ? 3 : 5;
        int branchDecisionTState = shortQueue ? 11 : 13;
        int targetFetchStart = shortQueue ? 13 : 15;
        int completionFetchTState = shortQueue ? 17 : 19;

        if (branchTState == 1)
        {
            PopQueue();
            _state.IP++;
            if (hasSegmentPrefix && !shortQueue)
            {
                uint fetchAddress = _nextFetchAddress;
                LastBusCycle = FetchCycle("T1", 'F', Opcode);
                PushQueue(0x90);
                PushQueue(0x90);
                _nextFetchAddress = (fetchAddress + 2) & 0xFFFFF;
            }
            else
            {
                LastBusCycle = IdleCycle('F', Opcode);
            }

            return;
        }

        if (branchTState == 2)
        {
            LastBusCycle = hasSegmentPrefix && !shortQueue
                ? FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
            return;
        }

        if (branchTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = hasSegmentPrefix && !shortQueue
                ? ConditionalBranchDisplacementCycle()
                : shortQueue
                    ? FetchCycle("T1", 'S', unchecked((byte)_conditionalBranchDisplacement))
                    : IdleCycle('S', unchecked((byte)_conditionalBranchDisplacement));
            return;
        }

        if (branchTState == 4)
        {
            LastBusCycle = hasSegmentPrefix && !shortQueue
                ? ConditionalBranchDisplacementContinuationCycle()
                : shortQueue
                    ? FetchContinuationCycle('-', 0)
                    : IdleCycle('-', 0);
            if (!_conditionalBranchTaken)
            {
                PopQueue();
                _instructionTState = 0;
            }

            return;
        }

        if (!_conditionalBranchTaken)
        {
            throw new InvalidOperationException("A not-taken Jcc exceeded its four T-states.");
        }

        int speculativeFetchPhase = branchTState - speculativeFetchStart;
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

        if (branchTState == branchDecisionTState)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.IP = _conditionalBranchTarget;
            _nextFetchAddress = PhysicalAddress(InstructionCs, _conditionalBranchTarget);
            LastBusCycle = IdleCycle('E', unchecked((byte)_conditionalBranchDisplacement));
            return;
        }

        if (branchTState == branchDecisionTState + 1)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (branchTState >= targetFetchStart && branchTState < targetFetchStart + 4)
        {
            int targetFetchPhase = branchTState - targetFetchStart;
            LastBusCycle = targetFetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => ConditionalBranchTargetDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (targetFetchPhase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                int bytesFetched = (_conditionalBranchTarget & 1) == 0 ? 2 : 1;
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
            }

            return;
        }

        if (branchTState == completionFetchTState)
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

    private Intel8086BusCycle ConditionalBranchTargetDataCycle() => new(
        0,
        0,
        "CS",
        "R--",
        "---",
        0,
        (_conditionalBranchTarget & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV",
        "T3",
        '-',
        0);

    private Intel8086BusCycle ConditionalBranchDisplacementCycle() => new(
        0,
        0,
        "CS",
        "R--",
        "---",
        0,
        37008,
        "PASV",
        "T3",
        'S',
        unchecked((byte)_conditionalBranchDisplacement));

    private static Intel8086BusCycle ConditionalBranchDisplacementContinuationCycle() => new(
        0,
        0,
        "CS",
        "---",
        "---",
        0,
        0,
        "PASV",
        "T4",
        '-',
        0);
}
