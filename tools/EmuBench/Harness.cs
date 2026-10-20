using System.Diagnostics;

namespace EmuBench;

/// <summary>
/// Wspólna warstwa pomiarowa: warmup, N przeplatanych rund, minimum (nie średnia).
/// Zgodne z .github/skills/csharp-perf-benchmark/SKILL.md.
/// </summary>
internal static class Harness
{
    /// <summary>Pochłania wynik — uniemożliwia JIT eliminację mierzonego kodu.</summary>
    public static long Sink;

    public static double Measure(int ops, Func<long> run, int rounds = 5)
    {
        Sink = run(); // warmup: pozwala JIT wejść w tier-1 z PGO

        double best = double.MaxValue;
        for (int r = 0; r < rounds; r++)
        {
            long t0 = Stopwatch.GetTimestamp();
            Sink = run();
            double seconds = Stopwatch.GetElapsedTime(t0).TotalSeconds;

            if (seconds < best)
            {
                best = seconds;
            }
        }

        return best / ops * 1e9;
    }

    public static void Print(string title, IReadOnlyList<Result> results)
    {
        Console.WriteLine(title);
        Console.WriteLine(new string('-', 74));
        Console.WriteLine($"{"test",-34}{"ns/op",12}{"  note",26}");

        foreach (Result r in results)
        {
            Console.WriteLine($"{r.Name,-34}{r.NsPerOp,12:F3}{"  " + r.Note,26}");
        }

        Console.WriteLine();
    }

    public static long Garbage()
    {
        Console.WriteLine($"sink = {Sink}");
        return Sink;
    }

    public readonly record struct Result(string Name, double NsPerOp, string Note = "");
}
