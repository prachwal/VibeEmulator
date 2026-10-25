namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool IsPrefixedAluRegisterOrAccumulator() =>
        _segmentOverride != Intel8086SegmentOverride.None &&
        _instructionPrefixLength == 1 &&
        IsSegmentPrefixedAluOpcode(Opcode) &&
        !IsSegmentPrefixedAluMemoryForm(Opcode, _memoryModRm, _aluMemoryModRm);

    private void TickPrefixedAluRegisterOrAccumulator()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', _instructionPrefix);
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
            LastBusCycle = GetPrefixedAluFetchCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 4)
        {
            if (HasAluModRmOperand())
            {
                PopQueue();
                _state.IP++;
                ExecutePrefixedAluRegisterOperand();
                LastBusCycle = GetPrefixedAluFetchCycle('S', _memoryModRm);
            }
            else
            {
                LastBusCycle = GetPrefixedAluFetchCycle('-', 0);
            }

            FinishPrefixedAluRegisterInstruction();
            return;
        }

        bool wordImmediate = IsAccumulatorImmediateWordAluOpcode(Opcode);
        bool consumesImmediate = IsAccumulatorImmediateAluOpcode(Opcode) && _instructionTState == 5 ||
            wordImmediate && _instructionTState is 5 or 6;
        byte immediate = 0;
        if (consumesImmediate)
        {
            immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            if (wordImmediate)
            {
                if (_instructionTState == 5)
                {
                    _immediateWord = immediate;
                }
                else
                {
                    _immediateWord |= (ushort)(immediate << 8);
                    ExecutePrefixedAccumulatorWordAlu();
                }
            }
            else
            {
                ExecutePrefixedAccumulatorByteAlu(immediate);
            }
        }

        LastBusCycle = GetPrefixedAluFetchCycle(consumesImmediate ? 'S' : '-', consumesImmediate ? immediate : (byte)0);
        if (HasAluModRmOperand() && _instructionTState == 5 && _state.QueueCount != 0)
        {
            PopQueue();
        }
        else if (IsAccumulatorAluImmediateOpcode(Opcode) &&
            _instructionTState == _instructionCycles && _state.QueueCount != 0)
        {
            PopQueue();
        }

        FinishPrefixedAluRegisterInstruction();
    }

    private bool HasAluModRmOperand() => IsRegisterDirectAluOpcode(Opcode) || IsWordAluModRmOpcode(Opcode);

    private static bool IsAccumulatorAluImmediateOpcode(byte opcode) =>
        IsAccumulatorImmediateAluOpcode(opcode) || IsAccumulatorImmediateWordAluOpcode(opcode);

    private Intel8086BusCycle GetPrefixedAluFetchCycle(char queueStatus, byte queueData) =>
        TryTickMemoryPrefetch(out Intel8086BusCycle cycle)
            ? cycle with { QueueStatus = queueStatus, QueueData = queueData }
            : IdleCycle(queueStatus, queueData);

    private void ExecutePrefixedAluRegisterOperand()
    {
        int modRm = _memoryModRm;
        int reg = (modRm >> 3) & 7;
        int rm = modRm & 7;
        if (IsRegisterDirectAluOpcode(Opcode))
        {
            bool directionToReg = Opcode != 0x84 && (Opcode & 0x02) != 0;
            int destination = directionToReg ? reg : rm;
            Intel8086AluOperation operation = GetRegisterDirectAluOperation(Opcode);
            Intel8086AluResult result = Intel8086Alu.Execute(
                GetByteRegister(destination),
                GetByteRegister(directionToReg ? rm : reg),
                _state.Flags,
                operation);
            if (operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
            {
                SetByteRegister(destination, result.Value);
            }

            _state.Flags = result.Flags;
            return;
        }

        bool wordDirectionToReg = Opcode != 0x85 && (Opcode & 0x02) != 0;
        int wordDestination = wordDirectionToReg ? reg : rm;
        Intel8086AluOperation wordOperation = GetRegisterDirectWordAluOperation(Opcode);
        Intel8086AluWordResult wordResult = Intel8086Alu.Execute(
            GetWordRegister(wordDestination),
            GetWordRegister(wordDirectionToReg ? rm : reg),
            _state.Flags,
            wordOperation);
        if (wordOperation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
        {
            SetWordRegister(wordDestination, wordResult.Value);
        }

        _state.Flags = wordResult.Flags;
    }

    private void ExecutePrefixedAccumulatorByteAlu(byte immediate)
    {
        Intel8086AluResult result = Intel8086Alu.Execute(
            (byte)_state.AX,
            immediate,
            _state.Flags,
            GetAccumulatorImmediateOperation(Opcode));
        if (IsAccumulatorResultWritten(Opcode))
        {
            SetByteRegister(0, result.Value);
        }

        _state.Flags = result.Flags;
    }

    private void ExecutePrefixedAccumulatorWordAlu()
    {
        Intel8086AluOperation operation = GetAccumulatorImmediateWordOperation(Opcode);
        Intel8086AluWordResult result = Intel8086Alu.Execute(_state.AX, _immediateWord, _state.Flags, operation);
        if (operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
        {
            _state.AX = result.Value;
        }

        _state.Flags = result.Flags;
    }

    private void FinishPrefixedAluRegisterInstruction()
    {
        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }
}
