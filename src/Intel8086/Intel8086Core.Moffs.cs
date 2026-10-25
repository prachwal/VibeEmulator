namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickMoffs()
    {
        bool store = Opcode is 0xA2 or 0xA3;
        bool word = Opcode is 0xA1 or 0xA3;
        bool prefixed = _moffsPrefixLength != 0;
        int state = _instructionTState;

        if (state == 1)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', consumed);
        }
        else if (prefixed && state == 3)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'F', consumed)
                : IdleCycle('F', consumed);
        }
        else if (prefixed && state == 4)
        {
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
        }
        else if (prefixed && _queueCountAtStart > 5 && state == 8)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }
        else if (!prefixed && state is 3 or 4 && _queueCountAtStart <= 5)
        {
            LastBusCycle = TickMoffsImmediateFetch(state);
        }
        else if (prefixed && state is 5 or 6)
        {
            LastBusCycle = TickMoffsImmediateFetch(state);
        }
        else if (!prefixed && _queueCountAtStart > 5 && state is 3 or 4)
        {
            LastBusCycle = ConsumeMoffsImmediateAsIdle();
        }
        else if (prefixed && _queueCountAtStart > 5 && state == 7)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "R--", "---", 0, 37008, "PASV", "T3", '-', 0);
        }
        else if (prefixed && _queueCountAtStart > 5 && store && state is 9 or 10)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (store && !prefixed && _queueCountAtStart <= 5 && state is 5 or 6)
        {
            LastBusCycle = state == 5
                ? FetchDataCycle('-', 0)
                : new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            if (state == 5 && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
            }

            if (state == 6)
            {
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else if (store && state >= (_queueCountAtStart <= 5 ? 7 : 5) && state < _moffsMemoryStart &&
            (prefixed || _queueCountAtStart > 5))
        {
            LastBusCycle = TickMoffsPrefetch(state);
        }
        else if (!store && prefixed && state is 7 or 8)
        {
            LastBusCycle = _queueCountAtStart > 5
                ? new Intel8086BusCycle(0, 0, "CS", state == 7 ? "R--" : "---", "---", 0, state == 7 ? (ushort)37008 : (ushort)0, "PASV", state == 7 ? "T3" : "T4", '-', 0)
                : IdleCycle('-', 0);
        }
        else if (!store && !prefixed && _queueCountAtStart <= 5 && state is 5 or 6)
        {
            LastBusCycle = state == 5
                ? FetchDataCycle('-', 0)
                : new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            if (state == 5)
            {
                PushQueue(0x90);
            }

            if (state == 6)
            {
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else
        {
            LastBusCycle = TickMoffsMemory(state, word, store);
        }

        if (!store && !prefixed && _queueCountAtStart > 5 && state == _instructionCycles && _state.QueueCount > 0)
        {
            PopQueue();
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle TickMoffsImmediateFetch(int state)
    {
        byte operand = _state.Queue[_state.QueueHead];
        PopQueue();
        _state.IP++;

        if (_moffsPrefixLength != 0 && _queueCountAtStart <= 5)
        {
            Intel8086BusCycle cycle = state == 5
                ? FetchDataCycle('S', operand)
                : new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", 'S', operand);
            if (state == 6)
            {
                if (Opcode is 0xA0 or 0xA1 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(0x90);
                }

                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return cycle;
        }

        if (_moffsPrefixLength != 0)
        {
            Intel8086BusCycle cycle = state == 5
                ? FetchCycle("T1", 'S', operand)
                : FetchContinuationCycle('S', operand);
            if (state == 6 && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
            }

            return cycle;
        }

        return state == 3
            ? FetchCycle("T1", 'S', operand)
            : FetchContinuationCycle('S', operand);
    }

    private Intel8086BusCycle ConsumeMoffsImmediateAsIdle()
    {
        byte operand = _state.Queue[_state.QueueHead];
        PopQueue();
        _state.IP++;
        return IdleCycle('S', operand);
    }

    private Intel8086BusCycle TickMoffsPrefetch(int state)
    {
        int first = _queueCountAtStart <= 5 ? 7 : 5;
        int phase = state - first;
        Intel8086BusCycle cycle = phase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle('-', 0),
            _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
        };

        if (phase == 2)
        {
            int pushes = _queueCountAtStart <= 5
                ? _moffsPrefixLength == 0 ? 2 : 3
                : _moffsPrefixLength == 0 ? 1 : 1;
            for (int i = 0; i < pushes && _state.QueueCount < _state.Queue.Length; i++)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }

        return cycle;
    }

    private Intel8086BusCycle TickMoffsMemory(int state, bool word, bool store)
    {
        int phase = state - _moffsMemoryStart;
        bool splitWord = word && (_moffsPhysicalAddress & 1) != 0;
        int phaseCount = splitWord ? store ? 7 : 8 : store ? 3 : 4;
        if (phase < 0 || phase >= phaseCount)
        {
            return IdleCycle('-', 0);
        }

        int transfer = splitWord ? phase / 4 : 0;
        int transferPhase = splitWord ? phase % 4 : phase;
        uint address = PhysicalAddress(_moffsSegment, unchecked((ushort)(_moffsOffset + transfer)));
        byte bhe = word ? splitWord ? (byte)((address & 1) == 0 ? 1 : 0) : (byte)0 : _moffsBhe;
        string status = GetSegmentStatus(_moffsSegmentOverride);
        string bus = store ? "MEMW" : "MEMR";
        if (transferPhase == 0)
        {
            return new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, bus, "T1", '-', 0);
        }

        if (transferPhase == 1)
        {
            return new Intel8086BusCycle(0, address, status, store ? "-A-" : "R--", "---", bhe, 0, bus, "T2", '-', 0);
        }

        if (transferPhase == 3)
        {
            return new Intel8086BusCycle(0, address, status, "---", "---", bhe, 0, "PASV", "T4", '-', 0);
        }

        ushort data;
        if (store)
        {
            ushort value = _state.AX;
            byte byteValue = (byte)(transfer == 0 ? value : value >> 8);
            if (!word)
            {
                byteValue = Opcode == 0xA2 ? (byte)_state.AX : (byte)(_state.AX >> 8);
            }

            if (word && !splitWord)
            {
                _bus.Write(address, (byte)_state.AX);
                uint highAddress = PhysicalAddress(_moffsSegment, unchecked((ushort)(_moffsOffset + 1)));
                _bus.Write(highAddress, (byte)(_state.AX >> 8));
                data = _state.AX;
            }
            else
            {
                _bus.Write(address, byteValue);
                data = bhe == 0 ? (ushort)(byteValue << 8) : byteValue;
            }
            return new Intel8086BusCycle(0, address, status, "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
        }

        if (!word)
        {
            byte value = _bus.Read(address);
            _state.AX = (ushort)((_state.AX & 0xFF00) | value);
            data = bhe == 0 ? (ushort)(value << 8) : value;
        }
        else if (!splitWord)
        {
            byte low = _bus.Read(address);
            uint highAddress = PhysicalAddress(_moffsSegment, unchecked((ushort)(_moffsOffset + 1)));
            byte high = _bus.Read(highAddress);
            _state.AX = (ushort)(low | (high << 8));
            data = _state.AX;
        }
        else
        {
            byte value = _bus.Read(address);
            _state.AX = transfer == 0
                ? (ushort)((_state.AX & 0xFF00) | value)
                : (ushort)((_state.AX & 0x00FF) | (value << 8));
            data = bhe == 0 ? (ushort)(value << 8) : value;
        }

        return new Intel8086BusCycle(0, address, status, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }
}
