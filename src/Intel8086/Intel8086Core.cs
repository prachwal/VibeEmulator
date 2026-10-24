using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Rdzeń Intel 8086; obsługuje transfery, adresowanie ModR/M, XLAT i ALU bajtowe.</summary>
public sealed partial class Intel8086Core : ICpuCore<Intel8086State>
{
    public Intel8086Core(IAddressBus bus, IPortBus? portBus = null)
    {
        ArgumentNullException.ThrowIfNull(bus);
        _bus = bus;
        _portBus = new Intel8086PortBus(portBus);
        _state = Intel8086State.Initial();
    }

    public ref Intel8086State State => ref _state;

    public int Pending => _instructionTState == 0 ? 0 : _instructionCycles - _instructionTState;

    public bool Jammed => false;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public ushort InstructionCs { get; private set; }

    public byte Opcode { get; private set; }

    public Intel8086BusCycle LastBusCycle { get; private set; }

    public void Tick()
    {
        _cycles++;
        if (_instructionTState == 0)
        {
            BeginInstruction();
        }

        if (_softwareInterruptIsDivideError)
        {
            TickSoftwareInterrupt();
            return;
        }

        if (Opcode is 0xD4 or 0xD5)
        {
            TickAsciiAdjust(Opcode == 0xD4);
            return;
        }

        if (Opcode is 0x27 or 0x2F or 0x37 or 0x3F)
        {
            TickDecimalAdjust();
            return;
        }

        if (Opcode is 0x98 or 0x99)
        {
            if (Opcode == 0x98)
            {
                TickCbw();
            }
            else
            {
                TickCwd();
            }

            return;
        }

        if (IsEscOpcode(Opcode))
        {
            if ((_memoryModRm >> 6) == 3)
            {
                TickEscRegisterDirect();
            }
            else
            {
                TickMemoryWordIncDec();
            }

            return;
        }

        if (_stringRepeatZeroCount)
        {
            TickStringRepeatZeroCount();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xA4)
        {
            TickRepeatedMovsByte();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xAA)
        {
            TickRepeatedStosByte();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xAC)
        {
            TickRepeatedLodsByte();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xAD)
        {
            TickRepeatedLodsWord();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xAB)
        {
            TickRepeatedStosWord();
            return;
        }

        if (_instructionPrefixLength != 0 && Opcode == 0xAE)
        {
            TickRepeatedScasByte();
            return;
        }

        if (IsMoveStringOpcode(Opcode))
        {
            TickMoveString();
            return;
        }

        if (IsStoreStringOpcode(Opcode))
        {
            TickStoreString();
            return;
        }

        if (IsLoadStringOpcode(Opcode))
        {
            TickLoadString();
            return;
        }

        if (IsScanStringOpcode(Opcode))
        {
            TickScanString();
            return;
        }

        if (IsCompareStringOpcode(Opcode))
        {
            TickCompareString();
            return;
        }

        if (IsByteInputIoOpcode(Opcode))
        {
            TickByteInputIo();
            return;
        }

        if (IsByteOutputIoOpcode(Opcode))
        {
            TickByteOutputIo();
            return;
        }

        if (IsWordInputIoOpcode(Opcode))
        {
            TickWordInputIo();
            return;
        }

        if (IsWordOutputIoOpcode(Opcode))
        {
            TickWordOutputIo();
            return;
        }

        if (IsConditionalBranchOpcode(Opcode))
        {
            TickConditionalBranch();
            return;
        }

        if (IsJumpOpcode(Opcode))
        {
            TickJump();
            return;
        }

        if (IsReturnOpcode(Opcode))
        {
            TickReturn();
            return;
        }

        if (IsLoopOpcode(Opcode))
        {
            TickLoop();
            return;
        }

        if (Opcode == 0xCF)
        {
            TickInterruptReturn();
            return;
        }

        if (IsSoftwareInterruptOpcode(Opcode))
        {
            TickSoftwareInterrupt();
            return;
        }

        if (IsFlagControlOpcode(Opcode))
        {
            TickFlagControl();
            return;
        }

        if (Opcode == 0xFE && _instructionPrefixLength != 0 && (_memoryModRm >> 6) == 3)
        {
            TickPrefixedRegisterByteIncDec();
            return;
        }

        if (Opcode == 0xFE && (_memoryModRm >> 6) != 3)
        {
            TickMemoryByteIncDec();
            return;
        }

        if (Opcode == 0xFF && (_memoryModRm >> 6) != 3)
        {
            TickMemoryWordIncDec();
            return;
        }

        if (IsRegisterDirectMultiplyOpcode(Opcode, _memoryModRm))
        {
            TickRegisterDirectMultiply();
            return;
        }

        if (IsRegisterDirectDivideOpcode(Opcode, _memoryModRm))
        {
            TickRegisterDirectDivide();
            return;
        }

        if (Opcode == 0xF6 && (_memoryModRm >> 6) != 3)
        {
            TickMemoryByteIncDec();
            return;
        }

        if (Opcode == 0xF7 && (_memoryModRm >> 6) != 3)
        {
            TickMemoryWordIncDec();
            return;
        }

        if ((Opcode is 0xD0 or 0xD2) && (_memoryModRm >> 6) != 3)
        {
            TickMemoryByteIncDec();
            return;
        }

        if ((Opcode is 0xD1 or 0xD3) && (_memoryModRm >> 6) != 3)
        {
            TickMemoryWordIncDec();
            return;
        }

        if (IsAnyWordAluModRmOpcode(Opcode) && (_memoryModRm >> 6) != 3)
        {
            TickMemoryWordIncDec();
            return;
        }

        _instructionTState++;
        if (Opcode == 0x8D)
        {
            TickLea();
            return;
        }

        if (Opcode == 0xD7)
        {
            TickXlat();
            return;
        }

        if (_aluMemoryTransfer)
        {
            TickMemoryAlu();
            return;
        }

        if (_aluGroupImmediateRegisterTransfer)
        {
            TickAluGroupImmediateRegister();
            return;
        }

        if (_aluGroupImmediateMemoryTransfer)
        {
            TickAluGroupImmediateMemory();
            return;
        }

        if (_movMemoryTransfer)
        {
            TickMemoryMov();
            return;
        }

        if (_movImmediateTransfer)
        {
            TickMovImmediate();
            return;
        }

        if (_xchgRegisterTransfer)
        {
            TickRegisterXchg();
            return;
        }

        if (_xchgMemoryTransfer)
        {
            TickMemoryXchg();
            return;
        }

        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 2)
        {
            if (Opcode is >= 0x40 and <= 0x4F)
            {
                PopQueue();
                int register = Opcode & 7;
                bool decrement = (Opcode & 0x08) != 0;
                Intel8086AluWordResult result = Intel8086Alu.ExecuteIncrementDecrement(GetWordRegister(register), _state.Flags, decrement);
                SetWordRegister(register, result.Value);
                _state.Flags = result.Flags;
                LastBusCycle = IdleCycle('-', 0);
                _instructionTState = 0;
                return;
            }

            if (IsWordAluImmediateGroupOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                _memoryModRm = modRm;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            if (IsUnaryNotNegGroupOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int register = modRm & 7;
                bool negate = ((modRm >> 3) & 7) == 3;
                if (Opcode == 0xF6)
                {
                    Intel8086AluResult result = negate
                        ? Intel8086Alu.ExecuteNegate(GetByteRegister(register), _state.Flags)
                        : Intel8086Alu.ExecuteNot(GetByteRegister(register), _state.Flags);
                    SetByteRegister(register, result.Value);
                    _state.Flags = result.Flags;
                }
                else
                {
                    Intel8086AluWordResult result = negate
                        ? Intel8086Alu.ExecuteNegate(GetWordRegister(register), _state.Flags)
                        : Intel8086Alu.ExecuteNot(GetWordRegister(register), _state.Flags);
                    SetWordRegister(register, result.Value);
                    _state.Flags = result.Flags;
                }

                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            if (IsShiftRotateGroupOpcode(Opcode) && (_memoryModRm >> 6) == 3)
            {
                if (IsShiftRotateClGroupOpcode(Opcode))
                {
                    BeginShiftRotateWithCl();
                }
                else
                {
                    ExecuteShiftRotateGroup();
                }

                return;
            }

            if (IsByteIncDecGroupOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int register = modRm & 7;
                bool decrement = ((modRm >> 3) & 7) == 1;
                Intel8086AluResult result = Intel8086Alu.ExecuteIncrementDecrement(GetByteRegister(register), _state.Flags, decrement);
                SetByteRegister(register, result.Value);
                _state.Flags = result.Flags;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            if (IsWordIncDecGroupOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int register = modRm & 7;
                bool decrement = ((modRm >> 3) & 7) == 1;
                Intel8086AluWordResult result = Intel8086Alu.ExecuteIncrementDecrement(GetWordRegister(register), _state.Flags, decrement);
                SetWordRegister(register, result.Value);
                _state.Flags = result.Flags;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            if (IsRegisterDirectWordAluOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int reg = (modRm >> 3) & 7;
                int rm = modRm & 7;
                bool directionToReg = Opcode != 0x85 && (Opcode & 0x02) != 0;
                int destination = directionToReg ? reg : rm;
                ushort left = GetWordRegister(destination);
                ushort right = GetWordRegister(directionToReg ? rm : reg);
                Intel8086AluOperation operation = GetRegisterDirectWordAluOperation(Opcode);
                Intel8086AluWordResult result = Intel8086Alu.Execute(left, right, _state.Flags, operation);
                if (operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
                {
                    SetWordRegister(destination, result.Value);
                }

                _state.Flags = result.Flags;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }

            if (IsRegisterDirectAluOpcode(Opcode))
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int reg = (modRm >> 3) & 7;
                int rm = modRm & 7;
                bool directionToReg = Opcode != 0x84 && (Opcode & 0x02) != 0;
                int destination = directionToReg ? reg : rm;
                byte left = GetByteRegister(destination);
                byte right = GetByteRegister(directionToReg ? rm : reg);
                Intel8086AluOperation operation = GetRegisterDirectAluOperation(Opcode);
                Intel8086AluResult result = Intel8086Alu.Execute(left, right, _state.Flags, operation);
                if (operation is not Intel8086AluOperation.Compare and not Intel8086AluOperation.Test)
                {
                    SetByteRegister(destination, result.Value);
                }

                _state.Flags = result.Flags;
                LastBusCycle = IdleCycle('S', modRm);
                return;
            }
            else if (Opcode is 0x88 or 0x8A)
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                // SST pokazuje dodatkowe przesunięcie kolejki podczas kończenia
                // tej rejestrowej formy, mimo że IP przesuwa się o dwa bajty.
                PopQueue();
                _state.IP++;
                int reg = (modRm >> 3) & 7;
                int rm = modRm & 7;
                SetByteRegister(Opcode == 0x88 ? rm : reg, GetByteRegister(Opcode == 0x88 ? reg : rm));
                LastBusCycle = IdleCycle('S', modRm);
                _instructionTState = 0;
                return;
            }

            else if (Opcode is 0x89 or 0x8B)
            {
                byte modRm = _state.Queue[_state.QueueHead];
                PopQueue();
                PopQueue();
                _state.IP++;
                int reg = (modRm >> 3) & 7;
                int rm = modRm & 7;
                SetWordRegister(Opcode == 0x89 ? rm : reg, GetWordRegister(Opcode == 0x89 ? reg : rm));
                LastBusCycle = IdleCycle('S', modRm);
                _instructionTState = 0;
                return;
            }

            if (Opcode is 0x90 or >= 0xB8 and <= 0xBF || IsAccumulatorImmediateWordAluOpcode(Opcode))
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
        }
        else if (IsWordAluImmediateGroupOpcode(Opcode) && (_memoryModRm >> 6) == 3 && _instructionTState is 3 or 4)
        {
            byte immediate = _instructionTState == 3 ? (byte)_immediateWord : (byte)(_immediateWord >> 8);
            if (Opcode == 0x81 || _instructionTState == 3)
            {
                PopQueue();
                _state.IP++;
            }

            if (_instructionTState == 4)
            {
                Intel8086AluOperation operation = GetWordAluImmediateGroupOperation((_memoryModRm >> 3) & 7);
                Intel8086AluWordResult result = Intel8086Alu.Execute(
                    GetWordRegister(_memoryModRm & 7), _immediateWord, _state.Flags, operation);
                if (operation != Intel8086AluOperation.Compare)
                {
                    SetWordRegister(_memoryModRm & 7, result.Value);
                }

                _state.Flags = result.Flags;
            }

            bool fetchForImmediate = ShouldFetchAtInstructionEnd();
            if (_instructionTState == 3)
            {
                LastBusCycle = fetchForImmediate ? FetchCycle("T1", 'S', immediate) : IdleCycle('S', immediate);
            }
            else if (Opcode == 0x83)
            {
                LastBusCycle = fetchForImmediate ? FetchContinuationCycle('-', 0) : FetchCycle("T1", '-', 0);
            }
            else
            {
                LastBusCycle = fetchForImmediate ? FetchContinuationCycle('S', immediate) : FetchCycle("T1", 'S', immediate);
            }

            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
        else if (IsShiftRotateClGroupOpcode(Opcode) && (_memoryModRm >> 6) == 3)
        {
            TickShiftRotateWithCl();
        }
        else if (Opcode is >= 0xB0 and <= 0xB7 && _instructionTState == 3)
        {
            byte immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            SetByteRegister(Opcode & 7, immediate);
            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? FetchCycle("T1", 'S', immediate)
                : IdleCycle('S', immediate);
        }
        else if (IsAccumulatorImmediateAluOpcode(Opcode) && _instructionTState == 3)
        {
            byte immediate = _state.Queue[_state.QueueHead];
            PopQueue();
            _state.IP++;
            Intel8086AluResult result = Intel8086Alu.Execute(
                (byte)_state.AX,
                immediate,
                _state.Flags,
                GetAccumulatorImmediateOperation(Opcode));
            if (IsAccumulatorResultWritten(Opcode))
            {
                SetByteRegister(0, result.Value);
            }

            _state.Flags = result.Flags;
            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? FetchCycle("T1", 'S', immediate)
                : IdleCycle('S', immediate);
        }
        else if (IsAccumulatorImmediateWordAluOpcode(Opcode) && _instructionTState is 3 or 4)
        {
            byte immediate = _instructionTState == 3 ? (byte)_immediateWord : (byte)(_immediateWord >> 8);
            PopQueue();
            _state.IP++;
            if (_instructionTState == 4)
            {
                Intel8086AluOperation operation = GetAccumulatorImmediateWordOperation(Opcode);
                Intel8086AluWordResult result = Intel8086Alu.Execute(_state.AX, _immediateWord, _state.Flags, operation);
                if (operation != Intel8086AluOperation.Compare)
                {
                    _state.AX = result.Value;
                }

                _state.Flags = result.Flags;
            }

            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? _instructionTState == 3
                    ? FetchCycle("T1", 'S', immediate)
                    : FetchContinuationCycle('S', immediate)
                : IdleCycle('S', immediate);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
        else if (Opcode is >= 0xB8 and <= 0xBF && _instructionTState is 3 or 4)
        {
            byte immediate = _instructionTState == 3 ? (byte)_immediateWord : (byte)(_immediateWord >> 8);
            PopQueue();
            _state.IP++;
            if (_instructionTState == 4)
            {
                SetWordRegister(Opcode & 7, _immediateWord);
            }

            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? _instructionTState == 3
                    ? FetchCycle("T1", 'S', immediate)
                    : FetchContinuationCycle('S', immediate)
                : IdleCycle('S', immediate);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
        else
        {
            if ((Opcode is >= 0xB0 and <= 0xB7 || IsAccumulatorImmediateAluOpcode(Opcode)) && _instructionTState == 4)
            {
                PopQueue();
            }

            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? Opcode == 0x90
                    ? FetchCycle("T1", '-', 0)
                    : IsRegisterDirectAluOpcode(Opcode) || IsRegisterDirectWordAluOpcode(Opcode) || IsByteIncDecGroupOpcode(Opcode) || IsWordIncDecGroupOpcode(Opcode) || IsUnaryNotNegGroupOpcode(Opcode)
                        ? FetchCycle("T1", '-', 0)
                        : FetchContinuationCycle('-', 0)
                : IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
    }

    private static bool IsAccumulatorResultWritten(byte opcode) =>
        opcode is not 0x3C and not 0xA8;

    public void Stall()
    {
        _cycles++;
    }

    private void PopQueue()
    {
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot consume an empty prefetch queue.");
        }

        _state.QueueHead++;
        _state.QueueCount--;
    }

    private bool ShouldFetchAtInstructionEnd() => _queueCountAtStart <= 5;

    private static bool TryGetXlatSegmentOverride(byte value, out Intel8086SegmentOverride segmentOverride)
    {
        segmentOverride = value switch
        {
            0x26 => Intel8086SegmentOverride.Es,
            0x2E => Intel8086SegmentOverride.Cs,
            0x36 => Intel8086SegmentOverride.Ss,
            0x3E => Intel8086SegmentOverride.Ds,
            _ => Intel8086SegmentOverride.None,
        };
        return segmentOverride != Intel8086SegmentOverride.None;
    }

    private static string GetSegmentStatus(Intel8086SegmentOverride segmentOverride) => segmentOverride switch
    {
        Intel8086SegmentOverride.None or Intel8086SegmentOverride.Ds => "DS",
        Intel8086SegmentOverride.Es => "ES",
        Intel8086SegmentOverride.Cs => "CS",
        Intel8086SegmentOverride.Ss => "SS",
        _ => throw new ArgumentOutOfRangeException(nameof(segmentOverride)),
    };

    private Intel8086BusCycle FetchCycle(string tState, char queueStatus, byte queueData) => new(
        1,
        _nextFetchAddress,
        "--",
        "---",
        "---",
        0,
        0,
        "CODE",
        tState,
        queueStatus,
        queueData);

    private static Intel8086BusCycle FetchContinuationCycle() => FetchContinuationCycle('-', 0);

    private Intel8086BusCycle FetchContinuationCycle(char queueStatus, byte queueData) => new(
        0,
        _nextFetchAddress,
        "CS",
        "R--",
        "---",
        0,
        0,
        "CODE",
        "T2",
        queueStatus,
        queueData);

    private static Intel8086BusCycle IdleCycle(char queueStatus, byte queueData) => new(
        0,
        0,
        "--",
        "---",
        "---",
        0,
        0,
        "PASV",
        "Ti",
        queueStatus,
        queueData);

    private static uint PhysicalAddress(ushort segment, ushort offset) =>
        (uint)(((segment << 4) + offset) & 0xFFFFF);
}
