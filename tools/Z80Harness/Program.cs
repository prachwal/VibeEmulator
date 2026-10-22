using System.Text.Json;

namespace VibeEmulator.Z80Harness;

public static class Program
{
    public static int Main(string[] args)
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse(args);
        if (options.Help)
        {
            PrintHelp();
            return 0;
        }

        if (options.Error is not null)
        {
            Console.Error.WriteLine($"error: {options.Error}");
            Console.Error.WriteLine("Użyj --help, aby zobaczyć dostępne opcje.");
            return 4;
        }

        byte[] image;
        try
        {
            image = File.ReadAllBytes(options.ImagePath!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"error: nie można wczytać „{options.ImagePath}”: {exception.Message}");
            return 4;
        }

        ZexHarnessResult result;
        try
        {
            result = ZexHarnessRunner.Run(image, options.LimitTStates, options.Json ? TextWriter.Null : Console.Out);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 4;
        }

        if (options.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"wynik:        {OutcomeText(result.Outcome)}");
            Console.WriteLine($"T-state:      {result.TStates}");
            Console.WriteLine($"czas:         {result.Elapsed.TotalSeconds:F3} s");
            Console.WriteLine($"przechwycone: {(result.OutputTruncated ? "ucięte po 1 Mi znaków" : "pełne")}");
            if (result.Error is not null)
            {
                Console.WriteLine($"powód:        {result.Error}");
            }
        }

        return result.ExitCode;
    }

    private static string OutcomeText(ZexHarnessOutcome outcome) => outcome switch
    {
        ZexHarnessOutcome.Pass => "pass (program zakończył się przez JP 0, bez ERROR)",
        ZexHarnessOutcome.Fail => "fail",
        _ => "limit (osiągnięto limit T-state)",
    };

    private static void PrintHelp()
    {
        Console.WriteLine($"""
            VibeEmulator.Z80Harness — CP/M shim dla zexdoc/zexall.

            Użycie:
              Z80Harness --image <plik.com> [opcje]

            Opcje:
              --image <plik.com>       plik COM ładowany pod 0x0100 (wymagany)
              --limit-tstates <liczba> limit T-state (domyślnie {ZexHarnessOptions.DefaultLimitTStates})
              --format <text|json>     format wyniku (domyślnie text)
              --help                   ta pomoc

            CP/M: BDOS 2 wypisuje E; BDOS 9 wypisuje łańcuch DE zakończony '$'.
            Zakończenie: PC=0 po JP 0. Wyjścia: 0=pass, 2=fail, 3=limit, 4=błąd wejścia.
            Pliki zexdoc.com/zexall.com nie są dołączone do repozytorium.
            """);
    }
}
