using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Z80;

/// <summary>
/// Rdzeń Z80 w modelu cykl po cyklu. Jeden <see cref="Tick"/> = jeden T-state.
/// Etap Z2: ładowanie 8-bitowe (LD r,r', LD r,n, LD (HL),r, LD r,(HL), LD A,(rr/nn), LD (rr/nn),A).
/// </summary>
public sealed class Z80Core : ICpuCore<Z80State>
{
    private readonly IMemoryBus _bus;
    private readonly IPortBus _ports;

    private Z80State _state;
    private bool _jammed;
    private long _cycles;

    private int _tIndex;
    private int _tStates;
    private byte _opcode;

    private ushort _refreshAddr;
    private ushort _memAddr;
    private ushort _immAddr;
    private ushort _nnAddr;

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

    public int Pending => _tIndex == 0 ? 0 : _tStates - _tIndex;

    public void Reset(ushort pc = 0)
    {
        _state = Z80State.Reset();
        _state.PC = pc;
        _tIndex = 0;
        _tStates = 0;
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

        if (_tIndex == 0)
        {
            Fetch();
        }

        int t = _tIndex;
        ProcessTState(t);

        if (t == 3)
        {
            _state.R = (byte)((_state.R + 1) & 0x7F);
        }

        _tIndex++;
        if (_tIndex >= _tStates)
        {
            Commit();
            _tIndex = 0;
        }
    }

    public void Stall()
    {
        _cycles++;
    }

    private void Fetch()
    {
        InstructionPc = _state.PC;
        _opcode = _bus.Read(_state.PC);
        Opcode = _opcode;
        _state.PC++;
        _refreshAddr = (ushort)((_state.I << 8) | _state.R);

        _tStates = TStateCount(_opcode);

        _hl = (ushort)((_state.H << 8) | _state.L);
        _memAddr = _hl;

        if (IsLdRn(_opcode))
        {
            _immAddr = _state.PC;
        }

        if (_opcode == 0x3A || _opcode == 0x32)
        {
            byte lo = _bus.Read(_state.PC);
            byte hi = _bus.Read((ushort)(_state.PC + 1));
            _nnAddr = (ushort)(lo | (hi << 8));
        }
    }

    private ushort _hl;

    private static int TStateCount(byte op)
    {
        if (op == 0x00) return 4;
        if ((op & 0xC0) == 0x40)
        {
            if (op == 0x76) return 4; // HALT (nie Z2, ale bezpieczne)
            bool mem = ((op & 0x07) == 0x06) || (((op >> 3) & 0x07) == 0x06);
            return mem ? 7 : 4;
        }
        if (op is 0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E) return 7;
        if (op is 0x0A or 0x1A or 0x02 or 0x12) return 7;
        if (op is 0x3A or 0x32) return 13;
        return 4;
    }

    private static bool IsLdRn(byte op) =>
        op is 0x06 or 0x0E or 0x16 or 0x1E or 0x26 or 0x2E or 0x36 or 0x3E;

    private void ProcessTState(int t)
    {
        byte op = _opcode;

        // LD r,r' (4 T)
        if ((op & 0xC0) == 0x40 && op != 0x76 && ((op & 0x07) != 0x06) && (((op >> 3) & 0x07) != 0x06))
        {
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            return;
        }

        // LD r,n / LD (HL),n (7 T)
        if (IsLdRn(op))
        {
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = _immAddr; LastData = null; LastPins = "----"; }
            else if (t == 5) { LastAddress = _immAddr; LastData = null; LastPins = "r-m-"; }
            else { LastAddress = _immAddr; LastData = _bus.Read(_immAddr); LastPins = "----"; }
            return;
        }

