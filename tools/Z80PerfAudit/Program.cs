using System.Diagnostics;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

const int rounds = 9;
const int tStatesPerRound = 4_000_000;
Workload[] workloads =
[
    new("z80-nop-jp", [0x00, 0xC3, 0x00, 0x00], 14),
    new("z80-register-alu", [0x78, 0x81, 0x14, 0xAB, 0xC3, 0x00, 0x00], 26),
];

foreach (Workload workload in workloads)
{
    Verify(workload);
    long sink = 0;
    for (int warmup = 0; warmup < 2; warmup++)
    {
        sink ^= Run(tStatesPerRound * 2, workload.Program);
    }

    double[] samples = new double[rounds];
    for (int round = 0; round < rounds; round++)
    {
        long start = Stopwatch.GetTimestamp();
        sink ^= Run(tStatesPerRound, workload.Program);
        samples[round] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / tStatesPerRound;
    }

    Array.Sort(samples);
    Console.WriteLine($"case={workload.Name} unit=ns/T-state rounds={rounds} work={tStatesPerRound} min={samples[0]:F3} median={samples[rounds / 2]:F3} sink={sink}");
}

static long Run(int tStates, byte[] program)
{
    FlatMemory memory = new();
    memory.Load(0, program);
    Z80Core cpu = new(memory, new FlatPorts());
    cpu.Reset(0);
    for (int i = 0; i < tStates; i++)
    {
        cpu.Tick();
    }

    return cpu.State.PC ^ cpu.State.R ^ cpu.Cycles;
}

static void Verify(Workload workload)
{
    FlatMemory memory = new();
    memory.Load(0, workload.Program);
    Z80Core cpu = new(memory, new FlatPorts());
    cpu.Reset(0);
    for (int i = 0; i < workload.TStatesPerIteration; i++)
    {
        cpu.Tick();
    }

    if (cpu.State.PC != 0 || cpu.Cycles != workload.TStatesPerIteration)
    {
        throw new InvalidOperationException($"Workload {workload.Name} nie kończy iteracji po {workload.TStatesPerIteration} T-state.");
    }
}

internal sealed record Workload(string Name, byte[] Program, int TStatesPerIteration);
