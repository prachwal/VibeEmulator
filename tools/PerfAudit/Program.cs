using System.Diagnostics;
using VibeEmulator.Core.C64;
var machine = C64Machine.CreateCycleExact();
machine.Reset(0x0200);
machine.Bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]);
double best = double.MaxValue;
for (int run=0; run<5; run++) {
  long start = Stopwatch.GetTimestamp();
  for (int frame=0; frame<200; frame++) machine.RunFrame();
  double elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
  if (elapsed < best) best = elapsed;
}
Console.WriteLine($"frame={best/200*1000:F4} ms  ns/tick={best/(200.0*C64Machine.CyclesPerFrame)*1e9:F2}");
return 0;
