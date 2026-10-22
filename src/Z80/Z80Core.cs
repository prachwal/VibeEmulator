using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Z80;

/// <summary>
/// Rdzeń Z80 w modelu cykl po cyklu. Jeden <see cref="Tick"/> = jeden T-state.
/// Na razie zaimplementowany tylko opkod <c>00</c> (NOP, 4 T-state).
/// </summary>
public sealed class Z80Core : ICpuCore<Z80State>
{
    private readonly IMemoryBus _bus;
    private readonly IPortBus _ports;

    private Z80State _state;
    private int _phase; // 0..3 = T-state pobrania NOP
    private ushort _refreshAddress;
    private bool _jammed;
    private long _cycles;

    public ushort LastAddress { get; private set; }
    public byte? LastData { get; private set; }
    public string LastPins { get; private set; } = "----";

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

    public byte Opcode { get; private set; }

    public int Pending => _phase == 0 ? 0 : 4 - _phase;

    public void Reset(ushort pc = 0)
    {
        _state = Z80State.Reset();
        _state.PC = pc;
        _phase = 0;
        _jammed = false;
        _cycles = 0;
    }

    public void Tick()
    {
        _cycles++;

        if (_jammed)
        {
            return;
        }

        if (_phase == 0)
        {
            InstructionPc = _state.PC;
            _refreshAddress = (ushort)((_state.I << 8) | _state.R);
            _phase = 1;
            LastAddress = InstructionPc;
            LastData = null;
            LastPins = "----";
            return;
        }

        if (_phase == 1)
        {
            Opcode = _bus.Read(_state.PC);
            _state.PC++;
            LastAddress = InstructionPc;
            LastData = null;
            LastPins = "r-m-";
            _phase = 2;
            return;
        }

        if (_phase == 2)
        {
            // T3: odświeżanie pamięci
            LastAddress = _refreshAddress;
            LastData = Opcode;
            LastPins = "----";
            _phase = 3;
            return;
        }

        // _phase == 3: T4
        LastAddress = _refreshAddress;
        LastData = null;
        LastPins = "----";
        _state.R = (byte)((_state.R + 1) & 0x7F);
        if (Opcode == 0x00 && _state.EI != 0)
        {
            _state.EI--;
        }

        if (Opcode == 0x00)
        {
            _state.P = 0;
            _state.Q = 0;
        }

        _phase = 0;
    }

    public void Stall()
    {
        // WAIT: zegar biegnie, stan stoi.
        _cycles++;
    }
}
