using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.C64;

public enum CpuAction
{
    Lda, Ldx, Ldy, Sta, Stx, Sty, Adc, Sbc,
    Inc, Dec, Inx, Dex, Iny, Dey,
    Tax, Tay, Txa, Tya, Jmp, Nop, Brk,
}

public enum OpKind
{
    Read,
    Write,
    ReadModifyWrite,
    Implied,
    Jump,
}

/// <summary>Wpis tablicy rdzenia cyklowego.</summary>
public readonly record struct CpuOpcode(
    string Mnemonic,
    AddressingMode Mode,
    OpKind Kind,
    CpuAction Action,
    int InternalCycles = 0);

/// <summary>
/// Rdzeń CPU w modelu cyklowym: maszyna stanów, jeden <see cref="Tick"/> = jeden cykl.
/// Cykle nie są deklarowane w tabeli — <b>wynikają</b> z liczby faz wykonania,
/// a każda faza to jeden dostęp do magistrali.
/// </summary>
/// <remarks>
/// Obsługiwane tryby: implied, immediate, zero page, zero page,X, absolute, absolute,X.
/// Tryb pośredni <c>(zp),Y</c> wymaga dodatkowych faz i nie jest jeszcze zaimplementowany —
/// tablica go nie zawiera, więc zamiast cichego błędu CPU wejdzie w stan JAM.
/// </remarks>
public sealed class StateMachineCpuCore : ICpuCore
{
    private enum Phase
    {
        Fetch,
        OperandLow,
        IndexInternal,
        OperandHigh,
        DummyRead,
        Access,
        Internal,
        RmwOld,
        RmwNew,
    }

    private static readonly CpuOpcode[] DefaultTable = BuildDefaultTable();

    private readonly IMemoryBus _bus;
    private readonly CpuOpcode?[] _table;
    private CpuState _state;

    private Phase _phase;
    private CpuOpcode _entry;
    private byte _lo;
    private byte _hi;
    private ushort _address;
    private ushort _baseAddress;
    private byte _value;
    private int _internal;
    private bool _pageCrossed;
    private bool _jammed;
    private long _cycles;

    public StateMachineCpuCore(IMemoryBus bus)
        : this(bus, DefaultTable)
    {
    }

    public StateMachineCpuCore(IMemoryBus bus, IReadOnlyList<CpuOpcode> table)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(table);

        _bus = bus;
        _table = new CpuOpcode?[256];

        foreach (CpuOpcode entry in table)
        {
            for (int opcode = 0; opcode < 256; opcode++)
            {
                _ = opcode;
            }

            _table[OpcodeOf(entry)] = entry;
        }

