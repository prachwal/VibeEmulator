namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickIndirectJump()
    {
        _instructionTState++;
        bool consumesOpcode = false;
        bool consumesModRm = false;
        if (_indirectJumpPrefixLength != 0)
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

        int displacementIndex = _instructionTState - _indirectJumpDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _indirectJumpDisplacementBytes;
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
            _jumpLastOperand = _indirectJumpModRm;
            if ((_indirectJumpModRm >> 6) == 3)
            {
                _indirectJumpTargetOffset = GetWordRegister(_indirectJumpModRm & 7);
            }
        }

        int fetchStart = GetIndirectJumpFetchStart(_instructionTState);
        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesOpcode ? 'F' : consumesDisplacement ? 'S' : '-',
                    consumesOpcode ? Opcode : displacementByte),
                1 => FetchContinuationCycle(consumesModRm || consumesDisplacement ? 'S' : '-',
                    consumesModRm ? _indirectJumpModRm : displacementByte),
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
                int bytesFetched = fetchStart > _indirectJumpFlushStart
                    ? (_jumpTargetIp & 1) == 0 ? 2 : 1
                    : Math.Min(2, _state.Queue.Length - _state.QueueCount);
                for (int index = 0; index < bytesFetched; index++)
                {
                    PushQueue(fetchStart > _indirectJumpFlushStart
                        ? (byte)0x90
                        : _bus.Read((fetchAddress + (uint)index) & 0xFFFFF));
                }

                _indirectJumpFetchOffset = unchecked((ushort)(_indirectJumpFetchOffset + bytesFetched));
                _nextFetchAddress = PhysicalAddress(_state.CS, _indirectJumpFetchOffset);
            }
        }
        else if (_instructionTState == _indirectJumpFlushStart)
        {
            _state.QueueHead = 0;
            _state.QueueCount = 0;
            _jumpTargetIp = _indirectJumpTargetOffset;
            if (_indirectJumpFar)
            {
                _jumpTargetCs = _indirectJumpTargetSegment;
            }

            _state.CS = _jumpTargetCs;
            _state.IP = _jumpTargetIp;
            _indirectJumpFetchOffset = _jumpTargetIp;
            _nextFetchAddress = PhysicalAddress(_jumpTargetCs, _jumpTargetIp);
            LastBusCycle = IdleCycle('E', _jumpLastOperand);
        }
        else if (TryTickIndirectJumpRead(_instructionTState, out Intel8086BusCycle readCycle))
        {
            LastBusCycle = readCycle;
        }
        else if (consumesOpcode)
        {
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (consumesModRm)
        {
            LastBusCycle = IdleCycle('S', _indirectJumpModRm);
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
            if (GetIndirectJumpFetchStart(_instructionTState) == 0)
            {
                LastBusCycle = FetchCycle("T1", '-', 0);
            }

            if (_state.QueueCount != 0)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private int GetIndirectJumpFetchStart(int state) =>
        IsWithinIndirectCallRead(state, _indirectJumpFetch1) ? _indirectJumpFetch1 :
        IsWithinIndirectCallRead(state, _indirectJumpFetch2) ? _indirectJumpFetch2 :
        IsWithinIndirectCallRead(state, _indirectJumpFetch3) ? _indirectJumpFetch3 :
        IsWithinIndirectCallRead(state, _indirectJumpFetch4) ? _indirectJumpFetch4 : 0;

    private int GetIndirectJumpReadStart(int state, out int index)
    {
        if (IsWithinIndirectCallRead(state, _indirectJumpRead1)) { index = 0; return _indirectJumpRead1; }
        if (IsWithinIndirectCallRead(state, _indirectJumpRead2)) { index = 1; return _indirectJumpRead2; }
        if (IsWithinIndirectCallRead(state, _indirectJumpRead3)) { index = 2; return _indirectJumpRead3; }
        if (IsWithinIndirectCallRead(state, _indirectJumpRead4)) { index = 3; return _indirectJumpRead4; }
        index = 0;
        return 0;
    }

    private bool TryTickIndirectJumpRead(int state, out Intel8086BusCycle cycle)
    {
        int start = GetIndirectJumpReadStart(state, out int transferIndex);
        if (start == 0)
        {
            cycle = default;
            return false;
        }

        int phase = state - start;
        bool alignedWord = (_indirectJumpPhysicalAddress & 1) == 0;
        int byteIndex = alignedWord ? transferIndex * 2 : transferIndex;
        uint address = GetIndirectJumpOperandAddress(byteIndex);
        byte bhe = alignedWord ? (byte)0 : (byte)((address & 1) == 0 ? 1 : 0);
        cycle = phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadIndirectJumpOperand(address, bhe, transferIndex, alignedWord),
            _ => new Intel8086BusCycle(0, 0, _memorySegmentStatus, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
        return true;
    }

    private Intel8086BusCycle ReadIndirectJumpOperand(uint address, byte bhe, int transferIndex, bool alignedWord)
    {
        if (alignedWord)
        {
            byte low = _bus.Read(address);
            byte high = _bus.Read(GetIndirectJumpOperandAddress(transferIndex * 2 + 1));
            SetIndirectJumpTargetByte(transferIndex * 2, low);
            SetIndirectJumpTargetByte(transferIndex * 2 + 1, high);
            ushort word = (ushort)(low | (high << 8));
            return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, word, "PASV", "T3", '-', 0);
        }

        byte value = _bus.Read(address);
        SetIndirectJumpTargetByte(transferIndex, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, _memorySegmentStatus, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void SetIndirectJumpTargetByte(int index, byte value)
    {
        if (index == 0)
        {
            _indirectJumpTargetOffset = (ushort)((_indirectJumpTargetOffset & 0xFF00) | value);
        }
        else if (index == 1)
        {
            _indirectJumpTargetOffset = (ushort)((_indirectJumpTargetOffset & 0x00FF) | (value << 8));
        }
        else if (index == 2)
        {
            _indirectJumpTargetSegment = (ushort)((_indirectJumpTargetSegment & 0xFF00) | value);
        }
        else
        {
            _indirectJumpTargetSegment = (ushort)((_indirectJumpTargetSegment & 0x00FF) | (value << 8));
        }
    }

    private uint GetIndirectJumpOperandAddress(int byteIndex)
    {
        ushort segment = _memorySegmentStatus switch
        {
            "ES" => _state.ES,
            "CS" => _state.CS,
            "SS" => _state.SS,
            _ => _state.DS,
        };
        ushort offset = unchecked((ushort)(_indirectJumpOffset + byteIndex));
        return PhysicalAddress(segment, offset);
    }
}
