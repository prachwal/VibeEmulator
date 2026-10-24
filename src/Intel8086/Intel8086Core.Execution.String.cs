namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsMoveStringOpcode(byte opcode) => opcode is 0xA4 or 0xA5;
    private static bool IsStoreStringOpcode(byte opcode) => opcode is 0xAA or 0xAB;
    private static bool IsLoadStringOpcode(byte opcode) => opcode is 0xAC or 0xAD;
    private static bool IsScanStringOpcode(byte opcode) => opcode is 0xAE or 0xAF;
    private static bool IsCompareStringOpcode(byte opcode) => opcode is 0xA6 or 0xA7;
    private static bool IsByteInputIoOpcode(byte opcode) => opcode is 0xE4 or 0xEC;

    private void TickStringRepeatZeroCount()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
        }
        else if (_instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (_instructionTState == 3)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 4)
        {
            LastBusCycle = IdleCycle('-', 0);
        }
        else if (_instructionTState == 5)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
        }
        else if (_instructionTState == 6)
        {
            LastBusCycle = FetchContinuationCycle('-', 0);
        }
        else if (_instructionTState == 7)
        {
            LastBusCycle = FetchDataCycle();
            PushQueue(0x90);
            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }
        else if (_instructionTState == 8)
        {
            LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
    }

    private void TickRepeatedScasByte()
    {
        _instructionTState++;
        switch (_instructionTState)
        {
            case 1:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
                return;
            case 2:
                LastBusCycle = IdleCycle('-', 0);
                return;
            case 3:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', Opcode);
                return;
            case 4:
                LastBusCycle = IdleCycle('-', 0);
                return;
            case 5:
                LastBusCycle = FetchCycle("T1", '-', 0);
                return;
            case 6:
                LastBusCycle = FetchContinuationCycle('-', 0);
                return;
            case 7:
                LastBusCycle = FetchDataCycle();
                PushQueue(0x90);
                _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
                return;
            case 8:
                LastBusCycle = new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0);
                return;
        }

        int iterationPhase = (_instructionTState - 17) % 15;
        if (iterationPhase is >= 0 and < 4)
        {
            _stringSourceAddress = PhysicalAddress(_state.ES, _state.DI);
            LastBusCycle = TickStringRead(_stringSourceAddress, iterationPhase);
            if (iterationPhase == 3)
            {
                _state.Flags = Intel8086Alu.Execute((byte)_state.AX, _stringByteValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
                int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
                _state.DI = unchecked((ushort)(_state.DI + delta));
                _state.CX--;

                bool keepRepeating = _state.CX != 0 && (_instructionPrefix == 0xF2
                    ? (_state.Flags & 0x0040) == 0
                    : (_state.Flags & 0x0040) != 0);
                if (!keepRepeating)
                {
                    _instructionCycles = _instructionTState + 6;
                }
            }
            return;
        }

        LastBusCycle = IdleCycle('-', 0);
        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private void TickCompareString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readStart = _instructionTState < _stringSecondReadStart ? _stringReadStart : _stringSecondReadStart;
        bool destinationRead = _instructionTState >= _stringSecondReadStart;
        uint baseAddress = destinationRead ? _stringDestinationAddress : _stringSourceAddress;
        int readPhase = _instructionTState - readStart;
        int readCycleCount = Opcode == 0xA7 && (baseAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xA7 && (baseAddress & 1) != 0 && readPhase >= 4
                ? (baseAddress + 1) & 0xFFFFF
                : baseAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            if (Opcode == 0xA6)
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareByteFirst, _stringCompareByteSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }
            else
            {
                _state.Flags = Intel8086Alu.Execute(_stringCompareWordFirst, _stringCompareWordSecond, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.SI = unchecked((ushort)(_state.SI + (Opcode == 0xA7 ? delta * 2 : delta)));
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xA7 ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickScanString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readPhase = _instructionTState - _stringReadStart;
        int readCycleCount = Opcode == 0xAF && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xAF && (_stringSourceAddress & 1) != 0 && readPhase >= 4
                ? (_stringSourceAddress + 1) & 0xFFFFF
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            if (Opcode == 0xAE)
            {
                _state.Flags = Intel8086Alu.Execute((byte)_state.AX, _stringByteValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }
            else
            {
                _state.Flags = Intel8086Alu.Execute(_state.AX, _stringWordValue, _state.Flags, Intel8086AluOperation.Compare).Flags;
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xAF ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickLoadString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readPhase = _instructionTState - _stringReadStart;
        int readCycleCount = Opcode == 0xAD && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xAD && (_stringSourceAddress & 1) != 0 && readPhase >= 4
                ? (_stringSourceAddress + 1) & 0xFFFFF
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            _state.AX = Opcode == 0xAC
                ? (ushort)((_state.AX & 0xFF00) | _stringByteValue)
                : _stringWordValue;
            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.SI = unchecked((ushort)(_state.SI + (Opcode == 0xAD ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private void TickStoreString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int writePhase = _instructionTState - _stringWriteStart;
        int writeCycleCount = Opcode == 0xAB && (_stringDestinationAddress & 1) != 0 ? 8 : 4;
        if (writePhase >= 0 && writePhase < writeCycleCount)
        {
            uint address = Opcode == 0xAB && (_stringDestinationAddress & 1) != 0 && writePhase >= 4
                ? (_stringDestinationAddress + 1) & 0xFFFFF
                : _stringDestinationAddress;
            LastBusCycle = TickStoreWrite(address, writePhase % 4);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            _state.DI = unchecked((ushort)(_state.DI + (Opcode == 0xAB ? delta * 2 : delta)));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle TickStoreWrite(uint address, int phase)
    {
        byte bhe = (byte)(Opcode == 0xAB && (_stringDestinationAddress & 1) == 0 ? 0 : (address & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "ES", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteStoreByte(address, bhe),
            _ => new Intel8086BusCycle(0, 0, "ES", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteStoreByte(uint address, byte bhe)
    {
        if (Opcode == 0xAB && (_stringDestinationAddress & 1) == 0)
        {
            _bus.Write(address, (byte)_state.AX);
            _bus.Write((address + 1) & 0xFFFFF, (byte)(_state.AX >> 8));
            return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, _state.AX, "PASV", "T3", '-', 0);
        }

        byte value = Opcode == 0xAB
            ? (byte)(_state.AX >> (address == _stringDestinationAddress ? 0 : 8))
            : (byte)(_state.AX >> ((address & 1) == 0 ? 0 : 8));
        _bus.Write(address, value);
        ushort data = (ushort)((address & 1) == 0 ? value : value << 8);
        return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private void TickMoveString()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_queueCountAtStart <= 5 && _instructionTState is >= 3 and <= 6)
        {
            LastBusCycle = _instructionTState switch
            {
                3 => FetchCycle("T1", '-', 0),
                4 => FetchContinuationCycle('-', 0),
                5 => FetchStringQueueData(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };
            return;
        }

        int readPhase = _instructionTState - _stringReadStart;
        int readCycleCount = Opcode == 0xA5 && (_stringSourceAddress & 1) != 0 ? 8 : 4;
        if (readPhase >= 0 && readPhase < readCycleCount)
        {
            uint address = Opcode == 0xA5 && (_stringSourceAddress & 1) != 0 && readPhase >= 4
                ? (_stringSourceAddress + 1) & 0xFFFFF
                : _stringSourceAddress;
            LastBusCycle = TickStringRead(address, readPhase % 4);
            return;
        }

        int writePhase = _instructionTState - _stringWriteStart;
        if (writePhase is >= 0 and < 4)
        {
            LastBusCycle = TickStringWrite(writePhase);
            return;
        }

        if (_instructionTState == 10)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        if (_instructionTState == _instructionCycles)
        {
            if (_queueCountAtStart > 5)
            {
                PopQueue();
            }

            int delta = (_state.Flags & 0x0400) == 0 ? 1 : -1;
            if (Opcode == 0xA5)
            {
                delta *= 2;
            }

            _state.SI = unchecked((ushort)(_state.SI + delta));
            _state.DI = unchecked((ushort)(_state.DI + delta));
            _instructionTState = 0;
        }

        LastBusCycle = IdleCycle('-', 0);
    }

    private Intel8086BusCycle FetchStringQueueData()
    {
        PopQueue();
        PushQueue(0x90);
        PushQueue(0x90);
        _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        return FetchDataCycle();
    }

    private Intel8086BusCycle TickStringRead(uint address, int phase)
    {
        byte bhe = (byte)((Opcode is 0xA5 or 0xAD or 0xAF) && (_stringSourceAddress & 1) == 0 ? 0 : (address & 1) == 0 ? 1 : 0);
        bool destinationRead = IsScanStringOpcode(Opcode) || (IsCompareStringOpcode(Opcode) && _instructionTState >= _stringSecondReadStart);
        if (IsCompareStringOpcode(Opcode))
        {
            uint wordBaseAddress = destinationRead ? _stringDestinationAddress : _stringSourceAddress;
            bhe = (byte)(Opcode == 0xA7 && (wordBaseAddress & 1) == 0 ? 0 : (address & 1) == 0 ? 1 : 0);
        }
        string segment = destinationRead ? "ES" : "DS";
        return phase switch
        {
            0 => new Intel8086BusCycle(1, address, "--", "---", "---", bhe, 0, "MEMR", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, segment, "R--", "---", bhe, 0, "MEMR", "T2", '-', 0),
            2 => ReadStringData(address, bhe),
            _ => new Intel8086BusCycle(0, 0, segment, "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle ReadStringData(uint address, byte bhe)
    {
        byte value = _bus.Read(address);
        ushort data;
        bool destinationRead = IsScanStringOpcode(Opcode) || (IsCompareStringOpcode(Opcode) && _instructionTState >= _stringSecondReadStart);
        string segment = destinationRead ? "ES" : "DS";
        uint wordBaseAddress = destinationRead && IsCompareStringOpcode(Opcode) ? _stringDestinationAddress : _stringSourceAddress;
        if (Opcode is 0xA4 or 0xA6 or 0xAC or 0xAE)
        {
            if (Opcode == 0xA6)
            {
                if (destinationRead)
                {
                    _stringCompareByteSecond = value;
                }
                else
                {
                    _stringCompareByteFirst = value;
                }
            }
            else
            {
                _stringByteValue = value;
            }
            data = (ushort)((address & 1) == 0 ? value : value << 8);
        }
        else if ((wordBaseAddress & 1) != 0)
        {
            if (address == wordBaseAddress)
            {
                if (IsCompareStringOpcode(Opcode))
                {
                    if (destinationRead)
                    {
                        _stringCompareWordSecond = (ushort)((_stringCompareWordSecond & 0xFF00) | value);
                    }
                    else
                    {
                        _stringCompareWordFirst = (ushort)((_stringCompareWordFirst & 0xFF00) | value);
                    }
                }
                else
                {
                    _stringWordValue = (ushort)((_stringWordValue & 0xFF00) | value);
                }
            }
            else
            {
                if (IsCompareStringOpcode(Opcode))
                {
                    if (destinationRead)
                    {
                        _stringCompareWordSecond = (ushort)((_stringCompareWordSecond & 0x00FF) | (value << 8));
                    }
                    else
                    {
                        _stringCompareWordFirst = (ushort)((_stringCompareWordFirst & 0x00FF) | (value << 8));
                    }
                }
                else
                {
                    _stringWordValue = (ushort)((_stringWordValue & 0x00FF) | (value << 8));
                }
            }

            data = (ushort)((address & 1) == 0 ? value : value << 8);
        }
        else
        {
            byte high = _bus.Read((address + 1) & 0xFFFFF);
            ushort word = (ushort)(value | (high << 8));
            if (IsCompareStringOpcode(Opcode))
            {
                if (destinationRead)
                {
                    _stringCompareWordSecond = word;
                }
                else
                {
                    _stringCompareWordFirst = word;
                }
            }
            else
            {
                _stringWordValue = word;
            }
            data = word;
        }

        return new Intel8086BusCycle(0, 0, segment, "R--", "---", bhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickStringWrite(int phase)
    {
        byte bhe = (byte)(Opcode == 0xA5 && (_stringDestinationAddress & 1) == 0 ? 0 : (_stringDestinationAddress & 1) == 0 ? 1 : 0);
        return phase switch
        {
            0 => new Intel8086BusCycle(1, _stringDestinationAddress, "--", "---", "---", bhe, 0, "MEMW", "T1", '-', 0),
            1 => new Intel8086BusCycle(0, 0, "ES", "-A-", "---", bhe, 0, "MEMW", "T2", '-', 0),
            2 => WriteStringByte(bhe),
            _ => new Intel8086BusCycle(0, 0, "ES", "---", "---", bhe, 0, "PASV", "T4", '-', 0),
        };
    }

    private Intel8086BusCycle WriteStringByte(byte bhe)
    {
        ushort data;
        if (Opcode == 0xA4)
        {
            _bus.Write(_stringDestinationAddress, _stringByteValue);
            data = (ushort)((_stringDestinationAddress & 1) == 0 ? _stringByteValue : _stringByteValue << 8);
        }
        else
        {
            _bus.Write(_stringDestinationAddress, (byte)_stringWordValue);
            _bus.Write((_stringDestinationAddress + 1) & 0xFFFFF, (byte)(_stringWordValue >> 8));
            data = _stringWordValue;
        }

        return new Intel8086BusCycle(0, 0, "ES", "-AW", "---", bhe, data, "PASV", "T3", '-', 0);
    }
}
