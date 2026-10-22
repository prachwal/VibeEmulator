namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private int GetInstructionTStates(byte opcode)
    {
        int controlTStates = GetControlTStates(opcode);
        if (controlTStates != 0)
        {
            return controlTStates;
        }

        if (opcode is 0x07 or 0x0F or 0x17 or 0x1F)
        {
            return 4;
        }

        if (IsLoadPair(opcode) || IsPop(opcode))
        {
            return 10;
        }

        if (IsChangePair(opcode) || opcode == 0xF9)
        {
            return 6;
        }

        if (IsAddPair(opcode) || IsPush(opcode))
        {
            return 11;
        }

        if (opcode is 0x22 or 0x2A)
        {
            return 16;
        }

        if (opcode == 0xE3)
        {
            return 19;
        }

        if (opcode is >= 0x80 and <= 0xBF)
        {
            return (opcode & 7) == 6 ? 7 : 4;
        }

        if (IsAluImmediate(opcode))
        {
            return 7;
        }

        if (IsIncrement(opcode))
        {
            return ((opcode >> 3) & 7) == 6 ? 11 : 4;
        }

        if (opcode == 0x00)
        {
            return 4;
        }

        if (opcode is 0xDB or 0xD3)
        {
            return 11;
        }

        if (opcode is >= 0x40 and <= 0x7F && opcode != 0x76)
        {
            return IsHlRegisterTransfer(opcode) ? 7 : 4;
        }

        if (IsLoadImmediate(opcode))
        {
            return opcode == 0x36 ? 10 : 7;
        }

        return opcode switch
        {
            0x0A or 0x1A or 0x02 or 0x12 => 7,
            0x3A or 0x32 => 13,
            _ => 4,
        };
    }

    private void PrepareFollowingCycle()
    {
        if (PrepareControlCycle() || PrepareWordCycle())
        {
            return;
        }

        if (_opcode is >= 0x80 and <= 0xBF && (_opcode & 7) == 6)
        {
            StartRead(HlAddress(), ReadPurpose.AluOperand);
            return;
        }

        if (IsAluImmediate(_opcode))
        {
            StartRead(_state.PC, ReadPurpose.AluImmediate);
            return;
        }

        if (IsIncrement(_opcode) && ((_opcode >> 3) & 7) == 6)
        {
            StartRead(HlAddress(), ReadPurpose.IncrementHl);
            return;
        }

        if (_opcode == 0x00 || (_opcode is >= 0x40 and <= 0x7F && _opcode != 0x76 && !IsHlRegisterTransfer(_opcode)))
        {
            return;
        }

        if (_opcode is >= 0x40 and <= 0x7F && _opcode != 0x76)
        {
            if ((_opcode & 0x07) == 0x06)
            {
                _registerCode = (byte)((_opcode >> 3) & 0x07);
                StartRead(HlAddress(), ReadPurpose.RegisterFromHl);
            }
            else
            {
                _cycleWriteValue = GetRegister((byte)(_opcode & 0x07));
                StartWrite(HlAddress(), _cycleWriteValue, WritePurpose.RegisterToHl);
            }

            return;
        }

        if (IsLoadImmediate(_opcode))
        {
            _registerCode = (byte)((_opcode >> 3) & 0x07);
            StartRead(_state.PC, ReadPurpose.Immediate);
            return;
        }

        switch (_opcode)
        {
            case 0xDB:
            case 0xD3:
                StartRead(_state.PC, ReadPurpose.Immediate);
                break;
            case 0x0A:
            case 0x1A:
                _pairAddress = _opcode == 0x0A ? PairBC() : PairDE();
                StartRead(_pairAddress, ReadPurpose.AccumulatorFromPair);
                break;
            case 0x02:
            case 0x12:
                _pairAddress = _opcode == 0x02 ? PairBC() : PairDE();
                StartWrite(_pairAddress, _state.A, WritePurpose.AccumulatorToPair);
                break;
            case 0x3A:
            case 0x32:
                StartRead(_state.PC, ReadPurpose.AbsoluteLow);
                break;
        }
    }

    private static bool IsAluImmediate(byte opcode) => (opcode & 0xC7) == 0xC6;

    private static bool IsIncrement(byte opcode) => (opcode & 0xC6) == 0x04;

    private bool ChangesFlags(byte opcode) =>
        _edPrefixActive
            ? EdChangesFlags(_edOpcode)
            : _cbPrefixActive
            ? CbChangesFlags(_cbOpcode)
            : opcode is >= 0x80 and <= 0xBF or 0x27 or 0x2F or 0x37 or 0x3F
                or 0x07 or 0x0F or 0x17 or 0x1F
                || IsAluImmediate(opcode) || IsIncrement(opcode) || IsAddPair(opcode);

    private static bool IsHlRegisterTransfer(byte opcode) =>
        (opcode & 0x07) == 0x06 || ((opcode >> 3) & 0x07) == 0x06;

    private static bool IsLoadImmediate(byte opcode) =>
        opcode is 0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E;
}
