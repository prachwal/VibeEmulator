namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool TryTickTestImmediatePrefetch(out Intel8086BusCycle cycle)
    {
        int profileIndex = GetTestImmediateProfileIndex();

        for (int slot = 0; slot < 3; slot++)
        {
            int fetchStart = TestImmediateFetchStarts[profileIndex + slot];
            if (fetchStart == 0 || _instructionTState < fetchStart || _instructionTState >= fetchStart + 4)
            {
                continue;
            }

            int fetchPhase = _instructionTState - fetchStart;
            cycle = fetchPhase switch
            {
                0 => FetchCycle("T1", '-', 0),
                1 => FetchContinuationCycle('-', 0),
                2 => FetchWordImmediateDataCycle(),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            };

            if (fetchPhase == 2)
            {
                for (int index = 0; index < 2 && _state.QueueCount < _state.Queue.Length; index++)
                {
                    PushQueue(ReadWordImmediatePrefetchByte((_nextFetchAddress + (uint)index) & 0xFFFFF));
                }

                _nextFetchAddress = AdvanceInstructionPrefetchWord(_nextFetchAddress);
            }

            return true;
        }

        cycle = default;
        return false;
    }

    private void ApplyTestImmediateQueueProfile()
    {
        int targetQueueCount = TestImmediateFinalQueueCounts[GetTestImmediateProfileIndex()];
        while (targetQueueCount != byte.MaxValue && _state.QueueCount > targetQueueCount)
        {
            _state.QueueCount--;
        }
    }

    private int GetTestImmediateProfileIndex()
    {
        int mod = _memoryModRm >> 6;
        int queueBucket = _queueCountAtStart >= 6 ? 1 : 0;
        int prefixBucket = _instructionPrefixLength == 0 ? 0 : 1;
        int oddAddressBucket = mod != 3 && (_memoryAddress & 1) != 0 ? 1 : 0;
        int widthBucket = Opcode == 0xF7 ? 1 : 0;
        return (((((widthBucket * 2) + queueBucket) * 2 + prefixBucket) * 2 + oddAddressBucket) * 256 + _memoryModRm) * 3;
    }

    private void TickRegisterTestImmediate()
    {
        _instructionTState++;
        int prefixLength = _instructionPrefixLength;
        if (prefixLength != 0 && _instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
            return;
        }

        if (prefixLength != 0 && _instructionTState == 2)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        int opcodeState = prefixLength == 0 ? 1 : 3;
        if (_instructionTState == opcodeState)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle fetchCycle)
                ? fetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                : IdleCycle('F', Opcode);
            return;
        }

        int modRmState = opcodeState + 1;
        if (_instructionTState == modRmState)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle fetchCycle)
                ? fetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                : IdleCycle('S', _memoryModRm);
            return;
        }

        if (_instructionTState == opcodeState + 2 && prefixLength == 0 && Opcode == 0xF7)
        {
            LastBusCycle = TryTickMemoryPrefetch(out Intel8086BusCycle fetchCycle)
                ? fetchCycle
                : IdleCycle('-', 0);
            return;
        }

        bool consumesImmediate = prefixLength == 0
            ? _instructionTState == 4 || Opcode == 0xF7 && _instructionTState == 5
            : _instructionTState == 6 || Opcode == 0xF7 && _instructionTState == 7;
        Intel8086BusCycle busCycle = TryTickMemoryPrefetch(out Intel8086BusCycle prefetchCycle)
            ? prefetchCycle
            : IdleCycle('-', 0);
        if (consumesImmediate)
        {
            byte immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            LastBusCycle = busCycle with { QueueStatus = 'S', QueueData = immediate };
            if (Opcode == 0xF6 || (_instructionTState == 5 && prefixLength == 0) || _instructionTState == 7)
            {
                ApplyRegisterTestImmediate();
            }
        }
        else
        {
            LastBusCycle = busCycle;
        }

        if (_instructionTState == _instructionCycles)
        {
            ApplyTestImmediateQueueProfile();
            _instructionTState = 0;
        }
    }

    private void ApplyRegisterTestImmediate()
    {
        int register = _memoryModRm & 7;
        if (Opcode == 0xF6)
        {
            Intel8086AluResult result = Intel8086Alu.Execute(
                GetByteRegister(register), (byte)_immediateWord, _state.Flags, Intel8086AluOperation.Test);
            _state.Flags = result.Flags;
            return;
        }

        Intel8086AluWordResult wordResult = Intel8086Alu.Execute(
            GetWordRegister(register), _immediateWord, _state.Flags, Intel8086AluOperation.Test);
        _state.Flags = wordResult.Flags;
    }

    private void CalculateMemoryTestImmediate()
    {
        if (Opcode == 0xF6)
        {
            _state.Flags = Intel8086Alu.Execute(
                _memoryValue, (byte)_immediateWord, _state.Flags, Intel8086AluOperation.Test).Flags;
            return;
        }

        _state.Flags = Intel8086Alu.Execute(
            _memoryWordValue, _immediateWord, _state.Flags, Intel8086AluOperation.Test).Flags;
    }
}
