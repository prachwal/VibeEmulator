using VibeEmulator.Core.C64;

namespace C64Trace;

/// <summary>Rodzaj warunku zatrzymania z --until.</summary>
internal enum StopKind
{
    Pc,
    Write,
    Read,
    Jam,
    Raster,
}

/// <summary>
/// Pojedynczy warunek z --until. <see cref="Text"/> to zapis użytkownika, np. <c>write=0300</c>,
/// i trafia do powodu zatrzymania jako <c>until:write=0300</c>.
/// </summary>
internal sealed record StopCondition(StopKind Kind, ushort Address, int Line, int LineCycle, string Text)
{
    /// <summary>Parsuje warunek; przy błędzie zwraca null i opis w <paramref name="error"/>.</summary>
    public static StopCondition? Parse(string text, out string? error)
    {
        error = null;
        string trimmed = text.Trim();
        string lower = trimmed.ToLowerInvariant();

        if (lower == "jam")
        {
            return new StopCondition(StopKind.Jam, 0, 0, 0, trimmed);
        }

        int eq = trimmed.IndexOf('=');
        string key = eq < 0 ? lower : lower[..eq];
        string value = eq < 0 ? string.Empty : trimmed[(eq + 1)..];

        switch (key)
        {
            case "pc" or "write" or "read":
                if (!TraceOptions.TryParseHex(value, 4, 0xFFFF, out int address))
                {
                    error = $"--until {key}=ADRES wymaga adresu hex 0000..FFFF (otrzymano '{value}').";
                    return null;
                }

                StopKind kind = key switch
                {
                    "pc" => StopKind.Pc,
                    "write" => StopKind.Write,
                    _ => StopKind.Read,
                };

                return new StopCondition(kind, (ushort)address, 0, 0, trimmed);

            case "raster":
                string[] parts = value.Split(':');

                if (parts.Length != 2
                    || !int.TryParse(parts[0], out int line) || line < 0 || line >= VicII.LinesPerFrame
                    || !int.TryParse(parts[1], out int lineCycle) || lineCycle < 0 || lineCycle >= VicII.CyclesPerLine)
                {
                    error = $"--until raster=LINIA:CYKL wymaga LINIA 0..{VicII.LinesPerFrame - 1} " +
                        $"i CYKL 0..{VicII.CyclesPerLine - 1} (otrzymano '{value}').";
                    return null;
                }

                return new StopCondition(StopKind.Raster, 0, line, lineCycle, trimmed);

            default:
                error = $"--until nie zna warunku '{trimmed}' " +
                    "(dozwolone: pc=ADRES, write=ADRES, read=ADRES, raster=LINIA:CYKL, jam).";
                return null;
        }
    }

    /// <summary>Czy warunek zaszedł w tym cyklu. Wartości linii i cyklu VIC to stan sprzed ticku.</summary>
    public bool IsMet(CycleTrace trace) => Kind switch
    {
        // Pierwszy cykl instrukcji: pobranie opkodu spod adresu.
        StopKind.Pc => trace.InstructionStart && trace.InstructionPc == Address,
        StopKind.Write => trace.Accesses.Any(a => a.Write && a.Address == Address),
        StopKind.Read => trace.Accesses.Any(a => !a.Write && a.Address == Address),
        StopKind.Jam => trace.Jammed,
        StopKind.Raster => trace.Line == Line && trace.LineCycle == LineCycle,
        _ => false,
    };
}

/// <summary>
/// Zbiera warunki zatrzymania i liczy ukończone instrukcje. Kolejność sprawdzania:
/// --until (w kolejności podania), potem JAM, na końcu --instr.
/// </summary>
internal sealed class StopConditions(IReadOnlyList<StopCondition> until, long? instructionLimit)
{
    private long _instructions;

    /// <summary>Zwraca powód zatrzymania po tym cyklu albo null, gdy symulacja ma trwać.</summary>
    public string? Evaluate(CycleTrace trace)
    {
        if (trace.InstructionDone)
        {
            _instructions++;
        }

        foreach (StopCondition condition in until)
        {
            if (condition.IsMet(trace))
            {
                return $"until:{condition.Text}";
            }
        }

        if (trace.Jammed)
        {
            return "jam";
        }

        if (instructionLimit is long limit && _instructions >= limit)
        {
            return "instr-limit";
        }

        return null;
    }
}
