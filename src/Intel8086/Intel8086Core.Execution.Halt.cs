namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickHalt()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        _halted = true;
        _instructionTState = 0;
        LastBusCycle = new Intel8086BusCycle(1, 0, "CS", "---", "---", 0, 0, "HALT", "T1", '-', 0);
    }
}
