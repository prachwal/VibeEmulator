namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void InitializeAluGroupImmediateRegisterTransfer()
    {
        _aluGroupImmediatePrefixLength = _instructionPrefixLength;
        int prefixOffset = _aluGroupImmediatePrefixLength;
        if (_state.QueueCount < prefixOffset + 3)
        {
            throw new InvalidOperationException("Opcode 80 requires ModR/M and immediate bytes in the prefetch queue.");
        }

        _aluGroupImmediateModRm = _state.Queue[_state.QueueHead + 1 + prefixOffset];
        _memoryModRm = _aluGroupImmediateModRm;
        int mod = _aluGroupImmediateModRm >> 6;
        int rm = _aluGroupImmediateModRm & 7;
        AluGroupImmediatePrefixProfile? prefixProfile = prefixOffset == 0
            ? null
            : GetAluGroupImmediatePrefixProfile(_aluGroupImmediateModRm, _queueCountAtStart);
        if (mod == 3)
        {
            _aluGroupImmediateRegisterTransfer = true;
            _instructionCycles = prefixProfile?.Cycles ?? 4;
            if (prefixProfile is AluGroupImmediatePrefixProfile registerProfile)
            {
                int registerInstructionLength = prefixOffset + 3;
                _aluGroupImmediatePrefetchBytesRemaining = Math.Max(
                    0,
                    registerProfile.FinalQueueCount + registerInstructionLength - _queueCountAtStart);
            }

            return;
        }

        _aluGroupImmediateMemoryTransfer = true;
        _aluGroupImmediateDisplacementBytes = mod == 0 ? rm == 6 ? 2 : 0 : mod == 1 ? 1 : 2;
        ushort displacement = _aluGroupImmediateDisplacementBytes switch
        {
            0 => 0,
            1 => _state.Queue[_state.QueueHead + 2 + prefixOffset],
            _ => (ushort)(_state.Queue[_state.QueueHead + 2 + prefixOffset] | (_state.Queue[_state.QueueHead + 3 + prefixOffset] << 8)),
        };
        Intel8086EffectiveAddress effectiveAddress = Intel8086EffectiveAddressDecoder.Decode(_aluGroupImmediateModRm, in _state, displacement, _segmentOverride);
        _aluGroupImmediatePhysicalAddress = effectiveAddress.PhysicalAddress;
        _memoryAddress = effectiveAddress.PhysicalAddress;
        _aluGroupImmediateSegmentStatus = GetMemorySegmentStatus(rm, mod, _segmentOverride);
        _aluGroupImmediateBhe = (byte)((_aluGroupImmediatePhysicalAddress & 1) == 0 ? 1 : 0);
        _aluGroupImmediateWrites = ((_aluGroupImmediateModRm >> 3) & 7) != 7;
        _aluGroupImmediateDisplacementStart = mod == 0 && rm == 6
            ? 4
            : mod == 0 ? int.MaxValue : GetMovEffectiveAddressCycles(mod, rm) + 1;
        _aluGroupImmediateDisplacementStart += prefixOffset * 2;
        _aluGroupImmediateReadStart = prefixProfile is AluGroupImmediatePrefixProfile memoryProfile
            ? memoryProfile.MemoryReadStart
            : GetMovMemoryBusStart(0x8A, mod, rm, _aluGroupImmediateDisplacementBytes, _queueCountAtStart);
        _memoryReadStart = _aluGroupImmediateReadStart;
        _aluGroupImmediateWriteStart = prefixProfile is AluGroupImmediatePrefixProfile writeProfile
            ? writeProfile.MemoryWriteStart
            : _aluGroupImmediateReadStart + 12;
        _aluGroupImmediateValueState = _aluGroupImmediateReadStart + 6;
        _instructionCycles = prefixProfile?.Cycles ?? (_aluGroupImmediateWrites ? _aluGroupImmediateWriteStart + 2 : _aluGroupImmediateReadStart + 8);
        int finalQueueCount = prefixProfile?.FinalQueueCount ?? GetAluGroupImmediateFinalQueueCount(mod, _queueCountAtStart, _aluGroupImmediateWrites);
        int instructionLength = prefixOffset + 3 + _aluGroupImmediateDisplacementBytes;
        _aluGroupImmediatePrefetchBytesRemaining = prefixProfile is AluGroupImmediatePrefixProfile
            ? Math.Max(0, finalQueueCount + instructionLength - _queueCountAtStart)
            : Math.Max(0, finalQueueCount - Math.Max(0, _queueCountAtStart - instructionLength));
        _aluGroupImmediateThirdPrefetchStart = prefixProfile?.ThirdFetchStart ?? 0;
    }

    private void TickAluGroupImmediateRegister()
    {
        if (_aluGroupImmediatePrefixLength != 0)
        {
            TickPrefixedAluGroupImmediateRegister();
            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _aluGroupImmediateModRm);
            return;
        }

        if (_instructionTState == 3)
        {
            byte immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            int register = _aluGroupImmediateModRm & 7;
            byte operand = GetByteRegister(register);
            Intel8086AluOperation operation = GetGroupImmediateAluOperation((byte)((_aluGroupImmediateModRm >> 3) & 7));
            Intel8086AluResult result = Intel8086Alu.Execute(operand, immediate, _state.Flags, operation);
            if (operation != Intel8086AluOperation.Compare)
            {
                SetByteRegister(register, result.Value);
            }

            _state.Flags = result.Flags;
            LastBusCycle = _queueCountAtStart <= 5
                ? FetchCycle("T1", 'S', immediate)
                : IdleCycle('S', immediate);
            return;
        }

        PopQueue();
        LastBusCycle = _queueCountAtStart <= 5
            ? FetchContinuationCycle()
            : FetchCycle("T1", '-', 0);
        _instructionTState = 0;
    }

    private void TickAluGroupImmediateMemory()
    {
        if (_aluGroupImmediatePrefixLength != 0 && _instructionTState <= 2)
        {
            if (_instructionTState == 1)
            {
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
            }
            else
            {
                LastBusCycle = IdleCycle('-', 0);
            }

            return;
        }

        if (_aluGroupImmediatePrefixLength != 0 && _instructionTState is 3 or 4)
        {
            PopQueue();
            _state.IP++;
            Intel8086BusCycle fetchCycle = default;
            bool hasFetch = _queueCountAtStart <= 5 && TryTickMemoryPrefetch(out fetchCycle);
            LastBusCycle = hasFetch
                ? fetchCycle with
                {
                    QueueStatus = _instructionTState == 3 ? 'F' : 'S',
                    QueueData = _instructionTState == 3 ? Opcode : _aluGroupImmediateModRm,
                }
                : _instructionTState == 3 ? IdleCycle('F', Opcode) : IdleCycle('S', _aluGroupImmediateModRm);
            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (_instructionTState == 2)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('S', _aluGroupImmediateModRm);
            return;
        }

        int mod = _aluGroupImmediateModRm >> 6;
        int rm = _aluGroupImmediateModRm & 7;
        int displacementIndex = _instructionTState - _aluGroupImmediateDisplacementStart;
        bool consumesDisplacement = displacementIndex >= 0 && displacementIndex < _aluGroupImmediateDisplacementBytes;
        byte queueByte = 0;
        if (consumesDisplacement)
        {
            queueByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
        }

        bool consumesImmediate = _instructionTState == _aluGroupImmediateValueState;
        if (consumesImmediate)
        {
            queueByte = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            _aluGroupImmediateValue = queueByte;
            ApplyAluGroupImmediateToMemory();
        }

        (int firstFetchStart, int secondFetchStart, int thirdFetchStart) = _aluGroupImmediatePrefixLength == 0
            ? GetAluGroupImmediateFetchStarts(mod, rm, _queueCountAtStart)
            : GetPrefixedAluGroupImmediateFetchStarts(mod, rm, _queueCountAtStart);
        int fetchStart = IsFetchWindow(_instructionTState, firstFetchStart)
            ? firstFetchStart
            : IsFetchWindow(_instructionTState, secondFetchStart)
                ? secondFetchStart
                : thirdFetchStart != 0 && IsFetchWindow(_instructionTState, thirdFetchStart) ? thirdFetchStart : 0;
        int readPhase = _instructionTState - _aluGroupImmediateReadStart;
        int writePhase = _instructionTState - _aluGroupImmediateWriteStart;
        if (readPhase is >= 0 and <= 3 || _aluGroupImmediateWrites && writePhase is >= 0 and <= 2)
        {
            fetchStart = 0;
        }

        if (fetchStart != 0)
        {
            int phase = _instructionTState - fetchStart;
            LastBusCycle = phase switch
            {
                0 => FetchCycle("T1", consumesDisplacement || consumesImmediate ? 'S' : '-', queueByte),
                1 => FetchContinuationCycle() with
                {
                    QueueStatus = consumesDisplacement || consumesImmediate ? 'S' : '-',
                    QueueData = queueByte,
                },
                2 => _aluGroupImmediatePrefixLength != 0
                    ? FetchPrefixedCodeDataCycle() with
                    {
                        QueueStatus = consumesDisplacement || consumesImmediate ? 'S' : '-',
                        QueueData = queueByte,
                    }
                    : FetchDataCycle(consumesDisplacement || consumesImmediate ? 'S' : '-', queueByte),
                _ => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", consumesDisplacement || consumesImmediate ? 'S' : '-', queueByte),
            };
            if (phase == 2)
            {
                int bytesToQueue = Math.Min(2, _aluGroupImmediatePrefetchBytesRemaining);
                int byteIndex = 0;
                while (bytesToQueue > 0 && _state.QueueCount < _state.Queue.Length)
                {
                    PushQueue(_aluGroupImmediatePrefixLength != 0
                        ? ReadPrefixedCodeByte((_nextFetchAddress + (uint)byteIndex) & 0xFFFFF)
                        : (byte)0x90);
                    _aluGroupImmediatePrefetchBytesRemaining--;
                    bytesToQueue--;
                    byteIndex++;
                }

                uint codeSegmentBase = (uint)InstructionCs << 4;
                ushort codeOffset = unchecked((ushort)(_nextFetchAddress - codeSegmentBase));
                _nextFetchAddress = PhysicalAddress(InstructionCs, unchecked((ushort)(codeOffset + 2)));
            }
        }
        else if (readPhase is >= 0 and <= 3)
        {
            LastBusCycle = readPhase switch
            {
                0 => new Intel8086BusCycle(1, _aluGroupImmediatePhysicalAddress, "--", "---", "---", _aluGroupImmediateBhe, 0, "MEMR", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluGroupImmediatePhysicalAddress, _aluGroupImmediateSegmentStatus, "R--", "---", _aluGroupImmediateBhe, 0, "MEMR", "T2", '-', 0),
                2 => ReadAluGroupImmediateData(),
                _ => new Intel8086BusCycle(0, _aluGroupImmediatePhysicalAddress, _aluGroupImmediateSegmentStatus, "---", "---", _aluGroupImmediateBhe, 0, "PASV", "T4", '-', 0),
            };
        }
        else if (_aluGroupImmediateWrites && writePhase is >= 0 and <= 2)
        {
            ushort data = _aluGroupImmediateBhe == 0 ? (ushort)(_aluGroupImmediateResult << 8) : _aluGroupImmediateResult;
            LastBusCycle = writePhase switch
            {
                0 => new Intel8086BusCycle(1, _aluGroupImmediatePhysicalAddress, "--", "---", "---", _aluGroupImmediateBhe, 0, "MEMW", "T1", '-', 0),
                1 => new Intel8086BusCycle(0, _aluGroupImmediatePhysicalAddress, _aluGroupImmediateSegmentStatus, "-A-", "---", _aluGroupImmediateBhe, 0, "MEMW", "T2", '-', 0),
                _ => WriteAluGroupImmediateData(data),
            };
        }
        else if (consumesDisplacement || consumesImmediate)
        {
            LastBusCycle = IdleCycle('S', queueByte);
        }
        else
        {
            LastBusCycle = IdleCycle('-', 0);
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private void TickPrefixedAluGroupImmediateRegister()
    {
        switch (_instructionTState)
        {
            case 1:
                PopQueue();
                _state.IP++;
                LastBusCycle = IdleCycle('F', _instructionPrefix);
                return;
            case 2:
                LastBusCycle = IdleCycle('-', 0);
                return;
            case 3:
                PopQueue();
                _state.IP++;
                LastBusCycle = GetPrefixedAluGroupImmediateRegisterFetchCycle('F', Opcode);
                return;
            case 4:
                PopQueue();
                _state.IP++;
                LastBusCycle = GetPrefixedAluGroupImmediateRegisterFetchCycle('S', _aluGroupImmediateModRm);
                return;
            case 5:
                byte immediate = _state.Queue[_state.QueueHead];
                PopQueue();
                _state.IP++;
                int register = _aluGroupImmediateModRm & 7;
                Intel8086AluOperation operation = GetGroupImmediateAluOperation((byte)((_aluGroupImmediateModRm >> 3) & 7));
                Intel8086AluResult result = Intel8086Alu.Execute(GetByteRegister(register), immediate, _state.Flags, operation);
                if (operation != Intel8086AluOperation.Compare)
                {
                    SetByteRegister(register, result.Value);
                }

                _state.Flags = result.Flags;
                LastBusCycle = GetPrefixedAluGroupImmediateRegisterFetchCycle('S', immediate);
                break;
            default:
                LastBusCycle = GetPrefixedAluGroupImmediateRegisterFetchCycle('-', 0);
                if (_queueCountAtStart >= 6 && _state.QueueCount > 0)
                {
                    PopQueue();
                }

                break;
        }

        if (_instructionTState == _instructionCycles)
        {
            _instructionTState = 0;
        }
    }

    private Intel8086BusCycle GetPrefixedAluGroupImmediateRegisterFetchCycle(char queueStatus, byte queueData)
    {
        AluGroupImmediatePrefixProfile profile = GetAluGroupImmediatePrefixProfile(_aluGroupImmediateModRm, _queueCountAtStart);
        int fetchStart = profile.FirstFetchStart;
        int phase = _instructionTState - fetchStart;
        Intel8086BusCycle cycle = phase switch
        {
            0 => FetchCycle("T1", queueStatus, queueData),
            1 => FetchContinuationCycle(queueStatus, queueData),
            2 => FetchPrefixedCodeDataCycle() with { QueueStatus = queueStatus, QueueData = queueData },
            3 => new Intel8086BusCycle(0, 0, "CS", "---", "---", 0, 0, "PASV", "T4", queueStatus, queueData),
            _ => IdleCycle(queueStatus, queueData),
        };

        if (phase == 2)
        {
            int byteCount = Math.Min(2, _aluGroupImmediatePrefetchBytesRemaining);
            for (int index = 0; index < byteCount && _state.QueueCount < _state.Queue.Length; index++)
            {
                PushQueue(0x90);
                _aluGroupImmediatePrefetchBytesRemaining--;
            }

            _nextFetchAddress = (_nextFetchAddress + 2) & 0xFFFFF;
        }

        return cycle;
    }

    private Intel8086BusCycle ReadAluGroupImmediateData()
    {
        _aluGroupImmediateMemoryValue = _bus.Read(_aluGroupImmediatePhysicalAddress);
        ushort data = _aluGroupImmediateBhe == 0 ? (ushort)(_aluGroupImmediateMemoryValue << 8) : _aluGroupImmediateMemoryValue;
        return new Intel8086BusCycle(0, _aluGroupImmediatePhysicalAddress, _aluGroupImmediateSegmentStatus, "R--", "---", _aluGroupImmediateBhe, data, "PASV", "T3", '-', 0);
    }

    private void ApplyAluGroupImmediateToMemory()
    {
        Intel8086AluOperation operation = GetGroupImmediateAluOperation((byte)((_aluGroupImmediateModRm >> 3) & 7));
        Intel8086AluResult result = Intel8086Alu.Execute(_aluGroupImmediateMemoryValue, _aluGroupImmediateValue, _state.Flags, operation);
        if (operation != Intel8086AluOperation.Compare)
        {
            _aluGroupImmediateResult = result.Value;
        }

        _state.Flags = result.Flags;
    }

    private Intel8086BusCycle WriteAluGroupImmediateData(ushort data)
    {
        _bus.Write(_aluGroupImmediatePhysicalAddress, _aluGroupImmediateResult);
        return new Intel8086BusCycle(0, _aluGroupImmediatePhysicalAddress, _aluGroupImmediateSegmentStatus, "-AW", "---", _aluGroupImmediateBhe, data, "PASV", "T3", '-', 0);
    }

    private static (int First, int Second, int Third) GetAluGroupImmediateFetchStarts(int mod, int rm, int queueCount)
    {
        int first = queueCount <= 5 ? 3 : 4;
        int second = mod switch
        {
            0 when rm is 0 or 3 => queueCount == 5 ? 18 : 0,
            0 when rm is 1 or 2 => queueCount == 5 ? 19 : 0,
            0 when rm == 6 => queueCount == 5 ? 13 : 14,
            0 => queueCount == 5 ? 17 : 0,
            1 when rm is 0 or 3 => queueCount == 5 ? 10 : 22,
            1 when rm is 1 or 2 => queueCount == 5 ? 11 : 23,
            1 => queueCount == 5 ? 16 : 20,
            2 when rm is 0 or 3 => queueCount == 5 ? 10 : 11,
            2 when rm is 1 or 2 => queueCount == 5 ? 11 : 12,
            2 => queueCount == 5 ? 16 : 17,
            _ => 0,
        };
        int third = mod switch
        {
            0 when rm == 6 && queueCount == 5 => 17,
            2 when queueCount == 5 && rm is 0 or 3 => 22,
            2 when queueCount == 5 && rm is 1 or 2 => 23,
            2 when queueCount == 5 => 20,
            _ => 0,
        };
        return (first, second, third);
    }

    private static int GetAluGroupImmediateFinalQueueCount(int mod, int queueCount, bool writes) =>
        mod switch
        {
            0 => writes || queueCount == 6 ? queueCount == 6 ? 4 : 5 : 3,
            1 => writes ? queueCount == 5 ? 4 : 5 : queueCount == 5 ? 4 : 3,
            2 => writes || queueCount == 6 ? queueCount == 6 ? 4 : 5 : 3,
            _ => throw new ArgumentOutOfRangeException(nameof(mod)),
        };

    private (int First, int Second, int Third) GetPrefixedAluGroupImmediateFetchStarts(int mod, int rm, int queueCount)
    {
        AluGroupImmediatePrefixProfile profile = GetAluGroupImmediatePrefixProfile(_aluGroupImmediateModRm, queueCount);
        return (profile.FirstFetchStart, profile.SecondFetchStart, profile.ThirdFetchStart);
    }

    private static AluGroupImmediatePrefixProfile GetAluGroupImmediatePrefixProfile(byte modRm, int queueCount)
    {
        int extension = (modRm >> 3) & 7;
        int mod = modRm >> 6;
        int rm = modRm & 7;
        int profileIndex = (extension * 64) + (mod * 16) + (rm * 2) + (queueCount >= 6 ? 1 : 0);
        return profileIndex < 256
            ? AluGroupImmediatePrefixProfilesA[profileIndex]
            : AluGroupImmediatePrefixProfilesB[profileIndex - 256];
    }
}
