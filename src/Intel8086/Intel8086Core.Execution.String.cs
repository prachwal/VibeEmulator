namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsMoveStringOpcode(byte opcode) => opcode is 0xA4 or 0xA5;
    private static bool IsStoreStringOpcode(byte opcode) => opcode is 0xAA or 0xAB;
    private static bool IsLoadStringOpcode(byte opcode) => opcode is 0xAC or 0xAD;
    private static bool IsScanStringOpcode(byte opcode) => opcode is 0xAE or 0xAF;
    private static bool IsCompareStringOpcode(byte opcode) => opcode is 0xA6 or 0xA7;
    private static bool IsByteInputIoOpcode(byte opcode) => opcode is 0xE4 or 0xEC;
    private static bool IsByteOutputIoOpcode(byte opcode) => opcode is 0xE6 or 0xEE;
    private static bool IsWordInputIoOpcode(byte opcode) => opcode is 0xE5 or 0xED;
    private static bool IsWordOutputIoOpcode(byte opcode) => opcode is 0xE7 or 0xEF;

    private void TickLoadString()
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
        int readCycleCount = Opcode == 0xAD && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xAD && (_stringSourceAddress & 1) != 0 && readPhase >= 4
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

            _state.AX = Opcode == 0xAC
                ? (ushort)((_state.AX & 0xFF00) | _stringByteValue)
                : _stringWordValue;
            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.SI = unchecked((ushort)(_state.SI + (Opcode == 0xAD ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickStoreString()
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

        int writePhase = _instructionTState - _stringWriteStart;
        int writeCycleCount = Opcode == 0xAB && (_stringDestinationAddress & 1) != 0 ? 8 : 4;
        if (writePhase >= 0 && writePhase < writeCycleCount)
        {
            uint address = Opcode == 0xAB && (_stringDestinationAddress & 1) != 0 && writePhase >= 4
                ? (_stringDestinationAddress + 1) & 0xFFFFF
                : _stringDestinationAddress;
            LastBusCycle = TickStoreWrite(address, writePhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xAB ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle TickStoreWrite(uint address, int phase)
    {
        byte bhe = (byte)(Opcode == 0xAB && (_stringDestinationAddress & 1) == 0 ? 0 : (address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "ES", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteStoreByte(address, bhe),
            _ => new Intel8086BusCycle(0, 0, "ES", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteStoreByte(uint address, byte bhe)
    {
        if (Opcode == 0xAB && (_stringDestinationAddress & 1) == 0)
        {
            _bus.Write(address, (byte)_state.AX);
            _bus.Write((address + 1) & 0xFFFFF, (byte)(_state.AX >> 8));
            return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, _state.AX, "PASV", "T3", '-', 0);
        }

        byte value = Opcode == 0xAB
            ? (byte)(_state.AX >> (address == _stringDestinationAddress ? 0 : 8))
            : (byte)_state.AX;
        _bus.Write(address, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void TickMoveString()
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
        int readCycleCount = Opcode == 0xA5 && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xA5 && (_stringSourceAddress & 1) != 0 && readPhase >= 4
                ? (_stringSourceAddress + 1) & 0xFFFFF
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
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
            if (Opcode == 0xA5)
            {
                delta *= 2;
            }

            _state.SI = unchecked((ushort)(_state.SI + delta));
            _state.DI = unchecked((ushort)(_state.DI + delta));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

}
