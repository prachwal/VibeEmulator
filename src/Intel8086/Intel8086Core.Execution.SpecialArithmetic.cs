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

    private void TickDecimalAdjust()
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

        bool fetchAvailable = _queueCountAtStart <= 5;
        LastBusCycle = fetchAvailable && _instructionTState <= (Opcode is 0x27 or 0x2F ? 4 : 6)
            ? _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            }
            : IdleCycle('-', 0);

        if (fetchAvailable && Opcode is 0x37 or 0x3F && _instructionTState == 5)
        {
            PushQueue(0x90);
            PushQueue(0x90);
        }

        if (_instructionTState == _instructionCycles)
        {
            Intel8086SpecialArithmeticResult result = Opcode switch
            {
                0x27 => Intel8086SpecialArithmetic.Daa(_state.AX, _state.Flags),
                0x2F => Intel8086SpecialArithmetic.Das(_state.AX, _state.Flags),
                0x37 => Intel8086SpecialArithmetic.Aaa(_state.AX, _state.Flags),
                0x3F => Intel8086SpecialArithmetic.Aas(_state.AX, _state.Flags),
                _ => throw new InvalidOperationException($"Unexpected decimal-adjust opcode {Opcode:X2}."),
            };
            _state.AX = result.AX;
            _state.Flags = result.Flags;
            _instructionTState = 0;
        }
    }
}
