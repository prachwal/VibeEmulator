namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private static bool EdChangesFlags(byte opcode) =>
        (opcode & 0xC7) == 0x40 || (opcode & 0xCF) is 0x42 or 0x4A
        || (opcode & 0xC7) == 0x44 || opcode is 0x57 or 0x5F or 0x67 or 0x6F
        || IsEdBlockInstruction(opcode);

    private void CompleteEdInstruction()
    {
        byte opcode = _edOpcode;
        if ((opcode & 0xCF) is 0x42 or 0x4A)
        {
            CompleteEdAdd16((opcode & 8) == 0);
        }
        else if ((opcode & 0xC7) == 0x44)
        {
            CompleteEdNegate();
        }
        else if ((opcode & 0xC7) == 0x45)
        {
            _state.IFF1 = _state.IFF2;
        }
        else if (opcode is 0x46 or 0x4E or 0x66 or 0x6E)
        {
            _state.IM = 0;
        }
        else if (opcode is 0x56 or 0x76)
        {
            _state.IM = 1;
        }
        else if (opcode is 0x5E or 0x7E)
        {
            _state.IM = 2;
        }
        else if (opcode == 0x47)
        {
            _state.I = _state.A;
        }
        else if (opcode == 0x4F)
        {
            _state.R = _state.A;
        }
        else if (opcode is 0x57 or 0x5F)
        {
            _state.A = opcode == 0x57 ? _state.I : _state.R;
            _state.F = (byte)((_state.F & 1) | (_state.A & 0xA8) | (_state.A == 0 ? 0x40 : 0)
                | (_state.IFF2 ? 4 : 0));
        }
    }

    private void CompleteEdAdd16(bool subtract)
    {
        ushort left = HlAddress();
        ushort right = GetPair((_edOpcode >> 4) & 3);
        int carry = _state.F & 1;
        int result = subtract ? left - right - carry : left + right + carry;
        ushort value = (ushort)result;
        int flags = (value >> 8) & 0xA8;
        if (value == 0)
        {
            flags |= 0x40;
        }

        flags |= ((left ^ right ^ value) >> 8) & 0x10;
        if (subtract ? ((left ^ right) & (left ^ value) & 0x8000) != 0 : (~(left ^ right) & (left ^ value) & 0x8000) != 0)
        {
            flags |= 4;
        }

        if ((subtract && result < 0) || (!subtract && result > 0xFFFF))
        {
            flags |= 1;
        }

        if (subtract)
        {
            flags |= 2;
        }

        SetPair(2, value);
        _state.F = (byte)flags;
        _state.WZ = (ushort)(left + 1);
    }

    private void CompleteEdNegate()
    {
        byte old = _state.A;
        byte value = (byte)-old;
        int flags = (value & 0xA8) | (value == 0 ? 0x40 : 0) | 2;
        if ((old & 0x0F) != 0)
        {
            flags |= 0x10;
        }

        if (old == 0x80)
        {
            flags |= 4;
        }

        if (old != 0)
        {
            flags |= 1;
        }
        _state.A = value;
        _state.F = (byte)flags;
    }

    private void CompleteEdMemoryRead(byte value)
    {
        switch (_readPurpose)
        {
            case ReadPurpose.EdAbsoluteLow:
                _absoluteLow = value;
                _state.PC++;
                StartRead(_state.PC, ReadPurpose.EdAbsoluteHigh);
                break;
            case ReadPurpose.EdAbsoluteHigh:
                _absoluteAddress = (ushort)(_absoluteLow | (value << 8));
                _state.PC++;
                int pair = (_edOpcode >> 4) & 3;
                if ((_edOpcode & 0x0F) == 3)
                {
                    ushort pairValue = GetPair(pair);
                    StartWrite(_absoluteAddress, (byte)pairValue, WritePurpose.EdAbsoluteLow);
                }
                else
                {
                    StartRead(_absoluteAddress, ReadPurpose.EdPairLow);
                }

                break;
            case ReadPurpose.EdPairLow:
                _wordReadValue = value;
                StartRead((ushort)(_absoluteAddress + 1), ReadPurpose.EdPairHigh);
                break;
            case ReadPurpose.EdPairHigh:
                SetPair((_edOpcode >> 4) & 3, (ushort)(_wordReadValue | (value << 8)));
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            case ReadPurpose.EdStackLow:
                _wordReadValue = value;
                _state.SP++;
                StartRead(_state.SP, ReadPurpose.EdStackHigh);
                break;
            case ReadPurpose.EdStackHigh:
                _state.SP++;
                _state.PC = (ushort)(_wordReadValue | (value << 8));
                _state.WZ = _state.PC;
                break;
            case ReadPurpose.EdNibble:
                CompleteEdNibble(value);
                break;
            case ReadPurpose.EdBlockRead:
                CompleteEdBlockRead(value);
                break;
        }
    }

    private void CompleteEdMemoryWrite()
    {
        switch (_writePurpose)
        {
            case WritePurpose.EdAbsoluteLow:
                ushort pair = GetPair((_edOpcode >> 4) & 3);
                StartWrite((ushort)(_absoluteAddress + 1), (byte)(pair >> 8), WritePurpose.EdAbsoluteHigh);
                break;
            case WritePurpose.EdAbsoluteHigh:
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            case WritePurpose.EdNibble:
                break;
            case WritePurpose.EdBlockWrite:
                if ((_edOpcode & 3) == 0)
                {
                    CompleteEdBlockTransfer();
                }
                else
                {
                    CompleteEdBlockIo(input: true, _cycleWriteValue);
                }

                break;
        }
    }

    private void CompleteEdNibble(byte memoryValue)
    {
        byte aLow = (byte)(_state.A & 0x0F);
        if (_edOpcode == 0x67)
        {
            _cycleWriteValue = (byte)((aLow << 4) | (memoryValue >> 4));
            _state.A = (byte)((_state.A & 0xF0) | (memoryValue & 0x0F));
        }
        else
        {
            _cycleWriteValue = (byte)((memoryValue << 4) | aLow);
            _state.A = (byte)((_state.A & 0xF0) | (memoryValue >> 4));
        }

        _state.F = (byte)((_state.F & 1) | (_state.A & 0xA8) | (_state.A == 0 ? 0x40 : 0) | Parity(_state.A));
        _state.WZ = (ushort)(HlAddress() + 1);
        StartInternal(_cycleAddress, 4, InternalPurpose.EdNibbleWrite);
    }

    private void CompleteEdPortRead(byte value)
    {
        if (IsEdBlockInput(_edOpcode))
        {
            _cycleWriteValue = value;
            StartWrite(HlAddress(), value, WritePurpose.EdBlockWrite);
            return;
        }

        int register = (_edOpcode >> 3) & 7;
        if (register != 6)
        {
            SetRegister((byte)register, value);
        }

        _state.F = (byte)((_state.F & 1) | (value & 0xA8) | (value == 0 ? 0x40 : 0) | Parity(value));
        _state.WZ = (ushort)(_portAddress + 1);
    }

    private void CompleteEdPortWrite()
    {
        if (IsEdBlockOutput(_edOpcode))
        {
            CompleteEdBlockIo(input: false, _cycleWriteValue);
        }
        else
        {
            _state.WZ = (ushort)(_portAddress + 1);
        }
    }

    private static int Parity(byte value) =>
        (System.Numerics.BitOperations.PopCount((uint)value) & 1) == 0 ? 4 : 0;
}
