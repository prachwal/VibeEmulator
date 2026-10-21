using VibeEmulator.Core.C64;
using VibeEmulator.Core.Cpu;

namespace C64Trace;

/// <summary>Ślad jednego cyklu maszyny.</summary>
/// <param name="InstructionStart">Cykl pobrania opkodu (CPU wykonał cykl i stał na granicy instrukcji).</param>
/// <param name="InstructionDone">Instrukcja ukończona: po ticku <c>Pending == 0</c> i cykl nie był ukradziony (AEC).</param>
internal sealed record CycleTrace(
    long Cycle,
    int Line,
    int LineCycle,
    bool Aec,
    bool Ba,
    ushort InstructionPc,
    byte Opcode,
    string Instruction,
    CpuState Registers,
    bool Jammed,
    bool InstructionStart,
    bool InstructionDone,
    IReadOnlyList<(bool Write, ushort Address, byte Value)> Accesses);

/// <summary>
/// Debugger maszyny C64: wykonuje program cykl po cyklu i zbiera kompletny stan,
/// łącznie z każdym dostępem do magistrali w danym cyklu.
/// </summary>
/// <remarks>
/// Tracer dopina własnego obserwatora do <see cref="C64Bus.Observer"/> bez nadpisywania cudzego:
/// wszyscy obserwatorzy siedzą w jednym <see cref="CompositeBusObserver"/>. <see cref="Dispose"/>
/// odłącza tylko własny rejestrator i przywraca stan slotu sprzed konstruktora.
/// </remarks>
internal sealed class MachineTracer : IDisposable
{
    private readonly C64Machine _machine;
    private readonly List<(bool Write, ushort Address, byte Value)> _accesses = [];

    // Delegat tworzony raz: dekoder dostaje Peek bez alokacji na każdy cykl.
    private readonly Func<ushort, byte> _peek;
    private readonly Recorder _recorder;
    private readonly CompositeBusObserver _composite;
    private readonly bool _ownsComposite;
    private readonly IBusObserver? _previousObserver;
    private bool _disposed;

    public MachineTracer(C64Machine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        _machine = machine;
        _peek = machine.Bus.Peek;
        _recorder = new Recorder(this);
        _previousObserver = machine.Bus.Observer;

        if (_previousObserver is CompositeBusObserver existing)
        {
            // Slot już składa wielu obserwatorów — dokładamy się do niego.
            _composite = existing;
        }
        else
        {
            // Slot pusty albo zajęty przez pojedynczego obserwatora: composite z istniejącym
            // obserwatorem na początku, a nasz rejestrator dopisujemy na końcu.
            _composite = new CompositeBusObserver();
            _ownsComposite = true;

            if (_previousObserver is not null)
            {
                _composite.Add(_previousObserver);
            }

            machine.Bus.Observer = _composite;
        }

        _composite.Add(_recorder);
    }

    public IEnumerable<CycleTrace> Trace(int cycles)
    {
        for (int i = 0; i < cycles; i++)
        {
            // Stan VIC przed tickiem — to on decyduje o AEC w tym cyklu.
            int line = _machine.Vic.Line;
            int lineCycle = _machine.Vic.Cycle;

            // Granica instrukcji przed tickiem: Pending == 0 oznacza pobranie opkodu.
            bool atBoundary = _machine.Cpu.Pending == 0;

            _accesses.Clear();

            _machine.Tick();

            bool aec = _machine.Vic.LastAec;
            ushort instructionPc = _machine.Cpu.InstructionPc;
            byte opcode = _machine.Cpu.Opcode;
            string instruction = Disassembler.Decode(_peek, instructionPc, out _);

            yield return new CycleTrace(
                _machine.TotalCycles,
                line,
                lineCycle,
                Aec: aec,
                Ba: _machine.Vic.LastBa,
                instructionPc,
                opcode,
                instruction,
                _machine.Cpu.State,
                _machine.Cpu.Jammed,
                InstructionStart: aec && atBoundary,
                InstructionDone: aec && _machine.Cpu.Pending == 0,
                [.. _accesses]);
        }
    }

    /// <summary>
    /// Usuwa własny rejestrator. Gdy tracer założył composite, a slot nadal na nim stoi, przywraca
    /// poprzedni stan: slot pusty, albo cudzego obserwatora z powrotem na slot (composite znika).
    /// Pusty composite założony przez kogoś innego zostawiamy w spokoju.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _composite.Remove(_recorder);

        if (!_ownsComposite)
        {
            return;
        }

        C64Bus bus = _machine.Bus;

        // Ktoś w międzyczasie podmienił slot — nie ruszamy go.
        if (!ReferenceEquals(bus.Observer, _composite))
        {
            return;
        }

        if (_composite.Count == 0)
        {
            bus.Observer = null;
        }
        else if (_composite.Count == 1 && _previousObserver is not null)
        {
            bus.Observer = _previousObserver;
        }
    }

    private sealed class Recorder(MachineTracer owner) : IBusObserver
    {
        public void OnAccess(bool write, ushort address, byte value) =>
            owner._accesses.Add((write, address, value));
    }
}
