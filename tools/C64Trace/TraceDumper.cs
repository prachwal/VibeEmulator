namespace C64Trace;

/// <summary>Zapisuje ślad do pliku w formacie text, vice lub json (ten sam format co stdout).</summary>
internal static class TraceDumper
{
    public static void Write(string path, IEnumerable<CycleTrace> traces, string format, RunSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);

        TraceFormatter.WriteTraces(writer, traces, format);

        if (format == "json")
        {
            TraceFormatter.WriteJsonSummary(writer, summary);
        }
    }
}
