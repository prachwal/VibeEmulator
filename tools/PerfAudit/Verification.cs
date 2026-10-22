using VibeEmulator.Core.C64;
using VibeEmulator.Memory;

namespace PerfAudit;

/// <summary>Sprawdzenie scenariuszy poza pomiarem; oczekiwania dotyczą bieżącego modelu, nie pełnego C64.</summary>
internal static class Verification
{
    public static void Run(byte seed)
    {
        C64Machine nop = TestPrograms.Create(full: false, seed);
        RunLoop(nop, expectedCycles: 5);

        // Sprawdzamy również zmianę DDR, a nie tylko rejestru danych.
        Settle(nop, () => nop.Bus.Port.WriteData(0x30), 0x30);
        Settle(nop, () => nop.Bus.Port.WriteDirection(0), 0x17);
        Settle(nop, () => nop.Bus.Port.WriteDirection(0x2F), 0x30);

        for (int bank = 0; bank < 8; bank++)
        {
            VerifyBank(seed, bank);
        }
    }

    private static void VerifyBank(byte seed, int bank)
    {
        C64Machine machine = TestPrograms.Create(full: true, seed);
        C64Bus bus = machine.Bus;
        byte data = (byte)(0x30 | bank);
        Settle(machine, () => bus.Port.WriteData(data), data);
        machine.Cpu.Reset(TestPrograms.Origin);

        // Obserwator istnieje tylko podczas sprawdzania; przypadki pomiarowe mają własne maszyny.
        var log = new AccessLog();
        bus.Observer = log;
        RunLoop(machine, expectedCycles: 59);
        bus.Observer = null;
        Equal(59, log.Accesses.Count, "liczba dostępów pełnej pętli");

        byte basic = (bank & 1) != 0 ? (byte)(seed ^ 0xA5) : seed;
        byte kernal = (bank & 2) != 0 ? (byte)(seed ^ 0x5A) : seed;
        byte character = (byte)(seed ^ 0xC3);
        byte low = bank == 0 ? character : seed;
        byte io = (bank & 4) != 0 ? (byte)0 : low;
        Read(log, 0xA000, basic);
        Read(log, 0xE000, kernal);
        Read(log, 0x1000, low);
        Read(log, 0xD012, io);
        Read(log, 0xD400, io);
        Read(log, 0xDC0D, io);
        Read(log, 0xDD00, io);
        Equal(basic, bus.Ram[0x0400], "zapis odczytu BASIC/RAM");
        Equal(kernal, bus.Ram[0x0401], "zapis odczytu KERNAL/RAM");
        Equal(low, bus.Ram[0x0402], "zapis odczytu CHAR/RAM");

        // W modelu odczyty zmieniają OpenBus; poprzedni dostęp to wysoki bajt operandu LDA $DE00.
        int open = log.Accesses.FindIndex(access => !access.Write && access.Address == 0xDE00);
        if (open <= 0)
        {
            throw new InvalidOperationException("brak odczytu $DE00 w pełnej pętli.");
        }

        byte expectedOpen = (bank & 4) != 0 ? log.Accesses[open - 1].Value : low;
        Equal(expectedOpen, log.Accesses[open].Value, "odczyt open bus/CHAR/RAM");
        Equal(log.Accesses[^1].Value, bus.OpenBus, "OpenBus po ostatnim odczycie");

        Access[] writes = log.Accesses.Where(access => access.Write && access.Address == 0x0300).ToArray();
        Equal(2, writes.Length, "liczba zapisów RMW");
        Equal(seed, writes[0].Value, "RMW: stara wartość");
        Equal((byte)(seed + 1), writes[1].Value, "RMW: nowa wartość");
        Equal((byte)(seed + 1), bus.Ram[0x0300], "RMW: wynik w RAM");
    }

    private static void Settle(C64Machine machine, Action schedule, byte expected)
    {
        byte previous = machine.Bus.Port.Effective;
        schedule();
        Flags(machine.Bus.Port, previous);
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            machine.Tick();
            Flags(machine.Bus.Port, cycle < 3 ? previous : expected);
        }
    }

    private static void Flags(ProcessorPort port, byte expected)
    {
        Equal(expected, port.Effective, "opóźnienie portu");
        Equal(expected & 1, port.BasicRomEnabled ? 1 : 0, "cache LORAM");
        Equal((expected >> 1) & 1, port.KernalRomEnabled ? 1 : 0, "cache HIRAM");
        Equal((expected >> 2) & 1, port.IoVisible ? 1 : 0, "cache CHAREN");
    }

    private static void RunLoop(C64Machine machine, int expectedCycles)
    {
        long start = machine.TotalCycles;
        do
        {
            machine.Tick();
            if (machine.Cpu.Jammed || machine.TotalCycles - start > 128)
            {
                throw new InvalidOperationException($"program nie wrócił do ${TestPrograms.Origin:X4}.");
            }
        }
        while (machine.Cpu.Pending != 0 || machine.Cpu.State.PC != TestPrograms.Origin);

        // 5 = NOP(2) + JMP(3); 59 = SEI(2) + 12×LDA/STA abs(4) + INC abs(6) + JMP(3).
        // Źródło: docs/cpu/opcodes/nmos6502.csv. Początek ramki nie zawiera badline.
        Equal(expectedCycles, machine.TotalCycles - start, "cykle pętli");
    }

    private static void Read(AccessLog log, ushort address, byte expected)
    {
        Access access = log.Accesses.Single(access => !access.Write && access.Address == address);
        Equal(expected, access.Value, $"odczyt ${address:X4}");
    }

    private static void Equal(long expected, long actual, string label)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{label}: oczekiwano {expected}, otrzymano {actual}.");
        }
    }

    private readonly record struct Access(bool Write, ushort Address, byte Value);

    private sealed class AccessLog : IBusObserver
    {
        public List<Access> Accesses { get; } = [];

        public void OnAccess(bool write, ushort address, byte value) => Accesses.Add(new Access(write, address, value));
    }
}
