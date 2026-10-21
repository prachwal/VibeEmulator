using System.Text;
using System.Text.Json;
using VibeEmulator.Core.C64;
using VibeEmulator.Core.Cpu;

namespace C64Trace;

/// <summary>Podsumowanie przebiegu symulacji (wspólne dla stdout i pliku zrzutu).</summary>
internal sealed record RunSummary(
    string Scenario,
    string Stop,
    long Cycles,
    long CpuCycles,
    long StolenCycles,
    bool Jammed,
    int Shown);

/// <summary>
/// Jedyne miejsce zamieniające ślad na tekst: tabela, VICE i NDJSON. Używa go stdout (Program)
/// oraz plik zrzutu (TraceDumper), więc formaty nie mogą się rozjechać.
/// </summary>
internal static class TraceFormatter
{
    private const string TableHeader =
        "cykl  lin  c/l  AEC  BA   PC    opk  instrukcja      A  X  Y  SP  flagi     magistrala";

    public static bool IsKnownFormat(string format) => format is "text" or "vice" or "json";

    /// <summary>Nagłówek scenariusza, np. <c>Scenariusz: demo · wykonano 32 cykli · ...</c>.</summary>
    public static string ScenarioLine(RunSummary summary) =>
        $"Scenariusz: {summary.Scenario} · wykonano {summary.Cycles} cykli · " +
        $"wyświetlono {summary.Shown} · klatka PAL = {C64Machine.CyclesPerFrame}";

    /// <summary>Zapisuje wiersze śladu w wybranym formacie, bez nagłówka scenariusza i podsumowania.</summary>
    public static void WriteTraces(TextWriter writer, IEnumerable<CycleTrace> traces, string format)
    {
        switch (format)
        {
            case "vice":
                WriteVice(writer, traces);
                break;

            case "json":
                WriteJsonCycles(writer, traces);
                break;

            default:
                WriteText(writer, traces);
                break;
        }
    }

    /// <summary>Tabela tekstowa: nagłówek, separator i wiersze.</summary>
    public static void WriteText(TextWriter writer, IEnumerable<CycleTrace> traces)
    {
        writer.WriteLine(TableHeader);
        writer.WriteLine(new string('-', 104));

        long lastInstructionPc = -1;

        foreach (CycleTrace trace in traces)
        {
            bool newInstruction = trace.InstructionPc != lastInstructionPc;
            lastInstructionPc = trace.InstructionPc;

            writer.WriteLine(FormatTextRow(trace, newInstruction));
        }
    }

    /// <summary>Podsumowanie tekstowe: liczniki cykli i powód zatrzymania.</summary>
    public static void WriteTextSummary(TextWriter writer, RunSummary summary)
    {
        writer.WriteLine();
        writer.WriteLine(
            $"Cykle maszyny: {summary.Cycles} · cykle CPU: {summary.CpuCycles} · " +
            $"ukradzione: {summary.StolenCycles}");
        writer.WriteLine($"Zatrzymano: {summary.Stop}");
    }

    /// <summary>
    /// Format zbliżony do logu VICE: <c>PC  A  X  Y  SP  NV-BDIZC  licznik_cykli  instrukcja</c>.
    /// Jedna linia na instrukcję, nie na cykl.
    /// </summary>
    public static void WriteVice(TextWriter writer, IEnumerable<CycleTrace> traces)
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

    /// <summary>NDJSON: jedna linia na cykl, typ <c>cycle</c>.</summary>
    public static void WriteJsonCycles(TextWriter writer, IEnumerable<CycleTrace> traces)
    {
        long lastInstructionPc = -1;

        foreach (CycleTrace trace in traces)
        {
            bool newInstruction = trace.InstructionPc != lastInstructionPc;
            lastInstructionPc = trace.InstructionPc;

            writer.WriteLine(FormatJsonCycle(trace, newInstruction));
        }
    }

    /// <summary>NDJSON: ostatnia linia, typ <c>summary</c>.</summary>
    public static void WriteJsonSummary(TextWriter writer, RunSummary summary)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("type", "summary");
            json.WriteString("scenario", summary.Scenario);
            json.WriteString("stop", summary.Stop);
            json.WriteNumber("cycles", summary.Cycles);
            json.WriteNumber("cpuCycles", summary.CpuCycles);
            json.WriteNumber("stolenCycles", summary.StolenCycles);
            json.WriteBoolean("jammed", summary.Jammed);
            json.WriteNumber("shown", summary.Shown);
            json.WriteEndObject();
        }

        writer.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
    }

    public static string FormatFlags(StatusFlags flags) =>
        string.Concat(
            flags.Has(StatusFlags.Negative) ? "N" : "n",
            flags.Has(StatusFlags.Overflow) ? "V" : "v",
            "-",
            flags.Has(StatusFlags.Break) ? "B" : "b",
            flags.Has(StatusFlags.Decimal) ? "D" : "d",
            flags.Has(StatusFlags.InterruptDisable) ? "I" : "i",
            flags.Has(StatusFlags.Zero) ? "Z" : "z",
            flags.Has(StatusFlags.Carry) ? "C" : "c");

    private static string FormatTextRow(CycleTrace trace, bool newInstruction)
    {
        string accesses = string.Join(
            " ",
            trace.Accesses.Select(static a => $"{(a.Write ? 'W' : 'R')}${a.Address:X4}={a.Value:X2}"));

        string instruction = newInstruction ? trace.Instruction : "  ·";

        return
            $"{trace.Cycle,4}  {trace.Line,3}  {trace.LineCycle,3}  " +
            $"{(trace.Aec ? "cpu" : "---"),3}  {(trace.Ba ? "ok" : "BA"),2}  " +
            $"{trace.InstructionPc:X4}  {trace.Opcode:X2}   {instruction,-15} " +
            $"{trace.Registers.A:X2} {trace.Registers.X:X2} {trace.Registers.Y:X2} " +
            $"{trace.Registers.SP:X2}  {FormatFlags(trace.Registers.P)}  " +
            $"{accesses}{(trace.Jammed ? "  [JAM]" : string.Empty)}";
    }

    private static string FormatJsonCycle(CycleTrace trace, bool newInstruction)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("type", "cycle");
            json.WriteNumber("cycle", trace.Cycle);
            json.WriteNumber("line", trace.Line);
            json.WriteNumber("lineCycle", trace.LineCycle);
            json.WriteBoolean("aec", trace.Aec);
            json.WriteBoolean("ba", trace.Ba);
            json.WriteString("pc", $"{trace.InstructionPc:X4}");
            json.WriteString("opcode", $"{trace.Opcode:X2}");
            json.WriteString("instruction", trace.Instruction);
            json.WriteBoolean("newInstruction", newInstruction);
            json.WriteString("a", $"{trace.Registers.A:X2}");
            json.WriteString("x", $"{trace.Registers.X:X2}");
            json.WriteString("y", $"{trace.Registers.Y:X2}");
            json.WriteString("sp", $"{trace.Registers.SP:X2}");
            json.WriteString("flags", FormatFlags(trace.Registers.P));
            json.WriteString("p", $"{(byte)trace.Registers.P:X2}");
            json.WriteBoolean("jammed", trace.Jammed);

            json.WriteStartArray("accesses");

            foreach ((bool write, ushort address, byte value) in trace.Accesses)
            {
                json.WriteStartObject();
                json.WriteString("rw", write ? "W" : "R");
                json.WriteString("addr", $"{address:X4}");
                json.WriteString("value", $"{value:X2}");
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
