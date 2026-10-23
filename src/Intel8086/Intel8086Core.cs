using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Rdzeń Intel 8086; obsługuje transfery, adresowanie ModR/M, XLAT i ALU bajtowe.</summary>
public sealed partial class Intel8086Core : ICpuCore<Intel8086State>
{
    public Intel8086Core(IAddressBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        _bus = bus;
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

            if (Opcode is 0x90 or >= 0xB8 and <= 0xBF)
            {
                PopQueue();
            }

            LastBusCycle = IdleCycle('-', 0);
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
                    : IsRegisterDirectAluOpcode(Opcode)
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

    private Intel8086BusCycle FetchContinuationCycle() => FetchContinuationCycle('-', 0);

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
