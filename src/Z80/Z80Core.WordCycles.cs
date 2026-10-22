namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private ushort _wordValue;
    private ushort _wordReadValue;

    private bool PrepareWordCycle()
    {
        if (IsLoadPair(_opcode))
        {
            StartRead(_state.PC, ReadPurpose.PairImmediateLow);
        }
        else if (_opcode is 0x22 or 0x2A)
        {
            StartRead(_state.PC, ReadPurpose.PairAddressLow);
        }
        else if (IsChangePair(_opcode) || _opcode == 0xF9)
        {
            StartInternal(_refreshAddress, 2, InternalPurpose.Finish);
        }
        else if (IsAddPair(_opcode))
        {
            StartInternal(_refreshAddress, 7, InternalPurpose.Finish);
        }
        else if (IsPush(_opcode))
        {
            _wordValue = GetPair((_opcode >> 4) & 3, af: true);
            StartInternal(_refreshAddress, 1, InternalPurpose.Push);
        }
        else if (IsPop(_opcode))
        {
            StartRead(_state.SP, ReadPurpose.PopLow);
        }
        else if (_opcode == 0xE3)
        {
            _wordValue = HlAddress();
            StartRead(_state.SP, ReadPurpose.ExchangeLow);
        }
        else
        {
            return false;
        }

        return true;
    }

    private void CompleteWordRead(byte value)
    {
        switch (_readPurpose)
        {
            case ReadPurpose.PairImmediateLow:
                _wordReadValue = value;
                _state.PC++;
                StartRead(_state.PC, ReadPurpose.PairImmediateHigh);
                break;
            case ReadPurpose.PairImmediateHigh:
                _state.PC++;
                SetPair((_opcode >> 4) & 3, (ushort)(_wordReadValue | (value << 8)));
                break;
            case ReadPurpose.PairAddressLow:
                _absoluteLow = value;
                _state.PC++;
                StartRead(_state.PC, ReadPurpose.PairAddressHigh);
                break;
            case ReadPurpose.PairAddressHigh:
                _absoluteAddress = (ushort)(_absoluteLow | (value << 8));
                _state.PC++;
                if (_opcode == 0x2A)
                {
                    StartRead(_absoluteAddress, ReadPurpose.PairMemoryLow);
                }
                else
                {
                    _wordValue = HlAddress();
                    StartWrite(_absoluteAddress, (byte)_wordValue, WritePurpose.PairMemoryLow);
                }

                break;
            case ReadPurpose.PairMemoryLow:
                _wordReadValue = value;
                StartRead((ushort)(_absoluteAddress + 1), ReadPurpose.PairMemoryHigh);
                break;
            case ReadPurpose.PairMemoryHigh:
                SetPair(2, (ushort)(_wordReadValue | (value << 8)));
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            default:
                CompleteStackRead(value);
                break;
        }
    }

    private void CompleteWordWrite()
    {
        switch (_writePurpose)
        {
            case WritePurpose.PairMemoryLow:
                StartWrite((ushort)(_absoluteAddress + 1), (byte)(_wordValue >> 8), WritePurpose.PairMemoryHigh);
                break;
            case WritePurpose.PairMemoryHigh:
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            default:
                CompleteStackWrite();
                break;
        }
    }
}
