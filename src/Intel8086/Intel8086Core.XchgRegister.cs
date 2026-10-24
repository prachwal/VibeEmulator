namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickRegisterXchg()
    {
        if (_xchgPrefixLength == 0)
        {
            TickUnprefixedRegisterXchg();
            return;
        }

        TickPrefixedRegisterXchg();
    }

    private void TickUnprefixedRegisterXchg()
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
            ExchangeByteRegisters();
            LastBusCycle = IdleCycle('S', _xchgModRm);
        }
        else if (_instructionTState == 3 && _queueCountAtStart == 5)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
        }
        else if (_instructionTState == 4 && _queueCountAtStart == 5)
        {
            LastBusCycle = FetchContinuationCycle();
        }
        else if (_instructionTState == 4)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            PopQueue();
            _instructionTState = 0;
        }
    }

    private void TickPrefixedRegisterXchg()
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
            ExchangeByteRegisters();
            LastBusCycle = _queueCountAtStart == 5
                ? FetchContinuationCycle() with { QueueStatus = 'S', QueueData = _xchgModRm }
                : IdleCycle('S', _xchgModRm);
        }
        else if (_instructionTState == 5 && _queueCountAtStart == 5)
        {
            LastBusCycle = FetchDataCycle('-', 0);
            if (_state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (_instructionTState == 6 && _queueCountAtStart == 5)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
        }
        else if (_instructionTState == 5)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
        }
        else
        {
            LastBusCycle = FetchContinuationCycle();
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart == 6)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private void ExchangeByteRegisters()
    {
        int rm = _xchgModRm & 7;
        int reg = (_xchgModRm >> 3) & 7;
        byte rmValue = GetByteRegister(rm);
        byte regValue = GetByteRegister(reg);
        SetByteRegister(rm, regValue);
        SetByteRegister(reg, rmValue);
    }
}
