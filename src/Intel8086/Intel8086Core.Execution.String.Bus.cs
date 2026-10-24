namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
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
