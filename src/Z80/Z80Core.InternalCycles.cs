namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private enum InternalPurpose : byte
    {
        Finish,
        IncrementWrite,
        Push,
        ExchangeWrite,
        RelativeRead,
        RelativeJump,
        ReturnRead,
        CallPush,
        CbWrite,
        EdNibbleWrite,
        EdBlockStart,
        IndexedMemory,
        IndexedImmediateWrite,
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
            case InternalPurpose.IndexedMemory:
                PrepareIndexedMemoryAccess();
                break;
            case InternalPurpose.IndexedImmediateWrite:
                _cycleAddress = _indexedEffectiveAddress;
                StartWrite(_cycleAddress, _cycleWriteValue, WritePurpose.ImmediateToHl);
                break;
            case InternalPurpose.RelativeRead:
                StartRead(_state.PC, ReadPurpose.RelativeOffset);
                break;
            case InternalPurpose.RelativeJump:
                _state.PC = _branchTarget;
                _state.WZ = _branchTarget;
                break;
            case InternalPurpose.ReturnRead:
                StartRead(_state.SP, ReadPurpose.ReturnLow);
                break;
            case InternalPurpose.CallPush:
                _state.SP--;
                StartWrite(_state.SP, (byte)(_wordValue >> 8), WritePurpose.CallHigh);
                break;
            case InternalPurpose.CbWrite:
                StartWrite(_cycleAddress, _cycleWriteValue, WritePurpose.CbModify);
                break;
            case InternalPurpose.EdNibbleWrite:
                StartWrite(_cycleAddress, _cycleWriteValue, WritePurpose.EdNibble);
                break;
            case InternalPurpose.EdBlockStart:
                if (IsEdBlockInput(_edOpcode))
                {
                    StartPortRead(PairBC());
                }
                else
                {
                    StartRead(HlAddress(), ReadPurpose.EdBlockRead);
                }

                break;
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
