namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickRepeatedCmpsByte()
    {
        _instructionTState++;
        int iterationPhase = (_instructionTState - 16) % 22;
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
            _stringSourceAddress = PhysicalAddress(_state.DS, _state.SI);
            _stringDestinationAddress = PhysicalAddress(_state.ES, _state.DI);
            _stringSecondReadStart = _instructionTState + 8;
        }

        if (iterationPhase < 4)
        {
            LastBusCycle = TickStringRead(_stringSourceAddress, iterationPhase);
            return;
        }

        if (iterationPhase is >= 8 and < 12)
        {
            LastBusCycle = TickStringRead(_stringDestinationAddress, iterationPhase - 8);
            if (iterationPhase == 11)
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareByteFirst, _stringCompareByteSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
                int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
                _state.SI = unchecked((ushort)(_state.SI + delta));
                _state.DI = unchecked((ushort)(_state.DI + delta));
                _state.CX--;

                bool keepRepeating = _state.CX != 0 && (_instructionPrefix == 0xF2
                    ? (_state.Flags & 0x0040) == 0
                    : (_state.Flags & 0x0040) != 0);
                if (_state.CX != 0 && !keepRepeating)
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
