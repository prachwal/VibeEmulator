namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void CompleteControlRead(byte value)
    {
        switch (_readPurpose)
        {
            case ReadPurpose.ControlLow:
                _absoluteLow = value;
                _state.PC++;
                StartRead(_state.PC, ReadPurpose.ControlHigh);
                break;
            case ReadPurpose.ControlHigh:
                _branchTarget = (ushort)(_absoluteLow | (value << 8));
                _state.PC++;
                _state.WZ = _branchTarget;
                if (_branchTaken)
                {
                    if (_opcode == 0xCD || IsConditionalCall(_opcode))
                    {
                        _wordValue = _state.PC;
                        StartInternal(_cycleAddress, 1, InternalPurpose.CallPush);
                    }
                    else
                    {
                        _state.PC = _branchTarget;
                    }
                }

                break;
            case ReadPurpose.RelativeOffset:
                _state.PC++;
                if (_branchTaken)
                {
                    _branchTarget = (ushort)(_state.PC + (sbyte)value);
                    StartInternal(_cycleAddress, 5, InternalPurpose.RelativeJump);
                }

                break;
            case ReadPurpose.ReturnLow:
                _wordReadValue = value;
                _state.SP++;
                StartRead(_state.SP, ReadPurpose.ReturnHigh);
                break;
            case ReadPurpose.ReturnHigh:
                _state.SP++;
                _state.PC = (ushort)(_wordReadValue | (value << 8));
                _state.WZ = _state.PC;
                break;
        }
    }
}
