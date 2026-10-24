namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private (int Second, int Third) GetUnprefixedMovFetchStarts(int opcode, int mod, int rm, int firstFetchStart, int queueCount, int memoryBusStart)
    {
        int second = opcode == 0x8A && ((mod == 0 && rm == 6) || (mod == 1 && rm >= 4 && queueCount == 5) || (mod == 2 && rm >= 4))
            ? memoryBusStart + 4
            : opcode == 0x88 || (mod == 2 && rm < 4) || (mod == 1 && rm < 4)
                ? GetLeaSecondFetchStart(mod, rm, firstFetchStart)
                : 0;
        return (second, 0);
    }

    private static (int Second, int Third) GetPrefixedMovFetchStarts(int opcode, int mod, int rm, int queueCount)
    {
        if (queueCount == 5)
        {
            int third = opcode == 0x88
                ? ((mod == 0 && rm == 6) || (mod == 2 && rm >= 4)) ? 11 : mod == 2 ? 14 : 0
                : mod == 0 && rm == 6 ? 15
                    : mod == 2 && (rm is 0 or 3) ? 20
                        : mod == 2 && (rm is 1 or 2) ? 14
                            : mod == 2 && rm >= 4 ? 11
                                : 0;
            return (7, third);
        }

        int secondFetch = opcode == 0x88
            ? mod == 0 && rm == 6 ? 9 : mod == 0 ? 0 : (rm is 1 or 2) ? 13 : 12
            : mod == 0 && rm == 6 ? 15
                : mod == 0 ? (rm is 4 or 5 or 7) ? 0 : 0
                    : mod == 1 && rm >= 4 ? 18
                        : mod == 2 && rm >= 4 ? 18
                            : (rm is 1 or 2) ? 13 : 12;
        return (secondFetch, 0);
    }

    private static int GetPrefixedMovMemoryQueuePrefetchBytes(int opcode, int mod, int rm, int queueCount, int displacementBytes)
    {
        int finalQueueCount = opcode == 0x88
            ? mod == 1 ? (queueCount == 5 ? 4 : 5) : queueCount == 5 ? 5 : 4
            : mod == 0 && rm == 6 ? (queueCount == 5 ? 3 : 2)
                : mod == 0 ? queueCount == 5 ? 5 : 4
                    : mod == 1 && queueCount == 5 ? 4
                        : mod == 1 && rm >= 4 ? 3
                            : mod == 1 ? 5
                                : queueCount == 5
                                    ? rm is 0 or 3 ? 3 : 5
                                    : rm >= 4 ? 2 : 4;
        int initialRemainder = queueCount - 3 - displacementBytes;
        return Math.Max(0, finalQueueCount - initialRemainder);
    }

    private Intel8086BusCycle TickMemoryMovWriteData()
    {
        byte value = GetByteRegister((_movMemoryModRm >> 3) & 7);
        _bus.Write(_movMemoryPhysicalAddress, value);
        ushort data = _movMemoryBhe == 0 ? (ushort)(value << 8) : value;
        return new Intel8086BusCycle(0, _movMemoryPhysicalAddress, _movMemorySegmentStatus, "-AW", "---", _movMemoryBhe, data, "PASV", "T3", '-', 0);
    }

    private Intel8086BusCycle TickMemoryMovReadData()
    {
        _movMemoryValue = _bus.Read(_movMemoryPhysicalAddress);
        SetByteRegister((_movMemoryModRm >> 3) & 7, _movMemoryValue);
        ushort data = _movMemoryBhe == 0 ? (ushort)(_movMemoryValue << 8) : _movMemoryValue;
        return new Intel8086BusCycle(0, _movMemoryPhysicalAddress, _movMemorySegmentStatus, "R--", "---", _movMemoryBhe, data, "PASV", "T3", '-', 0);
    }

    private static int GetMovEffectiveAddressCycles(int mod, int rm) => rm switch
    {
        0 or 3 => 7,
        1 or 2 => 8,
        4 or 5 or 7 => 5,
        6 => mod == 0 ? 6 : 5,
        _ => throw new ArgumentOutOfRangeException(nameof(rm)),
    };

    private int GetMovMemoryBusStart(int opcode, int mod, int rm, int displacementBytes, int queueCount)
    {
        int effectiveAddressCycles = GetMovEffectiveAddressCycles(mod, rm);
        int displacementPenalty = mod == 0 ? 0 : displacementBytes == 0 ? 0 : 4;
        int start = (opcode == 0x88 ? 7 : 3) + effectiveAddressCycles + displacementPenalty;
        if (opcode == 0x88)
        {
            if ((mod == 0 && rm == 6 && queueCount == 6) || (mod == 2 && rm >= 4 && queueCount == 6))
            {
                start++;
            }
        }
        else if ((mod == 0 && queueCount == 5 && rm is 4 or 5 or 7)
            || (mod == 0 && rm == 6 && queueCount == 6)
            || (mod == 2 && queueCount == 6))
        {
            start++;
        }

        if (_movMemoryPrefixLength == 1)
        {
            int prefixPenalty = opcode == 0x88
                ? queueCount == 6 && ((mod == 0 && rm == 6) || (mod == 2 && rm >= 4)) ? 1 : 2
                : GetPrefixedMovReadPenalty(mod, rm, queueCount);
            start += prefixPenalty;
        }

        return start;
    }

    private static int GetPrefixedMovReadPenalty(int mod, int rm, int queueCount)
    {
        if (queueCount == 5)
        {
            return (mod == 0 && (rm is 0 or 3)) || (mod == 2 && (rm is 1 or 2 or >= 4)) ? 3 : 2;
        }

        if (mod == 0)
        {
            return rm == 6 ? 1 : (rm is 4 or 5 or 7) ? 3 : 2;
        }

        return mod == 2 ? 1 : 2;
    }
}
