namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMovImmediateRegister()
    {
        if (_movImmediatePrefixLength == 0)
        {
            TickUnprefixedMovImmediateRegister();
        }
        else
        {
            TickPrefixedMovImmediateRegister();
        }
    }

    private void TickUnprefixedMovImmediateRegister()
    {
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _movImmediateModRm);
        }
        else if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            SetByteRegister(_movImmediateModRm & 7, _movImmediateValue);
            LastBusCycle = _queueCountAtStart == 5
                ? FetchCycle("T1", 'S', _movImmediateValue)
                : IdleCycle('S', _movImmediateValue);
        }
        else if (_instructionTState == 4)
        {
            if (_state.QueueCount > 0)
            {
                PopQueue();
            }

            LastBusCycle = _queueCountAtStart == 5
                ? FetchContinuationCycle()
                : FetchCycle("T1", '-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private void TickPrefixedMovImmediateRegister()
    {
        if (_instructionTState == 1)
        {
            byte prefix = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', prefix);
        }
        else if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart == 5
                ? FetchCycle("T1", 'F', Opcode)
                : IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 4)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart == 5
                ? FetchContinuationCycle() with { QueueStatus = 'S', QueueData = _movImmediateModRm }
                : IdleCycle('S', _movImmediateModRm);
        }
        else if (_instructionTState == 5)
        {
            PopQueue();
            _state.IP++;
            SetByteRegister(_movImmediateModRm & 7, _movImmediateValue);
            LastBusCycle = _queueCountAtStart == 5
                ? FetchDataCycle('S', _movImmediateValue)
                : FetchCycle("T1", 'S', _movImmediateValue);
            if (_queueCountAtStart == 5)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (_instructionTState == 6)
        {
            if (_queueCountAtStart == 6 && _state.QueueCount > 0)
            {
                PopQueue();
            }

            LastBusCycle = _queueCountAtStart == 5
                ? new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0)
                : FetchContinuationCycle();
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

}
