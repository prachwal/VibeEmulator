using System.Runtime.CompilerServices;
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

    // Etap E: sterowanie. Jsr odkłada adres powrotu (PCH, PCL) i skacze; Rts zdejmuje PC i dodaje 1;
    // Rti zdejmuje P (maska jak PLP), potem PC bez inkrementacji.
    Jsr, Rts, Rti,

    // Etap H: opkody wyłącznie 65C02. Phx/Phy/Plx/Ply korzystają z faz Push/Pull (etap D);
    // Rmb/Smb czyszczą/ustawiają bit o numerze z CpuOpcode.BitNumber w pamięci (zp, RMW);
    // Bbr/Bbs to gałęzie testujące bit pamięci (tryb zp,rel); Bra to gałąź bezwarunkowa;
    // Tsb/Trb to RMW z akumulatorem (Z z A & M); Wai czeka na IRQ, Stp zatrzymuje rdzeń.
    Phx, Phy, Plx, Ply, Rmb, Smb, Bbr, Bbs, Bra, Tsb, Trb, Wai, Stp,

    // Etap J: nieoficjalne NMOS. Rodziny łączące (RMW + ALU) i niestabilne (wynik zależy od układu —
    // zachowanie z wektorów): patrz OpcodeCatalog.Nmos.Unofficial.cs.
    Slo, Rla, Sre, Rra, Dcp, Isc,
    Lax, Sax, Anc, Alr, Arr, Axs,
    Las, Tas, Shx, Shy, Ahx, Xaa,

    // Etap I: NOP 65C02 w trybie abs (5C/DC/FC) czyta dummy spod PC-1, nie spod operandu —
    // NMOS-owe NOP-y (0C/1C/…) czytają operand, więc mają zwykłe Nop.
    NopDummy,

    // Etap K: KIL/JAM — rdzeń zatrzymuje się bez wyjątku. Akcja jest jawna (nie „nieznany bajt”),
    // a sekwencję dostępów wybiera OpKind.Kil.
    Kil,
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

    // Etap E: podprogramy i powroty. JSR/RTS/RTI mają własne fazy, bo sekwencje dostępów
    // (push adresu, pull PC/P, dummy ready) nie mieszczą się w Read/Write/Jump.
    Subroutine,

    // Etap F: BRK. Sekwencja siedmiu cykli: sygnatura, trzy pushy (PCH/PCL/P|B=1) i wektor
    // $FFFE/$FFFF — ta sama maszyna faz obsługuje też IRQ/NMI (wektor i inkrementacja PC
    // zależą od źródła, nie od tabeli).
    Interrupt,

    // Etap H: BBR/BBS (tryb zp,rel). Dwa odczyty zp (dummy i właściwy — wektory wdc65c02),
    // potem operand rel i — przy skoku — dummy read spod PC (+1 przy przekroczeniu strony).
    // Osobny rodzaj, bo sekwencja dostępów nie mieści się w Read (dwa odczyty zp) ani w Branch.
    BitBranch,

    // Etap I: jednobajtowe niezdefiniowane NOP-y 65C02 (02…FB oprócz imm/zp/zpx/abs).
    // Mają **1 cykl** — tylko fetch, bez drugiego odczytu (wektory wdc65c02); dlatego nie mogą
    // użyć Implied, które zawsze dokłada ImpliedRead (2 cykle).
    NopByte,

    // Etap K: KIL/JAM. Fetch + dummy read spod PC, potem 9 dostępów (wektory 02.json itd.:
    // $FFFF, $FFFE, $FFFE, $FFFF×6) — razem 11 cykli; na końcu rdzeń się zatrzymuje.
    // Osobny rodzaj, bo żadna istniejąca sekwencja nie daje tego wzorca dostępów.
    Kil,
}

