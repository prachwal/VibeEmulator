using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Rdzeń Intel 8086; A2 implementuje NOP z kolejki i jego trzy T-state.</summary>
public sealed class Intel8086Core : ICpuCore<Intel8086State>
{
    private readonly IAddressBus _bus;
    private Intel8086State _state;
    private int _instructionTState;
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

    public int Pending => _instructionTState == 0 ? 0 : 3 - _instructionTState;

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
            BeginNop();
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
            PopQueue();
            LastBusCycle = IdleCycle('-', 0);
        }
        else
        {
            LastBusCycle = CreateFinalNopCycle();
            _instructionTState = 0;
        }
    }

    public void Stall()
    {
        _cycles++;
    }

    private void BeginNop()
    {
        if (_state.QueueCount == 0 || _state.Queue[_state.QueueHead] != 0x90)
        {
            throw new InvalidOperationException("A2 supports only NOP (90) at the head of the prefetch queue.");
        }

        InstructionPc = _state.IP;
        InstructionCs = _state.CS;
        Opcode = _state.Queue[_state.QueueHead];
        _queueCountAtStart = _state.QueueCount;
        _nextFetchAddress = PhysicalAddress(_state.CS, (ushort)(_state.IP + _queueCountAtStart));
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

    private Intel8086BusCycle CreateFinalNopCycle()
    {
        if (_state.QueueCount > 3)
        {
            return IdleCycle('-', 0);
        }

        return new Intel8086BusCycle(
            1,
            _nextFetchAddress,
            "--",
            "---",
            "---",
            0,
            0,
            "CODE",
            "T1",
            '-',
            0);
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
