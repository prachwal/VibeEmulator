namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsStackTransferOpcode(byte opcode) =>
        opcode is >= 0x50 and <= 0x5F or 0x06 or 0x07 or 0x0E or 0x16 or 0x17 or 0x1E or 0x1F or 0x9C or 0x9D;

    private static bool IsStackPushOpcode(byte opcode) =>
        opcode is >= 0x50 and <= 0x57 or 0x06 or 0x0E or 0x16 or 0x1E or 0x9C;

    private bool IsGroupPushRm16() => IsGroupPushRm16(Opcode, _memoryModRm);

    private static bool IsSegmentStackOpcode(byte opcode) => opcode is 0x06 or 0x07 or 0x0E or 0x16 or 0x17 or 0x1E or 0x1F;

    private void InitializeRegisterStackInstruction()
    {
        bool groupPush = IsGroupPushRm16();
        bool push = IsStackPushOpcode(Opcode) || groupPush;
        ushort offset = push ? unchecked((ushort)(_state.SP - 2)) : _state.SP;
        _memoryAddress = PhysicalAddress(_state.SS, offset);
        _memorySegmentStatus = "SS";
        bool splitTransfer = (_memoryAddress & 1) != 0;
        int transferCycles = splitTransfer ? (push ? 7 : 8) : push ? 3 : 4;
        int start = push
            ? groupPush ? (_queueCountAtStart > 5 ? 10 : 9) : (_queueCountAtStart > 5 ? 8 : 9)
            : 5;
        if (groupPush && _instructionPrefixLength != 0)
        {
            start = 11;
        }

        _instructionCycles = start + transferCycles - 1;
    }

    private void TickRegisterStackInstruction()
    {
        if (IsGroupPushRm16() && _instructionPrefixLength != 0)
        {
            TickPrefixedGroupPushRegister();
            return;
        }

        _instructionTState++;
        bool groupPush = IsGroupPushRm16();
        int register = groupPush ? _memoryModRm & 7 : Opcode & 7;
        bool push = IsStackPushOpcode(Opcode) || groupPush;
        ushort stackOffset = push ? unchecked((ushort)(_state.SP - 2)) : _state.SP;
        ushort value = push ? GetStackPushValue(register, stackOffset) : (ushort)0;
        int start = push
            ? groupPush ? (_queueCountAtStart > 5 ? 10 : 9) : (_queueCountAtStart > 5 ? 8 : 9)
            : 5;

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (groupPush && _instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _memoryModRm);
            return;
        }

        if (groupPush && _instructionTState < start)
        {
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle groupPushPrefetchCycle)
                ? groupPushPrefetchCycle
                : IdleCycle('-', 0);
            return;
        }

        if (!groupPush && push && _queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            int fetchPhase = _instructionTState - 3;
            LastBusCycle = fetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchStackInstructionData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        if (_instructionTState >= start && _instructionTState <= _instructionCycles)
        {
            int phase = _instructionTState - start;
            LastBusCycle = TickRegisterStackWordTransfer(push, stackOffset, value, phase);
            if (_instructionTState == _instructionCycles)
            {
                if (push)
                {
                    _state.SP = stackOffset;
                }
                else
                {
                    ApplyStackPop(register, _memoryWordValue);
                }

                if (!groupPush && (!push || _queueCountAtStart > 5) && _state.QueueCount > 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
            }

            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickPrefixedGroupPushRegister()
    {
        _instructionTState++;
        ushort stackOffset = unchecked((ushort)(_state.SP - 2));
        ushort value = GetStackPushValue(_memoryModRm & 7, stackOffset);
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
            return;
        }

        if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionTState is 3 or 4)
        {
            PopQueue();
            _state.IP++;
            char queueStatus = _instructionTState == 3 ? 'F' : 'S';
            byte queueData = _instructionTState == 3 ? Opcode : _memoryModRm;
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle instructionFetchCycle)
                ? instructionFetchCycle with { QueueStatus = queueStatus, QueueData = queueData }
                : IdleCycle(queueStatus, queueData);
            return;
        }

        if (_instructionTState < 11)
        {
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle)
                ? prefetchCycle
                : IdleCycle('-', 0);
            return;
        }

        int phase = _instructionTState - 11;
        LastBusCycle = TickRegisterStackWordTransfer(true, stackOffset, value, phase);
        if (_instructionTState == _instructionCycles)
        {
            _state.SP = stackOffset;
            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle FetchStackInstructionData()
    {
        PushQueue(0x90);
        _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        return FetchDataCycle();
    }

    private ushort GetStackPushValue(int register, ushort stackOffset)
    {
        if (Opcode == 0x9C)
        {
            return (ushort)(_state.Flags | 0x0002);
        }

        if (IsGroupPushRm16())
        {
            register = _memoryModRm & 7;
            return register == 4 ? stackOffset : GetWordRegister(register);
        }

        if (!IsSegmentStackOpcode(Opcode))
        {
            return register == 4 ? stackOffset : GetWordRegister(register);
        }

        return Opcode switch
        {
            0x06 => _state.ES,
            0x0E => _state.CS,
            0x16 => _state.SS,
            0x1E => _state.DS,
            _ => throw new InvalidOperationException($"Opcode {Opcode:X2} is not a segment PUSH."),
        };
    }

    private void ApplyStackPop(int register, ushort value)
    {
        if (Opcode == 0x9D)
        {
            _state.Flags = (ushort)((value & 0x0FD5) | 0xF002);
            _state.SP = unchecked((ushort)(_state.SP + 2));
            return;
        }

        if (!IsSegmentStackOpcode(Opcode))
        {
            SetWordRegister(register, value);
            if (register == 4)
            {
                return;
            }
        }
        else
        {
            switch (Opcode)
            {
                case 0x07: _state.ES = value; break;
                case 0x17: _state.SS = value; break;
                case 0x1F: _state.DS = value; break;
                default: throw new InvalidOperationException($"Opcode {Opcode:X2} is not a segment POP.");
            }
        }

        _state.SP = unchecked((ushort)(_state.SP + 2));
    }

    private Intel8086BusCycle TickRegisterStackWordTransfer(bool write, ushort stackOffset, ushort value, int phase)
    {
        if ((_memoryAddress & 1) == 0)
        {
            byte bhe = 0;
            if (phase == 0)
            {
                return new Intel8086BusCycle(1, _memoryAddress, "--", "---", "---", bhe, 0, write ? "MEMW" : "MEMR", "T1", '-', 0);
            }

            if (phase == 1)
            {
                return new Intel8086BusCycle(0, 0, "SS", write ? "-A-" : "R--", "---", bhe, 0, write ? "MEMW" : "MEMR", "T2", '-', 0);
            }

            if (!write && phase == 3)
            {
                return new Intel8086BusCycle(0, 0, "SS", "---", "---", bhe, 0, "PASV", "T4", '-', 0);
            }

            ushort data = write ? value : (ushort)(_bus.Read(_memoryAddress) | (_bus.Read((_memoryAddress + 1) & 0xFFFFF) << 8));
            if (!write)
            {
                _memoryWordValue = data;
            }
            else
            {
                _bus.Write(_memoryAddress, (byte)value);
                _bus.Write((_memoryAddress + 1) & 0xFFFFF, (byte)(value >> 8));
            }

            return new Intel8086BusCycle(0, 0, "SS", write ? "-AW" : "R--", "---", bhe, data, "PASV", "T3", '-', 0);
        }

        int byteIndex = phase / 4;
        int busPhase = phase % 4;
        uint address = PhysicalAddress(_state.SS, unchecked((ushort)(stackOffset + byteIndex)));
        byte lane = (byte)((address & 1) == 0 ? 1 : 0);
        string status = write ? "MEMW" : "MEMR";
        if (busPhase == 0)
        {
            return new Intel8086BusCycle(1, address, "--", "---", "---", lane, 0, status, "T1", '-', 0);
        }

        if (busPhase == 1)
        {
            return new Intel8086BusCycle(0, 0, "SS", write ? "-A-" : "R--", "---", lane, 0, status, "T2", '-', 0);
        }

        if (busPhase == 2)
        {
            byte data = write ? (byte)(value >> (byteIndex * 8)) : _bus.Read(address);
            if (write)
            {
                _bus.Write(address, data);
            }
            else if (byteIndex == 0)
            {
                _memoryWordValue = (ushort)((_memoryWordValue & 0xFF00) | data);
            }
            else
            {
                _memoryWordValue = (ushort)((_memoryWordValue & 0x00FF) | (data << 8));
            }

            ushort busData = (ushort)((address & 1) == 0 ? data : data << 8);
            return new Intel8086BusCycle(0, 0, "SS", write ? "-AW" : "R--", "---", lane, busData, "PASV", "T3", '-', 0);
        }

        return new Intel8086BusCycle(0, 0, "SS", "---", "---", lane, 0, "PASV", "T4", '-', 0);
    }
}
