using System.Diagnostics;
using CycleModel;

// Dowód na dwóch modelach czasu: atomowym i cykl-po-cyklu.
// Uruchomienie: dotnet run -c Release --project tools/CycleModel

byte[] program =
[
    0xA9, 0xAA,             // LDA #$AA
    0x8D, 0x07, 0x20,       // STA $2007  (rejestr danych PPU)
    0xA9, 0x55,             // LDA #$55
    0x8D, 0x07, 0x20,       // STA $2007
    0xEE, 0x00, 0x03,       // INC $0300  (read-modify-write)
];

Console.WriteLine("Program: LDA #$AA ; STA $2007 ; LDA #$55 ; STA $2007 ; INC $0300");
Console.WriteLine();

List<BusEvent> atomicLog = RunAtomic(program);
List<BusEvent> cycleLog = RunCycles(program);

Console.WriteLine("MODEL A — instrukcja atomowa:");
PrintWrites(atomicLog);

Console.WriteLine("MODEL B — cykl po cyklu:");
PrintWrites(cycleLog);

Compare(atomicLog, cycleLog);
MeasurePerf();

return 0;

static List<BusEvent> RunAtomic(byte[] program)
{
    var bus = new RecordingBus { Recording = false };
    bus.Load(0, program);
    bus.Write(0x0300, 0x41);

    var cpu = new AtomicCpu(bus);
    cpu.Reset(0);

    bus.Recording = true;

    while (!cpu.Halted)
    {
        cpu.Step();
    }

    return bus.Log;
}

static List<BusEvent> RunCycles(byte[] program)
{
    var bus = new RecordingBus { Recording = false };
    bus.Load(0, program);
    bus.Write(0x0300, 0x41);

    var cpu = new CycleCpu(bus);
    cpu.Reset(0);

    bus.Recording = true;

    while (!cpu.Halted)
    {
        cpu.Tick();
    }

    return bus.Log;
}

static void PrintWrites(List<BusEvent> log)
{
    foreach (BusEvent e in log.Where(static e => e.Kind == 'W'))
    {
        Console.WriteLine($"  {e}");
    }

    Console.WriteLine();
}

static void Compare(List<BusEvent> atomicLog, List<BusEvent> cycleLog)
{
    int[] atomicWrites = [.. atomicLog.Where(static e => e.Kind == 'W' && e.Address == 0x2007).Select(static e => e.Dots)];
    int[] cycleWrites = [.. cycleLog.Where(static e => e.Kind == 'W' && e.Address == 0x2007).Select(static e => e.Dots)];

    int atomicInc = atomicLog.Count(static e => e.Kind == 'W' && e.Address == 0x0300);
    int cycleInc = cycleLog.Count(static e => e.Kind == 'W' && e.Address == 0x0300);

    Console.WriteLine("RÓŻNICA:");
    Console.WriteLine($"  STA $2007 widziane w dotach (A): [{string.Join(", ", atomicWrites)}]");
    Console.WriteLine($"  STA $2007 widziane w dotach (B): [{string.Join(", ", cycleWrites)}]");
    Console.WriteLine($"  przesunięcie: {cycleWrites[0] - atomicWrites[0]} dotów = {(cycleWrites[0] - atomicWrites[0]) / 3} piksele");
    Console.WriteLine($"  zapisy RMW do $0300: A = {atomicInc}, B = {cycleInc} (B zapisuje starą wartość)");
    Console.WriteLine($"  łączna liczba zdarzeń na magistrali: A = {atomicLog.Count}, B = {cycleLog.Count}");
    Console.WriteLine();
}

static void MeasurePerf()
{
    const int block = 1_000;
    const long target = 300_000;

    byte[] program = new byte[(block * 2) + 3];

    for (int i = 0; i < block; i++)
    {
        program[i * 2] = 0xA9;      // LDA #imm
        program[(i * 2) + 1] = 0x01;
    }

    int tail = block * 2;
    program[tail] = 0x4C;           // JMP $0000 — pętla
    program[tail + 1] = 0x00;
    program[tail + 2] = 0x00;

    double atomicNs = MeasureAtomic(program, target);
    double cycleNs = MeasureCycle(program, target);

    Console.WriteLine($"WYDAJNOŚĆ ({target:N0} instrukcji, min z 5 rund):");
    Console.WriteLine($"  A: instrukcja atomowa  {atomicNs,7:F2} ns/instrukcję");
    Console.WriteLine($"  B: cykl po cyklu       {cycleNs,7:F2} ns/instrukcję  ({cycleNs / atomicNs:F2}x)");
    Console.WriteLine("     (LDA #imm to 2 cykle → koszt ticka = ns/instrukcję / 2)");

    static double MeasureAtomic(byte[] program, long target)
    {
        double best = double.MaxValue;

        for (int run = 0; run < 5; run++)
        {
            var bus = new RecordingBus { Recording = false };
            bus.Load(0, program);
            var cpu = new AtomicCpu(bus);
            cpu.Reset(0);

            long start = Stopwatch.GetTimestamp();

            while (cpu.Instructions < target)
            {
                cpu.Step();
            }

            double elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
            best = Math.Min(best, elapsed);
        }

        return best / target * 1e9;
    }

    static double MeasureCycle(byte[] program, long target)
    {
        double best = double.MaxValue;

        for (int run = 0; run < 5; run++)
        {
            var bus = new RecordingBus { Recording = false };
            bus.Load(0, program);
            var cpu = new CycleCpu(bus);
            cpu.Reset(0);

            long start = Stopwatch.GetTimestamp();

            while (cpu.Instructions < target)
            {
                cpu.Tick();
            }

            double elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
            best = Math.Min(best, elapsed);
        }

        return best / target * 1e9;
    }
}
