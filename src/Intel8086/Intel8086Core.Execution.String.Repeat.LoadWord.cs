namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickRepeatedLodsWord()
    {
        _instructionTState++;
        uint sourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
        bool splitWordRead = (sourceAddress & 1) != 0;
        int iterationCycles = splitWordRead ? 17 : 13;
        int iterationPhase = (_instructionTState - 15) % iterationCycles;
        if (_instructionTState < 15)
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

        int readCycleCount = splitWordRead ? 8 : 4;
        if (iterationPhase < readCycleCount)
        {
            if (iterationPhase == 0)
            {
                _stringSourceAddress = sourceAddress;
            }

            uint address = splitWordRead && iterationPhase >= 4
                ? GetStringWordSecondByteAddress(_stringSourceAddress, GetStringSourceSegment())
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, iterationPhase % 4);
            if (iterationPhase == readCycleCount - 2)
            {
                _state.AX = _stringWordValue;
                int delta = (_state.Flags & 0x0400) == 0 ? 2 : -2;
                _state.SI = unchecked((ushort)(_state.SI + delta));
                _state.CX--;
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
