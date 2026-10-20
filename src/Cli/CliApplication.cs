namespace VibeEmulator.Cli;

/// <summary>
/// Punkt wejścia logiki CLI. Wydzielony z <c>Program.cs</c>, aby dał się testować
/// bez uruchamiania procesu (wstrzykiwane <see cref="TextWriter"/>).
/// </summary>
public static class CliApplication
{
    public const int ExitSuccess = 0;
    public const int ExitUsageError = 2;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0)
        {
            WriteHelp(output);
            return ExitSuccess;
        }

        switch (args[0])
        {
            case "help" or "-h" or "--help":
                WriteHelp(output);
                return ExitSuccess;

            case "version" or "-v" or "--version":
                output.WriteLine(GetVersion());
                return ExitSuccess;

            default:
                error.WriteLine($"error: unknown command '{args[0]}'");
                error.WriteLine("Run 'vibe-emulator help' for usage.");
                return ExitUsageError;
        }
    }

    public static string GetVersion() =>
        typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("vibe-emulator");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  vibe-emulator <command>");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  version   Print version information");
        output.WriteLine("  help      Show this help");
    }
}
