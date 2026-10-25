namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsEscOpcode(byte opcode) => opcode is >= 0xD8 and <= 0xDF;

    private void DecodeAdditionalGroupOperands(byte opcode)
    {
        if (IsEscOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException("ESC requires its ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            _memoryModRm = modRm;
            if ((modRm >> 6) != 3)
            {
                int mod = modRm >> 6;
                int rm = modRm & 7;
                int displacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < 2 + prefixOffset + displacementBytes)
                {
                    throw new InvalidOperationException("ESC memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + 2 + prefixOffset],
                    _ => (ushort)(_state.Queue[_state.QueueHead + 2 + prefixOffset] | (_state.Queue[_state.QueueHead + 3 + prefixOffset] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _segmentOverride);
                _memoryAddress = effectiveAddress.PhysicalAddress;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                int effectiveAddressCycles = rm switch
                {
                    0 or 3 => 7,
                    1 or 2 => 8,
                    4 or 5 or 7 => 5,
                    6 => mod == 0 ? 6 : 5,
                    _ => throw new InvalidOperationException("Invalid ESC ModR/M r/m field."),
                };
                int queueAdjustment = mod switch
                {
                    0 when _queueCountAtStart == 5 && rm is 4 or 5 or 7 => 1,
                    0 when _queueCountAtStart == 6 && rm == 6 => 1,
                    2 when _queueCountAtStart == 6 => 1,
                    _ => 0,
                };
                _memoryReadStart = prefixOffset != 0
                    ? GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart)
                    : 3 + effectiveAddressCycles + (mod == 0 ? 0 : 4) + queueAdjustment;
            }
        }

        if (IsWordIncDecGroupOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException("FF /0,/1 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            int extension = (modRm >> 3) & 7;
            if (extension is not 0 and not 1 and not 6 and not 7)
            {
                throw new InvalidOperationException("Only FF /0,/1,/6,/7 forms are implemented.");
            }

            _memoryModRm = modRm;
            if ((modRm >> 6) != 3)
            {
                int mod = modRm >> 6;
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < 2 + prefixOffset + displacementBytes)
                {
                    throw new InvalidOperationException("FF memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + 2 + prefixOffset],
                    _ => (ushort)(_state.Queue[_state.QueueHead + 2 + prefixOffset] | (_state.Queue[_state.QueueHead + 3 + prefixOffset] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _segmentOverride);
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                int effectiveAddressCycles = rm switch
                {
                    0 or 3 => 7,
                    1 or 2 => 8,
                    4 or 5 or 7 => 5,
                    6 => mod == 0 ? 6 : 5,
                    _ => throw new InvalidOperationException("Invalid FF ModR/M register field."),
                };
                int queueAdjustment = mod switch
                {
                    0 when _queueCountAtStart == 5 && rm is 4 or 5 or 7 => 1,
                    0 when _queueCountAtStart == 6 && rm == 6 => 1,
                    2 when _queueCountAtStart == 6 => 1,
                    _ => 0,
                };
                _memoryReadStart = prefixOffset != 0
                    ? GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart)
                    : 3 + effectiveAddressCycles + (mod == 0 ? 0 : 4) + queueAdjustment;
            }
        }

        if (IsByteIncDecGroupOpcode(opcode))
        {
            if (_state.QueueCount < 2 + _instructionPrefixLength)
            {
                throw new InvalidOperationException("FE /0,/1 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + _instructionPrefixLength];
            _memoryModRm = modRm;
            if (((modRm >> 3) & 7) is not 0 and not 1)
            {
                throw new InvalidOperationException("Only FE /0,/1 forms are implemented.");
            }

            if ((modRm >> 6) != 3)
            {
                int mod = modRm >> 6;
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < _instructionPrefixLength + 2 + displacementBytes)
                {
                    throw new InvalidOperationException("FE memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + _instructionPrefixLength + 2],
                    _ => (ushort)(_state.Queue[_state.QueueHead + _instructionPrefixLength + 2] | (_state.Queue[_state.QueueHead + _instructionPrefixLength + 3] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _segmentOverride);
                _memoryModRm = modRm;
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                if (_instructionPrefixLength != 0)
                {
                    _memoryReadStart = GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart);
                }
                else
                {
                    int effectiveAddressCycles = (modRm & 7) switch
                    {
                        0 or 3 => 7,
                        1 or 2 => 8,
                        4 or 5 or 7 => 5,
                        6 => mod == 0 ? 6 : 5,
                        _ => throw new InvalidOperationException("Invalid FE ModR/M register field."),
                    };
                    int queueAdjustment = mod switch
                    {
                        0 when _queueCountAtStart == 5 && (modRm & 7) is 4 or 5 or 7 => 1,
                        0 when _queueCountAtStart == 6 && (modRm & 7) == 6 => 1,
                        2 when _queueCountAtStart == 6 => 1,
                        _ => 0,
                    };
                    _memoryReadStart = 3 + effectiveAddressCycles + (mod == 0 ? 0 : 4) + queueAdjustment;
                }
            }
        }

        if (IsUnaryNotNegGroupOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            if (!IsSupportedUnaryNotNeg(opcode, modRm))
            {
                throw new InvalidOperationException("Only F6/F7 /2,/3,/6,/7 and /4,/5 forms are implemented.");
            }

            _memoryModRm = modRm;
            if ((modRm >> 6) != 3)
            {
                int mod = modRm >> 6;
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                bool combinedSegmentRepeatPrefix =
                    _segmentOverride != Intel8086SegmentOverride.None && prefixOffset == 2;
                if (_state.QueueCount < 2 + prefixOffset + displacementBytes && !combinedSegmentRepeatPrefix)
                {
                    throw new InvalidOperationException("F6/F7 memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => combinedSegmentRepeatPrefix
                        ? ReadInstructionByteForDecode(2 + prefixOffset)
                        : _state.Queue[_state.QueueHead + 2 + prefixOffset],
                    _ => combinedSegmentRepeatPrefix
                        ? (ushort)(ReadInstructionByteForDecode(2 + prefixOffset) |
                            (ReadInstructionByteForDecode(3 + prefixOffset) << 8))
                        : (ushort)(_state.Queue[_state.QueueHead + 2 + prefixOffset] |
                            (_state.Queue[_state.QueueHead + 3 + prefixOffset] << 8)),
                };
                _memoryDivideQueueData = displacementBytes == 0
                    ? modRm
                    : ReadInstructionByteForDecode(1 + prefixOffset + displacementBytes);
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _segmentOverride);
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                if (prefixOffset != 0)
                {
                    _memoryReadStart = GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart);
                    return;
                }

                int effectiveAddressCycles = rm switch
                {
                    0 or 3 => 7,
                    1 or 2 => 8,
                    4 or 5 or 7 => 5,
                    6 => mod == 0 ? 6 : 5,
                    _ => throw new InvalidOperationException("Invalid F6/F7 ModR/M register field."),
                };
                int queueAdjustment = mod switch
                {
                    0 when _queueCountAtStart == 5 && rm is 4 or 5 or 7 => 1,
                    0 when _queueCountAtStart == 6 && rm == 6 => 1,
                    2 when _queueCountAtStart == 6 => 1,
                    _ => 0,
                };
                _memoryReadStart = 3 + effectiveAddressCycles + (mod == 0 ? 0 : 4) + queueAdjustment;
            }
        }

        if (IsShiftRotateGroupOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1];
            if (!IsSupportedShiftRotate(opcode, modRm))
            {
                throw new InvalidOperationException("Only D0-D3 /0-/5,/7 register-direct forms are implemented.");
            }

            if (IsShiftRotateClGroupOpcode(opcode))
            {
                _shiftCount = (byte)_state.CX;
            }

            _memoryModRm = modRm;
            if ((modRm >> 6) != 3)
            {
                int mod = modRm >> 6;
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < 2 + displacementBytes)
                {
                    throw new InvalidOperationException("D0-D3 memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + 2],
                    _ => (ushort)(_state.Queue[_state.QueueHead + 2] | (_state.Queue[_state.QueueHead + 3] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement);
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, Intel8086SegmentOverride.None);
                int effectiveAddressCycles = rm switch
                {
                    0 or 3 => 7,
                    1 or 2 => 8,
                    4 or 5 or 7 => 5,
                    6 => mod == 0 ? 6 : 5,
                    _ => throw new InvalidOperationException("Invalid D0-D3 ModR/M register field."),
                };
                int queueAdjustment = mod switch
                {
                    0 when _queueCountAtStart == 5 && rm is 4 or 5 or 7 => 1,
                    0 when _queueCountAtStart == 6 && rm == 6 => 1,
                    2 when _queueCountAtStart == 6 => 1,
                    _ => 0,
                };
                _memoryReadStart = 3 + effectiveAddressCycles + (mod == 0 ? 0 : 4) + queueAdjustment;
            }
        }

    }
}
