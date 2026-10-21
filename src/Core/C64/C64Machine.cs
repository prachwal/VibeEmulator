using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.C64;

/// <summary>
/// Kompozycja całej maszyny: zegar + CPU + VIC-II + 2× CIA + SID + magistrala.
/// </summary>
/// <remarks>
/// Kolejność w <see cref="Tick"/> ma znaczenie: VIC-II decyduje, czy CPU dostanie
/// dostęp do magistrali w tym cyklu. To on jest nadrzędny, nie procesor.
/// </remarks>
public sealed class C64Machine
{
    public const int CyclesPerLine = VicII.CyclesPerLine;
    public const int LinesPerFrame = VicII.LinesPerFrame;
    public const int CyclesPerFrame = CyclesPerLine * LinesPerFrame;

    private readonly C64Bus _bus;
    private readonly VicII _vic;
    private readonly Cia6526 _cia1;
    private readonly Cia6526 _cia2;
    private readonly Sid6581 _sid;
    private readonly ICpuCore<CpuState> _cpu;

    private C64Machine(C64Bus bus, VicII vic, Cia6526 cia1, Cia6526 cia2, Sid6581 sid, ICpuCore<CpuState> cpu)
    {
        _bus = bus;
        _vic = vic;
        _cia1 = cia1;
        _cia2 = cia2;
        _sid = sid;
        _cpu = cpu;
    }

    /// <summary>Maszyna z rdzeniem atomowym — szybka, ale nie cycle-exact.</summary>
    public static C64Machine CreateAtomic(OpcodeTable? table = null)
    {
        (C64Bus bus, VicII vic, Cia6526 cia1, Cia6526 cia2, Sid6581 sid) = CreateDevices();
        var cpu = new AtomicCpuCore(bus, table ?? Cpu6502.CreateTable());

        return new C64Machine(bus, vic, cia1, cia2, sid, cpu);
    }

    private static (C64Bus Bus, VicII Vic, Cia6526 Cia1, Cia6526 Cia2, Sid6581 Sid) CreateDevices()
    {
        var vic = new VicII();
        var cia1 = new Cia6526();
        var cia2 = new Cia6526();
        var sid = new Sid6581();
        var bus = new C64Bus(vic, cia1, cia2, sid);

        return (bus, vic, cia1, cia2, sid);
    }

    /// <summary>Maszyna z rdzeniem cyklowym — maszyna stanów, zgodna co do cyklu.</summary>
    public static C64Machine CreateCycleExact() => CreateCycleExact(null, null, null);

    public static C64Machine CreateCycleExact(byte[]? basicRom, byte[]? kernalRom, byte[]? charRom)
    {
        var vic = new VicII();
        var cia1 = new Cia6526();
        var cia2 = new Cia6526();
        var sid = new Sid6581();
        var bus = new C64Bus(vic, cia1, cia2, sid, basicRom, kernalRom, charRom);
        var cpu = new StateMachineCpuCore(bus);

        return new C64Machine(bus, vic, cia1, cia2, sid, cpu);
    }

    public C64Bus Bus => _bus;

    public VicII Vic => _vic;

    public Cia6526 Cia1 => _cia1;

    public Cia6526 Cia2 => _cia2;

    public Sid6581 Sid => _sid;

    public ICpuCore<CpuState> Cpu => _cpu;

    public long TotalCycles { get; private set; }

    public long Frames { get; private set; }

    public void Reset(ushort pc = 0)
    {
        _vic.Reset();
        _cia1.Reset();
        _cia2.Reset();
        _sid.Reset();
        _cpu.Reset(pc);
        TotalCycles = 0;
        Frames = 0;
    }

    /// <summary>Jeden cykl maszyny.</summary>
    public void Tick()
    {
        VicTickResult vic = _vic.Tick();

        if (vic.Aec)
        {
            _cpu.Tick();
        }
        else
        {
            _cpu.Stall();
        }

        _cia1.Tick();
        _cia2.Tick();
        Sid6581.Tick();
        _bus.Port.Tick();

        TotalCycles++;
    }

    /// <summary>Pełna klatka PAL: 19 656 cykli.</summary>
    public void RunFrame()
    {
        for (int i = 0; i < CyclesPerFrame; i++)
        {
            Tick();
        }

        Frames++;
    }
}
