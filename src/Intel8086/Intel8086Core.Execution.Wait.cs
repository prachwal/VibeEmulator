namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickWaitInstruction()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            _waitInstructionIp = _state.IP;
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState < _instructionCycles)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        LastBusCycle = IdleCycle('-', 0);
        if (TestLevel)
        {
            _waitingForTest = true;
            _waitPollClock = 0;
            return;
        }

        _instructionTState = 0;
    }

    private void TickWaitForTest()
    {
        if (_nmiPending || IrqLevel && (_state.Flags & 0x0200) != 0 || _trapPending)
        {
            _instructionTState = 0;
            if (TryBeginHardwareInterrupt())
            {
                _waitingForTest = false;
                TickHardwareInterrupt();
                return;
            }

            _instructionTState = _instructionCycles;
        }

        LastBusCycle = IdleCycle('-', 0);
        _waitPollClock++;
        if (_waitPollClock < 5)
        {
            return;
        }

        _waitPollClock = 0;
        if (TestLevel)
        {
            return;
        }

        _waitingForTest = false;
        _instructionTState = 0;
        _waitInterruptInhibit = true;
        if (_trapFlagAtInstructionStart)
        {
            _trapPending = true;
        }
    }
}
