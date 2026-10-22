using System.Globalization;

namespace PerfAudit;

internal sealed record Options(int Rounds = 9, int Frames = 200, int Iterations = 1_000_000, byte Seed = 55,
    bool VerifyOnly = false, bool ShowHelp = false)
{
    public const string Usage = """
        PerfAudit — pomiary aktualnego modelu C64, bez zewnętrznych ROM-ów.
        dotnet run -c Release --project tools/PerfAudit -- [opcje]
          --rounds N       Liczba rund (minimum 9, domyślnie 9).
          --frames N       Klatki na rundę (minimum 1, domyślnie 200).
          --iterations N   Ticki CPU / iteracje magistrali na rundę (minimum 1, domyślnie 1000000).
          --seed N         Wartość początkowa danych (dziesiętnie 0..255, domyślnie 55).
          --verify-only    Tylko sprawdzenie programów i bankowania, bez pomiaru.
          --help           Pomoc.
        Błąd argumentów: kod 2; błąd weryfikacji: kod 1; sukces: kod 0.
        Porównanie rewizji wymaga tego samego narzędzia i naprzemiennych uruchomień baza/nowa.
        """;

    public static bool TryParse(string[] args, out Options options, out string? error)
    {
        options = new Options();
        error = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--help")
            {
                options = options with { ShowHelp = true };
                continue;
            }

            if (arg == "--verify-only")
            {
                options = options with { VerifyOnly = true };
                continue;
            }

            if (arg is not ("--rounds" or "--frames" or "--iterations" or "--seed"))
            {
                error = $"nieznana opcja '{arg}'; użyj --help.";
                return false;
            }

            int minimum = arg == "--seed" ? 0 : arg == "--rounds" ? 9 : 1;
            int maximum = arg == "--seed" ? byte.MaxValue : int.MaxValue;
            if (++i >= args.Length || !int.TryParse(args[i], NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                || value < minimum || value > maximum)
            {
                error = $"{arg} wymaga liczby całkowitej z zakresu {minimum}..{maximum}.";
                return false;
            }

            options = arg switch
            {
                "--rounds" => options with { Rounds = value },
                "--frames" => options with { Frames = value },
                "--iterations" => options with { Iterations = value },
                _ => options with { Seed = (byte)value },
            };
        }

        return true;
    }
}
