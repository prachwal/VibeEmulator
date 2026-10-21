using C64Trace;
using VibeEmulator.Core.C64;

// Tracer maszyny C64: wykonuje program cykl po cyklu i pokazuje stan.
// Użycie: dotnet run -c Release --project tools/C64Trace -- --help

TraceOptions options = TraceOptions.Parse(args, out string? error);

if (error is not null)
{
    Console.Error.WriteLine($"error: {error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(TraceOptions.Usage);
    return 2;
}

if (options.Scenario == "--help")
{
    Console.WriteLine(TraceOptions.Usage);
    return 0;
}

C64Machine machine = ScenarioBuilder.Build(options.Scenario, out int totalCycles);

if (totalCycles == 0)
{
    // Scenariusz bez śladu: tylko pomiar.
    Console.WriteLine($"Scenariusz: {options.Scenario} · klatka PAL = {C64Machine.CyclesPerFrame}");
    Console.WriteLine();
    ScenarioBuilder.RunFrameBenchmark();
    return 0;
}

long firstCycle = Math.Max(1, options.From);
long lastCycle = options.To == 0 ? totalCycles : Math.Min(options.To, totalCycles);

if (firstCycle > totalCycles)
{
    Console.Error.WriteLine(
        $"error: --from {firstCycle} przekracza dostępny zakres 1..{totalCycles}.");
    return 2;
}

// Wykonujemy tyle cykli, ile potrzeba, by dojść do ostatniego wyświetlanego.
int cyclesToRun = (int)lastCycle;

var tracer = new MachineTracer(machine);
var selected = new List<CycleTrace>();

foreach (CycleTrace trace in tracer.Trace(cyclesToRun))
{
    if (trace.Cycle >= firstCycle && trace.Cycle <= lastCycle)
    {
        selected.Add(trace);
    }
}

Console.WriteLine(
    $"Scenariusz: {options.Scenario} · wykonano {cyclesToRun} cykli · " +
    $"wyświetlono {selected.Count} · klatka PAL = {C64Machine.CyclesPerFrame}");
Console.WriteLine();
Console.WriteLine("cykl  lin  c/l  AEC  BA   PC    opk  instrukcja      A  X  Y  SP  flagi     magistrala");
Console.WriteLine(new string('-', 104));

long lastInstructionPc = -1;

foreach (CycleTrace trace in selected)
{
    bool newInstruction = trace.InstructionPc != lastInstructionPc;
    lastInstructionPc = trace.InstructionPc;

    string accesses = string.Join(
        " ",
        trace.Accesses.Select(static a => $"{(a.Write ? 'W' : 'R')}${a.Address:X4}={a.Value:X2}"));

    string instruction = newInstruction ? trace.Instruction : "  ·";

    Console.WriteLine(
        $"{trace.Cycle,4}  {trace.Line,3}  {trace.LineCycle,3}  " +
        $"{(trace.Aec ? "cpu" : "---"),3}  {(trace.Ba ? "ok" : "BA"),2}  " +
        $"{trace.InstructionPc:X4}  {trace.Opcode:X2}   {instruction,-15} " +
        $"{trace.Registers.A:X2} {trace.Registers.X:X2} {trace.Registers.Y:X2} " +
        $"{trace.Registers.SP:X2}  {TraceDumper.FormatFlags(trace.Registers.P)}  " +
        $"{accesses}{(trace.Jammed ? "  [JAM]" : string.Empty)}");
}

Console.WriteLine();
Console.WriteLine(
    $"Cykle maszyny: {machine.TotalCycles} · cykle CPU: {machine.Cpu.Cycles} · " +
    $"ukradzione: {machine.TotalCycles - machine.Cpu.Cycles}");

if (options.DumpPath is not null)
{
    TraceDumper.Write(options.DumpPath, selected, options.DumpFormat);

    Console.WriteLine();
    Console.WriteLine(
        $"Zrzut: {Path.GetFullPath(options.DumpPath)} " +
        $"({selected.Count} rekordów, format {options.DumpFormat})");
}

return 0;
