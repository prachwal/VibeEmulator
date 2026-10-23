namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsJumpOpcode(byte opcode) => opcode is 0x9A or 0xE8 or 0xEB or 0xE9 or 0xEA;

    private void InitializeJump(byte opcode)
    {
        _jumpInstructionLength = opcode switch { 0xEB => 2, 0xE8 or 0xE9 => 3, _ => 5 };
        if (_state.QueueCount < _jumpInstructionLength)
        {
            throw new InvalidOperationException($"JMP {opcode:X2} requires all instruction bytes in the prefetch queue.");
        }

        ushort nextIp = unchecked((ushort)(_state.IP + _jumpInstructionLength));
        _jumpTargetCs = _state.CS;
        _jumpIsCall = opcode is 0x9A or 0xE8;
        _jumpIsFarCall = opcode == 0x9A;
        _jumpReturnIp = nextIp;
        _jumpTargetIp = opcode switch
        {
            0xEB => unchecked((ushort)(nextIp + (sbyte)_state.Queue[_state.QueueHead + 1])),
            0xE8 or 0xE9 => unchecked((ushort)(nextIp + (short)(_state.Queue[_state.QueueHead + 1] | (_state.Queue[_state.QueueHead + 2] << 8)))),
            _ => (ushort)(_state.Queue[_state.QueueHead + 1] | (_state.Queue[_state.QueueHead + 2] << 8)),
        };
        if (opcode is 0x9A or 0xEA)
        {
            _jumpTargetCs = (ushort)(_state.Queue[_state.QueueHead + 3] | (_state.Queue[_state.QueueHead + 4] << 8));
        }

        _jumpLastOperand = _state.Queue[_state.QueueHead + _jumpInstructionLength - 1];
        bool shortQueue = _queueCountAtStart <= 5;
        bool alignedStack = (PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2))) & 1) == 0;
        _jumpQueueFlushTState = opcode switch
        {
            0xEA => shortQueue ? 9 : 11,
            0x9A => (shortQueue ? 23 : 21) + (alignedStack ? 0 : 4),
            _ => shortQueue ? 11 : 9,
        };
        int branchFetchEnd = _jumpQueueFlushTState + 6;
        if (_jumpIsCall)
        {
            ushort stackPointer = unchecked((ushort)(_state.SP - 2));
            _jumpStackAddress = PhysicalAddress(_state.SS, stackPointer);
            if (_jumpIsFarCall)
            {
                _jumpFirstStackWriteStartTState = shortQueue ? 16 : 14;
                _instructionCycles = branchFetchEnd + (alignedStack ? 2 : 6);
                _memoryAddress = _jumpStackAddress;
                _memoryWordResult = InstructionCs;
                _memorySegmentStatus = "SS";
            }
            else
            {
                _instructionCycles = branchFetchEnd + (alignedStack ? 2 : 6);
                _memoryAddress = _jumpStackAddress;
                _memoryWordResult = _jumpReturnIp;
                _memorySegmentStatus = "SS";
            }
        }
        else
        {
            _instructionCycles = branchFetchEnd;
        }
    }

    private void TickJump()
    {
        _instructionTState++;
        bool shortQueue = _queueCountAtStart <= 5;
        int speculativeFetchStart = shortQueue ? 3 : Opcode is 0xEA or 0x9A ? 5 : int.MaxValue;

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

        bool consumedOperand = _instructionTState >= 3 && _instructionTState < _jumpInstructionLength + 2;
        byte operand = 0;
        if (consumedOperand)
        {
            operand = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _jumpLastOperand = operand;
        }

        if (_instructionTState == _jumpQueueFlushTState)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _state.CS = _jumpTargetCs;
            _state.IP = _jumpTargetIp;
            _nextFetchAddress = PhysicalAddress(_jumpTargetCs, _jumpTargetIp);
            LastBusCycle = IdleCycle('E', _jumpLastOperand);
            return;
        }

        if (_jumpIsFarCall && _instructionTState >= _jumpFirstStackWriteStartTState && _instructionTState < _jumpQueueFlushTState)
        {
            TickFarCallFirstStackWrite();
            return;
        }

        int fetchPhase = _instructionTState - speculativeFetchStart;
        if (fetchPhase is >= 0 and <= 3)
        {
            LastBusCycle = fetchPhase switch
            {
                0 => FetchCycle("T1", consumedOperand ? 'S' : '-', operand),
                1 => FetchContinuationCycle(consumedOperand ? 'S' : '-', operand),
                2 => JumpPrefetchDataCycle(consumedOperand, operand),
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

        if (_jumpIsFarCall && shortQueue)
        {
            int farCallFetchPhase = _instructionTState - 7;
            if (farCallFetchPhase is >= 0 and <= 3)
            {
                LastBusCycle = farCallFetchPhase switch
                {
                    0 => FetchCycle("T1", '-', 0),
                    1 => FetchContinuationCycle('-', 0),
                    2 => JumpPrefetchDataCycle(false, 0),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
                if (farCallFetchPhase == 2)
                {
                    uint fetchAddress = _nextFetchAddress;
                    PushQueue(_bus.Read(fetchAddress));
                    PushQueue(_bus.Read((fetchAddress + 1) & 0xFFFFF));
                    _nextFetchAddress = (fetchAddress + 2) & 0xFFFFF;
                }

                return;
            }
        }

        if (_instructionTState > _jumpQueueFlushTState)
        {
            int targetFetchStart = _jumpQueueFlushTState + 2;
            int targetFetchPhase = _instructionTState - targetFetchStart;
            if (targetFetchPhase is >= 0 and <= 3)
            {
                LastBusCycle = targetFetchPhase switch
                {
                    0 => FetchCycle("T1", '-', 0),
                    1 => FetchContinuationCycle('-', 0),
                    2 => JumpTargetDataCycle(),
                    _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
                };
                if (targetFetchPhase == 2)
                {
                    int bytesFetched = (_jumpTargetIp & 1) == 0 ? 2 : 1;
                    for (int index = 0; index < bytesFetched; index++)
                    {
                        PushQueue(0x90);
                    }

                    _nextFetchAddress = (_nextFetchAddress + (uint)bytesFetched) & 0xFFFFF;
                }

                return;
            }

            if (_jumpIsCall && _instructionTState >= _jumpQueueFlushTState + 6)
            {
                if (_jumpIsFarCall)
                {
                    _memoryAddress = (_jumpStackAddress - 2) & 0xFFFFF;
                    _memoryWordResult = _jumpReturnIp;
                    TickFarCallReturnIpWrite();
                }
                else
                {
                    TickNearCallStackWrite();
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

    private void TickNearCallStackWrite()
    {
        int writeStart = _jumpQueueFlushTState + 6;
        int phase = _instructionTState - writeStart;
        LastBusCycle = TickCallStackWordWrite(_jumpStackAddress, phase);

        if (_instructionTState == _instructionCycles)
        {
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private void TickFarCallFirstStackWrite()
    {
        int phase = _instructionTState - _jumpFirstStackWriteStartTState;
        int writeDuration = (_jumpStackAddress & 1) == 0 ? 4 : 8;
        if (phase >= writeDuration)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        _memoryAddress = _jumpStackAddress;
        _memoryWordResult = InstructionCs;
        LastBusCycle = TickCallStackWordWrite(_jumpStackAddress, phase);
    }

    private void TickFarCallReturnIpWrite()
    {
        int writeStart = _jumpQueueFlushTState + 6;
        int phase = _instructionTState - writeStart;
        uint returnAddress = (_jumpStackAddress - 2) & 0xFFFFF;
        LastBusCycle = TickCallStackWordWrite(returnAddress, phase);
        if (_instructionTState == _instructionCycles)
        {
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle TickCallStackWordWrite(uint address, int phase)
    {
        _memoryAddress = address;
        _memorySegmentStatus = "SS";
        bool aligned = (address & 1) == 0;
        if (phase == 0)
        {
            _state.SP = unchecked((ushort)(_state.SP - 2));
        }

        if (aligned)
        {
            return phase < 3
                ? TickAlignedMemoryWordWrite(phase)
                : new Intel8086BusCycle(0, 0, "SS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
        }

        return phase < 4
            ? TickMemoryWordWrite(phase, 0)
            : TickMemoryWordWrite(phase - 4, 1);
    }

    private static Intel8086BusCycle JumpPrefetchDataCycle(bool consumedOperand, byte operand) =>
        new(0, 0, "CS", "R--", "---", 0, 37008, "PASV", "T3", consumedOperand ? 'S' : '-', operand);

    private Intel8086BusCycle JumpTargetDataCycle() => new(
        0,
        0,
        "CS",
        "R--",
        "---",
        0,
        (_jumpTargetIp & 1) == 0 ? (ushort)37008 : (ushort)36864,
        "PASV",
        "T3",
        '-',
        0);

}
