namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void TickWordInputIo()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        bool immediate = Opcode == 0xE5;
        ushort port = immediate ? (byte)_immediateWord : _state.DX;
        if (immediate && _instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', (byte)_immediateWord)
                : IdleCycle('S', (byte)_immediateWord);
            return;
        }

        if (immediate && _queueCountAtStart <= 5 && _instructionTState is 4 or 5 or 6)
        {
            LastBusCycle = _instructionTState switch
            {
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (_instructionTState == 5)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

        if (!immediate && _instructionTState == 2)
        {
            PopQueue();
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int firstStart = immediate ? 7 : 5;
        int transferPhase = _instructionTState - firstStart;
        if (port % 2 != 0 && transferPhase >= 4)
        {
            port++;
            transferPhase -= 4;
        }

        if (transferPhase is >= 0 and < 4)
        {
            byte bhe = (byte)(port == (immediate ? (byte)_immediateWord : _state.DX) ? 0 : 1);
            if (transferPhase == 0)
            {
                LastBusCycle = new Intel8086BusCycle(1, port, "--", "---", "---", bhe, 0, "IOR", "T1", '-', 0);
            }
            else if (transferPhase == 1)
            {
                LastBusCycle = new Intel8086BusCycle(0, 0x20000u | port, "CS", "---", "R--", bhe, 0, "IOR", "T2", '-', 0);
            }
            else if (transferPhase == 2)
            {
                ushort data;
                if ((byte)(immediate ? _immediateWord : _state.DX) % 2 == 0)
                {
                    byte low = _portBus.Input(port);
                    byte high = _portBus.Input((ushort)(port + 1));
                    data = (ushort)(low | (high << 8));
                }
                else
                {
                    byte value = _portBus.Input(port);
                    data = (ushort)((port & 1) == 0 ? value : value << 8);
                    if (port % 2 == 0)
                    {
                        _state.AX = (ushort)((_state.AX & 0xFF00) | value);
                    }
                    else
                    {
                        _state.AX = (ushort)((_state.AX & 0x00FF) | (value << 8));
                    }
                }

                if ((byte)(immediate ? _immediateWord : _state.DX) % 2 == 0)
                {
                    _state.AX = data;
                }

                LastBusCycle = new Intel8086BusCycle(0, 0x20000u | data, "CS", "---", "R--", bhe, data, "PASV", "T3", '-', 0);
            }
            else
            {
                LastBusCycle = new Intel8086BusCycle(0, 0x20000u | _state.AX, "CS", "---", "---", bhe, 0, "PASV", "T4", '-', 0);
            }

            if (_instructionTState == _instructionCycles)
            {
                if (immediate && _queueCountAtStart > 5)
                {
                    PopQueue();
                }

                _instructionTState = 0;
            }

            return;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickByteOutputIo()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (Opcode == 0xE6 && _instructionTState == 3)
        {
            byte immediatePort = (byte)_immediateWord;
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', immediatePort)
                : IdleCycle('S', immediatePort);
            return;
        }

        if (Opcode == 0xE6 && _queueCountAtStart <= 5 && _instructionTState is 4 or 5 or 6)
        {
            if (_instructionTState == 4)
            {
                LastBusCycle = FetchContinuationCycle('-', 0);
            }
            else if (_instructionTState == 5)
            {
                LastBusCycle = FetchDataCycle();
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }
            else
            {
                LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
            }

            return;
        }

        if (Opcode == 0xE6 && _queueCountAtStart > 5 && _instructionTState is >= 5 and <= 8)
        {
            LastBusCycle = _instructionTState switch
            {
                5 => FetchCycle("T1", '-', 0),
                6 => FetchContinuationCycle('-', 0),
                7 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (_instructionTState == 7)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

        if (Opcode == 0xEE && _queueCountAtStart > 5 && _instructionTState == 2)
        {
            PopQueue();
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (Opcode == 0xEE && _queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            if (_instructionTState == 5)
            {
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
            }

            return;
        }

        ushort port = Opcode == 0xE6 ? (byte)_immediateWord : _state.DX;
        byte value = (byte)_state.AX;
        byte bhe = (byte)((port & 1) == 0 ? 1 : 0);
        int ioStart = Opcode == 0xE6 ? 9 : _queueCountAtStart > 5 ? 6 : 7;
        int ioPhase = _instructionTState - ioStart;
        if (ioPhase == 0)
        {
            LastBusCycle = new Intel8086BusCycle(1, port, "--", "---", "---", bhe, 0, "IOW", "T1", '-', 0);
        }
        else if (ioPhase == 1)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0x20000u | _state.AX, "CS", "---", "-A-", bhe, 0, "IOW", "T2", '-', 0);
        }
        else if (ioPhase == 2)
        {
            _portBus.Output(port, value);
            ushort data = (ushort)((port & 1) == 0 ? value : value << 8);
            LastBusCycle = new Intel8086BusCycle(0, 0x20000u | _state.AX, "CS", "---", "-AW", bhe, data, "PASV", "T3", '-', 0);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

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
