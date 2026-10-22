using VibeEmulator.Z80Harness;

namespace VibeEmulator.Z80.Tests;

public sealed class ZexHarnessTests
{
    [Fact]
    public void Run_Call5Function2_PrintsCharacterAndReturnsToProgram()
    {
        byte[] image = [0x0E, 0x02, 0x1E, (byte)'!', 0xCD, 0x05, 0x00, 0xC3, 0x00, 0x00];
        StringWriter output = new();

        ZexHarnessResult result = ZexHarnessRunner.Run(image, 100, output);

        Assert.Equal(ZexHarnessOutcome.Pass, result.Outcome);
        Assert.Equal("!", output.ToString());
        Assert.Equal("!", result.ProgramOutput);
        Assert.InRange(result.TStates, 1, 100);
    }

    [Fact]
    public void Run_Call5Function9_PrintsDollarTerminatedString()
    {
        byte[] image =
        [
            0x0E, 0x09,
            0x11, 0x10, 0x01,
            0xCD, 0x05, 0x00,
            0xC3, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            (byte)'Z', (byte)'E', (byte)'X', (byte)'$',
        ];
        StringWriter output = new();

        ZexHarnessResult result = ZexHarnessRunner.Run(image, 200, output);

        Assert.Equal(ZexHarnessOutcome.Pass, result.Outcome);
        Assert.Equal("ZEX", output.ToString());
    }

    [Fact]
    public void Run_JumpToZero_CompletesSuccessfully()
    {
        ZexHarnessResult result = ZexHarnessRunner.Run([0xC3, 0x00, 0x00], 20, TextWriter.Null);

        Assert.Equal(ZexHarnessOutcome.Pass, result.Outcome);
        Assert.Equal(10, result.TStates);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Run_ProgramOutputContainsError_ReturnsFailure()
    {
        byte[] image =
        [
            0x0E, 0x09,
            0x11, 0x08, 0x01,
            0xCD, 0x05, 0x00,
            0xC3, 0x00, 0x00,
            (byte)'E', (byte)'R', (byte)'R', (byte)'O', (byte)'R', (byte)'$',
        ];

        ZexHarnessResult result = ZexHarnessRunner.Run(image, 200, TextWriter.Null);

        Assert.Equal(ZexHarnessOutcome.Fail, result.Outcome);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("ERROR", result.ProgramOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_InfiniteJump_StopsAtTStateLimit()
    {
        ZexHarnessResult result = ZexHarnessRunner.Run([0xC3, 0x00, 0x01], 5, TextWriter.Null);

        Assert.Equal(ZexHarnessOutcome.Limit, result.Outcome);
        Assert.Equal(5, result.TStates);
        Assert.Equal(3, result.ExitCode);
    }

    [Fact]
    public void Run_UnsupportedBdosFunction_ReturnsFailure()
    {
        byte[] image = [0x0E, 0x01, 0xCD, 0x05, 0x00, 0xC3, 0x00, 0x00];

        ZexHarnessResult result = ZexHarnessRunner.Run(image, 100, TextWriter.Null);

        Assert.Equal(ZexHarnessOutcome.Fail, result.Outcome);
        Assert.Equal("nieobsługiwana funkcja BDOS 1", result.Error);
    }

    [Fact]
    public void Run_RejectsEmptyOrOversizedComImage()
    {
        Assert.Throws<ArgumentException>(() => ZexHarnessRunner.Run([], 100, TextWriter.Null));
        Assert.Throws<ArgumentException>(() => ZexHarnessRunner.Run(new byte[0xFF01], 100, TextWriter.Null));
    }

    [Fact]
    public void Parse_ValidArguments_UsesImageLimitAndJsonFormat()
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse(["--image", "zexdoc.com", "--limit-tstates", "1234", "--format", "json"]);

        Assert.Null(options.Error);
        Assert.Equal("zexdoc.com", options.ImagePath);
        Assert.Equal(1234, options.LimitTStates);
        Assert.True(options.Json);
    }

    [Fact]
    public void Parse_MissingImage_ReturnsInputError()
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse([]);

        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Parse_MissingImageValue_ReturnsInputError()
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse(["--image"]);

        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Parse_NonPositiveLimit_ReturnsInputError()
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse(["--image", "x.com", "--limit-tstates", "0"]);

        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Parse_UnknownFormat_ReturnsInputError()
    {
        ZexHarnessOptions options = ZexHarnessOptions.Parse(["--image", "x.com", "--format", "xml"]);

        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Run_StopsReadingBdosStringWhenItWrapsAddressSpace()
    {
        byte[] image = [0x0E, 0x09, 0x11, 0x00, 0x01, 0xCD, 0x05, 0x00];
        StringWriter output = new();

        ZexHarnessResult result = ZexHarnessRunner.Run(image, 100, output);

        Assert.Equal(ZexHarnessOutcome.Fail, result.Outcome);
        Assert.Contains("terminatora", result.Error);
    }
}
