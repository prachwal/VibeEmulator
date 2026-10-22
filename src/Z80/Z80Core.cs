using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Z80;

/// <summary>
/// Rdzeń Z80 w modelu cykl po cyklu. Jeden <see cref="Tick"/> = jeden T-state.
/// Zaimplementowane: NOP oraz instrukcje ładowania 8-bitowego z etapów Z1–Z2.
/// </summary>
public sealed partial class Z80Core : ICpuCore<Z80State>
{
    private enum MachineCycle : byte
    {
        OpcodeFetch,
        MemoryRead,
        MemoryWrite,
    }

    private enum ReadPurpose : byte
    {
        Immediate,
        AbsoluteLow,
        AbsoluteHigh,
        RegisterFromHl,
        AccumulatorFromPair,
        AccumulatorFromAbsolute,
    }

    private enum WritePurpose : byte
    {
        RegisterToHl,
        ImmediateToHl,
        AccumulatorToPair,
        AccumulatorToAbsolute,
    }

    private readonly IMemoryBus _bus;
    private readonly IPortBus _ports;

    private Z80State _state;
    private bool _instructionActive;
    private bool _jammed;
    private long _cycles;
    private int _elapsedTStates;
    private int _instructionTStates;

    private MachineCycle _machineCycle;
    private ushort _refreshAddress;
    private ushort _cycleAddress;
    private ushort _pairAddress;
    private ushort _absoluteAddress;
    private byte _opcode;
    private byte _registerCode;
    private byte _absoluteLow;
    private byte _cycleReadValue;
    private byte _cycleWriteValue;
    private ReadPurpose _readPurpose;
    private WritePurpose _writePurpose;

    public Z80Core(IMemoryBus bus, IPortBus ports)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(ports);

        _bus = bus;
        _ports = ports;
        _state = Z80State.Reset();
    }

    public ref Z80State State => ref _state;

    public bool Jammed => _jammed;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public byte Opcode => _opcode;

    public int Pending => _instructionActive ? _instructionTStates - _elapsedTStates : 0;

    /// <summary>Adres zewnętrznych szyn po ostatnim T-state (ignorowany, gdy wektor ma adres <c>null</c>).</summary>
    public ushort LastAddress { get; private set; }

    /// <summary>Dana zewnętrznych szyn po ostatnim T-state.</summary>
    public byte? LastData { get; private set; }

    /// <summary>Sygnały R/W/MRQ/IORQ po ostatnim T-state.</summary>
    public string LastPins { get; private set; } = "----";

    public void Reset(ushort pc = 0)
    {
        _state = Z80State.Reset();
        _state.PC = pc;
        _instructionActive = false;
        _jammed = false;
        _cycles = 0;
        _elapsedTStates = 0;
        _instructionTStates = 0;
        _machineCycle = MachineCycle.OpcodeFetch;
    }

    public void Tick()
    {
        _cycles++;
        if (_jammed)
        {
            return;
        }

        if (!_instructionActive)
        {
            BeginInstruction();
        }

        if (_elapsedTStates < 4)
        {
            TickOpcodeFetch(_elapsedTStates);
        }
        else
        {
            TickMemoryCycle(_elapsedTStates - 4);
        }

        _elapsedTStates++;
        if (_elapsedTStates >= _instructionTStates)
        {
            CompleteInstruction();
        }
    }

    public void Stall()
    {
        // WAIT: zegar rdzenia biegnie, ale T-state instrukcji i rejestry pozostają bez zmian.
        _cycles++;
    }

    private void BeginInstruction()
    {
        InstructionPc = _state.PC;
        _opcode = 0;
        _refreshAddress = (ushort)((_state.I << 8) | _state.R);
        _elapsedTStates = 0;
        _instructionTStates = 4;
        _machineCycle = MachineCycle.OpcodeFetch;
        _instructionActive = true;
    }

    private void TickOpcodeFetch(int tState)
    {
        _machineCycle = MachineCycle.OpcodeFetch;
        switch (tState)
        {
            case 0:
                LastAddress = InstructionPc;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _opcode = _bus.Read(_state.PC);
                _state.PC++;
                _instructionTStates = GetInstructionTStates(_opcode);
                LastAddress = InstructionPc;
                LastData = null;
                LastPins = "r-m-";
                break;
            case 2:
                LastAddress = _refreshAddress;
                LastData = _opcode;
                LastPins = "----";
                break;
            default:
                LastAddress = _refreshAddress;
                LastData = null;
                LastPins = "----";
                _state.R = (byte)((_state.R + 1) & 0x7F);
                PrepareFollowingCycle();
                break;
        }
    }

}
