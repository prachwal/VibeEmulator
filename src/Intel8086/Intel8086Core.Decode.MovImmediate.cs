namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeMovImmediate()
    {
        int modRmIndex = _state.QueueHead + _movImmediatePrefixLength + 1;
        if (_state.QueueCount < _movImmediatePrefixLength + 2)
        {
            throw new InvalidOperationException("C6 wymaga ModR/M i immediate w kolejce prefetch.");
        }

        _movImmediateModRm = _state.Queue[modRmIndex];
        if (((_movImmediateModRm >> 3) & 7) != 0)
        {
            throw new InvalidOperationException("C6 obsługuje wyłącznie kodowanie /0.");
        }

        int mod = _movImmediateModRm >> 6;
        int rm = _movImmediateModRm & 7;
        _movImmediateTransfer = true;
        _movImmediateDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : mod == 2 ? 2 : 0;
        if (_state.QueueCount < _movImmediatePrefixLength + 2 + _movImmediateDisplacementBytes)
        {
            throw new InvalidOperationException("C6 wymaga bajtów displacementu w kolejce prefetch.");
        }

        int immediateIndex = _state.QueueHead + _movImmediatePrefixLength + 2 + _movImmediateDisplacementBytes;
        _movImmediateValue = immediateIndex < _state.QueueHead + _state.QueueCount
            ? _state.Queue[immediateIndex]
            : (byte)0;
        if (mod == 3)
        {
            _instructionCycles = _movImmediatePrefixLength == 0 ? 4 : 6;
            return;
        }

        ushort displacement = _movImmediateDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + _movImmediatePrefixLength + 2],
            _ => (ushort)(_state.Queue[_state.QueueHead + _movImmediatePrefixLength + 2]
                | (_state.Queue[_state.QueueHead + _movImmediatePrefixLength + 3] << 8)),
        };
        Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(
            _movImmediateModRm, in _state, displacement, _movImmediateSegmentOverride);
        _movImmediatePhysicalAddress = effectiveAddress.PhysicalAddress;
        _movImmediateSegmentStatus = GetSegmentStatus(_movImmediateSegmentOverride == Intel8086SegmentOverride.None
            ? effectiveAddress.Segment == _state.SS ? Intel8086SegmentOverride.Ss : Intel8086SegmentOverride.Ds
            : _movImmediateSegmentOverride);
        _movImmediateBhe = (byte)((_movImmediatePhysicalAddress & 1) == 0 ? 1 : 0);
        int modRm = _movImmediateModRm >> 6;
        _movImmediateDisplacementStart = modRm == 0 && rm == 6
            ? 4 + _movImmediatePrefixLength * 2
            : modRm == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(modRm, rm) + 1 + _movImmediatePrefixLength * 2;
        _movImmediateWriteStart = GetMovImmediateWriteStart(modRm, rm, _queueCountAtStart, _movImmediatePrefixLength == 1);
        _movImmediatePrefetchBytesRemaining = GetMovImmediatePrefetchBytes(
            _queueCountAtStart, modRm, rm, _movImmediatePrefixLength, _movImmediateDisplacementBytes);
        _movImmediateCodeBytesRemaining = Math.Max(
            0, _movImmediatePrefixLength + 3 + _movImmediateDisplacementBytes - _queueCountAtStart);
        _instructionCycles = _movImmediateWriteStart + 2;
    }
}
