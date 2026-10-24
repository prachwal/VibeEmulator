namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickSegmentPrefixedRegisterMove()
    {
        _instructionTState++;
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
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'F', Opcode)
                : IdleCycle('F', Opcode);
        }
        else
        {
            PopQueue();
            PopQueue();
            _state.IP++;
            ExecuteRegisterMove(_memoryModRm);
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchContinuationCycle('S', _memoryModRm)
                : IdleCycle('S', _memoryModRm);
            _instructionTState = 0;
        }
    }

    private void ExecuteRegisterMove(byte modRm)
    {
        int reg = (modRm >> 3) & 7;
        int rm = modRm & 7;
        if (Opcode is 0x88 or 0x8A)
        {
            SetByteRegister(Opcode == 0x88 ? rm : reg, GetByteRegister(Opcode == 0x88 ? reg : rm));
        }
        else
        {
            SetWordRegister(Opcode == 0x89 ? rm : reg, GetWordRegister(Opcode == 0x89 ? reg : rm));
        }
    }
}
