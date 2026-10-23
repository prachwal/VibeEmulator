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
            if (Opcode is 0x88 or 0x8A)
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

            if (Opcode == 0x90)
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
            if (Opcode != 0x3C)
            {
                SetByteRegister(0, result.Value);
            }

            _state.Flags = result.Flags;
            LastBusCycle = ShouldFetchAtInstructionEnd()
                ? FetchCycle("T1", 'S', immediate)
                : IdleCycle('S', immediate);
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
                    : FetchContinuationCycle()
                : IdleCycle('-', 0);
            if (_instructionTState == _instructionCycles)
            {
                _instructionTState = 0;
            }
        }
    }

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

    private Intel8086BusCycle FetchContinuationCycle() => new(
        0,
        _nextFetchAddress,
        "CS",
        "R--",
        "---",
        0,
        0,
        "CODE",
        "T2",
        '-',
        0);

    private void SetByteRegister(int register, byte value)
    {
        switch (register)
        {
            case 0:
                _state.AX = (ushort)((_state.AX & 0xFF00) | value);
                break;
            case 1:
                _state.CX = (ushort)((_state.CX & 0xFF00) | value);
                break;
            case 2:
                _state.DX = (ushort)((_state.DX & 0xFF00) | value);
                break;
            case 3:
                _state.BX = (ushort)((_state.BX & 0xFF00) | value);
                break;
            case 4:
                _state.AX = (ushort)((_state.AX & 0x00FF) | (value << 8));
                break;
            case 5:
                _state.CX = (ushort)((_state.CX & 0x00FF) | (value << 8));
                break;
            case 6:
                _state.DX = (ushort)((_state.DX & 0x00FF) | (value << 8));
                break;
            case 7:
                _state.BX = (ushort)((_state.BX & 0x00FF) | (value << 8));
                break;
        }
    }

    private byte GetByteRegister(int register) => register switch
    {
        0 => (byte)_state.AX,
        1 => (byte)_state.CX,
        2 => (byte)_state.DX,
        3 => (byte)_state.BX,
        4 => (byte)(_state.AX >> 8),
        5 => (byte)(_state.CX >> 8),
        6 => (byte)(_state.DX >> 8),
        7 => (byte)(_state.BX >> 8),
        _ => throw new ArgumentOutOfRangeException(nameof(register)),
    };

    private static bool IsAccumulatorImmediateAluOpcode(byte opcode) =>
        opcode is 0x04 or 0x0C or 0x14 or 0x1C or 0x24 or 0x2C or 0x34 or 0x3C;

    private static Intel8086AluOperation GetAccumulatorImmediateOperation(byte opcode) => opcode switch
    {
        0x04 => Intel8086AluOperation.Add,
        0x0C => Intel8086AluOperation.Or,
        0x14 => Intel8086AluOperation.Adc,
        0x1C => Intel8086AluOperation.Sbb,
        0x24 => Intel8086AluOperation.And,
        0x2C => Intel8086AluOperation.Sub,
        0x34 => Intel8086AluOperation.Xor,
        0x3C => Intel8086AluOperation.Compare,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
    };

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
