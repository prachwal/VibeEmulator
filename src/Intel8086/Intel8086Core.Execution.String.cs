namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsMovsbOpcode(byte opcode) => opcode == 0xA4;

    private void TickMovsb()
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
        if (readPhase is >= 0 and < 4)
        {
            LastBusCycle = TickStringRead(readPhase);
            return;
        }

        int writePhase = _instructionTState - _stringWriteStart;
        if (writePhase is >= 0 and < 4)
        {
            LastBusCycle = TickStringWrite(writePhase);
            return;
        }

        if (_instructionTState == 10)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.SI = unchecked((ushort)(_state.SI + delta));
            _state.DI = unchecked((ushort)(_state.DI + delta));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle FetchStringQueueData()
    {
        PopQueue();
        PushQueue(0x90);
        PushQueue(0x90);
        _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        return FetchDataCycle();
    }

    private Intel8086BusCycle TickStringRead(int phase)
    {
        byte bhe = (byte)((_stringSourceAddress & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _stringSourceAddress, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "DS", "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadStringByte(bhe),
            _ => new Intel8086BusCycle(0, 0, "DS", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadStringByte(byte bhe)
    {
        _stringByteValue = _bus.Read(_stringSourceAddress);
        ushort data = (ushort)((_stringSourceAddress & 1) == 0 ? _stringByteValue : _stringByteValue << 8);
        return new Intel8086BusCycle(0, 0, "DS", "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickStringWrite(int phase)
    {
        byte bhe = (byte)((_stringDestinationAddress & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _stringDestinationAddress, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "ES", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteStringByte(bhe),
            _ => new Intel8086BusCycle(0, 0, "ES", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteStringByte(byte bhe)
    {
        _bus.Write(_stringDestinationAddress, _stringByteValue);
        ushort data = (ushort)((_stringDestinationAddress & 1) == 0 ? _stringByteValue : _stringByteValue << 8);
        return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }
}
