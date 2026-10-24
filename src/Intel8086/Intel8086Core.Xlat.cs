namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickXlat()
    {
        if (_instructionTState == 1)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', consumed);
            return;
        }

        if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_xlatPrefixLength == 1 && _instructionTState == 3)
        {
            byte consumed = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = GetXlatPrefetchStart() == 3
                ? FetchCycle("T1", 'F', consumed)
                : IdleCycle('F', consumed);
            return;
        }

        int prefetchStart = GetXlatPrefetchStart();
        if (prefetchStart != 0 && _instructionTState >= prefetchStart && _instructionTState <= prefetchStart + 3)
        {
            int prefetchPhase = _instructionTState - prefetchStart;
            LastBusCycle = prefetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle(),
                2 => FetchDataCycle('-', 0),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (prefetchPhase == 2 && _state.QueueCount < _state.Queue.Length)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
        }
        else
        {
            int memoryReadStart = _instructionCycles - 3;
            int memoryReadPhase = _instructionTState - memoryReadStart;
            if (memoryReadPhase is >= 0 and <= 3)
            {
                LastBusCycle = memoryReadPhase switch
                {
                    0 => new Intel8086BusCycle(1, _xlatPhysicalAddress, "--", "---", "---", _xlatBhe, 0, "MEMR", "T1", '-', 0),
                    1 => new Intel8086BusCycle(0, _xlatPhysicalAddress, _xlatSegmentStatus, "R--", "---", _xlatBhe, 0, "MEMR", "T2", '-', 0),
                    2 => TickXlatMemoryReadData(),
                    _ => new Intel8086BusCycle(0, _xlatPhysicalAddress, _xlatSegmentStatus, "---", "---", _xlatBhe, 0, "PASV", "T4", '-', 0),
                };
            }
            else
            {
                LastBusCycle = IdleCycle('-', 0);
            }
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_xlatPrefixLength == 0 && _queueCountAtStart == 6)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }

    private int GetXlatPrefetchStart() => _xlatPrefixLength == 0
        ? _queueCountAtStart == 5 ? 3 : 0
        : _queueCountAtStart == 5 ? 3 : 5;

    private Intel8086BusCycle TickXlatMemoryReadData()
    {
        byte value = _bus.Read(_xlatPhysicalAddress);
        _state.AX = (ushort)((_state.AX & 0xFF00) | value);
        ushort data = _xlatBhe == 0 ? (ushort)(value << 8) : value;
        return new Intel8086BusCycle(0, _xlatPhysicalAddress, _xlatSegmentStatus, "R--", "---", _xlatBhe, data, "PASV", "T3", '-', 0);
    }
}
