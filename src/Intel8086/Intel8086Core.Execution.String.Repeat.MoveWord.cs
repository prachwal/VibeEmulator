namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickRepeatedMovsWord()
    {
        _instructionTState++;
        uint sourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
        uint destinationAddress = PhysicalAddress(_state.ES, _state.DI);
        bool splitSourceRead = (sourceAddress & 1) != 0;
        bool splitDestinationWrite = (destinationAddress & 1) != 0;
        int sourceReadCycles = splitSourceRead ? 8 : 4;
        int destinationWriteCycles = splitDestinationWrite ? 8 : 4;
        int writePhaseStart = sourceReadCycles + 3;
        int iterationCycles = writePhaseStart + destinationWriteCycles + 6;
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

        if (iterationPhase == 0)
        {
            _stringSourceAddress = sourceAddress;
            _stringDestinationAddress = destinationAddress;
            _stringSourceOffset = _state.SI;
            _stringDestinationOffset = _state.DI;
        }

        if (iterationPhase < sourceReadCycles)
        {
            uint address = splitSourceRead && iterationPhase >= 4
                ? PhysicalAddress(GetStringSourceSegment(), unchecked((ushort)(_stringSourceOffset + 1)))
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, iterationPhase % 4);
            return;
        }

        int writePhase = iterationPhase - writePhaseStart;
        if (writePhase is >= 0 && writePhase < destinationWriteCycles)
        {
            uint address = splitDestinationWrite && writePhase >= 4
                ? PhysicalAddress(_state.ES, unchecked((ushort)(_stringDestinationOffset + 1)))
                : _stringDestinationAddress;
            LastBusCycle = TickRepeatedMovsWordWrite(address, writePhase % 4);
            if (writePhase == destinationWriteCycles - 2)
            {
                int delta = (_state.Flags & 0x0400) == 0 ? 2 : -2;
                _state.SI = unchecked((ushort)(_state.SI + delta));
                _state.DI = unchecked((ushort)(_state.DI + delta));
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

    private Intel8086BusCycle TickRepeatedMovsWordWrite(uint address, int phase)
    {
        byte bhe = (byte)(splitWordWriteIsAligned() ? 0 : (address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "ES", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteRepeatedMovsWordByte(address, bhe),
            _ => new Intel8086BusCycle(0, 0, "ES", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private bool splitWordWriteIsAligned() => (_stringDestinationAddress & 1) == 0;

    private Intel8086BusCycle WriteRepeatedMovsWordByte(uint address, byte bhe)
    {
        if (splitWordWriteIsAligned())
        {
            _bus.Write(address, (byte)_stringWordValue);
            _bus.Write((address + 1) & 0xFFFFF, (byte)(_stringWordValue >> 8));
            return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, _stringWordValue, "PASV", "T3", '-', 0);
        }

        byte value = address == _stringDestinationAddress
            ? (byte)_stringWordValue
            : (byte)(_stringWordValue >> 8);
        _bus.Write(address, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }
}
