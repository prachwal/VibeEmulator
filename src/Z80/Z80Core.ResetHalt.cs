namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private bool TickResetLine()
    {
        if (!ResetLevel)
        {
            _resetHoldTStates = 0;
            _resetApplied = false;
            return false;
        }

        _lastNmiLevel = NmiLevel;
        _resetHoldTStates++;
        if (_resetHoldTStates >= 3 && !_resetApplied)
        {
            ApplyResetSignal();
            _resetApplied = true;
        }

        LastAddress = _state.PC;
        LastData = null;
        LastPins = "----";
        return true;
    }

    private void ApplyResetSignal()
    {
        _state.PC = 0;
        _state.I = 0;
        _state.R = 0;
        _state.IFF1 = false;
        _state.IFF2 = false;
        _state.IM = 0;
        _state.EI = 0;
        _halted = false;
        _jammed = false;
        _instructionActive = false;
        _interruptActive = false;
        _interruptKind = InterruptKind.None;
        _interruptElapsed = 0;
        _pendingNmi = false;
        _ldAiRiCompleted = false;
        _haltTState = 0;
        _im0InstructionActive = false;
        _im0IndexCbNeedsAcknowledge = false;
        _im0IndexCbAwaitingOpcode = false;
        _im0AcknowledgeCount = 0;
        _im0TotalTStates = 0;
        _elapsedTStates = 0;
        _instructionTStates = 0;
        _cbPrefixActive = false;
        _edPrefixActive = false;
        _indexPrefixActive = false;
        _indexCbPrefixActive = false;
        _indexIsIy = false;
    }

    private void TickHaltCycle()
    {
        ushort haltAddress = (ushort)(_state.PC - 1);
        switch (_haltTState)
        {
            case 0:
                LastAddress = haltAddress;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _bus.Read(haltAddress);
                LastAddress = haltAddress;
                LastData = null;
                LastPins = "r-m-";
                break;
            case 2:
                LastAddress = _refreshAddress;
                LastData = 0x76;
                LastPins = "----";
                break;
            default:
                LastAddress = _refreshAddress;
                LastData = null;
                LastPins = "----";
                _state.R = (byte)((_state.R + 1) & 0x7F);
                _refreshAddress = (ushort)((_state.I << 8) | _state.R);
                break;
        }

        _haltTState = (_haltTState + 1) & 3;
    }

}
