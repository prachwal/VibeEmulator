using System.Globalization;
using System.Text.RegularExpressions;
using VibeEmulator.Mos6502;

namespace VibeEmulator.CpuHarness;

/// <summary>Jedna linia wzorcowego logu `nestest` — stan **przed** wykonaniem instrukcji.</summary>
public sealed partial record NestestLine(
    int Number,
    ushort Pc,
    byte A,
    byte X,
    byte Y,
    byte P,
    byte Sp,
    long Cycles)
{
    /// <summary>Pierwsza linia logu ma licznik cykli 7 (reset w Nintendulatorze), rdzeń startuje od 0.</summary>
    public const long CycleOffset = 7;

    [GeneratedRegex(@"^(?<pc>[0-9A-Fa-f]{4}).*A:(?<a>[0-9A-Fa-f]{2}) X:(?<x>[0-9A-Fa-f]{2}) Y:(?<y>[0-9A-Fa-f]{2}) P:(?<p>[0-9A-Fa-f]{2}) SP:(?<sp>[0-9A-Fa-f]{2}).*CYC:(?<cyc>\d+)\s*$")]
    private static partial Regex LinePattern();

    /// <summary>Parsuje log; linie niepasujące do wzorca są pomijane (nagłówki, komentarze).</summary>
    public static List<NestestLine> Parse(string text)
    {
        var lines = new List<NestestLine>();
        int number = 0;

        foreach (string raw in text.Split('\n'))
        {
            number++;
            Match match = LinePattern().Match(raw);
            if (!match.Success)
            {
                continue;
            }

            lines.Add(new NestestLine(
                number,
                ushort.Parse(match.Groups["pc"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(match.Groups["a"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(match.Groups["x"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(match.Groups["y"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(match.Groups["p"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(match.Groups["sp"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                long.Parse(match.Groups["cyc"].Value, CultureInfo.InvariantCulture)));
        }

        return lines;
    }
}

/// <summary>Wynik porównania z logiem wzorcowym: liczba zgodnych linii i pierwsza różnica.</summary>
public sealed record NestestComparison(int Matched, int Total, string? FirstMismatch);

/// <summary>
/// Przebieg `nestest`: wykonuje instrukcje i po każdej porównuje stan z kolejną linią logu
/// (<c>PC A X Y P SP CYC</c>). Kolumny <c>PPU</c> nie porównujemy — wynika z <c>CYC</c>.
/// </summary>
public static class NestestRunner
{
    /// <summary>Uruchamia obraz NROM od <paramref name="entryPc"/> i porównuje z logiem.</summary>
    public static (NestestComparison Comparison, HarnessResult Result) Run(
        InesImage image,
        IReadOnlyList<NestestLine> log,
        ushort entryPc,
        long limitCycles)
    {
        var bus = new NesBus(image);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nes2A03);
        cpu.Reset(entryPc);

        long instructions = 0;
        string? mismatch = null;
        int matched = 0;

        foreach (NestestLine expected in log)
        {
            string? difference = Describe(cpu, expected, instructions);
            if (difference is not null)
            {
                mismatch = $"linia {expected.Number}: {difference}";
                break;
            }

            matched++;
            ExecuteInstruction(cpu, limitCycles);
            instructions++;

            if (cpu.Jammed)
            {
                return (new NestestComparison(matched, log.Count, $"linia {expected.Number}: rdzeń zatrzymany (JAM)"), Result(cpu, instructions, HarnessOutcome.Jam, bus));
            }

            if (cpu.Cycles >= limitCycles)
            {
                return (new NestestComparison(matched, log.Count, $"linia {expected.Number}: limit cykli"), Result(cpu, instructions, HarnessOutcome.Limit, bus));
            }
        }

        HarnessOutcome outcome = mismatch is null ? HarnessOutcome.Pass : HarnessOutcome.Fail;
        return (new NestestComparison(matched, log.Count, mismatch), Result(cpu, instructions, outcome, bus));
    }

    private static string? Describe(StateMachineCpuCore cpu, NestestLine expected, long instructions)
    {
        if (cpu.State.PC != expected.Pc)
        {
            return $"PC: oczekiwano ${expected.Pc:X4}, jest ${cpu.State.PC:X4} (instrukcja {instructions})";
        }

        if (cpu.State.A != expected.A || cpu.State.X != expected.X || cpu.State.Y != expected.Y)
        {
            return $"rejestry: oczekiwano A={expected.A:X2} X={expected.X:X2} Y={expected.Y:X2}, jest A={cpu.State.A:X2} X={cpu.State.X:X2} Y={cpu.State.Y:X2}";
        }

        if ((byte)cpu.State.P != expected.P)
        {
            return $"P: oczekiwano ${expected.P:X2}, jest ${(byte)cpu.State.P:X2}";
        }

        if ((byte)cpu.State.SP != expected.Sp)
        {
            return $"SP: oczekiwano ${expected.Sp:X2}, jest ${(byte)cpu.State.SP:X2}";
        }

        long expectedCycles = expected.Cycles - NestestLine.CycleOffset;
        return cpu.Cycles == expectedCycles
            ? null
            : $"CYC: oczekiwano {expected.Cycles} (offset {NestestLine.CycleOffset} → {expectedCycles}), jest {cpu.Cycles}";
    }

    private static void ExecuteInstruction(StateMachineCpuCore cpu, long limitCycles)
    {
        do
        {
            cpu.Tick();
        }
        while (cpu.Pending != 0 && cpu.Cycles < limitCycles);
    }

    private static HarnessResult Result(StateMachineCpuCore cpu, long instructions, HarnessOutcome outcome, NesBus bus) =>
        new(outcome, cpu.State.PC, cpu.Cycles, instructions, null, bus.IoWrites);
}
