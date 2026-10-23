namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsNearReturnOpcode(byte opcode) => opcode is 0xC2 or 0xC3;

    private void InitializeNearReturn(byte opcode)
    {
        _returnInstructionLength = opcode == 0xC2 ? (byte)3 : (byte)1;
        if (_state.QueueCount < _returnInstructionLength)
        {
            throw new InvalidOperationException($"RET {opcode:X2} requires all instruction bytes in the prefetch queue.");
        }

        _returnStackAddress = PhysicalAddress(_state.SS, _state.SP);
        _returnStackAdjustment = opcode == 0xC2
            ? (ushort)(_state.Queue[_state.QueueHead + 1] | (_state.Queue[_state.QueueHead + 2] << 8))
            : (ushort)0;
        _returnLastOperand = _state.Queue[_state.QueueHead + _returnInstructionLength - 1];
        _returnReadStartTState = opcode == 0xC2 ? 9 : 5;
        bool aligned = (_returnStackAddress & 1) == 0;
        _returnQueueFlushTState = _returnReadStartTState + (opcode == 0xC2
            ? aligned ? 6 : 10
            : aligned ? 5 : 9);
        _instructionCycles = _returnQueueFlushTState + 6;
    }

    private void TickNearReturn()
    {
        _instructionTState++;
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

        bool consumedOperand = _instructionTState >= 3 && _instructionTState < _returnInstructionLength + 2;
        byte operand = 0;
        if (consumedOperand)
        {
            operand = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _returnLastOperand = operand;
        }

        bool shortQueue = _queueCountAtStart <= 5;
        int speculativeFetchStart = Opcode == 0xC2 ? shortQueue ? 3 : 5 : int.MaxValue;
        int fetchPhase = _instructionTState - speculativeFetchStart;
        if (fetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = fetchPhase switch
            {
                0 => FetchCycle("T1", consumedOperand ? 'S' : '-', operand),
                1 => FetchContinuationCycle(consumedOperand ? 'S' : '-', operand),
                2 => ReturnPrefetchDataCycle(consumedOperand, operand),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumedOperand ? 'S' : '-', operand),
            };
            if (fetchPhase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                PushQueue(_bus.Read(fetchAddress));
                PushQueue(_bus.Read((fetchAddress + 1) & 0xFFFFF));
                _nextFetchAddress = (fetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

        if (_instructionTState >= _returnReadStartTState && _instructionTState < _returnQueueFlushTState)
        {
            TickNearReturnStackRead(_instructionTState - _returnReadStartTState);
            return;
        }

        if (_instructionTState == _returnQueueFlushTState)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.IP = _returnTargetIp;
            _state.SP = unchecked((ushort)(_state.SP + 2 + _returnStackAdjustment));
            _nextFetchAddress = PhysicalAddress(_state.CS, _state.IP);
            LastBusCycle = IdleCycle('E', _returnLastOperand);
            return;
        }

        if (_instructionTState > _returnQueueFlushTState)
        {
            int targetFetchPhase = _instructionTState - (_returnQueueFlushTState + 2);
            if (targetFetchPhase is >= 0 and <= 3)
            {
                LastBusCycle = targetFetchPhase switch
                {
                    0 => FetchCycle("T1", '-', 0),
                    1 => FetchContinuationCycle('-', 0),
                    2 => ReturnTargetDataCycle(),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
                if (targetFetchPhase == 2)
                {
                    int bytesFetched = (_returnTargetIp & 1) == 0 ? 2 : 1;
                    for (int index = 0; index < bytesFetched; index++)
                    {
                        PushQueue(0x90);
                    }

                    _nextFetchAddress = (_nextFetchAddress + (uint)bytesFetched) & 0xFFFFF;
                }

                return;
            }

            if (_instructionTState == _instructionCycles)
            {
                LastBusCycle = FetchCycle("T1", '-', 0);
                if (_state.QueueCount != 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
                return;
            }
        }

        LastBusCycle = consumedOperand ? IdleCycle('S', operand) : IdleCycle('-', 0);
    }

    private void TickNearReturnStackRead(int phase)
    {
        bool aligned = (_returnStackAddress & 1) == 0;
        if (phase >= (aligned ? 4 : 8))
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (aligned)
        {
            LastBusCycle = phase switch
            {
                0 => new Intel8086BusCycle(1, _returnStackAddress, "--", "---", "---", 0, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, 0, "SS", "R--", "---", 0, 0, "MEMR", "T2", '-', 0),
                2 => ReadNearReturnAlignedWord(),
                _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };

            return;
        }

        int byteIndex = phase < 4 ? 0 : 1;
        int bytePhase = phase < 4 ? phase : phase - 4;
        uint address = (_returnStackAddress + (uint)byteIndex) & 0xFFFFF;
        byte bhe = (byte)((address & 1) == 0 ? 1 : 0);
        LastBusCycle = bytePhase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "SS", "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadNearReturnByte(address, byteIndex, bhe),
            _ => new Intel8086BusCycle(0, 0, "SS", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadNearReturnAlignedWord()
    {
        _returnTargetIp = (ushort)(_bus.Read(_returnStackAddress) | (_bus.Read((_returnStackAddress + 1) & 0xFFFFF) << 8));
        return new Intel8086BusCycle(0, 0, "SS", "R--", "---", 0, _returnTargetIp, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle ReadNearReturnByte(uint address, int byteIndex, byte bhe)
    {
        byte value = _bus.Read(address);
        _returnTargetIp = byteIndex == 0
            ? (ushort)((_returnTargetIp & 0xFF00) | value)
            : (ushort)((_returnTargetIp & 0x00FF) | (value << 8));
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "SS", "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private static Intel8086BusCycle ReturnPrefetchDataCycle(bool consumedOperand, byte operand) =>
        new(0, 0, "CS", "R--", "---", 0, 37008, "PASV", "T3", consumedOperand ? 'S' : '-', operand);

    private Intel8086BusCycle ReturnTargetDataCycle() => new(
        0,
        0,
        "CS",
        "R--",
        "---",
        0,
        (_returnTargetIp & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV",
        "T3",
        '-',
        0);
}
