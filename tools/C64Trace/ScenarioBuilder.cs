using C64Trace;
using VibeEmulator.Core.C64;

namespace C64Trace;

/// <summary>Scenariusze demonstracyjne dla tracera.</summary>
internal static class ScenarioBuilder
{
    public static C64Machine Build(string scenario, out int totalCycles)
    {
        C64Machine machine = C64Machine.CreateCycleExact();

        switch (scenario)
        {
            case "badline":
                machine.Reset(0x0200);
                machine.Vic.Write(0xD011, 0x18); // DEN=1, YSCROLL=0
                machine.Bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]); // NOP; JMP $0200
                totalCycles = (0x30 * VicII.CyclesPerLine) + 58;
                break;

            case "rmw":
                machine.Reset(0x0200);
                machine.Bus.LoadRam(0x0200, [0xEE, 0x00, 0x03, 0x4C, 0x00, 0x02]); // INC $0300; JMP
                machine.Bus.Write(0x0300, 0x41);
                totalCycles = 20;
                break;

            case "count":
                totalCycles = 0;
                break;

            default:
                machine.Reset(0x0200);
                machine.Bus.LoadRam(0x0200,
                [
                    0xA9, 0x7F,       // LDA #$7F
                    0x8D, 0x00, 0x03, // STA $0300
                    0xEE, 0x00, 0x03, // INC $0300
                    0x4C, 0x00, 0x02, // JMP $0200
                ]);
                machine.Bus.Write(0x0300, 0x10);
                totalCycles = 32;
                break;
        }

        return machine;
    }

    /// <summary>Pomiar kosztu klatki — 200 klatek, minimum z 5 rund.</summary>
    public static void RunFrameBenchmark(TextWriter output)
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset(0x0200);
        machine.Bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]); // NOP; JMP $0200

        double best = double.MaxValue;

        for (int run = 0; run < 5; run++)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();

            for (int frame = 0; frame < 200; frame++)
            {
                machine.RunFrame();
            }

            double elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds;

            if (elapsed < best)
            {
                best = elapsed;
            }
        }

        double perFrameMs = best / 200 * 1000;
        double perTickNs = best / (200.0 * C64Machine.CyclesPerFrame) * 1e9;

        output.WriteLine("Koszt klatki (200 klatek, min z 5 rund):");
        output.WriteLine($"  klatka:            {perFrameMs,7:F3} ms  (budżet 20,000 ms)");
        output.WriteLine($"  tick:              {perTickNs,7:F2} ns");
        output.WriteLine($"  udział w budżecie: {perFrameMs / 20.0 * 100,7:F2} %");
    }
}
