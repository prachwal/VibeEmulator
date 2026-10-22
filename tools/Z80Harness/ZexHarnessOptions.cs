using System.Globalization;

namespace VibeEmulator.Z80Harness;

public sealed class ZexHarnessOptions
{
    public const long DefaultLimitTStates = 50_000_000_000;

    public string? ImagePath { get; private set; }

    public long LimitTStates { get; private set; } = DefaultLimitTStates;

    public bool Json { get; private set; }

    public bool Help { get; private set; }

    public string? Error { get; private set; }

    public static ZexHarnessOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ZexHarnessOptions options = new();

        for (int index = 0; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--help" or "-h" or "help":
                    options.Help = true;
                    break;
                case "--image":
                    options.ImagePath = Next(args, ref index, "--image", options);
                    break;
                case "--limit-tstates":
                    options.LimitTStates = ParseLimit(Next(args, ref index, "--limit-tstates", options), options);
                    break;
                case "--format":
                    options.ParseFormat(Next(args, ref index, "--format", options));
                    break;
                default:
                    options.Error ??= $"nieznana opcja: {args[index]}";
                    break;
            }

            if (options.Error is not null)
            {
                return options;
            }
        }

        if (!options.Help && string.IsNullOrWhiteSpace(options.ImagePath))
        {
            options.Error = "brak --image <plik.com>";
        }

        return options;
    }

    private static string? Next(IReadOnlyList<string> args, ref int index, string name, ZexHarnessOptions options)
    {
        if (index + 1 >= args.Count)
        {
            options.Error ??= $"opcja {name} wymaga wartości";
            return null;
        }

        index++;
        return args[index];
    }

    private static long ParseLimit(string? value, ZexHarnessOptions options)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long limit) || limit <= 0)
        {
            options.Error ??= $"opcja --limit-tstates wymaga dodatniej liczby T-state (otrzymano „{value}”)";
            return 0;
        }

        return limit;
    }

    private void ParseFormat(string? value)
    {
        if (string.Equals(value, "text", StringComparison.OrdinalIgnoreCase))
        {
            Json = false;
        }
        else if (string.Equals(value, "json", StringComparison.OrdinalIgnoreCase))
        {
            Json = true;
        }
        else
        {
            Error ??= $"opcja --format przyjmuje text albo json (otrzymano „{value}”)";
        }
    }
}