        // LD r,(HL) / LD (HL),r (7 T)
        if ((op & 0xC0) == 0x40 && op != 0x76 && (((op & 0x07) == 0x06) || (((op >> 3) & 0x07) == 0x06)))
        {
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = _memAddr; LastData = null; LastPins = "----"; }
            else if (t == 5)
            {
                if (((op >> 3) & 0x07) == 0x06)
                {
                    // LD (HL),r : write
                    byte src = GetReg(op & 0x07);
                    LastAddress = _memAddr;
                    LastData = src;
                    LastPins = "-wm-";
                    _bus.Write(_memAddr, src);
                }
                else
                {
                    LastAddress = _memAddr;
                    LastData = null;
                    LastPins = "r-m-";
                }
            }
            else
            {
                LastAddress = _memAddr;
                if ((op & 0x07) == 0x06)
                {
                    byte val = _bus.Read(_memAddr);
                    LastData = val;
                }
                else
                {
                    LastData = null;
                }
                LastPins = "----";
            }
            return;
        }

        // LD A,(BC)/(DE) (7 T)
        if (op is 0x0A or 0x1A)
        {
            ushort addr = op == 0x0A ? (ushort)((_state.B << 8) | _state.C) : (ushort)((_state.D << 8) | _state.E);
            _memAddr = addr;
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = addr; LastData = null; LastPins = "----"; }
            else if (t == 5) { LastAddress = addr; LastData = null; LastPins = "r-m-"; }
            else { LastAddress = addr; LastData = _bus.Read(addr); LastPins = "----"; }
            return;
        }

        // LD (BC)/(DE),A (7 T)
        if (op is 0x02 or 0x12)
        {
            ushort addr = op == 0x02 ? (ushort)((_state.B << 8) | _state.C) : (ushort)((_state.D << 8) | _state.E);
            _memAddr = addr;
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = addr; LastData = null; LastPins = "----"; }
            else if (t == 5) { LastAddress = addr; LastData = _state.A; LastPins = "-wm-"; _bus.Write(addr, _state.A); }
            else { LastAddress = addr; LastData = null; LastPins = "----"; }
            return;
        }

        // LD A,(nn) (13 T)
        if (op == 0x3A)
        {
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = _state.PC; LastData = null; LastPins = "----"; } // low addr (already PC advanced? need _nn low)
            else if (t == 5) { LastAddress = _state.PC; LastData = null; LastPins = "r-m-"; }
            else if (t == 6) { LastAddress = _state.PC; LastData = _bus.Read(_state.PC); LastPins = "----"; }
            else if (t == 7) { LastAddress = (ushort)(_state.PC + 1); LastData = null; LastPins = "----"; }
            else if (t == 8) { LastAddress = (ushort)(_state.PC + 1); LastData = null; LastPins = "r-m-"; }
            else if (t == 9) { LastAddress = (ushort)(_state.PC + 1); LastData = _bus.Read((ushort)(_state.PC + 1)); LastPins = "----"; }
            else if (t == 10) { LastAddress = _nnAddr; LastData = null; LastPins = "----"; }
            else if (t == 11) { LastAddress = _nnAddr; LastData = null; LastPins = "r-m-"; }
            else { LastAddress = _nnAddr; LastData = _bus.Read(_nnAddr); LastPins = "----"; }
            return;
        }

        // LD (nn),A (13 T)
        if (op == 0x32)
        {
            if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
            else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
            else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
            else if (t == 3) { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
            else if (t == 4) { LastAddress = _state.PC; LastData = null; LastPins = "----"; }
            else if (t == 5) { LastAddress = _state.PC; LastData = null; LastPins = "r-m-"; }
            else if (t == 6) { LastAddress = _state.PC; LastData = _bus.Read(_state.PC); LastPins = "----"; }
            else if (t == 7) { LastAddress = (ushort)(_state.PC + 1); LastData = null; LastPins = "----"; }
            else if (t == 8) { LastAddress = (ushort)(_state.PC + 1); LastData = null; LastPins = "r-m-"; }
            else if (t == 9) { LastAddress = (ushort)(_state.PC + 1); LastData = _bus.Read((ushort)(_state.PC + 1)); LastPins = "----"; }
            else if (t == 10) { LastAddress = _nnAddr; LastData = null; LastPins = "----"; }
            else if (t == 11) { LastAddress = _nnAddr; LastData = _state.A; LastPins = "-wm-"; _bus.Write(_nnAddr, _state.A); }
            else { LastAddress = _nnAddr; LastData = null; LastPins = "----"; }
            return;
        }

        // NOP fallback
        if (t == 0) { LastAddress = InstructionPc; LastData = null; LastPins = "----"; }
        else if (t == 1) { LastAddress = InstructionPc; LastData = null; LastPins = "r-m-"; }
        else if (t == 2) { LastAddress = _refreshAddr; LastData = op; LastPins = "----"; }
        else { LastAddress = _refreshAddr; LastData = null; LastPins = "----"; }
    }

    private byte GetReg(int code) => code switch
    {
        0 => _state.B,
        1 => _state.C,
        2 => _state.D,
        3 => _state.E,
        4 => _state.H,
        5 => _state.L,
        6 => _bus.Read(_memAddr),
        7 => _state.A,
        _ => 0,
    };

    private void SetReg(int code, byte value)
    {
        switch (code)
        {
            case 0: _state.B = value; break;
            case 1: _state.C = value; break;
            case 2: _state.D = value; break;
            case 3: _state.E = value; break;
            case 4: _state.H = value; break;
            case 5: _state.L = value; break;
            case 7: _state.A = value; break;
        }
    }

    private void Commit()
    {
        byte op = _opcode;

        // LD r,r'
        if ((op & 0xC0) == 0x40 && op != 0x76 && ((op & 0x07) != 0x06) && (((op >> 3) & 0x07) != 0x06))
        {
            int dest = (op >> 3) & 0x07;
            int src = op & 0x07;
            byte val = src == 6 ? _bus.Read(_memAddr) : GetReg(src);
            if (dest != 6) SetReg(dest, val);
            // LD (HL),r handled separately below? Actually dest==6 means write to (HL) with src value.
            if (dest == 6)
            {
                byte srcVal = GetReg(src);
                _bus.Write(_memAddr, srcVal);
            }
            return;
        }

        // LD r,(HL)
        if ((op & 0xC0) == 0x40 && op != 0x76 && ((op & 0x07) == 0x06) && (((op >> 3) & 0x07) != 0x06))
        {
            // memory read result already fetched at T6; re-read (idempotent for FlatMemory)
            byte val = _bus.Read(_memAddr);
            SetReg((op >> 3) & 0x07, val);
            return;
        }

        // LD (HL),r
        if ((op & 0xC0) == 0x40 && op != 0x76 && ((op & 0x07) != 0x06) && (((op >> 3) & 0x07) == 0x06))
        {
            byte srcVal = GetReg(op & 0x07);
            _bus.Write(_memAddr, srcVal);
            return;
        }

        // LD r,n  (r != (HL); immediate w niskich 3 bitach = 110)
        if (IsLdRn(op) && (op & 0x07) == 0x06 && ((op >> 3) & 0x07) != 0x06)
        {
            // immediate value at _immAddr was read at T6; re-read
            byte val = _bus.Read(_immAddr);
            int dest = (op >> 3) & 0x07;
            SetReg(dest, val);
            _state.PC = (ushort)(_state.PC + 1); // skip immediate byte
            return;
        }

        // LD (HL),n
        if (op == 0x36)
        {
            byte val = _bus.Read(_immAddr);
            _bus.Write(_memAddr, val);
            _state.PC = (ushort)(_state.PC + 1);
            return;
        }

        // LD A,(BC)/(DE)
        if (op is 0x0A or 0x1A)
        {
            _state.A = _bus.Read(_memAddr);
            return;
        }

        // LD (BC)/(DE),A
        if (op is 0x02 or 0x12)
        {
            // already written during T-state
            return;
        }

        // LD A,(nn)
        if (op == 0x3A)
        {
            _state.A = _bus.Read(_nnAddr);
            _state.PC = (ushort)(_state.PC + 2);
            return;
        }

        // LD (nn),A
        if (op == 0x32)
        {
            // already written
            _state.PC = (ushort)(_state.PC + 2);
            return;
        }
    }
}
