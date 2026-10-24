namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickStringRepeatZeroCount()
    {
        _instructionTState++;
        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchRepeatedMovsOpcodeCycle(),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState >= 7)
        {
            LastBusCycle = IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }

            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
        }
        else if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 4)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (_instructionTState == 5)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
        }
        else if (_instructionTState == 6)
        {
            LastBusCycle = FetchContinuationCycle('-', 0);
        }
        else if (_instructionTState == 7)
        {
            LastBusCycle = FetchDataCycle();
            PushQueue(0x90);
            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }
        else if (_instructionTState == 8)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
    }

    private void TickRepeatedScasByte()
    {
        _instructionTState++;
        switch (_instructionTState)
        {
            case 1:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
                return;
            case 2:
                LastBusCycle = IdleCycle('-', 0);
                return;
            case 3:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', Opcode);
                return;
            case 4:
                LastBusCycle = IdleCycle('-', 0);
                return;
            case 5:
                LastBusCycle = FetchCycle("T1", '-', 0);
                return;
            case 6:
                LastBusCycle = FetchContinuationCycle('-', 0);
                return;
            case 7:
                LastBusCycle = FetchDataCycle();
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
                return;
            case 8:
                LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
                return;
        }

        int iterationPhase = (_instructionTState - 17) % 15;
        if (iterationPhase is >= 0 and < 4)
        {
            _stringSourceAddress = PhysicalAddress(_state.ES, _state.DI);
            LastBusCycle = TickStringRead(_stringSourceAddress, iterationPhase);
            if (iterationPhase == 3)
            {
                _state.Flags = Intel8086Alu.Execute((byte)_state.AX, _stringByteValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
                int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
                _state.DI = unchecked((ushort)(_state.DI + delta));
                _state.CX--;

                bool keepRepeating = _state.CX != 0 && (_instructionPrefix == 0xF2
                    ? (_state.Flags & 0x0040) == 0
                    : (_state.Flags & 0x0040) != 0);
                if (!keepRepeating)
                {
                    _instructionCycles = _instructionTState + 6;
                }
            }
            return;
        }

        LastBusCycle = IdleCycle('-', 0);
        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

}
