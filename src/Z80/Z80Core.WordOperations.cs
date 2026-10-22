namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private static bool IsLoadPair(byte opcode) => (opcode & 0xCF) == 0x01;

    private static bool IsChangePair(byte opcode) => (opcode & 0xC7) == 0x03;

    private static bool IsAddPair(byte opcode) => (opcode & 0xCF) == 0x09;

    private static bool IsPush(byte opcode) => (opcode & 0xCF) == 0xC5;

    private static bool IsPop(byte opcode) => (opcode & 0xCF) == 0xC1;

    private ushort GetPair(int code, bool af = false) => code switch
    {
        0 => PairBC(),
        1 => PairDE(),
        2 => HlAddress(),
        _ => af ? (ushort)((_state.A << 8) | _state.F) : _state.SP,
    };

    private void SetPair(int code, ushort value, bool af = false)
    {
        byte high = (byte)(value >> 8);
        byte low = (byte)value;
        switch (code)
        {
            case 0: _state.B = high; _state.C = low; break;
            case 1: _state.D = high; _state.E = low; break;
            case 2: _state.H = high; _state.L = low; break;
            case 3:
                if (af)
                {
                    _state.A = high;
                    _state.F = low;
                }
                else
                {
                    _state.SP = value;
                }

                break;
        }
    }

    private void CompleteWordInstruction()
    {
        int code = (_opcode >> 4) & 3;
        if (IsChangePair(_opcode))
        {
            SetPair(code, (ushort)(GetPair(code) + ((_opcode & 8) == 0 ? 1 : -1)));
        }
        else if (IsAddPair(_opcode))
        {
            ushort hl = HlAddress();
            (ushort value, byte flags) = Z80Alu.Add16(hl, GetPair(code), _state.F);
            SetPair(2, value);
            _state.F = flags;
            _state.WZ = (ushort)(hl + 1);
        }
        else
        {
            switch (_opcode)
            {
                case 0xF9:
                    _state.SP = HlAddress();
                    break;
                case 0xEB:
                    ushort de = PairDE();
                    SetPair(1, HlAddress());
                    SetPair(2, de);
                    break;
                case 0x08:
                    ushort af = GetPair(3, af: true);
                    SetPair(3, _state.AF_, af: true);
                    _state.AF_ = af;
                    break;
                case 0xD9:
                    ushort bc = PairBC();
                    ushort oldDe = PairDE();
                    ushort hl = HlAddress();
                    SetPair(0, _state.BC_);
                    SetPair(1, _state.DE_);
                    SetPair(2, _state.HL_);
                    _state.BC_ = bc;
                    _state.DE_ = oldDe;
                    _state.HL_ = hl;
                    break;
            }
        }
    }
}
