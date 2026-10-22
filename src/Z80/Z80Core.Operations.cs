namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void CompleteInstruction()
    {
        if (_opcode is >= 0x40 and <= 0x7F && _opcode != 0x76 && !IsHlRegisterTransfer(_opcode))
        {
            byte value = GetRegister((byte)(_opcode & 0x07));
            SetRegister((byte)((_opcode >> 3) & 0x07), value);
        }

        CompleteAluInstruction();
        CompleteWordInstruction();
        CompleteControlInstruction();
        CompleteCbInstruction();
        CompleteEdInstruction();

        _ldAiRiCompleted = _edPrefixActive && (_edOpcode is 0x57 or 0x5F);
        _state.P = _edPrefixActive && (_edOpcode is 0x57 or 0x5F) ? (byte)1 : (byte)0;
        _state.Q = ChangesFlags(_opcode) ? _state.F : (byte)0;
        if (_state.EI != 0)
        {
            _state.EI--;
        }

        _instructionActive = false;
        _im0InstructionActive = false;
    }

    private ushort HlAddress() => GetHlOrIndex();

    private ushort PairBC() => (ushort)((_state.B << 8) | _state.C);

    private ushort PairDE() => (ushort)((_state.D << 8) | _state.E);

    private byte GetRegister(byte code, bool indexMemoryPeer = false) => code switch
    {
        0 => _state.B,
        1 => _state.C,
        2 => _state.D,
        3 => _state.E,
        4 => IndexSubstitutionActive && !indexMemoryPeer ? IndexHigh : _state.H,
        5 => IndexSubstitutionActive && !indexMemoryPeer ? IndexLow : _state.L,
        7 => _state.A,
        _ => 0,
    };

    private void SetRegister(byte code, byte value, bool indexMemoryPeer = false)
    {
        switch (code)
        {
            case 0: _state.B = value; break;
            case 1: _state.C = value; break;
            case 2: _state.D = value; break;
            case 3: _state.E = value; break;
            case 4:
                if (IndexSubstitutionActive && !indexMemoryPeer) SetIndexHigh(value); else _state.H = value;
                break;
            case 5:
                if (IndexSubstitutionActive && !indexMemoryPeer) SetIndexLow(value); else _state.L = value;
                break;
            case 7: _state.A = value; break;
        }
    }
}
