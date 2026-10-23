namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private uint _interruptReturnStackAddress;
    private ushort _interruptReturnIp;
    private ushort _interruptReturnCs;
    private ushort _interruptReturnFlags;
    private int _interruptReturnSecondReadStart;
    private int _interruptReturnFlush;
    private int _interruptReturnThirdReadStart;
    private int _interruptReturnEnd;

    private void InitializeInterruptReturn()
    {
        _interruptReturnStackAddress = PhysicalAddress(_state.SS, _state.SP);
        bool aligned = (_interruptReturnStackAddress & 1) == 0;
        int wordReadLength = aligned ? 4 : 8;
        _interruptReturnSecondReadStart = 7 + (aligned ? 9 : 13);
        _interruptReturnFlush = _interruptReturnSecondReadStart + wordReadLength;
        _interruptReturnThirdReadStart = _interruptReturnFlush + 4;
        _interruptReturnEnd = _interruptReturnThirdReadStart + wordReadLength + 4;
        _instructionCycles = _interruptReturnEnd;
    }

    private void TickInterruptReturn()
    {
        _instructionTState++;
        bool aligned = (_interruptReturnStackAddress & 1) == 0;
        int wordReadLength = aligned ? 4 : 8;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionTState is 3 or 4 or 5 or 6)
        {
            if (_queueCountAtStart <= 5)
            {
                LastBusCycle = (_instructionTState - 3) switch
                {
                    0 => FetchCycle("T1", '-', 0),
                    1 => FetchContinuationCycle('-', 0),
                    2 => FetchDataCycle(),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
            }
            else
            {
                LastBusCycle = IdleCycle('-', 0);
            }

            return;
        }

        if (_instructionTState >= 7 && _instructionTState < 7 + wordReadLength)
        {
            LastBusCycle = TickInterruptReturnWordRead(0, _instructionTState - 7);
            return;
        }

        int secondReadEnd = _interruptReturnSecondReadStart + wordReadLength;
        if (_instructionTState >= _interruptReturnSecondReadStart && _instructionTState < secondReadEnd)
        {
            LastBusCycle = TickInterruptReturnWordRead(1, _instructionTState - _interruptReturnSecondReadStart);
            return;
        }

        if (_instructionTState == _interruptReturnFlush)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.IP = _interruptReturnIp;
            _state.CS = _interruptReturnCs;
            _nextFetchAddress = PhysicalAddress(_state.CS, _state.IP);
            LastBusCycle = IdleCycle('E', Opcode);
            return;
        }

        if (_instructionTState >= _interruptReturnThirdReadStart && _instructionTState < _interruptReturnThirdReadStart + wordReadLength)
        {
            LastBusCycle = TickInterruptReturnWordRead(2, _instructionTState - _interruptReturnThirdReadStart);
            if (_instructionTState == _interruptReturnThirdReadStart + wordReadLength - 2)
            {
                _state.Flags = (ushort)((_interruptReturnFlags & ~0xF028) | 0xF002 | (_state.Flags & 0x0028));
                _state.SP = unchecked((ushort)(_state.SP + 6));
            }

            return;
        }

        if (_instructionTState >= _interruptReturnThirdReadStart + wordReadLength)
        {
            int targetFetchPhase = _instructionTState - (_interruptReturnThirdReadStart + wordReadLength);
            if (targetFetchPhase is >= 0 and <= 3)
            {
                LastBusCycle = targetFetchPhase switch
                {
                    0 => FetchCycle("T1", '-', 0),
                    1 => FetchContinuationCycle('-', 0),
                    2 => InterruptReturnTargetDataCycle(),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
                if (targetFetchPhase == 2)
                {
                    int bytesFetched = (_interruptReturnIp & 1) == 0 ? 2 : 1;
                    for (int index = 0; index < bytesFetched; index++)
                    {
                        PushQueue(0x90);
                    }

                    _nextFetchAddress = (_nextFetchAddress + (uint)bytesFetched) & 0xFFFFF;
                }

                return;
            }
        }

        if (_instructionTState == _interruptReturnEnd)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle TickInterruptReturnWordRead(int wordIndex, int phase)
    {
        uint wordAddress = (_interruptReturnStackAddress + ((uint)wordIndex * 2)) & 0xFFFFF;
        bool aligned = (wordAddress & 1) == 0;
        if (aligned)
        {
            return phase switch
            {
                0 => new Intel8086BusCycle(1, wordAddress, "--", "---", "---", 0, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, 0, "SS", "R--", "---", 0, 0, "MEMR", "T2", '-', 0),
                2 => ReadInterruptReturnAlignedWord(wordAddress, wordIndex),
                _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
        }

        int byteIndex = phase < 4 ? 0 : 1;
        int bytePhase = phase < 4 ? phase : phase - 4;
        uint address = (wordAddress + (uint)byteIndex) & 0xFFFFF;
        byte bhe = (byte)((address & 1) == 0 ? 1 : 0);
        return bytePhase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "SS", "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadInterruptReturnByte(address, wordIndex, byteIndex, bhe),
            _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadInterruptReturnAlignedWord(uint address, int wordIndex)
    {
        ushort value = (ushort)(_bus.Read(address) | (_bus.Read((address + 1) & 0xFFFFF) << 8));
        SetInterruptReturnWord(wordIndex, value);
        return new Intel8086BusCycle(0, 0, "SS", "R--", "---", 0, value, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle ReadInterruptReturnByte(uint address, int wordIndex, int byteIndex, byte bhe)
    {
        byte value = _bus.Read(address);
        ushort word = wordIndex switch
        {
            0 => _interruptReturnIp,
            1 => _interruptReturnCs,
            _ => _interruptReturnFlags,
        };
        word = byteIndex == 0
            ? (ushort)((word & 0xFF00) | value)
            : (ushort)((word & 0x00FF) | (value << 8));
        SetInterruptReturnWord(wordIndex, word);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "SS", "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void SetInterruptReturnWord(int wordIndex, ushort value)
    {
        switch (wordIndex)
        {
            case 0:
                _interruptReturnIp = value;
                break;
            case 1:
                _interruptReturnCs = value;
                break;
            default:
                _interruptReturnFlags = value;
                break;
        }
    }

    private Intel8086BusCycle InterruptReturnTargetDataCycle() => new(
        0, 0, "CS", "R--", "---", 0,
        (_interruptReturnIp & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV", "T3", '-', 0);
}
