using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Z80;

/// <summary>
/// Rdzeń Z80 w modelu cykl po cyklu. Jeden <see cref="Tick"/> = jeden T-state.
/// Zaimplementowane: NOP, LD, ALU, operacje 16-bitowe i sterowanie, prefiksy CB/ED/DD/FD, porty i instrukcje blokowe (Z1–Z10).
/// </summary>
public sealed partial class Z80Core : ICpuCore<Z80State>, IInterruptLines
{
    private enum MachineCycle : byte
    {
        OpcodeFetch,
        MemoryRead,
        MemoryWrite,
        Internal,
        PortRead,
        PortWrite,
    }

    private enum ReadPurpose : byte
    {
        Immediate,
        AbsoluteLow,
        AbsoluteHigh,
        RegisterFromHl,
        AccumulatorFromPair,
        AccumulatorFromAbsolute,
        AluOperand,
        AluImmediate,
        IncrementHl,
        PairImmediateLow,
        PairImmediateHigh,
        PairAddressLow,
        PairAddressHigh,
        PairMemoryLow,
        PairMemoryHigh,
        PopLow,
        PopHigh,
        ExchangeLow,
        ExchangeHigh,
        ControlLow,
        ControlHigh,
        RelativeOffset,
        ReturnLow,
        ReturnHigh,
        CbOperand,
        EdAbsoluteLow,
        EdAbsoluteHigh,
        EdPairLow,
        EdPairHigh,
        EdStackLow,
        EdStackHigh,
        EdNibble,
        EdBlockRead,
        IndexDisplacement,
        IndexCbOpcode,
    }

    private enum WritePurpose : byte
    {
        RegisterToHl,
        ImmediateToHl,
        AccumulatorToPair,
        AccumulatorToAbsolute,
        PairMemoryLow,
        PairMemoryHigh,
        PushHigh,
        PushLow,
        ExchangeHigh,
        ExchangeLow,
        CallHigh,
        CallLow,
        CbModify,
        EdAbsoluteLow,
        EdAbsoluteHigh,
        EdNibble,
        EdBlockWrite,
    }

    private readonly IMemoryBus _bus;
    private readonly IPortBus _ports;

    private Z80State _state;
    private bool _instructionActive;
    private bool _jammed;
    private bool _halted;
    private bool _cbPrefixActive;
    private bool _edPrefixActive;
    private bool _indexPrefixActive;
    private bool _indexIsIy;
    private bool _indexCbPrefixActive;
    private bool _pendingNmi;
    private bool _lastNmiLevel;
    private bool _ldAiRiCompleted;
    private long _cycles;
    private int _resetHoldTStates;
    private int _elapsedTStates;
    private int _instructionTStates;
    private int _memoryCycleStart;
    private int _haltTState;

    private MachineCycle _machineCycle;
    private ushort _refreshAddress;
    private ushort _cycleAddress;
    private ushort _pairAddress;
    private ushort _absoluteAddress;
    private ushort _portAddress;
    private ushort _indexDisplacementAddress;
    private ushort _indexCbOpcodeAddress;
    private bool _indexedImmediateRead;
    private byte _opcode;
    private byte _cbOpcode;
    private byte _edOpcode;
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

    /// <summary>Informuje, czy rdzeń wykonuje cykle HALT.</summary>
    public bool Halted => _halted;

    public bool IrqLevel { get; set; }

    public bool NmiLevel { get; set; }

    /// <summary>RESET aktywny przy wartości true; rdzeń przyjmuje go po 3 T-state.</summary>
    public bool ResetLevel { get; set; }

    /// <summary>Wartość magistrali danych podczas INTA; domyślne <c>FF</c> koduje <c>RST 38h</c>.</summary>
    public byte InterruptDataBus { get; set; } = 0xFF;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public byte Opcode => _opcode;

    public int Pending => _interruptActive
        ? GetInterruptTStates() - _interruptElapsed
        : _instructionActive ? _instructionTStates - _elapsedTStates : 0;

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
        _halted = false;
        _cycles = 0;
        _elapsedTStates = 0;
        _instructionTStates = 0;
        _machineCycle = MachineCycle.OpcodeFetch;
        _cbPrefixActive = false;
        _edPrefixActive = false;
        _indexPrefixActive = false;
        _indexCbPrefixActive = false;
        _indexIsIy = false;
        _indexedImmediateRead = false;
        _pendingNmi = false;
        _lastNmiLevel = NmiLevel;
        _ldAiRiCompleted = false;
        _resetHoldTStates = 0;
        _haltTState = 0;
        _interruptActive = false;
        _interruptKind = InterruptKind.None;
        _interruptElapsed = 0;
        _im0InstructionActive = false;
        _im0IndexCbNeedsAcknowledge = false;
        _im0IndexCbAwaitingOpcode = false;
        _im0AcknowledgeCount = 0;
        _im0TotalTStates = 0;
        _resetApplied = false;
    }

    public void Tick()
    {
        _cycles++;
        if (TickResetLine())
        {
            return;
        }

        SampleNmiEdge();

        if (_jammed)
        {
            return;
        }

        if (_im0InstructionActive && !_interruptActive)
        {
            _im0TotalTStates++;
        }

        if (_interruptActive)
        {
            TickInterruptCycle();
            return;
        }

        if (_halted)
        {
            if (_haltTState == 0 && TryBeginInterrupt())
            {
                TickInterruptCycle();
                return;
            }

            TickHaltCycle();
            return;
        }

        if (!_instructionActive)
        {
            if (TryBeginInterrupt())
            {
                TickInterruptCycle();
                return;
            }

            BeginInstruction();
        }

        if ((_cbPrefixActive || _edPrefixActive || _indexPrefixActive) && _elapsedTStates is >= 4 and < 8)
        {
            if (_cbPrefixActive)
            {
                TickCbOpcodeFetch(_elapsedTStates - 4);
            }
            else if (_edPrefixActive)
            {
                TickEdOpcodeFetch(_elapsedTStates - 4);
            }
            else
            {
                TickIndexOpcodeFetch(_elapsedTStates - 4);
            }
        }
        else if (_elapsedTStates < 4)
        {
            TickOpcodeFetch(_elapsedTStates);
        }
        else
        {
            TickMemoryCycle();
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
        _cbOpcode = 0;
        _edOpcode = 0;
        _cbPrefixActive = false;
        _edPrefixActive = false;
        _indexPrefixActive = false;
        _indexCbPrefixActive = false;
        _refreshAddress = (ushort)((_state.I << 8) | _state.R);
        _elapsedTStates = 0;
        _instructionTStates = 4;
        _machineCycle = MachineCycle.OpcodeFetch;
        _instructionActive = true;
    }

    private void TickOpcodeFetch(int tState)
    {
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
                _cbPrefixActive = _opcode == 0xCB;
                _edPrefixActive = _opcode == 0xED;
                _indexPrefixActive = _opcode is 0xDD or 0xFD;
                _indexIsIy = _opcode == 0xFD;
                _instructionTStates = _cbPrefixActive || _edPrefixActive || _indexPrefixActive ? 8 : GetInstructionTStates(_opcode);
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
                if (_cbPrefixActive || _edPrefixActive || _indexPrefixActive)
                {
                    _refreshAddress = (ushort)((_state.I << 8) | _state.R);
                }
                else
                {
                    PrepareFollowingCycle();
                }
                break;
        }
    }

}
