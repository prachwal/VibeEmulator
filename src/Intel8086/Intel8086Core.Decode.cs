namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void BeginInstruction()
    {
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot decode an empty prefetch queue.");
        }

        byte opcode = _state.Queue[_state.QueueHead];
        _xlatPrefixLength = 0;
        _xlatSegmentOverride = Intel8086SegmentOverride.None;
        _movMemoryTransfer = false;
        _movMemoryPrefixLength = 0;
        _movMemorySegmentOverride = Intel8086SegmentOverride.None;
        _movImmediateTransfer = false;
        _movImmediatePrefixLength = 0;
        _movImmediateSegmentOverride = Intel8086SegmentOverride.None;
        _aluMemoryTransfer = false;
        _aluMemoryWrites = false;
        _aluGroupImmediateRegisterTransfer = false;
        _xchgRegisterTransfer = false;
        _xchgMemoryTransfer = false;
        _xchgPrefixLength = 0;
        _xchgSegmentOverride = Intel8086SegmentOverride.None;
        if (TryGetXlatSegmentOverride(opcode, out Intel8086SegmentOverride segmentOverride))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("Prefiks segmentowy wymaga opkodu w kolejce prefetch.");
            }

            byte prefixedOpcode = _state.Queue[_state.QueueHead + 1];
            if (prefixedOpcode == 0xD7)
            {
                _xlatPrefixLength = 1;
                _xlatSegmentOverride = segmentOverride;
                opcode = 0xD7;
            }
            else if (prefixedOpcode is 0x88 or 0x8A)
            {
                if (_state.QueueCount < 3 || (_state.Queue[_state.QueueHead + 2] >> 6) == 3)
                {
                    throw new InvalidOperationException("Prefiks segmentowy w tym etapie wymaga pamięciowego MOV.");
                }

                _movMemoryPrefixLength = 1;
                _movMemorySegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode == 0x86)
            {
                if (_state.QueueCount < 3)
                {
                    throw new InvalidOperationException("Prefiks segmentowy XCHG wymaga bajtu ModR/M w kolejce prefetch.");
                }

                _xchgPrefixLength = 1;
                _xchgSegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode == 0xC6)
            {
                if (_state.QueueCount < 3 || ((_state.Queue[_state.QueueHead + 2] >> 3) & 7) != 0)
                {
                    throw new InvalidOperationException("Segment-prefixed C6 requires the /0 ModR/M form in this stage.");
                }

                _movImmediateTransfer = true;
                _movImmediatePrefixLength = 1;
                _movImmediateSegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else
            {
                throw new InvalidOperationException("W tym etapie prefiks segmentowy jest obsługiwany z XLAT, MOV, XCHG i C6.");
            }
        }

        if (opcode is not 0x90 and not (>= 0x40 and <= 0x4F) and not (>= 0xB0 and <= 0xBF) and not 0x80 and not 0x86 and not 0x88 and not 0x8A and not 0x89 and not 0x8B and not 0x8D and not 0xD7 and not 0xC6 && !IsAccumulatorImmediateAluOpcode(opcode) && !IsAccumulatorImmediateWordAluOpcode(opcode) && !IsRegisterDirectAluOpcode(opcode) && !IsRegisterDirectWordAluOpcode(opcode) && !IsByteIncDecGroupOpcode(opcode) && !IsWordIncDecGroupOpcode(opcode) && !IsUnaryNotNegGroupOpcode(opcode))
        {
            throw new InvalidOperationException($"Opcode {opcode:X2} is not implemented yet.");
        }

        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = opcode;
        _queueCountAtStart = _state.QueueCount;
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + _queueCountAtStart));
        if (opcode is >= 0xB8 and <= 0xBF || IsAccumulatorImmediateWordAluOpcode(opcode))
        {
            if (_state.QueueCount < 3)
            {
                throw new InvalidOperationException("A word immediate instruction requires all instruction bytes in the prefetch queue.");
            }

            _immediateWord = (ushort)(_state.Queue[_state.QueueHead + 1] | (_state.Queue[_state.QueueHead + 2] << 8));
        }

        if (opcode is 0x89 or 0x8B)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("MOV r/m16,r16 requires the ModR/M byte in the prefetch queue.");
            }

            if ((_state.Queue[_state.QueueHead + 1] >> 6) != 3)
            {
                throw new InvalidOperationException($"MOV {opcode:X2} memory ModR/M forms are not implemented yet.");
            }
        }

        if (IsRegisterDirectAluOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            InitializeAluMemoryTransfer();
        }
        if (IsRegisterDirectWordAluOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            if ((_state.Queue[_state.QueueHead + 1] >> 6) != 3)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} memory ModR/M forms are not implemented yet.");
            }
        }

        if (IsUnaryNotNegGroupOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1];
            if (!IsSupportedUnaryNotNeg(opcode, modRm))
            {
                throw new InvalidOperationException("Only F6/F7 /2,/3 register-direct forms are implemented.");
            }
        }

        if (IsByteIncDecGroupOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("FE /0,/1 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1];
            if ((modRm >> 6) != 3 || ((modRm >> 3) & 7) is not 0 and not 1)
            {
                throw new InvalidOperationException("Only FE /0,/1 register-direct forms are implemented.");
            }
        }

        if (IsWordIncDecGroupOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("FF /0,/1 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1];
            if ((modRm >> 6) != 3 || ((modRm >> 3) & 7) is not 0 and not 1)
            {
                throw new InvalidOperationException("Only FF /0,/1 register-direct forms are implemented.");
            }
        }

        if (opcode == 0x80)
        {
            InitializeAluGroupImmediateRegisterTransfer();
        }
        if (opcode == 0x86)
        {
            int modRmIndex = _state.QueueHead + _xchgPrefixLength + 1;
            if (_state.QueueCount < _xchgPrefixLength + 2)
            {
                throw new InvalidOperationException("XCHG r/m8,r8 wymaga bajtu ModR/M w kolejce prefetch.");
            }

            _xchgModRm = _state.Queue[modRmIndex];
            int mod = _xchgModRm >> 6;
            int rm = _xchgModRm & 7;
            _xchgMemoryTransfer = mod != 3;
            if (_xchgMemoryTransfer)
            {
                _xchgDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < _xchgPrefixLength + 2 + _xchgDisplacementBytes)
                {
                    throw new InvalidOperationException("Pamięciowy XCHG wymaga bajtów displacementu w kolejce prefetch.");
                }

                ushort displacement = _xchgDisplacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + _xchgPrefixLength + 2],
                    _ => (ushort)(_state.Queue[_state.QueueHead + _xchgPrefixLength + 2] | (_state.Queue[_state.QueueHead + _xchgPrefixLength + 3] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(_xchgModRm, in _state, displacement, _xchgSegmentOverride);
                _xchgPhysicalAddress = effectiveAddress.PhysicalAddress;
                _xchgSegmentStatus = GetSegmentStatus(_xchgSegmentOverride == Intel8086SegmentOverride.None
                    ? effectiveAddress.Segment == _state.SS ? Intel8086SegmentOverride.Ss : Intel8086SegmentOverride.Ds
                    : _xchgSegmentOverride);
                _xchgBhe = (byte)((_xchgPhysicalAddress & 1) == 0 ? 1 : 0);
                _xchgDisplacementStart = mod == 0 && rm == 6
                    ? 4
                    : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1;
                _xchgDisplacementStart += _xchgPrefixLength * 2;
                _xchgMemoryReadStart = GetXchgMemoryReadStart(mod, rm, _queueCountAtStart, _xchgPrefixLength == 1);
                _xchgPrefetchBytesRemaining = GetXchgPrefetchBytes(_queueCountAtStart, mod, _xchgPrefixLength, _xchgDisplacementBytes);
                _xchgRegisterValue = GetByteRegister((_xchgModRm >> 3) & 7);
                _instructionCycles = _xchgMemoryReadStart + 14;
            }
            else
            {
                _xchgRegisterTransfer = true;
                _instructionCycles = _xchgPrefixLength == 0 ? 4 : 6;
            }
        }

        if (opcode is 0x88 or 0x8A)
        {
            if (_state.QueueCount < _movMemoryPrefixLength + 2)
            {
                throw new InvalidOperationException("MOV r/m8,r8 requires the ModR/M byte in the prefetch queue.");
            }

            int modRmQueueIndex = _state.QueueHead + _movMemoryPrefixLength + 1;
            byte modRm = _state.Queue[modRmQueueIndex];
            _movMemoryTransfer = (modRm >> 6) != 3;
            if (_movMemoryTransfer)
            {
                _movMemoryModRm = modRm;
                int mod = modRm >> 6;
                int rm = modRm & 7;
                _movMemoryDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < _movMemoryPrefixLength + 2 + _movMemoryDisplacementBytes)
                {
                    throw new InvalidOperationException("MOV pamięciowy wymaga bajtów displacementu w kolejce prefetch.");
                }

                ushort displacement = _movMemoryDisplacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + _movMemoryPrefixLength + 2],
                    _ => (ushort)(_state.Queue[_state.QueueHead + _movMemoryPrefixLength + 2] | (_state.Queue[_state.QueueHead + _movMemoryPrefixLength + 3] << 8)),
                };
                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _movMemorySegmentOverride);
                _movMemoryPhysicalAddress = effectiveAddress.PhysicalAddress;
                _movMemorySegmentStatus = GetSegmentStatus(_movMemorySegmentOverride == Intel8086SegmentOverride.None
                    ? effectiveAddress.Segment == _state.SS ? Intel8086SegmentOverride.Ss : Intel8086SegmentOverride.Ds
                    : _movMemorySegmentOverride);
                _movMemoryBhe = (byte)((_movMemoryPhysicalAddress & 1) == 0 ? 1 : 0);
                _movMemoryDisplacementStart = mod == 0 && rm == 6
                    ? 4
                    : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1;
                _movMemoryDisplacementStart += _movMemoryPrefixLength * 2;
                _movMemoryBusStart = GetMovMemoryBusStart(opcode, mod, rm, _movMemoryDisplacementBytes, _queueCountAtStart);
                _instructionCycles = opcode == 0x88 ? _movMemoryBusStart + 2 : _movMemoryBusStart + 5;
                _movMemoryPrefetchBytesRemaining = _movMemoryPrefixLength == 1
                    ? GetPrefixedMovMemoryQueuePrefetchBytes(opcode, mod, rm, _queueCountAtStart, _movMemoryDisplacementBytes)
                    : 0;
            }
        }

        if (opcode == 0xC6)
        {
            InitializeMovImmediate();
        }

        if (opcode == 0x8D)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("LEA wymaga bajtu ModR/M w kolejce prefetch.");
            }

            _memoryModRm = _state.Queue[_state.QueueHead + 1];
            int mod = _memoryModRm >> 6;
            int rm = _memoryModRm & 7;
            if (mod == 3)
            {
                throw new InvalidOperationException("LEA wymaga operandu pamięciowego (ModR/M mod != 11).");
            }

            _leaDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
            if (_state.QueueCount < 2 + _leaDisplacementBytes)
            {
                throw new InvalidOperationException("LEA wymaga bajtów przesunięcia w kolejce prefetch.");
            }

            _leaEffectiveAddressCycles = rm switch
            {
                0 or 3 => 7,
                1 or 2 => 8,
                4 or 5 or 7 => 5,
                6 => mod == 0 ? 6 : 5,
                _ => throw new InvalidOperationException("Nieprawidłowe pole r/m dla LEA."),
            };
            ushort displacement = _leaDisplacementBytes switch
            {
                0 => 0,
                1 => _state.Queue[_state.QueueHead + 2],
                _ => (ushort)(_state.Queue[_state.QueueHead + 2] | (_state.Queue[_state.QueueHead + 3] << 8)),
            };
            _leaOffset = Intel8086EffectiveAddressDecoder.Decode(_memoryModRm, in _state, displacement).Offset;
        }

        if (opcode == 0xD7)
        {
            ushort segment = _xlatSegmentOverride switch
            {
                Intel8086SegmentOverride.None => _state.DS,
                Intel8086SegmentOverride.Es => _state.ES,
                Intel8086SegmentOverride.Cs => _state.CS,
                Intel8086SegmentOverride.Ss => _state.SS,
                Intel8086SegmentOverride.Ds => _state.DS,
                _ => throw new InvalidOperationException("Nieznany prefiks segmentowy XLAT."),
            };
            ushort offset = unchecked((ushort)(_state.BX + (byte)_state.AX));
            _xlatPhysicalAddress = (uint)(((segment << 4) + offset) & 0xFFFFF);
            _xlatSegmentStatus = GetSegmentStatus(_xlatSegmentOverride);
            _xlatBhe = (byte)((_xlatPhysicalAddress & 1) == 0 ? 1 : 0);
        }

        _instructionCycles = _xchgRegisterTransfer || _xchgMemoryTransfer || _movMemoryTransfer || _movImmediateTransfer || _aluMemoryTransfer || _aluGroupImmediateRegisterTransfer || _aluGroupImmediateMemoryTransfer ? _instructionCycles : opcode == 0x90 || IsRegisterDirectAluOpcode(opcode) || IsRegisterDirectWordAluOpcode(opcode) || IsByteIncDecGroupOpcode(opcode) || IsWordIncDecGroupOpcode(opcode) || IsUnaryNotNegGroupOpcode(opcode) ? 3 : opcode is >= 0x40 and <= 0x4F ? 2 : opcode == 0x8D
            ? 2 + _leaEffectiveAddressCycles + ((_memoryModRm >> 6) == 0 ? 0 : 4)
            : opcode == 0xD7 ? (_xlatPrefixLength == 0 ? 17 - _queueCountAtStart : 8 + _queueCountAtStart)
            : 4;
        if ((opcode is 0x88 or 0x8A) && !_movMemoryTransfer)
        {
            _instructionCycles = 2;
        }
    }

}