        _state = CpuState.Reset();
    }

    public ref CpuState State => ref _state;

    /// <summary>Przybliżenie — 0 gdy rdzeń stoi na granicy instrukcji, inaczej 1.</summary>
    public int Pending => _phase == Phase.Fetch ? 0 : 1;

    public bool Jammed => _jammed;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public byte Opcode { get; private set; }

    public static IReadOnlyList<CpuOpcode> Table => DefaultTable;

    public void Reset(ushort pc = 0)
    {
        _state = CpuState.Reset();
        _state.PC = pc;
        _phase = Phase.Fetch;
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

        Execute();
    }

    public void Stall()
    {
        // Cykl ukradziony przez VIC-II — CPU nie wykonuje pracy.
    }

    private void Execute()
    {
        switch (_phase)
        {
            case Phase.Fetch:
                Fetch();
                break;

            case Phase.OperandLow:
                FetchOperandLow();
                break;

            case Phase.IndexInternal:
                _address = (byte)(_lo + _state.X);
                _phase = Phase.Access;
                break;

            case Phase.OperandHigh:
                FetchOperandHigh();
                break;

            case Phase.DummyRead:
                // Prawdziwy 6510 czyta spod adresu bez korekty strony — urządzenia to widzą.
                _ = _bus.Read((ushort)((_baseAddress & 0xFF00) | ((_baseAddress + _state.X) & 0xFF)));
                _phase = Phase.Access;
                break;

            case Phase.Access:
                Access();
                break;

            case Phase.Internal:
                AccessImplied();
                break;

            case Phase.RmwOld:
                _bus.Write(_address, _value);
                _phase = Phase.RmwNew;
                break;

            case Phase.RmwNew:
                _bus.Write(_address, ApplyRead(_entry.Action, _value));
                SetZeroNegative(ApplyRead(_entry.Action, _value));
                _phase = Phase.Fetch;
                break;

            default:
                _jammed = true;
                break;
        }
    }

    private void Fetch()
    {
        InstructionPc = _state.PC;
        byte opcode = _bus.Read(_state.PC);
        Opcode = opcode;
        _state.PC++;

        CpuOpcode? found = _table[opcode];

        if (found is null)
        {
            _jammed = true;
            return;
        }

        _entry = found.Value;

        switch (_entry.Kind)
        {
            case OpKind.Implied:
                _internal = Math.Max(1, _entry.InternalCycles);
                _phase = Phase.Internal;
                break;

            case OpKind.Jump:
                _phase = Phase.OperandLow;
                break;

            default:
                _phase = _entry.Mode switch
                {
                    AddressingMode.Immediate => Phase.Access,
                    AddressingMode.ZeroPage or AddressingMode.ZeroPageX => Phase.OperandLow,
                    AddressingMode.Absolute or AddressingMode.AbsoluteX => Phase.OperandLow,
                    _ => Jam(),
                };
                break;
        }
    }

    private Phase Jam()
    {
        _jammed = true;
        return Phase.Fetch;
    }

    private void FetchOperandLow()
    {
        _lo = _bus.Read(_state.PC);
        _state.PC++;

        if (_entry.Mode == AddressingMode.ZeroPage)
        {
            _address = _lo;
            _phase = Phase.Access;
            return;
        }

        if (_entry.Mode == AddressingMode.ZeroPageX)
        {
            // Dodanie X w stronie zerowej to osobny cykl wewnętrzny.
            _phase = Phase.IndexInternal;
            return;
        }

        _phase = Phase.OperandHigh;
    }

    private void FetchOperandHigh()
    {
        _hi = _bus.Read(_state.PC);
        _state.PC++;

        _baseAddress = (ushort)(_lo | (_hi << 8));

        if (_entry.Kind == OpKind.Jump)
        {
            _state.PC = _baseAddress;
            _phase = Phase.Fetch;
            return;
        }

        if (_entry.Mode == AddressingMode.Absolute)
        {
            _address = _baseAddress;
            _phase = Phase.Access;
            return;
        }

        _address = (ushort)(_baseAddress + _state.X);
        _pageCrossed = (_baseAddress & 0xFF00) != (_address & 0xFF00);

        // Zapis i RMW zawsze płacą dummy read; odczyt tylko przy przekroczeniu strony.
        bool needsDummy = _pageCrossed || _entry.Kind is OpKind.Write or OpKind.ReadModifyWrite;
        _phase = needsDummy ? Phase.DummyRead : Phase.Access;
    }

    private void Access()
    {
        switch (_entry.Kind)
        {
            case OpKind.Read:
                // Tryb immediate: operand jest pod PC, a nie pod adresem wyliczonym.
                if (_entry.Mode == AddressingMode.Immediate)
                {
                    _address = _state.PC;
                    _state.PC++;
                }

                byte value = _bus.Read(_address);
                ApplyReadToRegister(_entry.Action, value);
                _phase = Phase.Fetch;
                break;

            case OpKind.Write:
                _bus.Write(_address, ReadSourceRegister(_entry.Action));
                _phase = Phase.Fetch;
                break;

            case OpKind.ReadModifyWrite:
                _value = _bus.Read(_address);
                _phase = Phase.RmwOld;
                break;

            default:
                _jammed = true;
                break;
        }
    }

    private void AccessImplied()
    {
        ApplyImplied(_entry.Action);
        _internal--;

        if (_internal <= 0)
        {
            _phase = Phase.Fetch;
        }
    }

    private byte ReadSourceRegister(CpuAction action) => action switch
    {
        CpuAction.Sta => _state.A,
        CpuAction.Stx => _state.X,
        CpuAction.Sty => _state.Y,
        _ => 0,
    };

    private void ApplyReadToRegister(CpuAction action, byte value)
    {
        switch (action)
        {
            case CpuAction.Lda:
                _state.A = value;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Ldx:
                _state.X = value;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Ldy:
                _state.Y = value;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Adc:
                Adc(value);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Sbc:
                Sbc(value);
                SetZeroNegative(_state.A);
                break;
            default:
                _jammed = true;
                break;
        }
    }

    private static byte ApplyRead(CpuAction action, byte value) => action switch
    {
        CpuAction.Inc => (byte)(value + 1),
        CpuAction.Dec => (byte)(value - 1),
        _ => value,
    };

    private void ApplyImplied(CpuAction action)
    {
        switch (action)
        {
            case CpuAction.Inx:
                _state.X++;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Dex:
                _state.X--;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Iny:
                _state.Y++;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Dey:
                _state.Y--;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Tax:
                _state.X = _state.A;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Tay:
                _state.Y = _state.A;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Txa:
                _state.A = _state.X;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Tya:
                _state.A = _state.Y;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Nop:
                break;
            case CpuAction.Brk:
                _state.P = _state.P.With(StatusFlags.Break, true);
                break;
            default:
                _jammed = true;
                break;
        }
    }

    private void Adc(byte operand)
    {
        int carry = _state.P.Has(StatusFlags.Carry) ? 1 : 0;
        int sum = _state.A + operand + carry;
        byte result = (byte)sum;
        bool overflow = (~(_state.A ^ operand) & (_state.A ^ result) & 0x80) != 0;

        _state.A = result;
        _state.P = _state.P
            .With(StatusFlags.Carry, sum > 0xFF)
            .With(StatusFlags.Overflow, overflow);
    }

    private void Sbc(byte operand)
    {
        int borrow = _state.P.Has(StatusFlags.Carry) ? 0 : 1;
        int difference = _state.A - operand - borrow;
        byte result = (byte)difference;
        bool overflow = ((_state.A ^ operand) & (_state.A ^ result) & 0x80) != 0;

        _state.A = result;
        _state.P = _state.P
            .With(StatusFlags.Carry, difference >= 0)
            .With(StatusFlags.Overflow, overflow);
    }

    private void SetZeroNegative(byte value)
    {
        _state.P = _state.P
            .With(StatusFlags.Zero, value == 0)
            .With(StatusFlags.Negative, (value & 0x80) != 0);
    }

    private static int OpcodeOf(CpuOpcode entry) => entry.Action switch
    {
        CpuAction.Lda when entry.Mode == AddressingMode.Immediate => 0xA9,
        CpuAction.Lda when entry.Mode == AddressingMode.ZeroPage => 0xA5,
        CpuAction.Lda when entry.Mode == AddressingMode.ZeroPageX => 0xB5,
        CpuAction.Lda when entry.Mode == AddressingMode.Absolute => 0xAD,
        CpuAction.Lda when entry.Mode == AddressingMode.AbsoluteX => 0xBD,
        CpuAction.Ldx when entry.Mode == AddressingMode.Immediate => 0xA2,
        CpuAction.Ldx when entry.Mode == AddressingMode.ZeroPage => 0xA6,
        CpuAction.Ldy when entry.Mode == AddressingMode.Immediate => 0xA0,
        CpuAction.Ldy when entry.Mode == AddressingMode.ZeroPage => 0xA4,
        CpuAction.Sta when entry.Mode == AddressingMode.ZeroPage => 0x85,
        CpuAction.Sta when entry.Mode == AddressingMode.Absolute => 0x8D,
        CpuAction.Sta when entry.Mode == AddressingMode.AbsoluteX => 0x9D,
        CpuAction.Stx when entry.Mode == AddressingMode.ZeroPage => 0x86,
        CpuAction.Sty when entry.Mode == AddressingMode.ZeroPage => 0x84,
        CpuAction.Adc when entry.Mode == AddressingMode.Immediate => 0x69,
        CpuAction.Sbc when entry.Mode == AddressingMode.Immediate => 0xE9,
        CpuAction.Inc when entry.Mode == AddressingMode.ZeroPage => 0xE6,
        CpuAction.Inc when entry.Mode == AddressingMode.Absolute => 0xEE,
        CpuAction.Dec when entry.Mode == AddressingMode.ZeroPage => 0xC6,
        CpuAction.Dec when entry.Mode == AddressingMode.Absolute => 0xCE,
        CpuAction.Inx => 0xE8,
        CpuAction.Dex => 0xCA,
        CpuAction.Iny => 0xC8,
        CpuAction.Dey => 0x88,
        CpuAction.Tax => 0xAA,
        CpuAction.Tay => 0xA8,
        CpuAction.Txa => 0x8A,
        CpuAction.Tya => 0x98,
        CpuAction.Jmp => 0x4C,
        CpuAction.Nop => 0xEA,
        CpuAction.Brk => 0x00,
        _ => -1,
    };

    private static CpuOpcode[] BuildDefaultTable()
    {
        return
        [
            new("LDA", AddressingMode.Immediate, OpKind.Read, CpuAction.Lda),
            new("LDA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lda),
            new("LDA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Lda),
            new("LDA", AddressingMode.Absolute, OpKind.Read, CpuAction.Lda),
            new("LDA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Lda),
            new("LDX", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldx),
            new("LDX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldx),
            new("LDY", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldy),
            new("LDY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldy),
            new("STA", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sta),
            new("STA", AddressingMode.Absolute, OpKind.Write, CpuAction.Sta),
            new("STA", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Sta),
            new("STX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stx),
            new("STY", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sty),
            new("ADC", AddressingMode.Immediate, OpKind.Read, CpuAction.Adc),
            new("SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc),
            new("INC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Inc),
            new("INC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Inc),
            new("DEC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dec),
            new("DEC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dec),
            new("INX", AddressingMode.Implied, OpKind.Implied, CpuAction.Inx, 1),
            new("DEX", AddressingMode.Implied, OpKind.Implied, CpuAction.Dex, 1),
            new("INY", AddressingMode.Implied, OpKind.Implied, CpuAction.Iny, 1),
            new("DEY", AddressingMode.Implied, OpKind.Implied, CpuAction.Dey, 1),
            new("TAX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tax, 1),
            new("TAY", AddressingMode.Implied, OpKind.Implied, CpuAction.Tay, 1),
            new("TXA", AddressingMode.Implied, OpKind.Implied, CpuAction.Txa, 1),
            new("TYA", AddressingMode.Implied, OpKind.Implied, CpuAction.Tya, 1),
            new("NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1),
            new("BRK", AddressingMode.Implied, OpKind.Implied, CpuAction.Brk, 6),
            new("JMP", AddressingMode.Absolute, OpKind.Jump, CpuAction.Jmp),
        ];
    }
}
