using System.Diagnostics;
using System.Globalization;
using VibeEmulator.Core;

// Pomiary: warmup, przeplatane rundy, minimum z rund (odporne na obciążenie systemu).
// Użycie: dotnet run -c Release --project tools/Bench -- [n] [rounds]

int n = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 38;
int rounds = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 7;

if (n is < 0 or > Fibonacci.MaxExactArgument)
{
    Console.Error.WriteLine($"n must be in 0..{Fibonacci.MaxExactArgument}");
    return 2;
}

// Liczba wywołań w rundzie dobrana tak, aby wynik był mierzalny (rzędu 50-200 ms).
// Rekurencja naiwna rośnie jak φ^n — powyżej progu pomiar trwałby godziny, więc ją pomijamy.
const int MaxRecursiveN = 40;

var cases = new List<(string Name, Func<int, long> Fn, int Calls)>
{
    ("Iterative    O(n)", Fibonacci.Iterative, 1_000_000),
    ("Memoized     O(n)", Fibonacci.Memoized, 100_000),
    ("FastDoubling O(log n)", Fibonacci.FastDoubling, 1_000_000),
};

if (n <= MaxRecursiveN)
{
    cases.Insert(0, ("Recursive    O(phi^n)", Fibonacci.Recursive, 2));
}
else
{
    Console.WriteLine($"Skipping Recursive for n = {n} (would take longer than a human lifetime).");
    Console.WriteLine();
}

// Warmup: pozwala JIT wejść w tier-1 z PGO.
foreach ((_, Func<int, long> fn, int calls) in cases)
{
    long warmupSink = 0;
    for (int i = 0; i < Math.Min(calls, 100_000); i++)
    {
        warmupSink += fn(n);
    }

    if (warmupSink == long.MinValue)
    {
        Console.Error.WriteLine("unreachable");
    }
}

double[] best = new double[cases.Count];
Array.Fill(best, double.MaxValue);

for (int round = 0; round < rounds; round++)
{
    for (int i = 0; i < cases.Count; i++)
    {
        Func<int, long> fn = cases[i].Fn;
        int calls = cases[i].Calls;

        long sink = 0;
        long start = Stopwatch.GetTimestamp();
        for (int k = 0; k < calls; k++)
        {
            sink += fn(n);
        }

        double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
        best[i] = Math.Min(best[i], seconds);

        if (sink == long.MinValue)
        {
            Console.Error.WriteLine("unreachable");
        }
    }
}

double[] nsPerCall = new double[cases.Count];
double fastestNs = double.MaxValue;
for (int i = 0; i < cases.Count; i++)
{
    nsPerCall[i] = best[i] / cases[i].Calls * 1e9;
    fastestNs = Math.Min(fastestNs, nsPerCall[i]);
}

Console.WriteLine($"n = {n}, rounds = {rounds}, best-of-rounds");
Console.WriteLine();
Console.WriteLine($"{"implementation",-24} {"total [ms]",12} {"ns/call",12} {"vs fastest",12}");
Console.WriteLine(new string('-', 62));

for (int i = 0; i < cases.Count; i++)
{
    Console.WriteLine(
        $"{cases[i].Name,-24} {best[i] * 1000,12:F3} {nsPerCall[i],12:F1} {nsPerCall[i] / fastestNs,11:F1}x");
}

return 0;
