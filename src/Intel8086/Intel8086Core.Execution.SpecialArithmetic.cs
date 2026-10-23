namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickCbw()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        _state.AX = Intel8086SpecialArithmetic.Cbw(_state.AX);
        if (_state.QueueCount != 0)
        {
            PopQueue();
        }

        LastBusCycle = IdleCycle('-', 0);
        _instructionTState = 0;
    }

    private void TickCwd()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        bool prefetchAvailable = _queueCountAtStart <= 5;
        LastBusCycle = prefetchAvailable
            ? _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            }
            : IdleCycle('-', 0);

        if (_instructionTState == _instructionCycles)
        {
            _state.DX = Intel8086SpecialArithmetic.Cwd(_state.AX);
            if (prefetchAvailable && _state.AX >= 0x8000)
            {
                PushQueue(0x90);
                PushQueue(0x90);
            }

            _instructionTState = 0;
        }
    }
}
