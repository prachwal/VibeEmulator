namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickRepeatedCmpsWord()
    {
        _instructionTState++;
        uint sourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
        uint destinationAddress = PhysicalAddress(_state.ES, _state.DI);
        bool splitSourceRead = (sourceAddress & 1) != 0;
        bool splitDestinationRead = (destinationAddress & 1) != 0;
        int sourceReadCycles = splitSourceRead ? 8 : 4;
        int destinationReadCycles = splitDestinationRead ? 8 : 4;
        int destinationPhaseStart = sourceReadCycles + 4;
        int iterationCycles = destinationPhaseStart + destinationReadCycles + 10;
        int iterationPhase = (_instructionTState - 16) % iterationCycles;
        if (_instructionTState < 16)
        {
            if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
            {
                LastBusCycle = _instructionTState switch
                {
                    3 => FetchRepeatedMovsOpcodeCycle(),
                    4 => FetchContinuationCycle('-', 0),
                    5 => FetchStringQueueData(),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
            }
            else
            {
                LastBusCycle = _instructionTState switch
                {
                    1 => ConsumeRepeatedMovsPrefixCycle(),
                    2 => IdleCycle('-', 0),
                    3 => ConsumeRepeatedMovsOpcodeCycle(),
                    4 => IdleCycle('-', 0),
                    5 => FetchCycle("T1", '-', 0),
                    6 => FetchContinuationCycle('-', 0),
                    7 => _queueCountAtStart <= 5 ? IdleCycle('-', 0) : FetchRepeatedMovsDataCycle(),
                    8 => _queueCountAtStart <= 5
                        ? IdleCycle('-', 0)
                        : new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                    _ => IdleCycle('-', 0),
                };
            }

            return;
        }

        if (iterationPhase == 0)
        {
            _stringSourceAddress = sourceAddress;
            _stringDestinationAddress = destinationAddress;
            _stringSecondReadStart = _instructionTState + destinationPhaseStart;
        }

        if (iterationPhase < sourceReadCycles)
        {
            uint address = splitSourceRead && iterationPhase >= 4
                ? GetStringWordSecondByteAddress(_stringSourceAddress, GetStringSourceSegment())
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, iterationPhase % 4);
            return;
        }

        int destinationPhase = iterationPhase - destinationPhaseStart;
        if (destinationPhase is >= 0 && destinationPhase < destinationReadCycles)
        {
            uint address = splitDestinationRead && destinationPhase >= 4
                ? GetStringWordSecondByteAddress(_stringDestinationAddress, _state.ES)
                : _stringDestinationAddress;
            LastBusCycle = TickStringRead(address, destinationPhase % 4);
            if (destinationPhase == destinationReadCycles - 1)
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareWordFirst, _stringCompareWordSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
                int delta = (_state.Flags & 0x0400) == 0 ? 2 : -2;
                _state.SI = unchecked((ushort)(_state.SI + delta));
                _state.DI = unchecked((ushort)(_state.DI + delta));
                _state.CX--;

                bool repeatConditionSatisfied = (_instructionPrefix == 0xF2
                    ? (_state.Flags & 0x0040) == 0
                    : (_state.Flags & 0x0040) != 0);
                if (!repeatConditionSatisfied)
                {
                    _instructionCycles = _instructionTState + 5;
                }
            }

            return;
        }

        LastBusCycle = IdleCycle('-', 0);
        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }
}
