namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickIndirectCall()
    {
        _instructionTState++;
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_indirectCallPrefixLength != 0)
        {
            if (_instructionTState == 1)
            {
                byte prefix = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', prefix);
                return;
            }

            if (_instructionTState == 2)
            {
                LastBusCycle = IdleCycle('-', 0);
                return;
            }

            if (_instructionTState == 3)
            {
                PopQueue();
                _state.IP++;
                consumesOpcode = true;
            }

            if (_instructionTState == 4)
            {
                PopQueue();
                _state.IP++;
                consumesModRm = true;
            }
        }
        else if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            consumesOpcode = true;
        }
        else if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            consumesModRm = true;
        }

        int displacementIndex = _instructionTState - _indirectCallDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _indirectCallDisplacementBytes;
        byte displacementByte = 0;
        if (consumesDisplacement)
        {
            displacementByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _jumpLastOperand = displacementByte;
        }

        if (consumesOpcode)
        {
            _jumpLastOperand = Opcode;
        }

        if (consumesModRm)
        {
            _jumpLastOperand = _indirectCallModRm;
            if ((_indirectCallModRm >> 6) == 3)
            {
                _indirectCallTargetOffset = GetWordRegister(_indirectCallModRm & 7);
            }
        }

        int fetchStart = GetIndirectCallFetchStart(_instructionTState);
        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesOpcode ? 'F' : consumesDisplacement ? 'S' : '-',
                    consumesOpcode ? Opcode : displacementByte),
                1 => FetchContinuationCycle(consumesModRm || consumesDisplacement ? 'S' : '-',
                    consumesModRm ? _indirectCallModRm : displacementByte),
                2 => FetchDataCycle() with
                {
                    Data = (_nextFetchAddress & 1) == 0 ? (ushort)0x9090 : (ushort)0x9000,
                    QueueStatus = consumesDisplacement ? 'S' : '-',
                    QueueData = displacementByte,
                },
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4",
                    consumesDisplacement ? 'S' : '-', displacementByte),
            };
            if (phase == 2)
            {
                uint fetchAddress = _nextFetchAddress;
                int bytesFetched = fetchStart > _indirectCallFlushStart
                    ? (_jumpTargetIp & 1) == 0 ? 2 : 1
                    : Math.Min(2, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(fetchStart > _indirectCallFlushStart
                        ? (byte)0x90
                        : _bus.Read((fetchAddress + (uint)index) & 0xFFFFF));
                }

                _nextFetchAddress = (fetchAddress + (uint)bytesFetched) & 0xFFFFF;
                _indirectCallFetchOffset = unchecked((ushort)(_indirectCallFetchOffset + bytesFetched));
                _nextFetchAddress = PhysicalAddress(_state.CS, _indirectCallFetchOffset);
            }
        }
        else if (_instructionTState == _indirectCallFlushStart)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _jumpTargetIp = _indirectCallTargetOffset;
            _indirectCallFetchOffset = _jumpTargetIp;
            if (_indirectCallFar)
            {
                _jumpTargetCs = _indirectCallTargetSegment;
            }

            _state.CS = _jumpTargetCs;
            _state.IP = _jumpTargetIp;
            _nextFetchAddress = PhysicalAddress(_jumpTargetCs, _jumpTargetIp);
            LastBusCycle = IdleCycle('E', _jumpLastOperand);
        }
        else if (TryTickIndirectCallRead(_instructionTState, out Intel8086BusCycle readCycle))
        {
            LastBusCycle = readCycle;
        }
        else if (TryTickIndirectCallStackWrite(_instructionTState, out Intel8086BusCycle writeCycle))
        {
            LastBusCycle = writeCycle;
        }
        else if (consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _indirectCallModRm);
        }
        else if (consumesDisplacement)
        {
            LastBusCycle = IdleCycle('S', displacementByte);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private int GetIndirectCallFetchStart(int state) =>
        IsWithinIndirectCallRead(state, _indirectCallFetch1) ? _indirectCallFetch1 :
        IsWithinIndirectCallRead(state, _indirectCallFetch2) ? _indirectCallFetch2 :
        IsWithinIndirectCallRead(state, _indirectCallFetch3) ? _indirectCallFetch3 :
        IsWithinIndirectCallRead(state, _indirectCallFetch4) ? _indirectCallFetch4 : 0;

    private bool TryTickIndirectCallRead(int state, out Intel8086BusCycle cycle)
    {
        int start = GetIndirectCallReadStart(state, out int transferIndex);
        if (start == 0)
        {
            cycle = default;
            return false;
        }

        int phase = state - start;
        int transfersPerWord = (_indirectCallPhysicalAddress & 1) == 0 ? 1 : 2;
        int byteIndex = transfersPerWord == 1 ? transferIndex * 2 : transferIndex;
        uint address = GetIndirectCallOperandAddress(byteIndex);
        bool alignedWord = transfersPerWord == 1;
        byte bhe = alignedWord ? (byte)0 : (byte)((address & 1) == 0 ? 1 : 0);
        cycle = phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadIndirectCallOperand(address, bhe, transferIndex, alignedWord),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
        return true;
    }

    private Intel8086BusCycle ReadIndirectCallOperand(uint address, byte bhe, int transferIndex, bool alignedWord)
    {
        if (alignedWord)
        {
            byte low = _bus.Read(address);
            byte high = _bus.Read(GetIndirectCallOperandAddress(transferIndex * 2 + 1));
            SetIndirectCallTargetByte(transferIndex * 2, low);
            SetIndirectCallTargetByte(transferIndex * 2 + 1, high);
            ushort word = (ushort)(low | (high << 8));
            return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, word, "PASV", "T3", '-', 0);
        }

        byte value = _bus.Read(address);
        SetIndirectCallTargetByte(transferIndex, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void SetIndirectCallTargetByte(int index, byte value)
    {
        if (index == 0)
        {
            _indirectCallTargetOffset = (ushort)((_indirectCallTargetOffset & 0xFF00) | value);
        }
        else if (index == 1)
        {
            _indirectCallTargetOffset = (ushort)((_indirectCallTargetOffset & 0x00FF) | (value << 8));
        }
        else if (index == 2)
        {
            _indirectCallTargetSegment = (ushort)((_indirectCallTargetSegment & 0xFF00) | value);
        }
        else
        {
            _indirectCallTargetSegment = (ushort)((_indirectCallTargetSegment & 0x00FF) | (value << 8));
        }
    }

    private uint GetIndirectCallOperandAddress(int byteIndex)
    {
        ushort segment = _memorySegmentStatus switch
        {
            "ES" => _state.ES,
            "CS" => _state.CS,
            "SS" => _state.SS,
            _ => _state.DS,
        };
        ushort offset = unchecked((ushort)(_indirectCallOffset + byteIndex));
        return PhysicalAddress(segment, offset);
    }

    private int GetIndirectCallReadStart(int state, out int index)
    {
        if (IsWithinIndirectCallRead(state, _indirectCallRead1)) { index = 0; return _indirectCallRead1; }
        if (IsWithinIndirectCallRead(state, _indirectCallRead2)) { index = 1; return _indirectCallRead2; }
        if (IsWithinIndirectCallRead(state, _indirectCallRead3)) { index = 2; return _indirectCallRead3; }
        if (IsWithinIndirectCallRead(state, _indirectCallRead4)) { index = 3; return _indirectCallRead4; }
        index = 0;
        return 0;
    }

    private static bool IsWithinIndirectCallRead(int state, int start) =>
        start != 0 && state >= start && state < start + 4;

    private bool TryTickIndirectCallStackWrite(int state, out Intel8086BusCycle cycle)
    {
        bool odd = (_jumpStackAddress & 1) != 0;
        int secondWordStart = odd ? _indirectCallWrite3 : _indirectCallWrite2;
        int firstWordStart = _indirectCallWrite1;
        int wordIndex = 0;
        int start = 0;
        if (firstWordStart != 0 && state >= firstWordStart && state < firstWordStart + (odd ? 8 : 4))
        {
            start = firstWordStart;
        }
        else if (_indirectCallFar && secondWordStart != 0 && state >= secondWordStart && state < secondWordStart + (odd ? 8 : 4))
        {
            start = secondWordStart;
            wordIndex = 1;
        }

        if (start == 0)
        {
            cycle = default;
            return false;
        }

        _memorySegmentStatus = "SS";
        _memoryAddress = (_jumpStackAddress - (uint)(wordIndex * 2)) & 0xFFFFF;
        _memoryWordResult = _indirectCallFar && wordIndex == 0 ? InstructionCs : _jumpReturnIp;
        cycle = TickCallStackWordWrite(_memoryAddress, state - start);
        return true;
    }
}
