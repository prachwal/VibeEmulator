namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickResetHigh()
    {
        if (!_resetPinWasHigh)
        {
            long cycleCount = _cycles;
            ResetHardware();
            _cycles = cycleCount;
            _resetPinWasHigh = true;
            _resetPinHighClocks = 0;
        }

        _resetPinHighClocks++;
        _cycles++;
        string busStatus = _resetPinHighClocks == 1 ? "PASV" : "---";
        LastBusCycle = new Intel8086BusCycle(0, 0, "--", "---", "---", 0, 0, busStatus, "---", '-', 0);
    }

    private void TickResetSequence()
    {
        _cycles++;
        if (_resetReleaseClocks > 0)
        {
            _resetReleaseClocks--;
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        _resetFetchTState++;
        switch (_resetFetchTState)
        {
            case 1:
                InstructionCs = 0xFFFF;
                InstructionPc = 0;
                _nextFetchAddress = 0xFFFF0;
                LastBusCycle = FetchCycle("T1", '-', 0);
                break;
            case 2:
                LastBusCycle = FetchContinuationCycle('-', 0);
                break;
            case 3:
                ushort word = (ushort)(_bus.Read(0xFFFF0) | (_bus.Read(0xFFFF1) << 8));
                _state.QueueHead = 0;
                _state.Queue[0] = (byte)word;
                _state.Queue[1] = (byte)(word >> 8);
                _state.QueueCount = 2;
                _nextFetchAddress = 0xFFFF2;
                LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "R--", "---", 0, word, "CODE", "T3", 'F', (byte)word);
                break;
            default:
                LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
                _resetSequenceActive = false;
                _resetFetchTState = 0;
                break;
        }
    }
}
