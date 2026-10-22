namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private enum InternalPurpose : byte
    {
        Finish,
        IncrementWrite,
        Push,
        ExchangeWrite,
    }

    private int _internalRemaining;
    private InternalPurpose _internalPurpose;

    private void StartInternal(ushort address, int tStates, InternalPurpose purpose)
    {
        _machineCycle = MachineCycle.Internal;
        _cycleAddress = address;
        _internalRemaining = tStates;
        _internalPurpose = purpose;
    }

    private void TickInternalCycle()
    {
        LastAddress = _cycleAddress;
        LastData = null;
        LastPins = "----";
        _internalRemaining--;
        if (_internalRemaining != 0)
        {
            return;
        }

        switch (_internalPurpose)
        {
            case InternalPurpose.IncrementWrite:
                StartWrite(_cycleAddress, _cycleWriteValue, WritePurpose.RegisterToHl);
                break;
            case InternalPurpose.Push:
                _state.SP--;
                StartWrite(_state.SP, (byte)(_wordValue >> 8), WritePurpose.PushHigh);
                break;
            case InternalPurpose.ExchangeWrite:
                StartWrite(_cycleAddress, (byte)(_wordValue >> 8), WritePurpose.ExchangeHigh);
                break;
        }
    }
}
