using System.Diagnostics;
using VibeEmulator.Core.C64;
using VibeEmulator.Cpu.Abstractions;
using VibeEmulator.Mos6502;

var machine = C64Machine.CreateCycleExact();
machine.Reset(0x0200);
machine.Bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]);

double best = double.MaxValue;
for (int run = 0; run < 5; run++)
{
    long start = Stopwatch.GetTimestamp();
    for (int frame = 0; frame < 200; frame++) machine.RunFrame();
    double elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
    if (elapsed < best) best = elapsed;
}
Console.WriteLine($"Frame: {best / 200 * 1000:F4} ms  ({best / (200.0 * C64Machine.CyclesPerFrame) * 1e9:F2} ns/tick)");

// CPU tick
{
    var bus = new C64Bus(new VicII(), new Cia6526(), new Cia6526(), new Sid6581());
    bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]);
    var cpu = new StateMachineCpuCore(bus);
    cpu.Reset(0x0200);
    for (int i = 0; i < 1000; i++) cpu.Tick();
    const int N = 5_000_000;
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < N; i++) cpu.Tick();
    sw.Stop();
    Console.WriteLine($"CpuTick: {sw.Elapsed.TotalMilliseconds * 1e6 / N:F2} ns");
}

// Bus read
{
    var bus = new C64Bus(new VicII(), new Cia6526(), new Cia6526(), new Sid6581());
    bus.LoadRam(0x0200, [0xEA, 0x4C, 0x00, 0x02]);
    for (int i = 0; i < 1000; i++) bus.Read(0x0200);
    const int N = 10_000_000;
    var sw = Stopwatch.StartNew();
    byte sink = 0;
    for (int i = 0; i < N; i++) sink ^= bus.Read(0x0200);
    sw.Stop();
    Console.WriteLine($"BusRead: {sw.Elapsed.TotalMilliseconds * 1e6 / N:F2} ns (sink={sink})");
}

return 0;