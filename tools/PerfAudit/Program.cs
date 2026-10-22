using System.Runtime.InteropServices;
using PerfAudit;

if (!Options.TryParse(args, out Options options, out string? error))
{
    Console.Error.WriteLine($"error: {error}");
    return 2;
}

if (options.ShowHelp)
{
    Console.WriteLine(Options.Usage);
    return 0;
}

#if DEBUG
if (!options.VerifyOnly)
{
    Console.Error.WriteLine("error: pomiary wymagają Release; użyj dotnet run -c Release --project tools/PerfAudit.");
    return 2;
}
#endif

try
{
    Verification.Run(options.Seed);
    Console.WriteLine("Weryfikacja: NOP/JMP, 8 konfiguracji banków, opóźnienie portu, RAM/ROM/I/O, RMW i open bus — OK.");

    if (options.VerifyOnly)
    {
        return 0;
    }

    Console.WriteLine($"{RuntimeInformation.FrameworkDescription}; {RuntimeInformation.ProcessArchitecture}; CPU: {Environment.ProcessorCount}");
    Console.WriteLine($"Load average: {(File.Exists("/proc/loadavg") ? File.ReadAllText("/proc/loadavg").Trim() : "niedostępne")}");
    Console.WriteLine($"Rundy: {options.Rounds}; klatki/rundę: {options.Frames}; iteracje/rundę: {options.Iterations}; seed: {options.Seed}");
    Console.WriteLine("Rozgrzewka: co najmniej 300 ms na przypadek; przypadki przeplatane w rundach.");
    Console.WriteLine($"{"przypadek",-26}{"min",12}{"mediana",12}  {"jednostka",-10}{"max B/rundę",13}  sink");

    foreach (Measurement result in BenchmarkHarness.Measure(Workloads.Create(options), options.Rounds))
    {
        Console.WriteLine($"{result.Name,-26}{result.Minimum,12:F3}{result.Median,12:F3}  {result.Unit,-10}{result.AllocatedBytes,13}  {result.Sink}");
    }

    return 0;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}
