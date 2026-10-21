namespace C64Trace;

/// <summary>Argumenty wiersza poleceń tracera.</summary>
internal sealed record TraceOptions
{
    public string Scenario { get; init; } = "demo";

    /// <summary>Pierwszy cykl do wyświetlenia (włącznie).</summary>
    public long From { get; init; }

    /// <summary>Ostatni cykl do wyświetlenia (włącznie); 0 = bez ograniczenia.</summary>
    public long To { get; init; }

    /// <summary>Plik wyjściowy zrzutu; pusty = brak zrzutu.</summary>
    public string? DumpPath { get; init; }

    /// <summary>Format zrzutu: <c>text</c> (czytelny) lub <c>vice</c> (do diffu z logiem VICE).</summary>
    public string DumpFormat { get; init; } = "text";

    public static TraceOptions Parse(string[] args, out string? error)
    {
        error = null;

        var options = new TraceOptions();
        string? scenario = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--from" or "-f":
                    if (!TryNext(args, ref i, out string fromText))
                    {
                        error = "Brak wartości po --from.";
                        return options;
                    }

                    if (!long.TryParse(fromText, out long from) || from < 0)
                    {
                        error = $"--from wymaga liczby cykli >= 0 (otrzymano '{fromText}').";
                        return options;
                    }

                    options = options with { From = from };
                    break;

                case "--to" or "-t":
                    if (!TryNext(args, ref i, out string toText))
                    {
                        error = "Brak wartości po --to.";
                        return options;
                    }

                    if (!long.TryParse(toText, out long to) || to < 0)
                    {
                        error = $"--to wymaga liczby cykli >= 0 (otrzymano '{toText}').";
                        return options;
                    }

                    options = options with { To = to };
                    break;

                case "--dump" or "-d":
                    if (!TryNext(args, ref i, out string path))
                    {
                        error = "Brak ścieżki po --dump.";
                        return options;
                    }

                    options = options with { DumpPath = path };
                    break;

                case "--format":
                    if (!TryNext(args, ref i, out string format))
                    {
                        error = "Brak wartości po --format.";
                        return options;
                    }

                    if (format is not ("text" or "vice"))
                    {
                        error = $"--format dopuszcza 'text' lub 'vice' (otrzymano '{format}').";
                        return options;
                    }

                    options = options with { DumpFormat = format };
                    break;

                case "--help" or "-h":
                    error = null;
                    return options with { Scenario = "--help" };

                default:
                    if (arg.StartsWith('-'))
                    {
                        error = $"Nieznana opcja '{arg}'.";
                        return options;
                    }

                    scenario = arg.ToLowerInvariant();
                    break;
            }
        }

        if (scenario is not null)
        {
            options = options with { Scenario = scenario };
        }

        if (options.To != 0 && options.To < options.From)
        {
            error = $"--to ({options.To}) jest mniejsze niż --from ({options.From}).";
        }

        return options;

        static bool TryNext(string[] args, ref int index, out string value)
        {
            if (index + 1 >= args.Length)
            {
                value = string.Empty;
                return false;
            }

            value = args[++index];
            return true;
        }
    }

    public const string Usage = """
        Tracer maszyny C64 — wykonuje program cykl po cyklu.

        Użycie:
          dotnet run -c Release --project tools/C64Trace -- [scenariusz] [opcje]

        Scenariusze:
          demo      LDA/STA/INC/JMP w pętli (domyślnie)
          rmw       INC abs — pokazuje dwa zapisy
          badline   linia z badline — pokazuje BA/AEC i kradzież 40 cykli
          count     pomiar kosztu klatki, bez śladu

        Opcje:
          --from <cykl>, -f <cykl>   pierwszy wyświetlany cykl (włącznie)
          --to <cykl>, -t <cykl>     ostatni wyświetlany cykl (włącznie)
          --dump <plik>, -d <plik>   zapisz ślad do pliku
          --format text|vice         format zrzutu (domyślnie text)
          --help, -h                 ta pomoc

        Przykłady:
          ... -- badline --from 3030 --to 3060
          ... -- demo --dump trace.txt
          ... -- badline --from 3030 --to 3060 --dump trace.txt --format vice
        """;
}
