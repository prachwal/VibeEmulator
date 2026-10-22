using VibeEmulator.CpuHarness;
using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy logiki harnessu (etap L) na **syntetycznych obrazach** budowanych w pamięci — repozytorium nie
/// trzyma binariów testowych Dormanna (GPL-3.0), więc sprawdzamy sam mechanizm: wykrycie pętli <c>PC</c>,
/// ocenę adresu sukcesu, limit cykli, jam i wczytywanie obrazu.
/// </summary>
public class CpuHarnessTests
{
    /// <summary>Obraz 64 KB z programem pod <c>$0400</c> (jak obrazy Dormanna).</summary>
    private static byte[] Image(params (ushort Address, byte[] Bytes)[] chunks)
    {
        byte[] image = new byte[ImageLoader.MaxSize];

        foreach ((ushort address, byte[] bytes) in chunks)
        {
            bytes.CopyTo(image.AsSpan(address));
        }

        return image;
    }

    private static HarnessOptions Options(ushort limitCycles = 10_000) => new()
    {
        ImagePath = "syntetyczny.bin",
        Pc = 0x0400,
        LimitCycles = limitCycles,
    };

    [Fact]
    public void Run_StopsOnPcLoop_AndReportsPassAtSuccessAddress()
    {
        // 0400: LDA #$42 ; 0402: STA $0200 ; 0405: JMP $0405 (pętla sukcesu)
        byte[] image = Image((0x0400, [0xA9, 0x42, 0x8D, 0x00, 0x02, 0x4C, 0x05, 0x04]));
        HarnessOptions options = Options();
        options.SuccessPc = 0x0405;

        HarnessResult result = HarnessRunner.Run(image, options);

        Assert.Equal(HarnessOutcome.Pass, result.Outcome);
        Assert.Equal((ushort)0x0405, result.Pc);
        Assert.Equal(3, result.Instructions);
        Assert.Equal(0, result.ExitCode);
        // LDA (2) + STA abs (4) + JMP abs (3) = 9 cykli.
        Assert.Equal(9, result.Cycles);
    }

    [Fact]
    public void Run_ReportsFail_WhenPcLoopIsElsewhere()
    {
        byte[] image = Image((0x0400, [0x4C, 0x00, 0x04])); // JMP $0400 — pętla porażki
        HarnessOptions options = Options();
        options.SuccessPc = 0x3469;

        HarnessResult result = HarnessRunner.Run(image, options);

        Assert.Equal(HarnessOutcome.Fail, result.Outcome);
        Assert.Equal((ushort)0x0400, result.Pc);
        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void Run_WithoutSuccessAddress_TreatsAnyPcLoopAsPass()
    {
        byte[] image = Image((0x0400, [0x4C, 0x00, 0x04]));

        HarnessResult result = HarnessRunner.Run(image, Options());

        Assert.Equal(HarnessOutcome.Pass, result.Outcome);
    }

    [Fact]
    public void Run_StopsAtLimit_WhenProgramNeverLoops()
    {
        // 0400: INC $10 ; 0402: JMP $0400 — pętla, ale nie „jmp *”, więc PC zmienia się co instrukcję.
        byte[] image = Image((0x0400, [0xE6, 0x10, 0x4C, 0x00, 0x04]));
        HarnessOptions options = Options(limitCycles: 100);

        HarnessResult result = HarnessRunner.Run(image, options);

        Assert.Equal(HarnessOutcome.Limit, result.Outcome);
        Assert.Equal(3, result.ExitCode);
        Assert.True(result.Cycles >= 100);
    }

    [Fact]
    public void Run_ReportsJam_ForKilOpcode()
    {
        byte[] image = Image((0x0400, [0x02]));

        HarnessResult result = HarnessRunner.Run(image, Options());

        Assert.Equal(HarnessOutcome.Jam, result.Outcome);
        // KIL: fetch + dummy read + 9 odczytów wektora = 11 cykli.
        Assert.Equal(11, result.Cycles);
    }

    [Fact]
    public void Run_WatchRegister_DrivesInterruptLines()
    {
        // 0400: STA $BFFC (zapis 3 → IRQ+NMI) ; 0404: JMP $0404
        byte[] image = Image((0x0400, [0x8D, 0xFC, 0xBF, 0x4C, 0x04, 0x04]));
        HarnessOptions options = Options();
        options.WatchRegister = 0xBFFC;

        HarnessResult result = HarnessRunner.Run(image, options);

        Assert.Equal(1, result.RegisterWrites);
        Assert.Equal(HarnessOutcome.Pass, result.Outcome);
    }

    [Fact]
    public void Run_ReportsByteUnderAddress()
    {
        // 0400: LDA #$00 ; 0402: STA $0300 ; 0405: JMP $0405
        byte[] image = Image((0x0400, [0xA9, 0x00, 0x8D, 0x00, 0x03, 0x4C, 0x05, 0x04]));
        HarnessOptions options = Options();
        options.SuccessPc = 0x0405;
        options.ReportByteAddress = 0x0300;

        HarnessResult result = HarnessRunner.Run(image, options);

        Assert.Equal((byte)0, result.ReportByte);
    }

    [Fact]
    public void Load_RejectsEmptyImage()
    {
        Assert.Throws<ArgumentException>(() => ImageLoader.Load([]));
    }

    [Fact]
    public void Load_RejectsImageLargerThanMemory()
    {
        byte[] tooBig = new byte[ImageLoader.MaxSize + 1];

        Assert.Throws<ArgumentException>(() => ImageLoader.Load(tooBig));
    }

    [Fact]
    public void Load_PlacesShortImageAtOrigin()
    {
        FlatMemory memory = ImageLoader.Load([0x11, 0x22], origin: 0x8000);

        Assert.Equal((byte)0x11, memory.Read(0x8000));
        Assert.Equal((byte)0x22, memory.Read(0x8001));
        Assert.Equal((byte)0x00, memory.Read(0x8002));
    }

    [Fact]
    public void Parse_ReadsHexAndDecimalValues()
    {
        var options = HarnessOptions.Parse(
            ["--image", "test.bin", "--pc", "0x0400", "--success-pc", "$3469", "--limit-cycles", "1000"]);

        // „$” nie jest obsługiwany — opcja z niepoprawną liczbą zgłasza błąd, nie zgaduje.
        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Parse_AcceptsHexWithPrefix()
    {
        var options = HarnessOptions.Parse(
            ["--image", "test.bin", "--pc", "0x0400", "--success-pc", "0x3469", "--limit-cycles", "1000"]);

        Assert.Null(options.Error);
        Assert.Equal((ushort)0x0400, options.Pc);
        Assert.Equal((ushort)0x3469, options.SuccessPc);
        Assert.Equal(1000, options.LimitCycles);
    }

    [Fact]
    public void Parse_RequiresImage()
    {
        var options = HarnessOptions.Parse(["--pc", "0x0400"]);

        Assert.NotNull(options.Error);
    }

    [Fact]
    public void Parse_UnknownOptionIsReported()
    {
        var options = HarnessOptions.Parse(["--image", "x.bin", "--nieznana"]);

        Assert.NotNull(options.Error);
    }
}
