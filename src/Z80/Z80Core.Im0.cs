namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void CompleteIm0Acknowledge()
    {
        _im0AcknowledgeCount++;
        if (_im0IndexCbAwaitingOpcode)
        {
            CompleteIm0IndexCbOpcode();
            return;
        }

        byte data = _interruptData;
        if (_im0PrefixOpcode == 0)
        {
            if (data is 0xCB or 0xDD or 0xED or 0xFD)
            {
                _im0PrefixOpcode = data;
                StartIm0Acknowledge();
                return;
            }

            BeginIm0Instruction(data);
            return;
        }

        if (_im0PrefixOpcode is 0xCB or 0xED)
        {
            BeginIm0PrefixedInstruction(data);
            return;
        }

        if (data is 0xDD or 0xFD)
        {
            _im0PrefixOpcode = data;
            StartIm0Acknowledge();
            return;
        }

        _indexIsIy = _im0PrefixOpcode == 0xFD;
        _indexPrefixActive = true;
        if (data == 0xCB)
        {
            _opcode = 0xCB;
            _indexCbPrefixActive = true;
            _im0IndexCbNeedsAcknowledge = true;
            BeginIm0InstructionBody(33);
            PrepareIndexInstruction();
            return;
        }

        if (data == 0xED)
        {
            _im0PrefixOpcode = data;
            _indexPrefixActive = false;
            StartIm0Acknowledge();
            return;
        }

        BeginIm0IndexedInstruction(data);
    }

    private void BeginIm0Instruction(byte opcode)
    {
        _opcode = opcode;
        _cbOpcode = 0;
        _edOpcode = 0;
        _cbPrefixActive = false;
        _edPrefixActive = false;
        _indexPrefixActive = false;
        _indexCbPrefixActive = false;
        _indexIsIy = false;
        if (IsRestart(opcode))
        {
            BeginIm0InstructionBody(13);
            _branchTarget = (ushort)(opcode & 0x38);
            _wordValue = _state.PC;
            StartInternal(_refreshAddress, 1, InternalPurpose.CallPush);
            return;
        }

        BeginIm0InstructionBody(GetInstructionTStates(opcode) + 2);
        PrepareFollowingCycle();
    }

    private void BeginIm0PrefixedInstruction(byte opcode)
    {
        _opcode = _im0PrefixOpcode;
        _cbOpcode = opcode;
        _edOpcode = opcode;
        _cbPrefixActive = _im0PrefixOpcode == 0xCB;
        _edPrefixActive = _im0PrefixOpcode == 0xED;
        _indexPrefixActive = false;
        _indexCbPrefixActive = false;
        BeginIm0InstructionBody((_cbPrefixActive ? GetCbInstructionTStates(opcode) : GetEdInstructionTStates(opcode)) + 4);
        if (_cbPrefixActive)
        {
            PrepareCbCycle();
        }
        else
        {
            PrepareEdCycle();
        }
    }

    private void BeginIm0IndexedInstruction(byte opcode)
    {
        _opcode = opcode;
        _cbOpcode = 0;
        _edOpcode = 0;
        _cbPrefixActive = false;
        _edPrefixActive = false;
        _indexCbPrefixActive = false;
        BeginIm0InstructionBody(GetInstructionTStates(opcode) + (_im0AcknowledgeCount * 2));
        PrepareIndexInstruction();
    }

    private void BeginIm0InstructionBody(int instructionTStates)
    {
        _interruptActive = false;
        _interruptKind = InterruptKind.None;
        _instructionActive = true;
        _im0InstructionActive = true;
        _instructionTStates = instructionTStates;
        _refreshAddress = (ushort)((_state.I << 8) | _state.R);
        _elapsedTStates = _im0TotalTStates;
        if (_elapsedTStates >= _instructionTStates)
        {
            CompleteInstruction();
        }
    }

    private void StartIm0Acknowledge()
    {
        _interruptKind = InterruptKind.Im0;
        _interruptActive = true;
        _interruptElapsed = 0;
        _interruptAcknowledgeTStates = 6;
        _interruptDataTState = 3;
        _interruptReturnAddress = _state.PC;
    }

    private void CompleteIm0IndexCbOpcode()
    {
        _cbOpcode = _interruptData;
        _opcode = 0xCB;
        _im0IndexCbAwaitingOpcode = false;
        int normalTStates = (_cbOpcode >> 6) == 1 ? 20 : 23;
        BeginIm0InstructionBody(normalTStates + 6);
        StartInternal(_state.PC, 2, InternalPurpose.IndexedCbStart);
    }

    private void StartIm0IndexCbOpcodeAcknowledge()
    {
        _im0IndexCbNeedsAcknowledge = false;
        _im0IndexCbAwaitingOpcode = true;
        StartIm0Acknowledge();
    }

}
