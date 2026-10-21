using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

public enum CpuAction
{
    Lda, Ldx, Ldy, Sta, Stx, Sty, Adc, Sbc,
    Inc, Dec, Inx, Dex, Iny, Dey,
    Tax, Tay, Txa, Tya, Jmp, Nop, Brk,
    Stz, IncA, DecA,
}

public enum OpKind
{
    Read,
    Write,
    ReadModifyWrite,
    Implied,
    Jump,
    Accumulator,
}

/// <summary>Wpis tablicy wariantu rdzenia cyklowego.</summary>
public readonly record struct CpuOpcode(
    byte Opcode,
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
/// Wariant (<see cref="CpuVariant"/>) dostarcza tabelę opkodów i styl RMW; domyślnie NMOS 6502.
/// Obsługiwane tryby: implied, immediate, zero page, zero page,X, absolute, absolute,X.
/// Tryb pośredni <c>(zp),Y</c> wymaga dodatkowych faz i nie jest jeszcze zaimplementowany —
/// tablica go nie zawiera, więc zamiast cichego błędu CPU wejdzie w stan JAM.
/// </remarks>
public sealed class StateMachineCpuCore : ICpuCore<CpuState>
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
        AccumulatorRead,
        RmwOld,
        RmwNew,
    }

    private readonly IMemoryBus _bus;
    private readonly CpuOpcode?[] _table;

    // Styl RMW wariantu czytany raz w konstruktorze — w Tick tylko odczyt pola.
    private readonly bool _doubleReadRmw;

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

    /// <summary>Rdzeń NMOS 6502 (<see cref="CpuVariants.Nmos6502"/>).</summary>
    public StateMachineCpuCore(IMemoryBus bus)
        : this(bus, CpuVariants.Nmos6502)
    {
    }

    /// <summary>Rdzeń wskazanego wariantu rodziny 6502.</summary>
    public StateMachineCpuCore(IMemoryBus bus, CpuVariant variant)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(variant);

        _bus = bus;
        _doubleReadRmw = variant.Rmw == RmwStyle.DoubleRead;
        _table = new CpuOpcode?[256];

        foreach (CpuOpcode entry in variant.Opcodes)
        {
            _table[entry.Opcode] = entry;
        }

        _state = CpuState.Reset();
    }

    /// <summary>Tablica o dowolnej zawartości; styl RMW jak w NMOS (zgodność wsteczna).</summary>
    public StateMachineCpuCore(IMemoryBus bus, IReadOnlyList<CpuOpcode> table)
        : this(bus, new CpuVariant("niestandardowy", table, RmwStyle.DoubleWrite))
    {
    }

    public ref CpuState State => ref _state;

    /// <summary>Przybliżenie — 0 gdy rdzeń stoi na granicy instrukcji, inaczej 1.</summary>
    public int Pending => _phase == Phase.Fetch ? 0 : 1;

    public bool Jammed => _jammed;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public byte Opcode { get; private set; }

    /// <summary>Tablica wariantu NMOS 6502 (zgodność wsteczna).</summary>
    public static IReadOnlyList<CpuOpcode> Table => CpuVariants.Nmos6502.Opcodes;

    /// <summary>
    /// Wpis wariantu NMOS dla danego bajtu opkodu; <c>null</c>, gdy nie jest obsługiwany.
    /// Ścieżka diagnostyczna — nie używaj na gorącej ścieżce <see cref="Tick"/>.
    /// </summary>
    public static CpuOpcode? Describe(byte opcode) => CpuVariants.Nmos6502.Describe(opcode);

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
                // 65C02 STZ zp,X czyta spod adresu zerowej strony przed dodaniem X (wektory SingleStepTests).
                // Dla NMOS ta faza pozostaje bez dummy read — znany błąd, patrz ETAP 2 w planie.
                if (_entry.Action == CpuAction.Stz)
                {
                    _ = _bus.Read(_lo);
                }

                _address = (byte)(_lo + _state.X);
                _phase = Phase.Access;
                break;

            case Phase.OperandHigh:
                FetchOperandHigh();
                break;

            case Phase.DummyRead:
                // NMOS czyta spod adresu bez korekty strony (ghost byte) — urządzenia to widzą.
                // 65C02 STZ abs,X czyta ponownie bajt operandu wysokiego (wektory SingleStepTests).
                _ = _bus.Read(_entry.Action == CpuAction.Stz
                    ? (ushort)(_state.PC - 1)
                    : (ushort)((_baseAddress & 0xFF00) | ((_baseAddress + _state.X) & 0xFF)));
                _phase = Phase.Access;
                break;

            case Phase.Access:
                Access();
                break;

            case Phase.Internal:
                AccessImplied();
                break;

            case Phase.AccumulatorRead:
                // 65C02 INC A / DEC A: dummy read spod PC i zmiana akumulatora w tym samym cyklu.
                _ = _bus.Read(_state.PC);
                ApplyImplied(_entry.Action);
                _phase = Phase.Fetch;
                break;

            case Phase.RmwOld:
                if (_doubleReadRmw)
                {
                    _ = _bus.Read(_address);
                }
                else
                {
                    _bus.Write(_address, _value);
                }

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

            case OpKind.Accumulator:
                _phase = Phase.AccumulatorRead;
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
        _ => 0, // STZ zapisuje zero.
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
            case CpuAction.IncA:
                _state.A++;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.DecA:
                _state.A--;
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
}
