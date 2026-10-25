namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeIndirectCall(byte modRm, bool far)
    {
        int mod = modRm >> 6;
        int rm = modRm & 7;
        if (far && mod == 3)
        {
            throw new InvalidOperationException("FF /3 nie występuje jako operand rejestrowy w przypiętych wektorach.");
        }

        _indirectCallTransfer = true;
        _indirectCallFar = far;
        _indirectCallModRm = modRm;
        _indirectCallPrefixLength = _instructionPrefixLength;
        _indirectCallDisplacementBytes = mod == 3 ? 0 : mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        if (_state.QueueCount < _indirectCallPrefixLength + 2 + _indirectCallDisplacementBytes)
        {
            throw new InvalidOperationException("CALL pośredni wymaga ModR/M i displacementu w kolejce prefetch.");
        }

        ushort displacement = _indirectCallDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + _indirectCallPrefixLength + 2],
            _ => (ushort)(_state.Queue[_state.QueueHead + _indirectCallPrefixLength + 2] |
                (_state.Queue[_state.QueueHead + _indirectCallPrefixLength + 3] << 8)),
        };
        if (mod != 3)
        {
            Intel8086EffectiveAddress address = Intel8086EffectiveAddressDecoder.Decode(
                modRm, in _state, displacement, _segmentOverride);
            _indirectCallPhysicalAddress = address.PhysicalAddress;
            _indirectCallOffset = address.Offset;
            _memoryAddress = address.PhysicalAddress;
            _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
        }

        int stackParity = (int)(PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2))) & 1);
        int operandParity = mod == 3 ? 0 : (int)(_indirectCallPhysicalAddress & 1);
        int profileKey = ((((modRm * 2 + (_queueCountAtStart >= 6 ? 1 : 0)) * 2 + (_indirectCallPrefixLength != 0 ? 1 : 0)) * 2 + operandParity) * 2) + stackParity;
        Intel8086IndirectCallProfile profile = GetIndirectCallProfile(far, profileKey);
        _instructionCycles = profile.Cycles;
        _indirectCallRead1 = profile.Read1;
        _indirectCallRead2 = profile.Read2;
        _indirectCallRead3 = profile.Read3;
        _indirectCallRead4 = profile.Read4;
        _indirectCallWrite1 = profile.Write1;
        _indirectCallWrite2 = profile.Write2;
        _indirectCallWrite3 = profile.Write3;
        _indirectCallWrite4 = profile.Write4;
        _indirectCallFlushStart = profile.Flush;
        _indirectCallFetch1 = profile.Fetch1;
        _indirectCallFetch2 = profile.Fetch2;
        _indirectCallFetch3 = profile.Fetch3;
        _indirectCallFetch4 = profile.Fetch4;
        int effectiveAddressCycles = rm switch
        {
            0 or 3 => 7,
            1 or 2 => 8,
            4 or 5 or 7 => 5,
            6 => mod == 0 ? 6 : 5,
            _ => 0,
        };
        _indirectCallDisplacementStart = _indirectCallPrefixLength != 0
            ? mod == 0 && rm == 6 ? 6 : mod == 0 ? int.MaxValue : effectiveAddressCycles + 3
            : mod == 0 && rm == 6 ? 4 : mod == 0 ? int.MaxValue : effectiveAddressCycles + 1;
        _jumpReturnIp = unchecked((ushort)(InstructionPc + _indirectCallPrefixLength + 2 + _indirectCallDisplacementBytes));
        _indirectCallFetchOffset = unchecked((ushort)(InstructionPc + _queueCountAtStart));
        _jumpStackAddress = PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2)));
        _jumpTargetCs = _state.CS;
        _indirectCallTargetOffset = mod == 3 ? GetWordRegister(rm) : (ushort)0;
        _indirectCallTargetSegment = 0;
        _jumpLastOperand = 0;
        _memoryWordValue = 0;
    }

    private static Intel8086IndirectCallProfile GetIndirectCallProfile(bool far, int key)
    {
        ushort[] keys = far ? IndirectCall3ProfileKeys : IndirectCall2ProfileKeys;
        int low = 0;
        int high = keys.Length - 1;
        while (low <= high)
        {
            int middle = (low + high) >> 1;
            int candidate = keys[middle];
            if (candidate == key)
            {
                byte[] values = far ? IndirectCall3ProfileValues : IndirectCall2ProfileValues;
                int offset = middle * 14;
                return new Intel8086IndirectCallProfile(
                    values[offset], values[offset + 1], values[offset + 2], values[offset + 3], values[offset + 4],
                    values[offset + 5], values[offset + 6], values[offset + 7], values[offset + 8], values[offset + 9],
                    values[offset + 10], values[offset + 11], values[offset + 12], values[offset + 13]);
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

        throw new InvalidOperationException($"Brak profilu źródłowego CALL pośredniego dla klucza {key}.");
    }

    private readonly record struct Intel8086IndirectCallProfile(
        byte Cycles,
        byte Read1,
        byte Read2,
        byte Read3,
        byte Read4,
        byte Write1,
        byte Write2,
        byte Write3,
        byte Write4,
        byte Flush,
        byte Fetch1,
        byte Fetch2,
        byte Fetch3,
        byte Fetch4);

}
