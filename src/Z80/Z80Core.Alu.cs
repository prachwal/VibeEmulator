namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void ApplyAlu(byte operand)
    {
        (_state.A, _state.F) = Z80Alu.Execute((_opcode >> 3) & 7, _state.A, operand, _state.F);
    }

    private void CompleteAluInstruction()
    {
        if (_opcode is >= 0x80 and <= 0xBF && (_opcode & 7) != 6)
        {
            ApplyAlu(GetRegister((byte)(_opcode & 7)));
        }
        else if (IsIncrement(_opcode) && ((_opcode >> 3) & 7) != 6)
        {
            byte code = (byte)((_opcode >> 3) & 7);
            (byte value, byte flags) = Z80Alu.Increment(GetRegister(code), _state.F, (_opcode & 1) != 0);
            SetRegister(code, value);
            _state.F = flags;
        }
        else
        {
            switch (_opcode)
            {
                case 0x27:
                    (_state.A, _state.F) = Z80Alu.DecimalAdjust(_state.A, _state.F);
                    break;
                case 0x2F:
                    (_state.A, _state.F) = Z80Alu.Complement(_state.A, _state.F);
                    break;
                case 0x37:
                case 0x3F:
                    // Wektory DD/FD pokazują, że przed SCF/CCF z prefiksem indeksowym Q jest zerowane.
                    byte previousQ = _indexPrefixActive ? (byte)0 : _state.Q;
                    _state.F = Z80Alu.CarryFlags(_state.A, _state.F, previousQ, _opcode == 0x3F);
                    break;
            }
        }
    }
}
