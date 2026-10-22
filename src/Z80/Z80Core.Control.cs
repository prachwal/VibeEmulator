namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private bool _branchTaken;
    private ushort _branchTarget;

    private static bool IsConditionalJump(byte opcode) => (opcode & 0xC7) == 0xC2;
    private static bool IsConditionalCall(byte opcode) => (opcode & 0xC7) == 0xC4;
    private static bool IsConditionalReturn(byte opcode) => (opcode & 0xC7) == 0xC0;
    private static bool IsRestart(byte opcode) => (opcode & 0xC7) == 0xC7;
    private static bool IsRelativeJump(byte opcode) => opcode == 0x18 || (opcode & 0xE7) == 0x20;

    private bool CheckCondition(int code) => code switch
    {
        0 => (_state.F & 0x40) == 0,
        1 => (_state.F & 0x40) != 0,
        2 => (_state.F & 1) == 0,
        3 => (_state.F & 1) != 0,
        4 => (_state.F & 4) == 0,
        5 => (_state.F & 4) != 0,
        6 => (_state.F & 0x80) == 0,
        _ => (_state.F & 0x80) != 0,
    };

    private int GetControlTStates(byte opcode)
    {
        if (opcode == 0xC3 || IsConditionalJump(opcode))
        {
            return 10;
        }

        if (opcode == 0xCD || IsConditionalCall(opcode))
        {
            return opcode == 0xCD || CheckCondition((opcode >> 3) & 7) ? 17 : 10;
        }

        if (IsConditionalReturn(opcode))
        {
            return CheckCondition((opcode >> 3) & 7) ? 11 : 5;
        }

        if (IsRestart(opcode))
        {
            return 11;
        }

        if (IsRelativeJump(opcode))
        {
            return opcode == 0x18 || CheckCondition((opcode >> 3) & 3) ? 12 : 7;
        }

        return opcode switch
        {
            0xC9 => 10,
            0x10 => _state.B == 1 ? 8 : 13,
            0xE9 or 0x76 or 0xF3 or 0xFB => 4,
            _ => 0,
        };
    }

    private bool PrepareControlCycle()
    {
        if (_opcode == 0xC3 || _opcode == 0xCD || IsConditionalJump(_opcode) || IsConditionalCall(_opcode))
        {
            _branchTaken = _opcode is 0xC3 or 0xCD || CheckCondition((_opcode >> 3) & 7);
            StartRead(_state.PC, ReadPurpose.ControlLow);
        }
        else if (IsRelativeJump(_opcode))
        {
            _branchTaken = _opcode == 0x18 || CheckCondition((_opcode >> 3) & 3);
            StartRead(_state.PC, ReadPurpose.RelativeOffset);
        }
        else if (_opcode == 0x10)
        {
            _state.B--;
            _branchTaken = _state.B != 0;
            StartInternal(_refreshAddress, 1, InternalPurpose.RelativeRead);
        }
        else if (_opcode == 0xC9)
        {
            StartRead(_state.SP, ReadPurpose.ReturnLow);
        }
        else if (IsConditionalReturn(_opcode))
        {
            bool taken = CheckCondition((_opcode >> 3) & 7);
            StartInternal(_refreshAddress, 1, taken ? InternalPurpose.ReturnRead : InternalPurpose.Finish);
        }
        else if (IsRestart(_opcode))
        {
            _branchTarget = (ushort)(_opcode & 0x38);
            _wordValue = _state.PC;
            StartInternal(_refreshAddress, 1, InternalPurpose.CallPush);
        }
        else
        {
            return false;
        }

        return true;
    }

    private void CompleteControlInstruction()
    {
        switch (_opcode)
        {
            case 0xE9:
                _state.PC = HlAddress();
                break;
            case 0x76:
                _halted = true;
                break;
            case 0xF3:
                _state.IFF1 = false;
                _state.IFF2 = false;
                _state.EI = 0;
                break;
            case 0xFB:
                _state.IFF1 = true;
                _state.IFF2 = true;
                // CompleteInstruction zmniejsza licznik: po EI zostaje 1, po następnej instrukcji 0.
                _state.EI = 2;
                break;
        }
    }
}
