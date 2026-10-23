using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Rdzeń Intel 8086; obecnie obsługuje NOP oraz MOV r8,imm8 zgodnie z aktywną próbką.</summary>
public sealed class Intel8086Core : ICpuCore<Intel8086State>
{
    private readonly IAddressBus _bus;
    private Intel8086State _state;
    private int _instructionTState;
    private int _instructionCycles;
    private int _queueCountAtStart;
    private uint _nextFetchAddress;
    private long _cycles;

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

    public void Reset(ushort pc = 0)
    {
        _state = Intel8086State.Initial(ip: pc);
        _instructionTState = 0;
        _instructionCycles = 0;
        _queueCountAtStart = 0;
        _nextFetchAddress = 0;
        _cycles = 0;
        InstructionPc = pc;
        InstructionCs = 0;
        Opcode = 0;
        LastBusCycle = default;
    }

    public void Tick()
    {
        _cycles++;
        if (_instructionTState == 0)
        {
            BeginInstruction();
        }

        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
        }
        else if (_instructionTState == 2)
        {
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
        else
        {
            if (Opcode is >= 0xB0 and <= 0xB7 && _instructionTState == 4)
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

    private void BeginInstruction()
    {
        if (_state.QueueCount == 0)
        {
            throw new InvalidOperationException("Cannot decode an empty prefetch queue.");
        }

        byte opcode = _state.Queue[_state.QueueHead];
        if (opcode is not 0x90 and not (>= 0xB0 and <= 0xB7))
        {
            throw new InvalidOperationException($"Opcode {opcode:X2} is not implemented yet.");
        }

        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = opcode;
        _queueCountAtStart = _state.QueueCount;
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + _queueCountAtStart));
        _instructionCycles = opcode == 0x90 ? 3 : 4;
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
