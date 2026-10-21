using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.C64;

/// <summary>
/// Adapter nad istniejącym <see cref="Cpu{TBus}"/>. Pozwala podłączyć model
/// atomowy do zegara maszyny bez przepisywania logiki opkodów.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ograniczenie:</b> <see cref="Cpu{TBus}.Step"/> wykonuje całą instrukcję naraz,
/// więc wszystkie jej dostępy do magistrali widzą ten sam moment. Cykle są rozłożone
/// w czasie po fakcie, na podstawie <see cref="CycleCostTable"/>. To wystarcza do testów
/// logiki (ALU, adresowanie, bankowanie), ale <b>nie jest zgodne cyklowo</b> —
/// efekt rastrowy zobaczy zdarzenie o kilka dotów za wcześnie
/// (patrz <c>tools/CycleModel</c>: 9 dotów = 3 piksele).
/// </para>
/// <para>
/// Do zgodności cyklowej używaj <see cref="StateMachineCpuCore"/>.
/// </para>
/// </remarks>
public sealed class AtomicCpuCore : ICpuCore<CpuState>
{
    private readonly Cpu<C64Bus> _cpu;
    private int _pending;
    private long _cycles;
    private bool _jammed;

    public AtomicCpuCore(C64Bus bus, OpcodeTable table)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(table);

        _cpu = new Cpu<C64Bus>(bus, table);
    }

    public ref CpuState State => ref _cpu.State;

    public int Pending => _pending;

    public bool Jammed => _jammed;

    public long Cycles => _cycles;

    public ushort InstructionPc { get; private set; }

    public byte Opcode { get; private set; }

    public void Reset(ushort pc = 0)
    {
        _cpu.Reset(pc);
        _pending = 0;
        _cycles = 0;
        _jammed = false;
        InstructionPc = pc;
        Opcode = 0;
    }

    public void Tick()
    {
        _cycles++;

        if (_jammed)
        {
            return;
        }

        if (_pending > 0)
        {
            _pending--;
            return;
        }

        Instruction instruction;

        InstructionPc = _cpu.State.PC;
        Opcode = _cpu.Bus.Read(InstructionPc);

        try
        {
            instruction = _cpu.Step();
        }
        catch (InvalidOperationException)
        {
            // Niezdefiniowany opkod — zachowujemy się jak JAM, a nie wywalamy maszyny.
            _jammed = true;
            return;
        }

        _pending = CycleCostTable.For(instruction) - 1;
    }

    public void Stall()
    {
        // Cykl ukradziony przez VIC-II: CPU stoi, więc <see cref="Cycles"/> nie rośnie.
    }
}
