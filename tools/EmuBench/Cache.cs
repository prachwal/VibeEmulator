using System.Runtime.CompilerServices;

namespace EmuBench;

/// <summary>
/// Hierarchia pamięci bez PMU: mierzy przepustowość przy rosnącym working secie.
/// Kolana krzywej wskazują granice L1 (32 KB/rdzeń), L2 (256 KB) i L3 (12 MB).
/// </summary>
internal static class Cache
{
    private static readonly int[] SizesKb = [4, 32, 256, 1024, 12 * 1024, 64 * 1024];

    public static IReadOnlyList<Harness.Result> Run(int rounds)
    {
        var results = new List<Harness.Result>();

        foreach (int sizeKb in SizesKb)
        {
            int elements = sizeKb * 1024 / sizeof(long);
            var data = new long[elements];
            var rng = new Random(sizeKb);

            for (int i = 0; i < elements; i++)
            {
                data[i] = i;
            }

            // Permutacja indeksów — dostęp losowy bez powtórzeń w krótkim oknie.
            int[] order = new int[elements];
            for (int i = 0; i < elements; i++)
            {
                order[i] = rng.Next(elements);
            }

            int ops = AccessCountFor(sizeKb);

            double sequential = Harness.Measure(ops, () => Sequential(data, ops), rounds);
            double random = Harness.Measure(ops, () => RandomAccess(data, order, ops), rounds);

            results.Add(new($"seq {FormatSize(sizeKb)}", sequential, Throughput(sequential)));
            results.Add(new($"rand {FormatSize(sizeKb)}", random, Throughput(random)));
        }

        return results;
    }

    private static int AccessCountFor(int sizeKb) => sizeKb switch
    {
        <= 32 => 4_000_000,
        <= 256 => 3_000_000,
        <= 1024 => 2_000_000,
        <= 12 * 1024 => 1_000_000,
        _ => 500_000,
    };

    private static string FormatSize(int sizeKb) =>
        sizeKb >= 1024 ? $"{sizeKb / 1024,3} MB" : $"{sizeKb,3} KB";

    private static string Throughput(double nsPerOp) => $"{8.0 / nsPerOp:F1} GB/s";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Sequential(long[] data, int ops)
    {
        long acc = 0;
        int mask = data.Length - 1;

        for (int i = 0; i < ops; i++)
        {
            acc += data[i & mask];
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long RandomAccess(long[] data, int[] order, int ops)
    {
        long acc = 0;
        int mask = order.Length - 1;

        for (int i = 0; i < ops; i++)
        {
            acc += data[order[i & mask]];
        }

        return acc;
    }
}
