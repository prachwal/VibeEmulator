namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void PrepareEdBlockCycle()
    {
        if (IsEdBlockInput(_edOpcode) || IsEdBlockOutput(_edOpcode))
        {
            StartInternal(_refreshAddress, 1, InternalPurpose.EdBlockStart);
        }
        else
        {
            StartRead(HlAddress(), ReadPurpose.EdBlockRead);
        }
    }

    private static bool IsEdBlockInput(byte opcode) => (opcode & 0x03) == 2;

    private static bool IsEdBlockOutput(byte opcode) => (opcode & 0x03) == 3;

    private void CompleteEdBlockRead(byte value)
    {
        int kind = _edOpcode & 3;
        if (kind == 0)
        {
            StartWrite(PairDE(), value, WritePurpose.EdBlockWrite);
        }
        else if (kind == 1)
        {
            CompleteEdBlockCompare(value);
        }
        else
        {
            _cycleWriteValue = value;
            _state.B--;
            _portAddress = PairBC();
            StartPortWrite(_portAddress, value);
        }
    }

    private void CompleteEdBlockCompare(byte value)
    {
        int direction = (_edOpcode & 8) == 0 ? 1 : -1;
        int result = _state.A - value;
        int halfCarry = ((_state.A ^ value ^ result) & 0x10) != 0 ? 0x10 : 0;
        byte adjusted = (byte)(result - (halfCarry != 0 ? 1 : 0));
        ushort remaining = (ushort)(PairBC() - 1);
        SetPair(0, remaining);
        SetPair(2, (ushort)(HlAddress() + direction));
        _state.F = (byte)((_state.F & 1) | (result & 0x80) | ((byte)result == 0 ? 0x40 : 0)
            | 2 | halfCarry | (remaining != 0 ? 4 : 0) | (adjusted & 8) | ((adjusted & 2) << 4));
        _state.WZ = (ushort)(_state.WZ + direction);
        bool repeat = (_edOpcode & 0x10) != 0 && remaining != 0 && (byte)result != 0;
        PrepareEdRepeat(repeat);
        StartInternal(_cycleAddress, _instructionTStates - (_elapsedTStates + 1), InternalPurpose.Finish);
    }

    private void CompleteEdBlockIo(bool input, byte value)
    {
        int direction = (_edOpcode & 8) == 0 ? 1 : -1;
        SetPair(2, (ushort)(HlAddress() + direction));
        if (input)
        {
            _state.B--;
        }

        int addend = input ? (byte)(_state.C + direction) : _state.L;
        int sum = value + addend;
        byte b = _state.B;
        int flags = (b & 0xA8) | (b == 0 ? 0x40 : 0) | ((value & 0x80) != 0 ? 2 : 0);
        if (sum > 0xFF)
        {
            flags |= 0x11;
        }

        if ((System.Numerics.BitOperations.PopCount((uint)(((sum & 7) ^ b) & 0xFF)) & 1) == 0)
        {
            flags |= 4;
        }

        _state.F = (byte)flags;
        _state.WZ = (ushort)(_portAddress + direction);
        bool repeat = (_edOpcode & 0x10) != 0 && b != 0;
        PrepareEdRepeat(repeat);
        if (repeat)
        {
            CompleteEdBlockIoRepeat(value);
        }
        StartInternal(input ? _cycleAddress : _portAddress, _instructionTStates - (_elapsedTStates + 1), InternalPurpose.Finish);
    }

    private void CompleteEdBlockTransfer()
    {
        int direction = (_edOpcode & 8) == 0 ? 1 : -1;
        SetPair(2, (ushort)(HlAddress() + direction));
        SetPair(1, (ushort)(PairDE() + direction));
        ushort remaining = (ushort)(PairBC() - 1);
        SetPair(0, remaining);
        int sum = _state.A + _cycleWriteValue;
        _state.F = (byte)((_state.F & 0xC1) | (remaining != 0 ? 4 : 0) | (sum & 8) | ((sum & 2) << 4));
        bool repeat = (_edOpcode & 0x10) != 0 && remaining != 0;
        PrepareEdRepeat(repeat);
        StartInternal(_cycleAddress, _instructionTStates - (_elapsedTStates + 1), InternalPurpose.Finish);
    }

    private void PrepareEdRepeat(bool repeat)
    {
        if (repeat)
        {
            _state.PC = InstructionPc;
            _state.WZ = (ushort)(InstructionPc + 1);
            _instructionTStates = 21;
            SetRepeatFlagsFromPc();
        }
        else if ((_edOpcode & 0x10) != 0)
        {
            _instructionTStates = 16;
        }
    }

    private void CompleteEdBlockIoRepeat(byte value)
    {
        SetRepeatFlagsFromPc();
        bool carry = (_state.F & 1) != 0;
        bool parityFlip;
        if (carry && (value & 0x80) != 0)
        {
            parityFlip = Parity((byte)((_state.B - 1) & 7)) == 0;
            if ((_state.B & 0x0F) == 0)
            {
                _state.F |= 0x10;
            }
            else
            {
                _state.F &= 0xEF;
            }
        }
        else if (carry)
        {
            parityFlip = Parity((byte)((_state.B + 1) & 7)) == 0;
            if ((_state.B & 0x0F) == 0x0F)
            {
                _state.F |= 0x10;
            }
            else
            {
                _state.F &= 0xEF;
            }
        }
        else
        {
            parityFlip = Parity((byte)(_state.B & 7)) == 0;
        }

        if (parityFlip)
        {
            _state.F ^= 4;
        }
    }

    private void SetRepeatFlagsFromPc()
    {
        _state.F = (byte)((_state.F & 0xD7) | (((_state.PC >> 11) & 1) << 3) | (((_state.PC >> 13) & 1) << 5));
    }
}
