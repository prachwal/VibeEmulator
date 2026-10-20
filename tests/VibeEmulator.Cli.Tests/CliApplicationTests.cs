namespace VibeEmulator.Cli.Tests;

public class CliApplicationTests
{
    [Fact]
    public void NoArgs_PrintsHelp_AndSucceeds()
    {
        var (code, stdout, stderr) = Run();

        Assert.Equal(CliApplication.ExitSuccess, code);
        Assert.Contains("Usage:", stdout);
        Assert.Empty(stderr);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_PrintsUsage(string arg)
    {
        var (code, stdout, _) = Run(arg);

        Assert.Equal(CliApplication.ExitSuccess, code);
        Assert.Contains("vibe-emulator", stdout);
    }

    [Fact]
    public void Version_PrintsAssemblyVersion()
    {
        var (code, stdout, _) = Run("version");

        Assert.Equal(CliApplication.ExitSuccess, code);
        Assert.Equal(CliApplication.GetVersion(), stdout.Trim());
    }

    [Fact]
    public void UnknownCommand_ReturnsUsageError()
    {
        var (code, stdout, stderr) = Run("nope");

        Assert.Equal(CliApplication.ExitUsageError, code);
        Assert.Empty(stdout);
        Assert.Contains("unknown command 'nope'", stderr);
    }

    private static (int Code, string Stdout, string Stderr) Run(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int code = CliApplication.Run(args, stdout, stderr);
        return (code, stdout.ToString(), stderr.ToString());
    }
}
