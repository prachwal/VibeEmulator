using System.Globalization;

namespace VibeEmulator.CpuHarness;

/// <summary>Jak zakończył się przebieg harnessu.</summary>
public enum HarnessOutcome
{
    /// <summary>Pętla <c>PC</c> pod adresem sukcesu (albo brak wymaganego adresu).</summary>
    Pass,

    /// <summary>Pętla <c>PC</c> pod adresem innym niż sukces — test zgłosił błąd.</summary>
    Fail,

    /// <summary>Przekroczony limit cykli — test nie doszedł do pętli.</summary>
    Limit,

    /// <summary>Rdzeń zatrzymany przez KIL/JAM.</summary>
    Jam,
}

/// <summary>Wynik jednego przebiegu: stan końcowy, licznik i (opcjonalnie) bajt raportu.</summary>
public sealed record HarnessResult(
    HarnessOutcome Outcome,
    ushort Pc,
    long Cycles,
    long Instructions,
    byte? ReportByte,
    int RegisterWrites)
{
    /// <summary>Kod wyjścia procesu: 0 = sukces, 2 = porażka, 3 = limit, 4 = błąd wejścia.</summary>
    public int ExitCode => Outcome switch
    {
        HarnessOutcome.Pass => 0,
        HarnessOutcome.Fail => 2,
        HarnessOutcome.Limit => 3,
        _ => 2,
    };
}

/// <summary>Opcje przebiegu. Wartości liczbowe przyjmują hex z prefiksem <c>0x</c> albo bez.</summary>
public sealed class HarnessOptions
{
    public const long DefaultLimitCycles = 200_000_000;

    /// <summary>Ścieżka obrazu pamięci (wymagana).</summary>
    public string? ImagePath { get; set; }

    /// <summary>Adres wczytania obrazu (domyślnie <c>$0000</c>, jak w testach Dormanna).</summary>
    public ushort Origin { get; set; }

    /// <summary>Początkowy <c>PC</c>.</summary>
    public ushort Pc { get; set; } = 0x0400;

    /// <summary>Adres pętli sukcesu; <c>null</c> = każda pętla jest raportowana bez oceny.</summary>
    public ushort? SuccessPc { get; set; }

    public long LimitCycles { get; set; } = DefaultLimitCycles;

    /// <summary>Nazwa wariantu: <c>nmos</c>, <c>c02</c> albo <c>2a03</c>.</summary>
    public string Variant { get; set; } = "nmos";

    /// <summary>Adres rejestru zwrotnego (etap O): zapisy sterują liniami IRQ (bit 0) i NMI (bit 1).</summary>
    public ushort? WatchRegister { get; set; }

    /// <summary>Adres bajtu wyniku odczytywanego po zakończeniu (np. <c>ERROR</c> w teście dziesiętnym).</summary>
    public ushort? ReportByteAddress { get; set; }

    public bool Json { get; set; }

    public bool Help { get; set; }

    /// <summary>Błąd parsowania; <c>null</c>, gdy opcje są poprawne.</summary>
    public string? Error { get; private set; }

    /// <summary>Parsuje argumenty wiersza poleceń. Nie rzuca wyjątków — błąd trafia do <see cref="Error"/>.</summary>
    public static HarnessOptions Parse(IReadOnlyList<string> args)
    {
        var options = new HarnessOptions();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--help" or "-h" or "help":
                    options.Help = true;
                    break;
                case "--image":
                    options.ImagePath = Next(args, ref i, arg, options);
                    break;
                case "--origin":
                    options.Origin = (ushort)Number(args, ref i, arg, options);
                    break;
                case "--pc":
                    options.Pc = (ushort)Number(args, ref i, arg, options);
                    break;
                case "--success-pc":
                    options.SuccessPc = (ushort)Number(args, ref i, arg, options);
                    break;
                case "--limit-cycles":
                    options.LimitCycles = Number(args, ref i, arg, options);
                    break;
                case "--variant":
                    options.Variant = Next(args, ref i, arg, options) ?? options.Variant;
                    break;
                case "--watch-register":
                    options.WatchRegister = (ushort)Number(args, ref i, arg, options);
                    break;
                case "--report-byte":
                    options.ReportByteAddress = (ushort)Number(args, ref i, arg, options);
                    break;
                case "--format":
                    string? format = Next(args, ref i, arg, options);
                    options.Json = string.Equals(format, "json", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    options.Error ??= $"nieznana opcja: {arg}";
                    break;
            }

            if (options.Error is not null)
            {
                return options;
            }
        }

        if (!options.Help && string.IsNullOrWhiteSpace(options.ImagePath))
        {
            options.Error = "brak --image <plik>";
        }

        return options;
    }

    private static string? Next(IReadOnlyList<string> args, ref int index, string name, HarnessOptions options)
    {
        if (index + 1 >= args.Count)
        {
            options.Error ??= $"opcja {name} wymaga wartości";
            return null;
        }

        index++;
        return args[index];
    }

    private static long Number(IReadOnlyList<string> args, ref int index, string name, HarnessOptions options)
    {
        string? text = Next(args, ref index, name, options);
        if (text is null)
        {
            return 0;
        }

        bool hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        string digits = hex ? text[2..] : text;
        if (long.TryParse(digits, hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
        {
            return value;
        }

        options.Error ??= $"opcja {name}: niepoprawna liczba „{text}”";
        return 0;
    }
}
