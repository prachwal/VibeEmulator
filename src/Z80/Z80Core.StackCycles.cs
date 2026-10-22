namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void CompleteStackRead(byte value)
    {
        switch (_readPurpose)
        {
            case ReadPurpose.PopLow:
                _wordReadValue = value;
                _state.SP++;
                StartRead(_state.SP, ReadPurpose.PopHigh);
                break;
            case ReadPurpose.PopHigh:
                _state.SP++;
                SetPair((_opcode >> 4) & 3, (ushort)(_wordReadValue | (value << 8)), af: true);
                break;
            case ReadPurpose.ExchangeLow:
                _wordReadValue = value;
                StartRead((ushort)(_state.SP + 1), ReadPurpose.ExchangeHigh);
                break;
            case ReadPurpose.ExchangeHigh:
                _wordReadValue |= (ushort)(value << 8);
                StartInternal(_cycleAddress, 1, InternalPurpose.ExchangeWrite);
                break;
        }
    }

    private void CompleteStackWrite()
    {
        switch (_writePurpose)
        {
            case WritePurpose.PushHigh:
                _state.SP--;
                StartWrite(_state.SP, (byte)_wordValue, WritePurpose.PushLow);
                break;
            case WritePurpose.ExchangeHigh:
                StartWrite(_state.SP, (byte)_wordValue, WritePurpose.ExchangeLow);
                break;
            case WritePurpose.ExchangeLow:
                SetPair(2, _wordReadValue);
                _state.WZ = _wordReadValue;
                StartInternal(_state.SP, 2, InternalPurpose.Finish);
                break;
        }
    }
}
