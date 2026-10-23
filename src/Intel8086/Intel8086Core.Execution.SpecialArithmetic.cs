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
}
