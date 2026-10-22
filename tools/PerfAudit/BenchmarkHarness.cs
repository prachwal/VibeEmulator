using System.Diagnostics;

namespace PerfAudit;

internal sealed record BenchmarkCase(string Name, string Unit, double Scale, Func<long> Run);
internal sealed record Measurement(string Name, string Unit, double Minimum, double Median, long AllocatedBytes, long Sink);

internal static class BenchmarkHarness
{
    public static Measurement[] Measure(BenchmarkCase[] cases, int rounds)
    {
        // Każdy przypadek rozgrzewamy czasowo, a nie kilkoma wywołaniami tier-0.
        var sinks = new long[cases.Length];
        for (int i = 0; i < cases.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            do
            {
                sinks[i] = cases[i].Run();
            }
            while (Stopwatch.GetElapsedTime(start).TotalMilliseconds < 300);
        }

        var samples = new double[cases.Length][];
        var allocated = new long[cases.Length];
        for (int i = 0; i < cases.Length; i++)
        {
            samples[i] = new double[rounds];
        }

        for (int round = 0; round < rounds; round++)
        {
            for (int i = 0; i < cases.Length; i++)
            {
                long bytes = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                sinks[i] = cases[i].Run();
                double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
                allocated[i] = Math.Max(allocated[i], GC.GetAllocatedBytesForCurrentThread() - bytes);
                samples[i][round] = seconds * cases[i].Scale;
            }
        }

        var results = new Measurement[cases.Length];
        for (int i = 0; i < cases.Length; i++)
        {
            Array.Sort(samples[i]);
            int middle = rounds / 2;
            double median = rounds % 2 == 0 ? (samples[i][middle - 1] + samples[i][middle]) / 2 : samples[i][middle];
            results[i] = new Measurement(cases[i].Name, cases[i].Unit, samples[i][0], median, allocated[i], sinks[i]);
        }

        return results;
    }
}
