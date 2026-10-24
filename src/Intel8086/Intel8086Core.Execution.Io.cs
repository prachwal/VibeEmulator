namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickByteInputIo()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (Opcode == 0xE4 && _instructionTState == 3)
        {
            byte immediatePort = (byte)_immediateWord;
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', immediatePort)
                : IdleCycle('S', immediatePort);
            return;
        }

        if (Opcode == 0xE4 && _instructionTState == 4 && _queueCountAtStart <= 5)
        {
            LastBusCycle = FetchContinuationCycle('-', 0);
            return;
        }

        if (Opcode == 0xE4 && _instructionTState == 5 && _queueCountAtStart <= 5)
        {
            LastBusCycle = FetchDataCycle();
            PushQueue(0x90);
            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            return;
        }

        if (Opcode == 0xE4 && _instructionTState == 6 && _queueCountAtStart <= 5)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            return;
        }

        if (Opcode == 0xEC && _instructionTState == 2)
        {
            PopQueue();
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int ioStart = Opcode == 0xE4 ? 7 : 5;
        int ioPhase = _instructionTState - ioStart;
        ushort port = Opcode == 0xE4 ? (byte)_immediateWord : _state.DX;
        byte bhe = (byte)((port & 1) == 0 ? 1 : 0);
        if (ioPhase == 0)
        {
            LastBusCycle = new Intel8086BusCycle(1, port, "--", "---", "---", bhe, 0, "IOR", "T1", '-', 0);
        }
        else if (ioPhase == 1)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0x20000u | port, "CS", "---", "R--", bhe, 0, "IOR", "T2", '-', 0);
        }
        else if (ioPhase == 2)
        {
            byte value = _portBus.Input(port);
            ushort data = (ushort)((port & 1) == 0 ? value : value << 8);
            _state.AX = (ushort)((_state.AX & 0xFF00) | value);
            LastBusCycle = new Intel8086BusCycle(0, 0x20000u | data, "CS", "---", "R--", bhe, data, "PASV", "T3", '-', 0);
        }
        else if (ioPhase == 3)
        {
            ushort data = (ushort)((port & 1) == 0 ? (byte)_state.AX : (byte)_state.AX << 8);
            LastBusCycle = new Intel8086BusCycle(0, 0x20000u | data, "CS", "---", "---", bhe, 0, "PASV", "T4", '-', 0);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            if (Opcode == 0xE4 && _queueCountAtStart > 5)
            {
                PopQueue();
            }

            _instructionTState = 0;
        }
    }
}
