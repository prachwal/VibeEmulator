namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private static int GetInstructionTStates(byte opcode)
    {
        if (opcode == 0x00)
        {
            return 4;
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

    private static bool IsHlRegisterTransfer(byte opcode) =>
        (opcode & 0x07) == 0x06 || ((opcode >> 3) & 0x07) == 0x06;

    private static bool IsLoadImmediate(byte opcode) =>
        opcode is 0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E;
}
