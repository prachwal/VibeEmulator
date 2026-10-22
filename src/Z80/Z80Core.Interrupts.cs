namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private enum InterruptKind : byte
    {
        None,
        Nmi,
        Im0,
        Im1,
        Im2,
    }

    private InterruptKind _interruptKind;
    private bool _interruptActive;
    private bool _resetApplied;
    private int _interruptElapsed;
    private int _interruptAcknowledgeTStates;
    private int _interruptDataTState;
    private ushort _interruptReturnAddress;
    private ushort _interruptVectorAddress;
    private byte _interruptVectorLow;
    private byte _interruptVectorHigh;
    private byte _interruptData;
    private byte _im0PrefixOpcode;
    private bool _im0InstructionActive;
    private bool _im0IndexCbNeedsAcknowledge;
    private bool _im0IndexCbAwaitingOpcode;
    private int _im0AcknowledgeCount;
    private int _im0TotalTStates;

    private void SampleNmiEdge()
    {
        if (!_lastNmiLevel && NmiLevel)
        {
            _pendingNmi = true;
        }

        _lastNmiLevel = NmiLevel;
    }

    private bool TryBeginInterrupt()
    {
        if (_pendingNmi)
        {
            BeginInterrupt(InterruptKind.Nmi);
            return true;
        }

        if (!IrqLevel || !_state.IFF1 || _state.EI != 0)
        {
            return false;
        }

        BeginInterrupt(_state.IM switch
        {
            1 => InterruptKind.Im1,
            2 => InterruptKind.Im2,
            _ => InterruptKind.Im0,
        });
        return true;
    }

    private void BeginInterrupt(InterruptKind kind)
    {
        _interruptKind = kind;
        _interruptActive = true;
        _interruptElapsed = 0;
        _interruptReturnAddress = _state.PC;
        _interruptAcknowledgeTStates = kind == InterruptKind.Nmi ? 5 : kind == InterruptKind.Im0 ? 6 : 7;
        _interruptDataTState = kind == InterruptKind.Nmi ? -1 : kind == InterruptKind.Im0 ? 3 : 4;
        _interruptData = InterruptDataBus;
        _im0PrefixOpcode = 0;
        _im0InstructionActive = false;
        _im0IndexCbNeedsAcknowledge = false;
        _im0IndexCbAwaitingOpcode = false;
        _im0AcknowledgeCount = 0;
        _im0TotalTStates = 0;
        _halted = false;
        _haltTState = 0;
        _instructionActive = false;
        InstructionPc = _state.PC;

        if (_ldAiRiCompleted)
        {
            _state.F &= 0xFB;
        }

        if (kind == InterruptKind.Nmi)
        {
            _pendingNmi = false;
            _state.IFF1 = false;
        }
        else
        {
            _state.IFF1 = false;
            _state.IFF2 = false;
            _state.EI = 0;
        }

        _ldAiRiCompleted = false;
    }

    private void TickInterruptCycle()
    {
        int tState = _interruptElapsed;
        if (tState < _interruptAcknowledgeTStates)
        {
            TickInterruptAcknowledge(tState);
            _interruptElapsed++;
            if (_interruptElapsed == _interruptAcknowledgeTStates)
            {
                CompleteInterruptAcknowledge();
            }

            return;
        }

        TickInterruptStackAndVector(tState - _interruptAcknowledgeTStates);
        _interruptElapsed++;
        if (_interruptElapsed == GetInterruptTStates())
        {
            CompleteInterruptSequence();
        }
    }

    private void TickInterruptAcknowledge(int tState)
    {
        if (_interruptKind == InterruptKind.Im0)
        {
            _im0TotalTStates++;
        }

        LastAddress = _interruptReturnAddress;
        LastData = null;
        LastPins = "----";

        if (_interruptKind != InterruptKind.Nmi && tState == _interruptDataTState)
        {
            _interruptData = InterruptDataBus;
            LastData = _interruptData;
            LastPins = "r--i";
        }

        if (tState == _interruptAcknowledgeTStates - 1)
        {
            _state.R = (byte)((_state.R + 1) & 0x7F);
            LastAddress = (ushort)((_state.I << 8) | _state.R);
        }
    }

    private void CompleteInterruptAcknowledge()
    {
        if (_interruptKind == InterruptKind.Im0)
        {
            CompleteIm0Acknowledge();
        }
    }

    private int GetInterruptTStates() => _interruptKind switch
    {
        InterruptKind.Nmi => 11,
        InterruptKind.Im1 => 13,
        InterruptKind.Im2 => 19,
        _ => _interruptAcknowledgeTStates,
    };

    private void TickInterruptStackAndVector(int tState)
    {
        LastAddress = _state.SP;
        LastData = null;
        LastPins = "----";

        if (tState == 1)
        {
            _state.SP--;
            LastAddress = _state.SP;
            LastData = (byte)(_interruptReturnAddress >> 8);
            LastPins = "-wm-";
            _bus.Write(_state.SP, LastData.Value);
            return;
        }

        if (tState == 4)
        {
            _state.SP--;
            LastAddress = _state.SP;
            LastData = (byte)_interruptReturnAddress;
            LastPins = "-wm-";
            _bus.Write(_state.SP, LastData.Value);
            return;
        }

        if (_interruptKind == InterruptKind.Im2 && tState == 7)
        {
            _interruptVectorAddress = (ushort)((_state.I << 8) | _interruptData);
            LastAddress = _interruptVectorAddress;
            LastPins = "r-m-";
            _interruptVectorLow = _bus.Read(_interruptVectorAddress);
            return;
        }

        if (_interruptKind == InterruptKind.Im2 && tState == 8)
        {
            LastAddress = _interruptVectorAddress;
            LastData = _interruptVectorLow;
            return;
        }

        if (_interruptKind == InterruptKind.Im2 && tState == 10)
        {
            LastAddress = (ushort)(_interruptVectorAddress + 1);
            LastPins = "r-m-";
            _interruptVectorHigh = _bus.Read((ushort)(_interruptVectorAddress + 1));
            return;
        }

        if (_interruptKind == InterruptKind.Im2 && tState == 11)
        {
            _state.PC = (ushort)(_interruptVectorLow | (_interruptVectorHigh << 8));
            _state.WZ = _state.PC;
            LastAddress = (ushort)(_interruptVectorAddress + 1);
            LastData = _interruptVectorHigh;
        }
    }

    private void CompleteInterruptSequence()
    {
        if (_interruptKind == InterruptKind.Nmi)
        {
            _state.PC = 0x0066;
        }
        else if (_interruptKind == InterruptKind.Im1)
        {
            _state.PC = 0x0038;
        }

        if (_interruptKind != InterruptKind.Im2)
        {
            _state.WZ = _state.PC;
        }

        _interruptKind = InterruptKind.None;
        _interruptActive = false;
        _instructionActive = false;
    }


}