/// <summary>Wpis tablicy wariantu rdzenia cyklowego.</summary>
public readonly record struct CpuOpcode(
    byte Opcode,
    string Mnemonic,
    AddressingMode Mode,
    OpKind Kind,
    CpuAction Action,
    int InternalCycles = 0,
    byte BitNumber = 0);

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
public sealed partial class StateMachineCpuCore : ICpuCore<CpuState>, IInterruptLines
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

        // Etap E: sterowanie. JSR: OperandLow (low celu), JsrDummy (dummy read stosu),
        // JsrPushHigh (zapis PCH adresu powrotu, S--), JsrPushLow (zapis PCL, S--),
        // JsrFetch (odczyt high-byte celu i skok). RTS: RtsDummy (dummy PC),
        // RtsPullDummy (dummy stos, S++), RtsPullLow (PCL, S++), RtsPullHigh (PCH, bez S++),
        // RtsFetch (dummy spod zdjętego PC, PC++). RTI: RtiDummy, RtiPullDummy, RtiPullStatus (P z maską
        // PLP, S++), RtiPullLow (PCL, S++), RtiPullHigh (PCH i skok bez inkrementacji, bez S++).
        // JMP (ind.): JmpIndirectLow (low spod wskaźnika), JmpIndirectHigh (high spod wskaźnika
        // z błędem strony w NMOS), JmpIndirectFixup (65C02: ponowny odczyt poprawnego high-byte).
        JsrDummy,
        JsrPushHigh,
        JsrPushLow,
        JsrFetch,
        RtsDummy,
        RtsPullDummy,
        RtsPullLow,
        RtsPullHigh,
        RtsFetch,
        RtiDummy,
        RtiPullDummy,
        RtiPullStatus,
        RtiPullLow,
        RtiPullHigh,
        JmpIndirectLow,
        JmpIndirectHigh,
        JmpIndirectFixup,

        // Etap F: przerwania (BRK/IRQ/NMI — siedem cykli, docs/cpu/interrupts-and-reset.md).
        // Cykl 1: BRK robi własny Fetch; IRQ/NMI czytają opkod odrzucony w StartInterrupt (bez PC++).
        // InterruptSignature = cykl 2: bajt sygnatury odrzucony; BRK robi PC++, IRQ/NMI nie.
        // Trzy pushy (PCH, PCL, P|B) i dwa odczyty wektora.
        InterruptSignature,
        InterruptPushPch,
        InterruptPushPcl,
        InterruptPushP,
        InterruptVectorLow,
        InterruptVectorHigh,

        // Etap G: 65C02, ADC/SBC z D=1 — jeden dodatkowy cykl po odczycie operandu (wektory wdc65c02).
        DecimalExtra,

        // Etap H: BBR/BBS (zp,rel). BitBranchDummy = dummy read zp (wektory: dwa odczyty zp);
        // BitBranchRead = odczyt zp i test bitu; BitBranchRel = operand rel (zawsze czytany);
        // BitBranchTaken/Crossed = dummy ready przy skoku (jak rozgałęzienia etapu C).
        BitBranchDummy,
        BitBranchRead,
        BitBranchRel,
        BitBranchTaken,
        BitBranchCrossed,

        // Etap H: JMP (abs,X) — odczyt wskaźnika po dummy read (bez zawijania strony, w przeciwieństwie
        // do wskaźników zerostronicowych w PointerLow/PointerHigh).
        JmpAxPointerLow,
        JmpAxPointerHigh,

        // Etap K: KIL — dummy read spod PC (KilDummy), potem kolejne cykle (KilRead), których adres
        // zależy od liczby pozostałych dostępów ($FFFF, $FFFE, $FFFE, potem $FFFF). Na końcu rdzeń
        // się zatrzymuje.
        KilDummy,
        KilRead,
    }

    /// <summary>Źródło sekwencji przerwań: instrukcja BRK albo linia IRQ/NMI.</summary>
    private enum InterruptSource : byte
    {
        Brk,
        Irq,
        Nmi,
    }

    private readonly IMemoryBus _bus;
    private readonly CpuOpcode?[] _table;

    // Ustawienia wariantu czytane raz w konstruktorze — w Tick tylko odczyt pola.
    private readonly bool _doubleReadRmw;
    private readonly bool _ghostIndexedDummy;
    private readonly bool _jmpNoWrap;
    private readonly bool _interruptClearsDecimal;
    private readonly bool _decimalExtraCycle;
    private readonly bool _decimalNzFromResult;

    // Etap P: Ricoh 2A03 nie ma trybu dziesiętnego, choć flaga D istnieje.
    private readonly bool _decimalMode;

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

    // Etap F: przerwania. _nmiPending = zalatentowane zbocze NMI; _deferIrq = opóźnienie IRQ
    // o jedną instrukcję po CLI/SEI/PLP (próbkowanie przed zmianą I, docs/cpu/interrupts-and-reset.md).
    private InterruptSource _interruptSource;
    private ushort _interruptVector;
    private bool _nmiPending;
    private bool _previousNmiLevel;
    private bool _deferIrq;

    // Etap H: WAI czeka na IRQ (Pending=1 do nadejścia), STP zatrzymuje rdzeń (jak JAM, ale jawnie).
    private bool _waiting;
    private bool _stopped;

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
        _jmpNoWrap = variant.JmpWrap == JmpIndirectWrap.NoWrap;
        _interruptClearsDecimal = variant.InterruptClearsDecimal;
        _decimalExtraCycle = variant.DecimalExtraCycle;
        _decimalNzFromResult = variant.DecimalNzFromResult;
        _decimalMode = variant.HasDecimalMode;
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

    /// <summary>Linia IRQ (aktywny poziom niski); próbkowana na granicy instrukcji.</summary>
    public bool IrqLevel { get; set; }

    /// <summary>Linia NMI (aktywny poziom niski); rdzeń wykrywa zbocze opadające.</summary>
    public bool NmiLevel { get; set; }

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
        _nmiPending = false;
        _previousNmiLevel = false;
        _deferIrq = false;
        _waiting = false;
        _stopped = false;
    }

    public void Tick()
    {
        _cycles++;

        // NMI jest wrażliwe na zbocze: detektor próbkuje linię w każdej fazie φ2 i utrzymuje
        // sygnał do obsłużenia (docs/cpu/interrupts-and-reset.md). Latch działa też przy JAM.
        if (NmiLevel && !_previousNmiLevel)
        {
            _nmiPending = true;
        }

        _previousNmiLevel = NmiLevel;

        if (_jammed || _stopped)
        {
            return;
        }

        // Etap H: WAI — CPU czeka w miejscu, aż IRQ zgłosi poziom (NMI też wybudza; obsłuży je Fetch).
        // Blokada tylko na granicy instrukcji — ostatnia faza Internal doliczy się (3 cykle WAI).
        if (_waiting && _phase == Phase.Fetch)
        {
            if (!IrqLevel && !_nmiPending)
            {
                return;
            }

            _waiting = false;
        }

        Execute();
    }

    public void Stall()
    {
        // Cykl ukradziony przez VIC-II — CPU nie wykonuje pracy.
    }
}
