namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool _previousNmiLevel;
    private bool _nmiPending;
    private bool _hasExecutedInstruction;
    private bool _hardwareInterruptActive;
    private bool _hardwareInterruptIsIrq;

    private void SampleHardwareInterruptLines()
    {
        if (NmiLevel && !_previousNmiLevel)
        {
            _nmiPending = true;
        }

        _previousNmiLevel = NmiLevel;
    }

    private bool TryBeginHardwareInterrupt()
    {
        bool interruptEnabled = (_state.Flags & 0x0200) != 0;
        bool irqEnabled = interruptEnabled && !_intrDeferred;
        bool externalRequestPending = _nmiPending || IrqLevel && irqEnabled;
        bool acceptNmi = _hasExecutedInstruction && _nmiPending;
        bool acceptIrq = _hasExecutedInstruction && !_waitInterruptInhibit && !acceptNmi && IrqLevel && irqEnabled;
        bool acceptTrap = _trapPending && (!_hasExecutedInstruction || !externalRequestPending);
        if (!acceptNmi && !acceptIrq && !acceptTrap)
        {
            return false;
        }

        if (acceptNmi)
        {
            _nmiPending = false;
        }

        if (acceptTrap)
        {
            _trapPending = false;
        }

        _hardwareInterruptActive = true;
        _hardwareInterruptIsIrq = acceptIrq;
        _softwareInterruptIsHardware = true;
        _softwareInterruptIsDivideError = false;
        _softwareInterruptTaken = true;
        _softwareInterruptVector = acceptNmi ? (byte)2 : acceptIrq ? IrqVector : (byte)1;
        _softwareInterruptReturnIp = _waitingForTest ? _waitInstructionIp : _state.IP;
        _softwareInterruptSavedFlags = _state.Flags;
        _softwareInterruptInitialSp = _state.SP;
        _instructionPrefixLength = 0;
        _instructionPrefix = 0;
        _lockPrefix = false;
        _lockSignalActive = false;
        _lockStringPrefix = false;
        _stringRepeatZeroCount = false;
        _stringSegmentPrefix = false;
        _stringSegmentPrefixShortQueue = false;
        _stringCombinedPrefixPreludeComplete = false;
        _combinedDividePrefixPreludeComplete = false;
        _segmentOverride = Intel8086SegmentOverride.None;
        _memoryModRm = 0;
        _softwareInterruptEntryStart = acceptIrq ? 21 : 10;
        _queueCountAtStart = _state.QueueCount;
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + _state.QueueCount));
        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = 0xCC;

        bool alignedStack = (PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2))) & 1) == 0;
        _softwareInterruptFirstWriteStart = _softwareInterruptEntryStart + 15;
        _softwareInterruptSecondWriteStart = _softwareInterruptFirstWriteStart + (alignedStack ? 10 : 14);
        _softwareInterruptFlush = _softwareInterruptSecondWriteStart + (alignedStack ? 7 : 11);
        _softwareInterruptThirdWriteStart = _softwareInterruptFlush + 6;
        _softwareInterruptEnd = _softwareInterruptThirdWriteStart + (alignedStack ? 2 : 6);
        _instructionCycles = _softwareInterruptEnd;
        _instructionTState = 0;
        return true;
    }

    private void TickHardwareInterrupt()
    {
        bool wasInFlight = _instructionTState != 0;
        if (_hardwareInterruptIsIrq && TryTickInterruptAcknowledge())
        {
            return;
        }

        TickSoftwareInterrupt();
        if (wasInFlight && _instructionTState == 0)
        {
            _hardwareInterruptActive = false;
            _hardwareInterruptIsIrq = false;
            _softwareInterruptIsHardware = false;
            _hasExecutedInstruction = false;
        }
    }

    private bool TryTickInterruptAcknowledge()
    {
        int tState = _instructionTState + 1;
        if (tState is < 3 or > 13)
        {
            return false;
        }

        _instructionTState = tState;
        if (tState is >= 4 and <= 11)
        {
            _lockSignalActive = true;
        }
        else
        {
            _lockSignalActive = false;
        }

        string busStatus;
        string tStateName;
        ushort data = 0;
        if (tState is >= 3 and <= 6)
        {
            int phase = tState - 3;
            busStatus = phase == 0 ? "PASV" : "INTA";
            tStateName = $"T{phase + 1}";
        }
        else if (tState is >= 7 and <= 9)
        {
            busStatus = "PASV";
            tStateName = $"T{tState - 6}";
        }
        else
        {
            int phase = tState - 10;
            busStatus = phase == 0 ? "PASV" : "INTA";
            tStateName = $"T{phase + 1}";
            if (phase == 2)
            {
                data = IrqVector;
                _softwareInterruptVector = IrqVector;
            }
        }

        LastBusCycle = new Intel8086BusCycle(
            0, 0, "CS", "---", "---", 0, data, busStatus, tStateName, '-', 0);
        return true;
    }

}
