namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickCompareString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readStart = _instructionTState < _stringSecondReadStart ? _stringReadStart : _stringSecondReadStart;
        bool destinationRead = _instructionTState >= _stringSecondReadStart;
        uint baseAddress = destinationRead ? _stringDestinationAddress : _stringSourceAddress;
        int readPhase = _instructionTState - readStart;
        int readCycleCount = Opcode == 0xA7 && (baseAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xA7 && (baseAddress & 1) != 0 && readPhase >= 4
                ? (baseAddress + 1) & 0xFFFFF
                : baseAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            if (Opcode == 0xA6)
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareByteFirst, _stringCompareByteSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }
            else
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareWordFirst, _stringCompareWordSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.SI = unchecked((ushort)(_state.SI + (Opcode == 0xA7 ? delta * 2 : delta)));
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xA7 ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickScanString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readPhase = _instructionTState - _stringReadStart;
        int readCycleCount = Opcode == 0xAF && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xAF && (_stringSourceAddress & 1) != 0 && readPhase >= 4
                ? (_stringSourceAddress + 1) & 0xFFFFF
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            if (Opcode == 0xAE)
            {
                _state.Flags = Intel8086Alu.Execute((byte)_state.AX, _stringByteValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }
            else
            {
                _state.Flags = Intel8086Alu.Execute(_state.AX, _stringWordValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xAF ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

}
