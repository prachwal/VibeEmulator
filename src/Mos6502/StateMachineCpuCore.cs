using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

public enum CpuAction
{
    Lda, Ldx, Ldy, Sta, Stx, Sty, Adc, Sbc,
    Inc, Dec, Inx, Dex, Iny, Dey,
    Tax, Tay, Txa, Tya, Jmp, Nop, Brk,
    Stz, IncA, DecA,
    Tsx, Txs, Clc, Sec, Cli, Sei, Clv, Cld, Sed,

    // Etap A: logika i porównania (opkody Read; bez zmian faz).
    And, Ora, Eor, Cmp, Cpx, Cpy, Bit,

    // Etap B: przesunięcia i rotacje. Wariant pamięciowy to RMW, akumulatorowy to Accumulator.
    Asl, Lsr, Rol, Ror,

    // Etap C: rozgałęzienia warunkowe (OpKind.Branch; warunek sprawdzany w fazie BranchTaken).
    Bpl, Bmi, Bvc, Bvs, Bcc, Bcs, Bne, Beq,

    // Etap D: stos. Push zapisuje pod $0100+S i dekrementuje S; pull czyta spod $0100+S po inkrementacji.
    Pha, Php, Pla, Plp,
}

public enum OpKind
{
    Read,
    Write,
    ReadModifyWrite,
    Implied,
    Jump,
    Accumulator,

    // Etap C: rozgałęzienia warunkowe. Mają własną fazę (BranchTaken), bo cykle zależą od warunku
    // i przekroczenia strony — nie da się ich wyrazić przez Read ani Jump.
    Branch,

    // Etap D: stos. Push = dummy read spod PC + zapis pod $0100+S; pull = dummy read spod PC
    // + dummy read spod $0100+S + odczyt spod $0100+S po inkrementacji. Osobne rodzaje, bo liczba
    // faz (3 vs 4) wynika z sekwencji dostępów, nie z trybu adresowania (wszystkie Implied).
    Push,
    Pull,
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
/// Wariant (<see cref="CpuVariant"/>) dostarcza tabelę opkodów, styl RMW i adres dummy read przy indeksowaniu;
/// domyślnie NMOS 6502. Obsługiwane tryby: implied, immediate, zero page (+X, +Y), absolute (+X, +Y),
/// (zp,X), (zp),Y i relative (rozgałęzienia). Zgodność z wektorami SingleStepTests/65x02 — patrz <c>Vectors/README.md</c>.
/// Tryb <c>Indirect</c> (JMP (abs)) nie ma jeszcze fazy — tablica go nie zawiera.
/// </remarks>
public sealed partial class StateMachineCpuCore : ICpuCore<CpuState>
{
    private enum Phase
    {
        Fetch,
        OperandLow,
        IndexInternal,
        OperandHigh,
        DummyRead,
        Access,
        ImpliedRead,
        Internal,
        AccumulatorRead,
        PointerLow,
        PointerHigh,
        RmwOld,
        RmwNew,

        // Etap C: rozgałęzienia. BranchOperand = odczyt offsetu; BranchTaken = dummy read spod PC
        // i test warunku; BranchCrossed = dummy read przy przejściu strony. Rozdzielone, bo harness
        // wymaga dokładnie jednego dostępu na Tick, a każdy cykl rozgałęzienia to jeden odczyt.
        BranchOperand,
        BranchTaken,
        BranchCrossed,

        // Etap D: stos. StackDummy = dummy read spod PC (wspólny dla push i pull);
        // Push = zapis pod $0100+S i S--; PullDummy = dummy read spod $0100+S (stare S) i S++;
        // Pull = odczyt spod $0100+S (nowe S) i zastosowanie (PLA/PLP). Rozdzielone, bo każdy
        // cykl to jeden dostęp, a pull ma dwa odczyty stosu (dummy + właściwy).
        StackDummy,
        Push,
        PullDummy,
        Pull,
    }

    private readonly IMemoryBus _bus;
    private readonly CpuOpcode?[] _table;

    // Ustawienia wariantu czytane raz w konstruktorze — w Tick tylko odczyt pola.
    private readonly bool _doubleReadRmw;
    private readonly bool _ghostIndexedDummy;

    private CpuState _state;

    private Phase _phase;
    private CpuOpcode _entry;
    private byte _lo;
    private byte _hi;
    private byte _pointer;
    private byte _index;
    private ushort _address;
    private ushort _baseAddress;
    private byte _value;
    private int _internal;
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
        _ghostIndexedDummy = variant.IndexedDummy == IndexedDummyRead.GhostByte;
        _table = new CpuOpcode?[256];

        foreach (CpuOpcode entry in variant.Opcodes)
        {
            _table[entry.Opcode] = entry;
        }

        _state = CpuState.Reset();
    }

    /// <summary>Tablica o dowolnej zawartości; styl RMW i dummy read jak w NMOS (zgodność wsteczna).</summary>
    public StateMachineCpuCore(IMemoryBus bus, IReadOnlyList<CpuOpcode> table)
        : this(bus, new CpuVariant("niestandardowy", table, RmwStyle.DoubleWrite, IndexedDummyRead.GhostByte))
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
}
