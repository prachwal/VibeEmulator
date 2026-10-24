namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsRegisterStackOpcode(byte opcode) => opcode is >= 0x50 and <= 0x5F;

    private void InitializeRegisterStackInstruction()
    {
        bool push = Opcode < 0x58;
        ushort offset = push ? unchecked((ushort)(_state.SP - 2)) : _state.SP;
        _memoryAddress = PhysicalAddress(_state.SS, offset);
        _memorySegmentStatus = "SS";
        bool splitTransfer = (_memoryAddress & 1) != 0;
        int transferCycles = splitTransfer ? (push ? 7 : 8) : push ? 3 : 4;
        int start = push
            ? (_queueCountAtStart > 5 ? 8 : 9)
            : 5;
        _instructionCycles = start + transferCycles - 1;
    }

    private void TickRegisterStackInstruction()
    {
        _instructionTState++;
        int register = Opcode & 7;
        bool push = Opcode < 0x58;
        ushort stackOffset = push ? unchecked((ushort)(_state.SP - 2)) : _state.SP;
        ushort value = push ? register == 4 ? unchecked((ushort)(_state.SP - 2)) : GetWordRegister(register) : (ushort)0;

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (push && _queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
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

        int start = push ? (_queueCountAtStart > 5 ? 8 : 9) : 5;
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
                    ushort popped = _memoryWordValue;
                    SetWordRegister(register, popped);
                    if (register != 4)
                    {
                        _state.SP = unchecked((ushort)(_state.SP + 2));
                    }
                }

                if ((!push || _queueCountAtStart > 5) && _state.QueueCount > 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
            }

            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle FetchStackInstructionData()
    {
        PushQueue(0x90);
        _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        return FetchDataCycle();
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
