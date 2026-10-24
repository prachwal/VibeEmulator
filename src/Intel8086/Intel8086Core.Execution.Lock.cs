namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickLockPrefixedNop()
    {
        _instructionTState++;
        switch (_instructionTState)
        {
            case 1:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
                break;
            case 2:
                LastBusCycle = IdleCycle('-', 0);
                break;
            case 3:
                PopQueue();
                _state.IP++;
                LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                    ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                    : IdleCycle('F', Opcode);
                break;
            case 4:
                LastBusCycle = IdleCycle('-', 0);
                break;
            default:
                LastBusCycle = ShouldFetchAtInstructionEnd()
                    ? FetchCycle("T1", '-', 0)
                    : IdleCycle('-', 0);
                _instructionTState = 0;
                break;
        }
    }

    private void TickLockPrefixedString()
    {
        if (_instructionTState < 2)
        {
            _instructionTState++;
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

        _instructionTState -= 2;
        _instructionCycles -= 2;
        TickMoveString();
        if (_instructionTState != 0)
        {
            _instructionTState += 2;
        }

        _instructionCycles += 2;
    }
}
