using C64Trace;
using VibeEmulator.Core.C64;

// Tracer maszyny C64: wykonuje program cykl po cyklu i pokazuje stan.
// Użycie: dotnet run -c Release --project tools/C64Trace -- --help
// Kody wyjścia: 0 = OK, 2 = błąd użycia, 3 = CPU zawiesił się (JAM) bez --until jam.

const int ExitUsage = 2;
const int ExitJam = 3;

TraceOptions options = TraceOptions.Parse(args, out string? error);

if (error is not null)
{
    Console.Error.WriteLine($"error: {error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(TraceOptions.Usage);
    return ExitUsage;
}

if (options.ShowHelp)
{
    Console.WriteLine(TraceOptions.Usage);
    return 0;
}

// Komunikaty informacyjne i ostrzeżenia: stdout tylko dla text, w vice/json zawsze stderr.
TextWriter info = options.Format == "text" ? Console.Out : Console.Error;

C64Machine machine;
string scenario;
long maxCycles;

if (options.IsCustom)
{
    scenario = TraceOptions.CustomScenario;

    C64Machine? custom = CustomProgram.Create(options, out error);

    if (custom is null)
    {
        Console.Error.WriteLine($"error: {error}");
        Console.Error.WriteLine();
        Console.Error.WriteLine(TraceOptions.Usage);
        return ExitUsage;
    }

    machine = custom;
    // Bez --cycles, --until i --instr obowiązuje krótki domyślny limit; warunki zatrzymania
    // dostają przestrzeń do MaxCycles (ochrona przed zawieszeniem tracera).
    bool hasStopCondition = options.Until.Count > 0 || options.Instructions is not null;
    maxCycles = options.Cycles ?? (hasStopCondition ? TraceOptions.MaxCycles : TraceOptions.DefaultCustomCycles);
}
else
{
    scenario = options.Scenario;
    machine = ScenarioBuilder.Build(scenario, out int builtInCycles);

    if (builtInCycles == 0)
    {
        // Scenariusz bez śladu: tylko pomiar.
        info.WriteLine($"Scenariusz: {scenario} · klatka PAL = {C64Machine.CyclesPerFrame}");
        info.WriteLine();
        ScenarioBuilder.RunFrameBenchmark(info);
        return 0;
    }

    maxCycles = options.Cycles ?? builtInCycles;
}

CustomProgram.ApplyOverrides(machine, options);

if (options.To > maxCycles)
{
    Console.Error.WriteLine($"warning: --to {options.To} przekracza zakres, obcięto do {maxCycles}.");
}

long firstCycle = Math.Max(1, options.From);
long lastCycle = options.To == 0 ? maxCycles : Math.Min(options.To, maxCycles);

if (firstCycle > maxCycles)
{
    Console.Error.WriteLine(
        $"error: --from {firstCycle} przekracza dostępny zakres 1..{maxCycles}.");
    return ExitUsage;
}

// Powód "cycle-limit" dotyczy każdego przebiegu z jawnym limitem; "end" tylko wbudowanego bez limitu.
string limitReason =
    options.To != 0 || options.Cycles is not null || options.IsCustom ? "cycle-limit" : "end";

var stops = new StopConditions(options.Until, options.Instructions);
using var tracer = new MachineTracer(machine);
var selected = new List<CycleTrace>();
CycleTrace? last = null;
string? stop = null;

// Symulacja przerywa się przy zatrzymaniu; do listy trafiają tylko wiersze z okna --from/--to.
foreach (CycleTrace trace in tracer.Trace((int)lastCycle))
{
    last = trace;

    if (trace.Cycle >= firstCycle)
    {
        selected.Add(trace);
    }

    stop = stops.Evaluate(trace);

    if (stop is not null)
    {
        break;
    }
}

stop ??= limitReason;

var summary = new RunSummary(
    scenario,
    stop,
    machine.TotalCycles,
    machine.Cpu.Cycles,
    machine.TotalCycles - machine.Cpu.Cycles,
    machine.Cpu.Jammed,
    selected.Count);

info.WriteLine(TraceFormatter.ScenarioLine(summary));
info.WriteLine();

TraceFormatter.WriteTraces(Console.Out, selected, options.Format);

if (options.Format == "json")
{
    TraceFormatter.WriteJsonSummary(Console.Out, summary);
}
else
{
    TraceFormatter.WriteTextSummary(info, summary);
}

if (options.DumpPath is not null)
{
    try
    {
        TraceDumper.Write(options.DumpPath, selected, options.Format, summary);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
    {
        // Ślad jest już na stdout; błąd zapisu zgłaszamy bez Usage, kodem 2.
        Console.Error.WriteLine($"error: --dump: {ex.Message}");
        return ExitUsage;
    }

    info.WriteLine();
    info.WriteLine(
        $"Zrzut: {Path.GetFullPath(options.DumpPath)} " +
        $"({selected.Count} rekordów, format {options.Format})");
}

if (stop == "jam" && last is not null)
{
    Console.Error.WriteLine($"warning: CPU JAM w cyklu {last.Cycle} (PC={last.InstructionPc:X4})");
    return ExitJam;
}

return 0;
