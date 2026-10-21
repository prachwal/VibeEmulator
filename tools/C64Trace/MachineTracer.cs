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
internal sealed class MachineTracer
{
    private readonly C64Machine _machine;
    private readonly List<(bool Write, ushort Address, byte Value)> _accesses = [];
    private readonly RamReader _reader;

    public MachineTracer(C64Machine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        _machine = machine;
        _reader = new RamReader(machine.Bus);
        _machine.Bus.Observer = new Recorder(this);
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
            string instruction = Disassembler.Decode(_reader, instructionPc, out _);

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

    private sealed class Recorder(MachineTracer owner) : IBusObserver
    {
        public void OnAccess(bool write, ushort address, byte value) =>
            owner._accesses.Add((write, address, value));
    }

    private readonly struct RamReader(C64Bus bus) : Disassembler.IMemoryReader
    {
        public byte Peek(ushort address) => bus.Ram[address];
    }
}
