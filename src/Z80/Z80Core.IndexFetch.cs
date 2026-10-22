namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void TickIndexOpcodeFetch(int tState)
    {
        _machineCycle = MachineCycle.OpcodeFetch;
        switch (tState)
        {
            case 0:
                LastAddress = _state.PC;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _opcode = _bus.Read(_state.PC);
                _state.PC++;
                _instructionTStates = GetInstructionTStates(_opcode) + 4;
                if (_opcode == 0xCB)
                {
                    _instructionTStates = 23;
                    _indexCbPrefixActive = true;
                }
                else if (UsesIndexedMemory(_opcode))
                {
                    _instructionTStates += _opcode == 0x36 ? 5 : 8;
                }

                LastAddress = (ushort)(_state.PC - 1);
                LastData = null;
                LastPins = "r-m-";
                break;
            case 2:
                LastAddress = _refreshAddress;
                LastData = _opcode;
                LastPins = "----";
                break;
            default:
                LastAddress = _refreshAddress;
                LastData = null;
                LastPins = "----";
                _state.R = (byte)((_state.R + 1) & 0x7F);
                PrepareIndexInstruction();
                break;
        }
    }

    private bool UsesIndexedMemory(byte opcode) =>
        opcode is 0x34 or 0x35 or 0x36
        || (opcode is >= 0x40 and <= 0x7F && opcode != 0x76 && ((opcode & 7) == 6 || ((opcode >> 3) & 7) == 6))
        || (opcode is >= 0x80 and <= 0xBF && (opcode & 7) == 6);

    private void PrepareIndexInstruction()
    {
        if (_opcode == 0xCB)
        {
            StartRead(_state.PC, ReadPurpose.IndexDisplacement);
            return;
        }

        if (UsesIndexedMemory(_opcode))
        {
            StartRead(_state.PC, ReadPurpose.IndexDisplacement);
            return;
        }

        PrepareFollowingCycle();
    }

    private ushort IndexAddress => _indexIsIy ? _state.IY : _state.IX;

    private byte IndexHigh => (byte)(IndexAddress >> 8);

    private byte IndexLow => (byte)IndexAddress;

    private void SetIndexHigh(byte value) => SetSelectedIndex((ushort)((value << 8) | IndexLow));

    private void SetIndexLow(byte value) => SetSelectedIndex((ushort)((IndexHigh << 8) | value));

    private void SetSelectedIndex(ushort value)
    {
        if (_indexIsIy) _state.IY = value; else _state.IX = value;
    }

    private bool IndexSubstitutionActive => _indexPrefixActive &&
        (_opcode is 0x21 or 0x22 or 0x2A or 0x23 or 0x2B or 0x09 or 0x19 or 0x29 or 0x39 or 0xF9 or 0xE3 or 0xE9 or 0xE1 or 0xE5
            || IsIndexByteOperation(_opcode));

    private static bool IsIndexByteOperation(byte opcode) =>
        (opcode is >= 0x40 and <= 0x7F && opcode != 0x76 && (opcode & 7) != 6
            && (((opcode & 0x38) is 0x20 or 0x28) || ((opcode & 7) is 4 or 5)))
        || (opcode is >= 0x80 and <= 0xBF && (opcode & 7) is 4 or 5)
        || opcode is 0x24 or 0x25 or 0x26 or 0x2C or 0x2D or 0x2E;

    private ushort GetHlOrIndex() => IndexSubstitutionActive ? IndexAddress : (ushort)((_state.H << 8) | _state.L);

    private ushort _indexedEffectiveAddress;

    private void SetHlOrIndex(ushort value)
    {
        if (IndexSubstitutionActive)
        {
            if (_indexIsIy)
            {
                _state.IY = value;
            }
            else
            {
                _state.IX = value;
            }

            return;
        }

        _state.H = (byte)(value >> 8);
        _state.L = (byte)value;
    }

    private void CompleteIndexDisplacement(byte displacement)
    {
        _indexDisplacementAddress = _state.PC;
        _state.PC++;
        _indexedEffectiveAddress = (ushort)(IndexAddress + (sbyte)displacement);
        _cycleAddress = _indexedEffectiveAddress;
        _state.WZ = _indexedEffectiveAddress;
        if (_indexCbPrefixActive)
        {
            StartRead(_state.PC, ReadPurpose.IndexCbOpcode);
            return;
        }

        StartInternal(_opcode == 0x36 ? _state.PC : _indexDisplacementAddress, _opcode == 0x36 ? 1 : 5, InternalPurpose.IndexedMemory);
    }

    private void CompleteIndexCbOpcode(byte opcode)
    {
        _indexCbOpcodeAddress = _state.PC;
        _state.PC++;
        _cbOpcode = opcode;
        _instructionTStates = (opcode >> 6) == 1 ? 20 : 23;
        StartInternal(_indexCbOpcodeAddress, 2, InternalPurpose.IndexedCbStart);
    }

    private void PrepareIndexedMemoryAccess()
    {
        _cycleAddress = _indexedEffectiveAddress;
        if (_opcode == 0x34 || _opcode == 0x35)
        {
            StartRead(_cycleAddress, ReadPurpose.IncrementHl);
        }
        else if (_opcode == 0x36)
        {
            _machineCycle = MachineCycle.MemoryRead;
            _cycleAddress = _state.PC;
            _readPurpose = ReadPurpose.Immediate;
            _memoryCycleStart = _elapsedTStates;
            _indexedImmediateRead = true;
        }
        else if ((_opcode & 7) == 6 && _opcode is >= 0x40 and <= 0x7F)
        {
            _registerCode = (byte)((_opcode >> 3) & 7);
            StartRead(_cycleAddress, ReadPurpose.RegisterFromHl);
        }
        else if ((_opcode & 7) == 6 && _opcode is >= 0x80 and <= 0xBF)
        {
            StartRead(_cycleAddress, ReadPurpose.AluOperand);
        }
        else
        {
            _cycleWriteValue = GetRegister((byte)(_opcode & 7), indexMemoryPeer: true);
            StartWrite(_cycleAddress, _cycleWriteValue, WritePurpose.RegisterToHl);
        }
    }
}
