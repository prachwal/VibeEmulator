namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeIndirectJump(byte modRm, bool far)
    {
        int mod = modRm >> 6;
        int rm = modRm & 7;
        if (far && mod == 3)
        {
            throw new InvalidOperationException("FF /5 nie występuje jako operand rejestrowy w przypiętych wektorach.");
        }

        _indirectJumpTransfer = true;
        _indirectJumpFar = far;
        _indirectJumpModRm = modRm;
        _indirectJumpPrefixLength = _instructionPrefixLength;
        _indirectJumpDisplacementBytes = mod == 3 ? 0 : mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        if (_state.QueueCount < _indirectJumpPrefixLength + 2 + _indirectJumpDisplacementBytes)
        {
            throw new InvalidOperationException("JMP pośredni wymaga ModR/M i displacementu w kolejce prefetch.");
        }

        ushort displacement = _indirectJumpDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + _indirectJumpPrefixLength + 2],
            _ => (ushort)(_state.Queue[_state.QueueHead + _indirectJumpPrefixLength + 2] |
                (_state.Queue[_state.QueueHead + _indirectJumpPrefixLength + 3] << 8)),
        };
        if (mod != 3)
        {
            Intel8086EffectiveAddress address = Intel8086EffectiveAddressDecoder.Decode(
                modRm, in _state, displacement, _segmentOverride);
            _indirectJumpPhysicalAddress = address.PhysicalAddress;
            _indirectJumpOffset = address.Offset;
            _memoryAddress = address.PhysicalAddress;
            _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
        }

        int operandParity = mod == 3 ? 0 : (int)(_indirectJumpPhysicalAddress & 1);
        int profileKey = (((modRm * 2 + (_queueCountAtStart >= 6 ? 1 : 0)) * 2 +
            (_indirectJumpPrefixLength != 0 ? 1 : 0)) * 2) + operandParity;
        Intel8086IndirectJumpProfile profile = GetIndirectJumpProfile(far, profileKey);
        _instructionCycles = profile.Cycles;
        _indirectJumpRead1 = profile.Read1;
        _indirectJumpRead2 = profile.Read2;
        _indirectJumpRead3 = profile.Read3;
        _indirectJumpRead4 = profile.Read4;
        _indirectJumpFlushStart = profile.Flush;
        _indirectJumpFetch1 = profile.Fetch1;
        _indirectJumpFetch2 = profile.Fetch2;
        _indirectJumpFetch3 = profile.Fetch3;
        _indirectJumpFetch4 = profile.Fetch4;
        _indirectJumpDisplacementStart = GetIndirectJumpDisplacementStart(mod, rm);
        _indirectJumpFetchOffset = unchecked((ushort)(InstructionPc + _queueCountAtStart));
        _indirectJumpTargetOffset = mod == 3 ? GetWordRegister(rm) : (ushort)0;
        _indirectJumpTargetSegment = 0;
        _jumpTargetCs = _state.CS;
        _jumpLastOperand = 0;
        _memoryWordValue = 0;
    }

    private int GetIndirectJumpDisplacementStart(int mod, int rm)
    {
        if (mod == 3 || mod == 0 && rm != 6)
        {
            return int.MaxValue;
        }

        int effectiveAddressCycles = rm switch
        {
            0 or 3 => 7,
            1 or 2 => 8,
            4 or 5 or 7 => 5,
            6 => mod == 0 ? 6 : 5,
            _ => 0,
        };
        return _indirectJumpPrefixLength != 0
            ? mod == 0 ? 6 : effectiveAddressCycles + 3
            : mod == 0 ? 4 : effectiveAddressCycles + 1;
    }

    private static Intel8086IndirectJumpProfile GetIndirectJumpProfile(bool far, int key)
    {
        ushort[] keys = far ? IndirectJump5ProfileKeys : IndirectJump4ProfileKeys;
        int low = 0;
        int high = keys.Length - 1;
        while (low <= high)
        {
            int middle = (low + high) >> 1;
            int candidate = keys[middle];
            if (candidate == key)
            {
                byte[] values = far ? IndirectJump5ProfileValues : IndirectJump4ProfileValues;
                int offset = middle * 10;
                return new Intel8086IndirectJumpProfile(
                    values[offset], values[offset + 1], values[offset + 2], values[offset + 3], values[offset + 4],
                    values[offset + 5], values[offset + 6], values[offset + 7], values[offset + 8], values[offset + 9]);
            }

            if (candidate < key)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        throw new InvalidOperationException($"Brak profilu źródłowego indirect JMP dla klucza {key}.");
    }

    private readonly record struct Intel8086IndirectJumpProfile(
        byte Cycles, byte Read1, byte Read2, byte Read3, byte Read4, byte Flush,
        byte Fetch1, byte Fetch2, byte Fetch3, byte Fetch4);
}
