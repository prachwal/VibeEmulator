using System.Globalization;
using EmuBench;

// Mikrobenchmarki aspektów emulacji: dispatch, pamięć, cache, ALU, bloki, prawdziwy CPU.
// Użycie: dotnet run -c Release --project tools/EmuBench -- [dispatch|memory|cache|alu|blocks|cpu|all] [rounds]

string section = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
int rounds = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 5;

Console.WriteLine($"EmuBench · .NET {Environment.Version} · {Environment.ProcessorCount} rdzeni · rounds={rounds} (best-of)");
Console.WriteLine();

const int DispatchOps = 10_000_000;
const int MemoryOps = 10_000_000;
const int AluOps = 20_000_000;
const int BlockOps = 10_000_000;

bool all = section is "all";

if (all || section is "dispatch")
{
    Harness.Print("A. DISPATCH — koszt pobrania i wywołania instrukcji", Dispatch.Run(DispatchOps, rounds));
}

if (all || section is "memory")
{
    Harness.Print("B. PAMIĘĆ — szerokość, wyrównanie, bankowanie, stronicowanie", Memory.Run(MemoryOps, rounds));
}

if (all || section is "cache")
{
    Harness.Print("C. CACHE — przepustowość przy rosnącym working secie", Cache.Run(rounds));
}

if (all || section is "alu")
{
    Harness.Print("D. ALU — szerokość rejestru, flagi, mnożenie, dzielenie", Alu.Run(AluOps, rounds));
}

if (all || section is "blocks")
{
    Harness.Print("E. BLOKI — interpreter vs uprzednio rozwiązana sekwencja", Blocks.Run(BlockOps, rounds));
}

if (all || section is "cpu")
{
    Harness.Print("F. PRAWDZIWY CPU — Cpu<TBus> z src/Mos6502", CpuRun.Run(BlockOps, rounds));
}

Harness.Garbage();
return 0;
