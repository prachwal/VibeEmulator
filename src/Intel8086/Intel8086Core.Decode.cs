namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private byte ReadInstructionByteForDecode(int queueOffset)
    {
        if (queueOffset < _state.QueueCount)
        {
            return _state.Queue[_state.QueueHead + queueOffset];
        }

        uint address = PhysicalAddress(InstructionCs, (ushort)(InstructionPc + queueOffset));
        return _bus.Read(address);
    }

    private void BeginInstruction()
    {
        _trapFlagAtInstructionStart = (_state.Flags & 0x0100) != 0;
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot decode an empty prefetch queue.");
        }

        byte opcode = _state.Queue[_state.QueueHead];
        _instructionPrefixLength = 0;
        _instructionPrefix = 0;
        _lockPrefix = false;
        _lockSignalActive = false;
        _lockStringPrefix = false;
        _stringSegmentPrefix = false;
        _stringCombinedPrefixPreludeComplete = false;
        _segmentOverride = Intel8086SegmentOverride.None;
        _xlatPrefixLength = 0;
        _xlatSegmentOverride = Intel8086SegmentOverride.None;
        _movMemoryTransfer = false;
        _movMemoryWordTransfer = false;
        _movMemoryWordValue = 0;
        _movSegmentTransfer = false;
        _lesLdsTransfer = false;
        _popRm16Transfer = false;
        _popRm16ModRm = 0;
        _popRm16ConsumedBytes = 0;
        _lesLdsModRm = 0;
        _lesLdsPrefixLength = 0;
        _lesLdsDisplacementBytes = 0;
        _lesLdsDisplacementStart = 0;
        _lesLdsBusStart = 0;
        _lesLdsPrefetchBytesRemaining = 0;
        _lesLdsPhysicalAddress = 0;
        _lesLdsSegmentStatus = "DS";
        _lesLdsPointerValue = 0;
        _indirectCallTransfer = false;
        _indirectJumpTransfer = false;
        _movMemoryPrefixLength = 0;
        _movMemorySegmentOverride = Intel8086SegmentOverride.None;
        _moffsTransfer = false;
        _moffsPrefixLength = 0;
        _moffsSegmentOverride = Intel8086SegmentOverride.None;
        _movImmediateTransfer = false;
        _movImmediatePrefixLength = 0;
        _movImmediateSegmentOverride = Intel8086SegmentOverride.None;
        _movImmediateWordTransfer = false;
        _movImmediateWordPrefixLength = 0;
        _movImmediateWordSegmentOverride = Intel8086SegmentOverride.None;
        _aluMemoryTransfer = false;
        _aluMemoryWrites = false;
        _aluGroupImmediateRegisterTransfer = false;
        _aluGroupImmediatePrefixLength = 0;
        _xchgRegisterTransfer = false;
        _xchgMemoryTransfer = false;
        _xchgWordTransfer = false;
        _xchgPrefixLength = 0;
        _xchgSegmentOverride = Intel8086SegmentOverride.None;
        if (opcode == 0xF0)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("Prefiks LOCK wymaga następnego opkodu w kolejce.");
            }

            _lockPrefix = true;
            _lockSignalActive = true;
            byte followingOpcode = _state.Queue[_state.QueueHead + 1];
            if (followingOpcode is 0xF2 or 0xF3 && _state.QueueCount >= 3 &&
                _state.Queue[_state.QueueHead + 2] is >= 0xA4 and <= 0xAF)
            {
                _instructionPrefixLength = 2;
                _instructionPrefix = followingOpcode;
                _lockStringPrefix = true;
                _stringSegmentPrefix = true;
                opcode = _state.Queue[_state.QueueHead + 2];
            }
            else
            {
                _instructionPrefixLength = 1;
                _instructionPrefix = opcode;
                opcode = followingOpcode;
            }
        }
        else if (opcode is 0xF2 or 0xF3)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("Prefiks REP wymaga dostępnego opkodu w kolejce.");
            }

            _instructionPrefixLength = 1;
            _instructionPrefix = opcode;
            byte followingOpcode = _state.Queue[_state.QueueHead + 1];
            if (followingOpcode is >= 0xA4 and <= 0xAF)
            {
                if (_state.CX != 0 && followingOpcode is not 0xA5 and not 0xA6 and not 0xA7 and not 0xAE and not 0xAF and not 0xA4 and not 0xAA and not 0xAB and not 0xAC and not 0xAD)
                {
                    throw new InvalidOperationException("REP dla niezerowego CX jest obecnie zaimplementowane tylko dla MOVSB, STOSB/STOSW, LODSB/LODSW i SCASB.");
                }

                opcode = followingOpcode;
            }
            else if (_state.QueueCount >= 3 && (followingOpcode is 0xF6 or 0xF7) && ((_state.Queue[_state.QueueHead + 2] >> 3) & 7) == 7)
            {
                opcode = followingOpcode;
            }
            else
            {
                throw new InvalidOperationException("REP/REPNE jest obsługiwane wyłącznie dla IDIV oraz stringów z CX=0.");
            }
        }
        else if (TryGetXlatSegmentOverride(opcode, out Intel8086SegmentOverride segmentOverride))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("Prefiks segmentowy wymaga opkodu w kolejce prefetch.");
            }

            byte prefixedOpcode = _state.Queue[_state.QueueHead + 1];
            _segmentOverride = segmentOverride;
            if (prefixedOpcode is 0x88 or 0x89 or 0x8A or 0x8B && _state.QueueCount >= 3
                && (_state.Queue[_state.QueueHead + 2] >> 6) == 3)
            {
                _instructionPrefixLength = 1;
                _instructionPrefix = opcode;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is 0xF2 or 0xF3 && _state.QueueCount >= 3
                && _state.Queue[_state.QueueHead + 2] is >= 0xA4 and <= 0xAF)
            {
                _stringSegmentPrefix = true;
                _instructionPrefixLength = 2;
                _instructionPrefix = prefixedOpcode;
                opcode = _state.Queue[_state.QueueHead + 2];
            }
            else if (prefixedOpcode is 0xF2 or 0xF3 && _state.QueueCount >= 4
                && _state.Queue[_state.QueueHead + 2] is 0xF6 or 0xF7
                && ((_state.Queue[_state.QueueHead + 3] >> 3) & 7) == 7)
            {
                _instructionPrefixLength = 2;
                _instructionPrefix = prefixedOpcode;
                opcode = _state.Queue[_state.QueueHead + 2];
            }
            else if (prefixedOpcode == 0xD7)
            {
                _xlatPrefixLength = 1;
                _xlatSegmentOverride = segmentOverride;
                opcode = 0xD7;
            }
            else if (prefixedOpcode == 0xD6)
            {
                _instructionPrefixLength = 1;
                _instructionPrefix = opcode;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is 0x88 or 0x89 or 0x8A or 0x8B)
            {
                if (_state.QueueCount < 3 || (_state.Queue[_state.QueueHead + 2] >> 6) == 3)
                {
                    throw new InvalidOperationException("Prefiks segmentowy w tym etapie wymaga pamięciowego MOV.");
                }

                _movMemoryPrefixLength = 1;
                _movMemorySegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is >= 0xA0 and <= 0xA3)
            {
                _moffsPrefixLength = 1;
                _moffsSegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is 0x86 or 0x87)
            {
                if (_state.QueueCount < 3)
                {
                    throw new InvalidOperationException("Prefiks segmentowy XCHG wymaga bajtu ModR/M w kolejce prefetch.");
                }

                _xchgPrefixLength = 1;
                _xchgSegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is >= 0xA4 and <= 0xAF)
            {
                _stringSegmentPrefix = true;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode == 0xFE)
            {
                _instructionPrefixLength = 1;
                _instructionPrefix = opcode;
                _segmentOverride = segmentOverride;
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
            else if (prefixedOpcode == 0xC7)
            {
                if (_state.QueueCount < 3 || ((_state.Queue[_state.QueueHead + 2] >> 3) & 7) != 0)
                {
                    throw new InvalidOperationException("Segment-prefixed C7 requires the /0 ModR/M form in this stage.");
                }

                _movImmediateWordTransfer = true;
                _movImmediateWordPrefixLength = 1;
                _movImmediateWordSegmentOverride = segmentOverride;
                opcode = prefixedOpcode;
            }
            else if (prefixedOpcode is 0x80 or 0x81 or 0x82 or 0x83 or 0x8C or 0x8E or 0x8F or 0xC4 or 0xC5 or 0xD6 or 0xF6 or 0xF7 or 0xFF or >= 0x60 and <= 0x7F or >= 0xD0 and <= 0xD3 or >= 0xD8 and <= 0xDF)
            {
                _instructionPrefixLength = 1;
                _instructionPrefix = opcode;
                opcode = prefixedOpcode;
            }
            else
            {
                throw new InvalidOperationException("W tym etapie prefiks segmentowy jest obsługiwany z XLAT, MOV, XCHG, C6 i FE.");
            }
        }

        if (opcode is not 0x90 and not 0x98 and not 0x99 and not 0x9B and not 0x9C and not 0x9D and not 0xD4 and not 0xD5 and not 0xD6 and not 0x27 and not 0x2F and not 0x37 and not 0x3F and not (>= 0x40 and <= 0x5F) and not (>= 0x60 and <= 0x7F) and not (>= 0xA0 and <= 0xA3) and not (>= 0xB0 and <= 0xBF) and not 0xEB and not 0xE9 and not 0xEA and not 0xC0 and not 0xC1 and not 0xC2 and not 0xC3 and not 0xC4 and not 0xC5 and not 0xC8 and not 0xC9 and not 0xCA and not 0xCB and not 0x9A and not 0x9E and not 0x9F and not 0xF4 and not 0xF5 and not 0xF8 and not 0xF9 and not 0xFA and not 0xFB and not 0xFC and not 0xFD and not 0xCC and not 0xCD and not 0xCE and not 0xCF and not 0xE8 and not 0xE0 and not 0xE1 and not 0xE2 and not 0xE3 and not 0x80 and not 0x82 and not 0x86 and not 0x87 and not 0x88 and not 0x8A and not 0x89 and not 0x8B and not 0x8C and not 0x8D and not 0x8E and not 0x8F and not 0xD7 and not 0xC6 and not 0xC7 && !IsAccumulatorExchangeOpcode(opcode) && !IsAccumulatorImmediateAluOpcode(opcode) && !IsAccumulatorImmediateWordAluOpcode(opcode) && !IsRegisterDirectAluOpcode(opcode) && !IsWordAluModRmOpcode(opcode) && !IsWordAluImmediateGroupOpcode(opcode) && !IsByteIncDecGroupOpcode(opcode) && !IsWordIncDecGroupOpcode(opcode) && !IsUnaryNotNegGroupOpcode(opcode) && !IsShiftRotateGroupOpcode(opcode) && !IsEscOpcode(opcode) && !IsMoveStringOpcode(opcode) && !IsStoreStringOpcode(opcode) && !IsLoadStringOpcode(opcode) && !IsScanStringOpcode(opcode) && !IsCompareStringOpcode(opcode) && !IsByteInputIoOpcode(opcode) && !IsByteOutputIoOpcode(opcode) && !IsWordInputIoOpcode(opcode) && !IsWordOutputIoOpcode(opcode) && !IsSegmentStackOpcode(opcode))
        {
            throw new InvalidOperationException($"Opcode {opcode:X2} is not implemented yet.");
        }

        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = opcode;
        _stringRepeatZeroCount = _instructionPrefix is 0xF2 or 0xF3 && opcode is >= 0xA4 and <= 0xAF && _state.CX == 0;
        int initialQueueCount = _state.QueueCount;
        _stringSegmentPrefixShortQueue = _stringSegmentPrefix && initialQueueCount <= 5;
        bool hasCombinedSegmentPrefix = _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 2;
        bool hasSegmentPrefixedBranch = _segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 1 && IsConditionalBranchOpcode(opcode);
        bool hasSegmentPrefixedEsc = _segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength == 1 && IsEscOpcode(opcode) && _state.QueueCount >= 3 &&
            (_state.Queue[_state.QueueHead + 2] >> 6) == 3;
        bool combinedStringPrefix = _stringSegmentPrefix && _instructionPrefixLength != 0;
        _queueCountAtStart = hasSegmentPrefixedEsc
            ? initialQueueCount - 1
            : (combinedStringPrefix || hasCombinedSegmentPrefix || hasSegmentPrefixedBranch) && initialQueueCount == 5
                ? 6
                : initialQueueCount - (_stringSegmentPrefix || hasCombinedSegmentPrefix || hasSegmentPrefixedBranch ? 1 : 0);
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + initialQueueCount));
        if (opcode is >= 0xA0 and <= 0xA3)
        {
            _moffsTransfer = true;
            if (_state.QueueCount < _moffsPrefixLength + 3)
            {
                throw new InvalidOperationException("MOV accumulator,moffs wymaga obu bajtów adresu w kolejce prefetch.");
            }

            int addressIndex = _moffsPrefixLength + 1;
            _moffsOffset = (ushort)(ReadInstructionByteForDecode(addressIndex) |
                (ReadInstructionByteForDecode(addressIndex + 1) << 8));
            ushort segment = _moffsSegmentOverride switch
            {
                Intel8086SegmentOverride.None => _state.DS,
                Intel8086SegmentOverride.Es => _state.ES,
                Intel8086SegmentOverride.Cs => _state.CS,
                Intel8086SegmentOverride.Ss => _state.SS,
                Intel8086SegmentOverride.Ds => _state.DS,
                _ => throw new InvalidOperationException("Nieznany segment MOV moffs."),
            };
            _moffsSegment = segment;
            _moffsPhysicalAddress = PhysicalAddress(segment, _moffsOffset);
            bool word = opcode is 0xA1 or 0xA3;
            _moffsBhe = word ? (byte)0 : (byte)((_moffsPhysicalAddress & 1) == 0 ? 1 : 0);
            bool store = opcode is 0xA2 or 0xA3;
            _moffsMemoryStart = store
                ? 9 + _moffsPrefixLength * 2
                : 7 + _moffsPrefixLength * 2;
            bool splitWord = word && (_moffsPhysicalAddress & 1) != 0;
            int busCycles = store ? splitWord ? 7 : 3 : 4;
            if (splitWord && !store)
            {
                busCycles += 4;
            }

            _instructionCycles = _moffsMemoryStart + busCycles - 1;
        }
        if (opcode is >= 0xB8 and <= 0xBF || IsAccumulatorImmediateWordAluOpcode(opcode))
        {
            if (_state.QueueCount < 3)
            {
                throw new InvalidOperationException("A word immediate instruction requires all instruction bytes in the prefetch queue.");
            }

            _immediateWord = (ushort)(_state.Queue[_state.QueueHead + 1] | (_state.Queue[_state.QueueHead + 2] << 8));
        }

        if (opcode is 0xD4 or 0xD5)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("AAM/AAD requires its immediate radix in the prefetch queue.");
            }

            _immediateWord = _state.Queue[_state.QueueHead + 1];
        }

        if (opcode == 0xE7)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("OUT imm8,AX wymaga bajtu numeru portu w kolejce.");
            }

            _immediateWord = _state.Queue[_state.QueueHead + 1];
            _instructionCycles = (byte)_immediateWord % 2 == 0 ? 11 : 15;
        }
        else if (opcode == 0xEF)
        {
            _instructionCycles = (_queueCountAtStart > 5 ? 8 : 9) + ((_state.DX & 1) == 0 ? 0 : 4);
        }
        else if (opcode == 0xE5)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("IN AX,imm8 wymaga bajtu numeru portu w kolejce.");
            }

            _immediateWord = _state.Queue[_state.QueueHead + 1];
            _instructionCycles = (byte)_immediateWord % 2 == 0 ? 10 : 14;
        }
        else if (opcode == 0xED)
        {
            _instructionCycles = (_state.DX & 1) == 0 ? 8 : 12;
        }
        else if (opcode == 0xE4)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("IN AL,imm8 wymaga bajtu numeru portu w kolejce.");
            }

            _immediateWord = _state.Queue[_state.QueueHead + 1];
            _instructionCycles = 10;
        }
        else if (opcode == 0xEC)
        {
            _instructionCycles = 8;
        }
        else if (opcode == 0xE6)
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException("OUT imm8,AL wymaga bajtu numeru portu w kolejce.");
            }

            _immediateWord = _state.Queue[_state.QueueHead + 1];
            _instructionCycles = 11;
        }
        else if (opcode == 0xEE)
        {
            _instructionCycles = _queueCountAtStart > 5 ? 8 : 9;
        }

        if (IsEscOpcode(opcode))
        {
            DecodeAdditionalGroupOperands(opcode);
        }

        if (IsWordAluImmediateGroupOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException("Word ALU immediate form requires ModR/M and immediate bytes in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            _memoryModRm = modRm;
            if ((modRm >> 6) == 3)
            {
                int requiredQueueCount = (opcode == 0x81 ? 4 : 3) + prefixOffset;
                if (_state.QueueCount < requiredQueueCount)
                {
                    throw new InvalidOperationException("Word ALU immediate form requires ModR/M and immediate bytes in the prefetch queue.");
                }

                byte immediateLow = _state.Queue[_state.QueueHead + 2 + prefixOffset];
                _immediateWord = opcode == 0x81
                    ? (ushort)(immediateLow | (_state.Queue[_state.QueueHead + 3 + prefixOffset] << 8))
                    : unchecked((ushort)(short)(sbyte)immediateLow);
            }
        }

        if (opcode is 0x89 or 0x8B)
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException("MOV r/m16,r16 requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            _memoryModRm = modRm;
        }

        if (IsRegisterDirectAluOpcode(opcode))
        {
            if (_state.QueueCount < 2)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            InitializeAluMemoryTransfer();
        }
        if (IsAnyWordAluModRmOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            _memoryModRm = modRm;
            int mod = modRm >> 6;
            if (mod != 3)
            {
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < 2 + prefixOffset + displacementBytes)
                {
                    throw new InvalidOperationException("Word ALU memory form requires its displacement bytes in the prefetch queue.");
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
                    _ => throw new InvalidOperationException("Invalid word ALU ModR/M register field."),
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
                if (IsWordAluImmediateGroupOpcode(opcode))
                {
                    int immediateIndex = 2 + prefixOffset + displacementBytes;
                    if (_state.QueueCount >= immediateIndex + (opcode == 0x81 ? 2 : 1))
                    {
                        byte immediateLow = _state.Queue[_state.QueueHead + immediateIndex];
                        _immediateWord = opcode == 0x81
                            ? (ushort)(immediateLow | (_state.Queue[_state.QueueHead + immediateIndex + 1] << 8))
                            : unchecked((ushort)(short)(sbyte)immediateLow);
                    }
                }
            }
        }

        if (IsWordAluImmediateGroupOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException("Word ALU immediate form requires the ModR/M byte in the prefetch queue.");
            }

            _memoryModRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
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
                throw new InvalidOperationException("Only F6/F7 /0-/7 forms covered by this core are implemented.");
            }

            _memoryModRm = modRm;
            int mod = modRm >> 6;
            int displacementBytes = mod == 3 ? 0 : mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
            if (IsTestImmediateGroupOpcode(opcode, modRm))
            {
                int immediateIndex = prefixOffset + 2 + displacementBytes;
                byte immediateLow = ReadInstructionByteForDecode(immediateIndex);
                _immediateWord = opcode == 0xF6
                    ? immediateLow
                    : (ushort)(immediateLow | (ReadInstructionByteForDecode(immediateIndex + 1) << 8));
            }

            if (mod != 3)
            {
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
                if (IsMemoryDivideOpcode(opcode, modRm))
                {
                    _memoryDivideQueueData = displacementBytes == 0
                        ? modRm
                        : combinedSegmentRepeatPrefix
                            ? ReadInstructionByteForDecode(1 + prefixOffset + displacementBytes)
                            : _state.Queue[_state.QueueHead + 1 + prefixOffset + displacementBytes];
                }

                Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(modRm, in _state, displacement, _segmentOverride);
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                if (prefixOffset != 0)
                {
                    _memoryReadStart = GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart);
                }
                else
                {
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

            int mod = modRm >> 6;
            if (mod != 3)
            {
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
                _memoryAddress = effectiveAddress.PhysicalAddress;
                int rm = modRm & 7;
                _memorySegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
                if (_instructionPrefixLength != 0)
                {
                    _memoryReadStart = GetPrefixedMemoryReadStart(mod, rm, _queueCountAtStart);
                }
                else
                {
                    int effectiveAddressCycles = rm switch
                    {
                        0 or 3 => 7,
                        1 or 2 => 8,
                        4 or 5 or 7 => 5,
                        6 => mod == 0 ? 6 : 5,
                        _ => throw new InvalidOperationException("Invalid FE ModR/M register field."),
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

        if (IsShiftRotateGroupOpcode(opcode))
        {
            int prefixOffset = _instructionPrefixLength;
            if (_state.QueueCount < 2 + prefixOffset)
            {
                throw new InvalidOperationException($"Opcode {opcode:X2} requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            if (!IsSupportedShiftRotate(opcode, modRm))
            {
                throw new InvalidOperationException("Only D0-D3 /0-/5,/7 forms are implemented.");
            }

            _memoryModRm = modRm;
            _shiftCount = IsShiftRotateClGroupOpcode(opcode) ? (byte)_state.CX : (byte)1;
            int mod = modRm >> 6;
            if (mod != 3)
            {
                int displacementBytes = mod == 0 ? (modRm & 7) == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
                if (_state.QueueCount < 2 + prefixOffset + displacementBytes)
                {
                    throw new InvalidOperationException("D0-D3 memory form requires its displacement bytes in the prefetch queue.");
                }

                ushort displacement = displacementBytes switch
                {
                    0 => 0,
                    1 => _state.Queue[_state.QueueHead + prefixOffset + 2],
                    _ => (ushort)(_state.Queue[_state.QueueHead + prefixOffset + 2] | (_state.Queue[_state.QueueHead + prefixOffset + 3] << 8)),
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
                    _ => throw new InvalidOperationException("Invalid D0-D3 ModR/M register field."),
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
                throw new InvalidOperationException("FF requires the ModR/M byte in the prefetch queue.");
            }

            byte modRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
            int extension = (modRm >> 3) & 7;
            if (extension is 2 or 3)
            {
                InitializeIndirectCall(modRm, extension == 3);
            }
            else if (extension is 4 or 5)
            {
                InitializeIndirectJump(modRm, extension == 5);
            }
            else if (extension is not 0 and not 1 and not 6 and not 7)
            {
                throw new InvalidOperationException("Only FF /0-/7 forms are implemented.");
            }

            _memoryModRm = modRm;
            int mod = modRm >> 6;
            if (!_indirectCallTransfer && !_indirectJumpTransfer && mod != 3)
            {
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

        if (opcode is 0x80 or 0x82)
        {
            InitializeAluGroupImmediateRegisterTransfer();
        }
        if (opcode is 0x86 or 0x87)
        {
            _xchgWordTransfer = opcode == 0x87;
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
                _xchgMemoryReadStart = _xchgWordTransfer
                    ? GetXchgWordMemoryReadStart(mod, rm, _queueCountAtStart, _xchgPrefixLength == 1, (_xchgPhysicalAddress & 1) != 0)
                    : GetXchgMemoryReadStart(mod, rm, _queueCountAtStart, _xchgPrefixLength == 1);
                _xchgPrefetchBytesRemaining = GetXchgPrefetchBytes(_queueCountAtStart, mod, _xchgPrefixLength, _xchgDisplacementBytes);
                if (_xchgWordTransfer)
                {
                    _xchgRegisterWordValue = GetWordRegister((_xchgModRm >> 3) & 7);
                    _xchgRegisterValue = (byte)_xchgRegisterWordValue;
                }
                else
                {
                    _xchgRegisterValue = GetByteRegister((_xchgModRm >> 3) & 7);
                }
                int wordAlignmentPenalty = _xchgWordTransfer && (_xchgPhysicalAddress & 1) != 0 ? 8 : 0;
                _instructionCycles = _xchgMemoryReadStart + 14 + wordAlignmentPenalty;
            }
            else
            {
                _xchgRegisterTransfer = true;
                _xchgRegisterWordValue = GetWordRegister((_xchgModRm >> 3) & 7);
                _xchgRegisterValue = (byte)_xchgRegisterWordValue;
                _instructionCycles = _xchgPrefixLength == 0 ? 4 : 6;
            }
        }

        if (opcode is 0x88 or 0x89 or 0x8A or 0x8B)
        {
            int prefixOffset = _movMemoryPrefixLength != 0 ? _movMemoryPrefixLength : _instructionPrefixLength;
            if (_state.QueueCount < prefixOffset + 2)
            {
                throw new InvalidOperationException("MOV r/m8,r8 requires the ModR/M byte in the prefetch queue.");
            }

            int modRmQueueIndex = _state.QueueHead + prefixOffset + 1;
            byte modRm = _state.Queue[modRmQueueIndex];
            _memoryModRm = modRm;
            _movMemoryWordTransfer = opcode is 0x89 or 0x8B;
            _movMemoryTransfer = (modRm >> 6) != 3;
            if (_movMemoryTransfer)
            {
                _movMemoryModRm = modRm;
                _movMemoryWordValue = opcode == 0x89 ? GetWordRegister((modRm >> 3) & 7) : (ushort)0;
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
                int timingOpcode = opcode switch
                {
                    0x89 => 0x88,
                    0x8B => 0x8A,
                    _ => opcode,
                };
                _movMemoryBusStart = GetMovMemoryBusStart(timingOpcode, mod, rm, _movMemoryDisplacementBytes, _queueCountAtStart);
                bool wordOddAddress = _movMemoryWordTransfer && (_movMemoryPhysicalAddress & 1) != 0;
                _instructionCycles = timingOpcode == 0x88
                    ? _movMemoryBusStart + (wordOddAddress ? 6 : 2)
                    : _movMemoryBusStart + (wordOddAddress ? 9 : 5);
                _movMemoryPrefetchBytesRemaining = _movMemoryPrefixLength == 1
                    ? GetPrefixedMovMemoryQueuePrefetchBytes(timingOpcode, mod, rm, _queueCountAtStart, _movMemoryDisplacementBytes)
                    : 0;
            }
        }

        if (opcode == 0xC6)
        {
            InitializeMovImmediate();
        }

        if (opcode == 0xC7)
        {
            InitializeMovImmediateWord();
        }

        if (opcode is 0x8C or 0x8E)
        {
            InitializeMovSegmentRegisterTransfer(opcode);
        }

        if (opcode is 0xC4 or 0xC5)
        {
            InitializeLesLds(opcode);
        }

        if (opcode == 0x8F)
        {
            InitializePopRm16();
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

        _instructionCycles = _moffsTransfer || _xchgRegisterTransfer || _xchgMemoryTransfer || _movMemoryTransfer || _movSegmentTransfer || _lesLdsTransfer || _popRm16Transfer || _indirectCallTransfer || _indirectJumpTransfer || _movImmediateTransfer || _movImmediateWordTransfer || _aluMemoryTransfer || _aluGroupImmediateRegisterTransfer || _aluGroupImmediateMemoryTransfer ? _instructionCycles : opcode == 0x90 || IsAccumulatorExchangeOpcode(opcode) || IsRegisterDirectAluOpcode(opcode) || IsRegisterDirectWordAluOpcode(opcode) || IsByteIncDecGroupOpcode(opcode) || IsWordIncDecGroupOpcode(opcode) || IsUnaryNotNegGroupOpcode(opcode) ? 3 : opcode is >= 0x40 and <= 0x4F ? 2 : opcode == 0x8D
            ? 2 + _leaEffectiveAddressCycles + ((_memoryModRm >> 6) == 0 ? 0 : 4)
            : opcode == 0xD7 ? (_xlatPrefixLength == 0 ? 17 - _queueCountAtStart : 8 + _queueCountAtStart)
            : 4;
        if (opcode == 0xD6)
        {
            _instructionCycles = 3 + (_state.Flags & 0x0001) + (_instructionPrefixLength == 0 ? 0 : 2);
        }
        if (opcode is 0xF4 or 0x9B)
        {
            _instructionCycles = opcode == 0xF4 ? 2 : 3;
        }

        if (opcode == 0xE7)
        {
            _instructionCycles = (byte)_immediateWord % 2 == 0 ? 11 : 15;
        }
        else if (opcode == 0xEF)
        {
            _instructionCycles = (_queueCountAtStart > 5 ? 8 : 9) + ((_state.DX & 1) == 0 ? 0 : 4);
        }
        else if (opcode == 0xE5)
        {
            _instructionCycles = (byte)_immediateWord % 2 == 0 ? 10 : 14;
        }
        else if (opcode == 0xED)
        {
            _instructionCycles = (_state.DX & 1) == 0 ? 8 : 12;
        }
        else if (opcode == 0xE4)
        {
            _instructionCycles = 10;
        }
        else if (opcode == 0xEC)
        {
            _instructionCycles = 8;
        }
        else if (opcode == 0xE6)
        {
            _instructionCycles = 11;
        }
        else if (opcode == 0xEE)
        {
            _instructionCycles = _queueCountAtStart > 5 ? 8 : 9;
        }
        if (opcode == 0xD4)
        {
            _instructionCycles = GetAamCycles(_state.AX, (byte)_immediateWord);
        }
        else if (opcode == 0xD5)
        {
            _instructionCycles = 59 + System.Numerics.BitOperations.PopCount((uint)(byte)_immediateWord);
        }
        else if (opcode == 0x99)
        {
            _instructionCycles = 5 + ((_state.AX & 0x8000) == 0 ? 0 : 1);
        }

        if (opcode is 0x27 or 0x2F)
        {
            _instructionCycles = 4;
        }
        else if (opcode is 0x37 or 0x3F)
        {
            _instructionCycles = (((byte)_state.AX & 0x0F) > 9 || (_state.Flags & 0x0010) != 0) ? 8 : 9;
        }

        if (IsRegisterDirectMultiplyOpcode(opcode, _memoryModRm))
        {
            _instructionCycles = GetRegisterDirectMultiplyCycles(opcode, _memoryModRm);
            if (_segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 1)
            {
                _instructionCycles += 2;
            }
        }

        if (IsRegisterDirectDivideOpcode(opcode, _memoryModRm))
        {
            _instructionCycles = IsRegisterDirectUnsignedDivideOpcode(opcode, _memoryModRm)
                ? GetRegisterDirectUnsignedDivideCycles(opcode, _memoryModRm)
                : GetRegisterDirectSignedDivideCycles(opcode, _memoryModRm);
            if (_instructionPrefixLength != 0 && IsRegisterDirectSignedDivideOpcode(opcode, _memoryModRm))
            {
                _instructionCycles += 2;
            }
            else if (_segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength == 1)
            {
                _instructionCycles += 2;
            }
        }

        if (IsEscOpcode(opcode))
        {
            _instructionCycles = (_memoryModRm >> 6) == 3
                ? _instructionPrefixLength == 0 ? 2 : 4
                : _memoryReadStart + ((_memoryAddress & 1) == 0 ? 5 : 9);
        }

        if (IsFlagControlOpcode(opcode))
        {
            _instructionCycles = opcode == 0x9E ? 4 : 2;
        }

        if ((opcode is 0x88 or 0x8A) && !_movMemoryTransfer)
        {
            _instructionCycles = _segmentOverride != Intel8086SegmentOverride.None && _instructionPrefixLength != 0 ? 4 : 2;
        }

        if ((opcode is 0x89 or 0x8B) && _segmentOverride != Intel8086SegmentOverride.None &&
            _instructionPrefixLength != 0 && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = 4;
        }

        if (IsByteIncDecGroupOpcode(opcode) && (_memoryModRm >> 6) != 3)
        {
            _instructionCycles = _memoryReadStart + 12;
        }

        if (IsByteIncDecGroupOpcode(opcode) && _instructionPrefixLength != 0 && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = 5;
        }

        if (IsGroupPushRm16(opcode, _memoryModRm) && (_memoryModRm >> 6) != 3)
        {
            uint stackAddress = PhysicalAddress(_state.SS, unchecked((ushort)(_state.SP - 2)));
            int stackWriteStart = _memoryReadStart + ((_memoryAddress & 1) == 0 ? 11 : 15);
            _instructionCycles = stackWriteStart + ((stackAddress & 1) == 0 ? 2 : 6);
        }
        else if (!_indirectCallTransfer && !_indirectJumpTransfer && IsWordIncDecGroupOpcode(opcode) && (_memoryModRm >> 6) != 3)
        {
            _instructionCycles = _memoryReadStart + ((_memoryAddress & 1) == 0 ? 12 : 20);
        }

        if (!_indirectCallTransfer && !_indirectJumpTransfer && IsWordIncDecGroupOpcode(opcode) && _instructionPrefixLength != 0 && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = 5;
        }

        if (IsUnaryNotNegGroupOpcode(opcode) && _instructionPrefixLength != 0 &&
            (_memoryModRm >> 6) == 3 && ((_memoryModRm >> 3) & 7) is 2 or 3)
        {
            _instructionCycles = 5;
        }

        if (IsTestImmediateGroupOpcode(opcode, _memoryModRm) && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = _instructionPrefixLength == 0 ? 5 : 7;
        }

        if (IsTestImmediateGroupOpcode(opcode, _memoryModRm) && (_memoryModRm >> 6) != 3)
        {
            _instructionCycles = _memoryReadStart + (opcode == 0xF7 && (_memoryAddress & 1) != 0 ? 12 : 8);
        }
        else if (opcode == 0xF6 && (_memoryModRm >> 6) != 3)
        {
            _instructionCycles = _memoryReadStart + 12;
        }

        if (opcode == 0xF7 && !IsTestImmediateGroupOpcode(opcode, _memoryModRm) && (_memoryModRm >> 6) != 3)
        {
            _instructionCycles = _memoryReadStart + ((_memoryAddress & 1) == 0 ? 12 : 20);
        }

        if (IsShiftRotateGroupOpcode(opcode))
        {
            if ((_memoryModRm >> 6) == 3)
            {
                _instructionCycles = IsShiftRotateClGroupOpcode(opcode)
                    ? _shiftCount == 0 ? 8 : 8 + (4 * _shiftCount)
                    : 2;
                if (_instructionPrefixLength != 0)
                {
                    _instructionCycles += 2;
                }
            }
            else
            {
                int operandCycles = opcode is 0xD0 or 0xD2 || (_memoryAddress & 1) == 0 ? 12 : 20;
                _instructionCycles = _memoryReadStart + operandCycles + (IsShiftRotateClGroupOpcode(opcode) ? 5 + (4 * _shiftCount) : 0);
            }
        }

        if (IsWordAluModRmOpcode(opcode) && (_memoryModRm >> 6) != 3)
        {
            bool splitWordTransfer = (_memoryAddress & 1) != 0;
            _instructionCycles = _memoryReadStart + (IsWordAluMemoryReadOnly(opcode)
                ? splitWordTransfer ? 10 : 6
                : splitWordTransfer ? 21 : 13);
        }

        if (IsWordAluImmediateGroupOpcode(opcode) && (_memoryModRm >> 6) != 3)
        {
            int mod = _memoryModRm >> 6;
            bool splitWordTransfer = (_memoryAddress & 1) != 0;
            int extension = (_memoryModRm >> 3) & 7;
            int trailingCycles;
            if (extension == 7)
            {
                trailingCycles = 8 + (splitWordTransfer ? 4 : 0);
            }
            else
            {
                int queueAdjustment = opcode == 0x81 && _instructionPrefixLength == 1 && _segmentOverride != Intel8086SegmentOverride.None
                    ? mod switch
                    {
                        0 when _queueCountAtStart == 5 => 1,
                        1 when _queueCountAtStart == 6 => 1,
                        2 when _queueCountAtStart == 5 => 1,
                        _ => 0,
                    }
                    : mod switch
                    {
                        0 when _queueCountAtStart == 6 => 1,
                        1 when opcode == 0x81 && _queueCountAtStart == 5 => 1,
                        2 when opcode == 0x81 && _queueCountAtStart == 6 => 1,
                        _ => 0,
                    };
                trailingCycles = (opcode == 0x81 ? 14 + queueAdjustment : 14) +
                    (splitWordTransfer ? 8 : 0);
            }

            _instructionCycles = _memoryReadStart + trailingCycles;
        }

        if (IsWordAluImmediateGroupOpcode(opcode) && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = 4;
        }

        if (IsWordAluImmediateGroupOpcode(opcode) && _instructionPrefixLength != 0 && (_memoryModRm >> 6) == 3)
        {
            _instructionCycles = GetPrefixedWordImmediateRegisterCycleCount();
        }

        if (IsMoveStringOpcode(opcode))
        {
            _stringSourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
            _stringDestinationAddress = PhysicalAddress(_state.ES, _state.DI);
            _stringReadStart = _queueCountAtStart <= 5 ? 7 : 6;
            bool splitWordRead = opcode == 0xA5 && (_stringSourceAddress & 1) != 0;
            bool splitWordWrite = opcode == 0xA5 && (_stringDestinationAddress & 1) != 0;
            _stringWriteStart = _stringReadStart + (splitWordRead ? 11 : 7);
            _instructionCycles = _stringWriteStart + (splitWordWrite ? 9 : 5);
        }

        if (IsStoreStringOpcode(opcode))
        {
            _stringDestinationAddress = PhysicalAddress(_state.ES, _state.DI);
            _stringWriteStart = opcode == 0xAB
                ? (_queueCountAtStart <= 5 ? 7 : 6)
                : _stringSegmentPrefix ? 9 : (_queueCountAtStart <= 5 ? 7 : 6);
            bool splitWordWrite = opcode == 0xAB && (_stringDestinationAddress & 1) != 0;
            _instructionCycles = _stringWriteStart + (splitWordWrite ? 9 : 5);
        }

        if (IsLoadStringOpcode(opcode))
        {
            _stringSourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
            _stringReadStart = _queueCountAtStart <= 5 ? 7 : 6;
            bool splitWordRead = opcode == 0xAD && (_stringSourceAddress & 1) != 0;
            _instructionCycles = _stringReadStart + (splitWordRead ? 10 : 6);
        }

        if (IsScanStringOpcode(opcode))
        {
            _stringSourceAddress = PhysicalAddress(_state.ES, _state.DI);
            _stringReadStart = _queueCountAtStart <= 5 ? 9 : 8;
            bool splitWordRead = opcode == 0xAF && (_stringSourceAddress & 1) != 0;
            _instructionCycles = _stringReadStart + (splitWordRead ? 11 : 7);
        }

        if (IsCompareStringOpcode(opcode))
        {
            _stringSourceAddress = PhysicalAddress(GetStringSourceSegment(), _state.SI);
            _stringDestinationAddress = PhysicalAddress(_state.ES, _state.DI);
            _stringReadStart = 7;
            _stringSecondReadStart = opcode == 0xA6 || (_stringSourceAddress & 1) == 0 ? 15 : 19;
            bool splitDestination = opcode == 0xA7 && (_stringDestinationAddress & 1) != 0;
            _instructionCycles = _stringSecondReadStart + (splitDestination ? 11 : 7);
        }

        if (_instructionPrefix is 0xF2 or 0xF3 && opcode is >= 0xA4 and <= 0xAF && _state.CX == 0)
        {
            _instructionCycles = 9;
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAE)
        {
            _instructionCycles = 11 + (15 * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xA4)
        {
            _instructionCycles = 11 + (17 * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAA)
        {
            _instructionCycles = 11 + (10 * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAC)
        {
            _instructionCycles = 11 + (13 * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAD)
        {
            uint address = PhysicalAddress(_state.DS, _state.SI);
            _instructionCycles = 11 + (((address & 1) == 0 ? 13 : 17) * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAB)
        {
            uint address = PhysicalAddress(_state.ES, _state.DI);
            _instructionCycles = 11 + (((address & 1) == 0 ? 10 : 14) * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xAF)
        {
            uint address = PhysicalAddress(_state.ES, _state.DI);
            _instructionCycles = 11 + (((address & 1) == 0 ? 15 : 19) * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xA6)
        {
            _instructionCycles = 11 + (22 * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xA7)
        {
            uint sourceAddress = PhysicalAddress(_state.DS, _state.SI);
            uint destinationAddress = PhysicalAddress(_state.ES, _state.DI);
            int iterationCycles = 22 + ((sourceAddress & 1) != 0 ? 4 : 0) + ((destinationAddress & 1) != 0 ? 4 : 0);
            _instructionCycles = 11 + (iterationCycles * _state.CX);
        }
        else if (_instructionPrefix is 0xF2 or 0xF3 && opcode == 0xA5)
        {
            uint sourceAddress = PhysicalAddress(_state.DS, _state.SI);
            uint destinationAddress = PhysicalAddress(_state.ES, _state.DI);
            int iterationCycles = 17 + ((sourceAddress & 1) != 0 ? 4 : 0) + ((destinationAddress & 1) != 0 ? 4 : 0);
            _instructionCycles = 11 + (iterationCycles * _state.CX);
        }

        if (IsConditionalBranchOpcode(opcode))
        {
            InitializeConditionalBranch(opcode);
        }

        if (IsJumpOpcode(opcode))
        {
            InitializeJump(opcode);
        }

        if (IsStackTransferOpcode(opcode) ||
            IsGroupPushRm16(opcode, _memoryModRm) && (_memoryModRm >> 6) == 3)
        {
            InitializeRegisterStackInstruction();
        }

        if (IsReturnOpcode(opcode))
        {
            InitializeReturn(opcode);
        }

        if (IsLoopOpcode(opcode))
        {
            InitializeLoop(opcode);
        }

        if (opcode == 0xCF)
        {
            InitializeInterruptReturn();
        }

        if (IsSoftwareInterruptOpcode(opcode))
        {
            InitializeSoftwareInterrupt(opcode);
        }

        if (opcode == 0xD4 && (byte)_immediateWord == 0)
        {
            _memoryDivideQueueData = 0;
            _state.Flags = (ushort)((_state.Flags & 0xF400) | 0x0046);
            InitializeDivideError(18);
            _divideErrorPrefetchStart = _queueCountAtStart <= 5 ? 3 : 5;
        }

        if (_stringSegmentPrefix)
        {
            if (_instructionPrefixLength != 0)
            {
                _instructionCycles += 2;
            }
            else
            {
                bool splitMovswDestination = Opcode == 0xA5 && (_stringDestinationAddress & 1) != 0;
                if (Opcode == 0xA7 && _stringSegmentPrefixShortQueue)
                {
                    _stringSecondReadStart = (_stringSourceAddress & 1) != 0 ? 21 : 17;
                    _instructionCycles = _stringSecondReadStart + ((_stringDestinationAddress & 1) != 0 ? 11 : 7);
                }
                else if (splitMovswDestination && _stringSegmentPrefixShortQueue)
                {
                    _stringWriteStart += 2;
                    _instructionCycles += 2;
                }
                else if (splitMovswDestination)
                {
                    _instructionCycles += 2;
                }
                else if (Opcode != 0xAA)
                {
                    _instructionCycles += IsScanStringOpcode(Opcode) ? (_stringSegmentPrefixShortQueue ? 1 : 2) : 2;
                }

                if (_stringSegmentPrefixShortQueue)
                {
                    _stringReadStart += IsScanStringOpcode(Opcode) ? 1 : 2;
                    if (!splitMovswDestination && Opcode != 0xAA)
                    {
                        _stringWriteStart += 2;
                    }

                    if (Opcode != 0xA7)
                    {
                        _stringSecondReadStart += 2;
                    }
                }
                else if (Opcode == 0xAA)
                {
                    _stringWriteStart -= 2;
                }
            }
        }

        if (_lockPrefix && !_lockStringPrefix && opcode is not (0xFE or 0xFF or 0xF6 or 0xF7 or 0x81 or 0x83))
        {
            _instructionCycles += 2;
        }
    }

}
