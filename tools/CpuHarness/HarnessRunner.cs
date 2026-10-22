using VibeEmulator.Memory;
using VibeEmulator.Mos6502;

namespace VibeEmulator.CpuHarness;

/// <summary>
/// Magistrala harnessu: deleguje do płaskiej pamięci i (opcjonalnie) przechwytuje zapisy do rejestru
/// zwrotnego testu przerwań (etap O). Zapisy do rejestru nadal trafiają do pamięci — test może je odczytać.
/// </summary>
public sealed class HarnessBus(FlatMemory memory, ushort? watchRegister) : IMemoryBus
{
    private readonly FlatMemory _memory = memory;
    private readonly ushort? _watchRegister = watchRegister;

    /// <summary>Liczba zapisów do rejestru zwrotnego.</summary>
    public int RegisterWrites { get; private set; }

    /// <summary>Ostatnia wartość zapisana do rejestru zwrotnego (bity: 0 = IRQ, 1 = NMI).</summary>
    public byte RegisterValue { get; private set; }

    /// <summary>Linia IRQ wynikająca z ostatniego zapisu do rejestru.</summary>
    public bool IrqLevel => (RegisterValue & 0x01) != 0;

    /// <summary>Linia NMI wynikająca z ostatniego zapisu do rejestru.</summary>
    public bool NmiLevel => (RegisterValue & 0x02) != 0;

    public byte Read(ushort address) => _memory.Read(address);

    public void Write(ushort address, byte value)
    {
        if (_watchRegister == address)
        {
            RegisterWrites++;
            RegisterValue = value;
        }

        _memory.Write(address, value);
    }

    /// <summary>Odczyt bez efektów ubocznych (po zakończeniu przebiegu).</summary>
    public byte Peek(ushort address) => _memory.Read(address);
}

/// <summary>
/// Pętla przebiegu: wykonuje instrukcje cykl po cyklu i kończy na pętli <c>PC</c> (konwencja testów
/// Klausa Dormanna: <c>jmp *</c> pod adresem sukcesu).
/// </summary>
public static class HarnessRunner
{
    /// <summary>Uruchamia obraz wg opcji i zwraca wynik. Nie rzuca wyjątków na jam/limit.</summary>
    public static HarnessResult Run(ReadOnlySpan<byte> image, HarnessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        CpuVariant variant = ResolveVariant(options.Variant);
        FlatMemory memory = ImageLoader.Load(image, options.Origin);
        var bus = new HarnessBus(memory, options.WatchRegister);
        var cpu = new StateMachineCpuCore(bus, variant);
        cpu.Reset(options.Pc);

        long instructions = 0;

        while (true)
        {
            if (options.WatchRegister is not null)
            {
                cpu.IrqLevel = bus.IrqLevel;
                cpu.NmiLevel = bus.NmiLevel;
            }

            ushort before = cpu.State.PC;
            ExecuteInstruction(cpu, options.LimitCycles);
            instructions++;

            if (cpu.Jammed)
            {
                return Result(HarnessOutcome.Jam, cpu, instructions, bus, options, memory);
            }

            if (cpu.Cycles >= options.LimitCycles)
            {
                return Result(HarnessOutcome.Limit, cpu, instructions, bus, options, memory);
            }

            // Pętla PC = koniec testu: instrukcja skacze sama do siebie (jmp * albo gałąź *).
            if (cpu.State.PC == before)
            {
                bool pass = options.SuccessPc is not ushort success || success == before;
                return Result(pass ? HarnessOutcome.Pass : HarnessOutcome.Fail, cpu, instructions, bus, options, memory);
            }
        }
    }

    private static void ExecuteInstruction(StateMachineCpuCore cpu, long limitCycles)
    {
        // Granica instrukcji: Pending == 0 tylko w fazie Fetch. Limit sprawdzany w środku instrukcji,
        // żeby bardzo długi przebieg nie zawisł na jednej instrukcji.
        do
        {
            cpu.Tick();
        }
        while (cpu.Pending != 0 && cpu.Cycles < limitCycles);
    }

    private static HarnessResult Result(
        HarnessOutcome outcome,
        StateMachineCpuCore cpu,
        long instructions,
        HarnessBus bus,
        HarnessOptions options,
        FlatMemory memory)
    {
        byte? report = options.ReportByteAddress is ushort address ? bus.Peek(address) : null;
        return new HarnessResult(outcome, cpu.State.PC, cpu.Cycles, instructions, report, bus.RegisterWrites);
    }

    /// <summary>Mapowanie nazwy wariantu na tabelę opkodów; nieznana nazwa to błąd użytkownika.</summary>
    public static CpuVariant ResolveVariant(string name) => name.ToLowerInvariant() switch
    {
        "nmos" or "6502" or "6510" => CpuVariants.Nmos6502,
        "c02" or "65c02" => CpuVariants.Cmos65C02,
        "2a03" or "nes" => CpuVariants.Nes2A03,
        _ => throw new ArgumentException($"Nieznany wariant „{name}”. Dostępne: nmos, c02, 2a03.", nameof(name)),
    };
}
