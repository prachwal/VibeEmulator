namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void ExecuteUnaryNotNegGroup()
    {
        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        PopQueue();
        _state.IP++;
        int register = modRm & 7;
        bool negate = ((modRm >> 3) & 7) == 3;
        if (Opcode == 0xF6)
        {
            Intel8086AluResult result = negate
                ? Intel8086Alu.ExecuteNegate(GetByteRegister(register), _state.Flags)
                : Intel8086Alu.ExecuteNot(GetByteRegister(register), _state.Flags);
            SetByteRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
        else
        {
            Intel8086AluWordResult result = negate
                ? Intel8086Alu.ExecuteNegate(GetWordRegister(register), _state.Flags)
                : Intel8086Alu.ExecuteNot(GetWordRegister(register), _state.Flags);
            SetWordRegister(register, result.Value);
            _state.Flags = result.Flags;
        }

        LastBusCycle = IdleCycle('S', modRm);
    }

    private void ExecuteShiftRotateGroup()
    {
        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        PopQueue();
        _state.IP++;
        int register = modRm & 7;
        int extension = (modRm >> 3) & 7;
        if (Opcode == 0xD0)
        {
            Intel8086AluResult result = Intel8086Alu.ExecuteShiftRotate(GetByteRegister(register), _state.Flags, extension);
            SetByteRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
        else
        {
            Intel8086AluWordResult result = Intel8086Alu.ExecuteShiftRotate(GetWordRegister(register), _state.Flags, extension);
            SetWordRegister(register, result.Value);
            _state.Flags = result.Flags;
        }

        LastBusCycle = IdleCycle('S', modRm);
        _instructionTState = 0;
    }

    private void BeginShiftRotateWithCl()
    {
        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        int register = modRm & 7;
        int extension = (modRm >> 3) & 7;
        if (Opcode == 0xD2)
        {
            byte value = GetByteRegister(register);
            ushort flags = _state.Flags;
            for (int count = 0; count < _shiftCount; count++)
            {
                Intel8086AluResult result = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = result.Value;
                flags = result.Flags;
            }

            if (_shiftCount != 0)
            {
                SetByteRegister(register, value);
                _state.Flags = flags;
            }
        }
        else
        {
            ushort value = GetWordRegister(register);
            ushort flags = _state.Flags;
            for (int count = 0; count < _shiftCount; count++)
            {
                Intel8086AluWordResult result = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = result.Value;
                flags = result.Flags;
            }

            if (_shiftCount != 0)
            {
                SetWordRegister(register, value);
                _state.Flags = flags;
            }
        }

        _state.IP++;
        LastBusCycle = IdleCycle('S', modRm);
    }

    private void TickShiftRotateWithCl()
    {
        int fetchStart = _instructionPrefixLength != 0
            ? _queueCountAtStart <= 5
                ? _instructionTState <= 6 ? 3 : _instructionTState <= 10 ? 7 : int.MaxValue
                : 5
            : _queueCountAtStart <= 5 ? 3 : 4;
        int fetchPhase = _instructionTState - fetchStart;
        LastBusCycle = fetchPhase switch
        {
            0 => FetchCycle("T1", '-', 0),
            1 => FetchContinuationCycle('-', 0),
            2 => FetchDataCycle(),
            3 => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", '-', 0),
            _ => IdleCycle('-', 0),
        };

        if (fetchPhase == 2)
        {
            PushQueue(unchecked((byte)37008));
            if (_instructionPrefixLength != 0 && _queueCountAtStart <= 5 && fetchStart == 7)
            {
                PushQueue(0x90);
            }

            _nextFetchAddress = AdvanceInstructionPrefetchWord(_nextFetchAddress);
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private void TickPrefixedRegisterShiftRotate()
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
            LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle opcodeFetchCycle)
                ? opcodeFetchCycle with { QueueStatus = 'F', QueueData = Opcode }
                : IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 4)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out Intel8086BusCycle modRmFetchCycle)
                ? modRmFetchCycle with { QueueStatus = 'S', QueueData = _memoryModRm }
                : IdleCycle('S', _memoryModRm);
            ApplyPrefixedRegisterShiftRotate();
            if (!IsShiftRotateClGroupOpcode(Opcode))
            {
                if (_state.QueueCount > 0)
                {
                    PopQueue();
                }

                _instructionTState = 0;
            }
        }
        else
        {
            TickShiftRotateWithCl();
        }
    }

    private void ApplyPrefixedRegisterShiftRotate()
    {
        int register = _memoryModRm & 7;
        int extension = (_memoryModRm >> 3) & 7;
        if (Opcode is 0xD0 or 0xD2)
        {
            byte value = GetByteRegister(register);
            ushort flags = _state.Flags;
            int count = Opcode == 0xD0 ? 1 : _shiftCount;
            for (int index = 0; index < count; index++)
            {
                Intel8086AluResult result = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = result.Value;
                flags = result.Flags;
            }

            if (count != 0)
            {
                SetByteRegister(register, value);
                _state.Flags = flags;
            }
        }
        else
        {
            ushort value = GetWordRegister(register);
            ushort flags = _state.Flags;
            int count = Opcode == 0xD1 ? 1 : _shiftCount;
            for (int index = 0; index < count; index++)
            {
                Intel8086AluWordResult result = Intel8086Alu.ExecuteShiftRotate(value, flags, extension);
                value = result.Value;
                flags = result.Flags;
            }

            if (count != 0)
            {
                SetWordRegister(register, value);
                _state.Flags = flags;
            }
        }
    }

    private static Intel8086BusCycle FetchDataCycle() => new(0, 0, "CS", "R--", "---", 0, 37008, "PASV", "T3", '-', 0);
}
