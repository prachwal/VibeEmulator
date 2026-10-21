using VibeEmulator.Core.Cpu;

namespace C64Trace;

/// <summary>Zapisuje ślad do pliku w formacie czytelnym lub zgodnym z logiem VICE.</summary>
internal static class TraceDumper
{
    public static void Write(string path, IEnumerable<CycleTrace> traces, string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);

        if (format == "vice")
        {
            WriteVice(writer, traces);
        }
        else
        {
            WriteText(writer, traces);
        }
    }

    /// <summary>
    /// Format zbliżony do logu VICE: <c>PC  A  X  Y  SP  NV-BDIZC  licznik_cykli</c>.
    /// Pozwala na <c>diff</c> z logiem referencyjnym bez własnego parsera.
    /// </summary>
    private static void WriteVice(TextWriter writer, IEnumerable<CycleTrace> traces)
    {
        long lastInstructionPc = -1;

        foreach (CycleTrace trace in traces)
        {
            // VICE loguje raz na instrukcję, nie raz na cykl.
            if (trace.InstructionPc == lastInstructionPc)
            {
                continue;
            }

            lastInstructionPc = trace.InstructionPc;

            writer.WriteLine(
                $"{trace.InstructionPc:X4}  " +
                $"{trace.Registers.A:X2}  " +
                $"{trace.Registers.X:X2}  " +
                $"{trace.Registers.Y:X2}  " +
                $"{trace.Registers.SP:X2}  " +
                $"{FormatFlags(trace.Registers.P)}  " +
                $"{trace.Cycle,8}  " +
                $"{trace.Instruction}");
        }
    }

    private static void WriteText(TextWriter writer, IEnumerable<CycleTrace> traces)
    {
        writer.WriteLine("cykl  lin  c/l  AEC  BA   PC    opk  instrukcja      A  X  Y  SP  flagi     magistrala");
        writer.WriteLine(new string('-', 104));

        long lastInstructionPc = -1;

        foreach (CycleTrace trace in traces)
        {
            bool newInstruction = trace.InstructionPc != lastInstructionPc;
            lastInstructionPc = trace.InstructionPc;

            string accesses = string.Join(
                " ",
                trace.Accesses.Select(static a => $"{(a.Write ? 'W' : 'R')}${a.Address:X4}={a.Value:X2}"));

            writer.WriteLine(
                $"{trace.Cycle,4}  {trace.Line,3}  {trace.LineCycle,3}  " +
                $"{(trace.Aec ? "cpu" : "---"),3}  {(trace.Ba ? "ok" : "BA"),2}  " +
                $"{trace.InstructionPc:X4}  {trace.Opcode:X2}   " +
                $"{(newInstruction ? trace.Instruction : "  ·"),-15} " +
                $"{trace.Registers.A:X2} {trace.Registers.X:X2} {trace.Registers.Y:X2} " +
                $"{trace.Registers.SP:X2}  {FormatFlags(trace.Registers.P)}  " +
                $"{accesses}{(trace.Jammed ? "  [JAM]" : string.Empty)}");
        }
    }

    public static string FormatFlags(VibeEmulator.Core.Cpu.StatusFlags flags) =>
        string.Concat(
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Negative) ? "N" : "n",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Overflow) ? "V" : "v",
            "-",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Break) ? "B" : "b",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Decimal) ? "D" : "d",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.InterruptDisable) ? "I" : "i",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Zero) ? "Z" : "z",
            flags.Has(VibeEmulator.Core.Cpu.StatusFlags.Carry) ? "C" : "c");
}
