using VibeEmulator.Core.C64;

namespace PerfAudit;

internal static class Workloads
{
    private static ReadOnlySpan<ushort> Addresses =>
    [
        0x0300, 0x0400, 0xA000, 0xBFFF, 0xC000, 0xE000, 0xFFFF, 0x1000,
        0xD012, 0xD400, 0xD800, 0xDC0D, 0xDD00, 0xDE00, 0xDF00, 0x1FFF,
    ];

    public static BenchmarkCase[] Create(Options options) =>
    [
        Frames(options, full: false),
        Frames(options, full: true),
        Cpu(options, full: false),
        Cpu(options, full: true),
        Bus(options),
    ];

    private static BenchmarkCase Frames(Options options, bool full)
    {
        C64Machine machine = TestPrograms.Create(full, options.Seed);
        int bank = options.Seed & 7;
        return new BenchmarkCase(full ? "Frame/full + banking" : "Frame/nop", "ms/frame", 1000.0 / options.Frames, () =>
        {
            for (int frame = 0; frame < options.Frames; frame++)
            {
                if (full)
                {
                    // Port nie jest jeszcze mapowany pod $00/$01. Opóźnienie odlicza maszyna.
                    machine.Bus.Port.WriteData((byte)(0x30 | bank));
                    bank = (bank + 1) & 7;
                }

                machine.RunFrame();
            }

            EnsureRunning(machine);
            return machine.TotalCycles ^ machine.Cpu.State.PC ^ machine.Bus.Ram[0x0300] ^ machine.Bus.Ram[0x0400];
        });
    }

    private static BenchmarkCase Cpu(Options options, bool full)
    {
        C64Machine machine = TestPrograms.Create(full, options.Seed);
        var cpu = machine.Cpu;
        return new BenchmarkCase(full ? "CpuTick/full" : "CpuTick/nop", "ns/tick", 1e9 / options.Iterations, () =>
        {
            // Mierzymy sam rdzeń: bez ticków urządzeń i bez przełączania banków.
            for (int i = 0; i < options.Iterations; i++)
            {
                cpu.Tick();
            }

            EnsureRunning(machine);
            return cpu.Cycles ^ cpu.State.PC ^ cpu.State.A ^ machine.Bus.Ram[0x0300];
        });
    }

    private static BenchmarkCase Bus(Options options)
    {
        C64Bus bus = TestPrograms.Create(full: true, options.Seed).Bus;
        ProcessorPort port = bus.Port;
        int bank = options.Seed & 7;
        return new BenchmarkCase("Bus/mixed + banking", "ns/iter", 1e9 / options.Iterations, () =>
        {
            long sink = 0;
            for (int i = 0; i < options.Iterations; i++)
            {
                if ((i & 255) == 0)
                {
                    port.WriteData((byte)(0x30 | bank));
                    bank = (bank + 1) & 7;
                }

                port.Tick();
                bus.Write((ushort)(0x0400 | (i & 255)), (byte)(options.Seed + i));
                sink += bus.Read(Addresses[(int)(((uint)i + options.Seed) & 15)]);
            }

            return sink ^ bus.OpenBus ^ bus.Ram[0x0400];
        });
    }

    private static void EnsureRunning(C64Machine machine)
    {
        if (machine.Cpu.Jammed)
        {
            throw new InvalidOperationException($"CPU zatrzymał się pod ${machine.Cpu.State.PC:X4}.");
        }
    }
}
