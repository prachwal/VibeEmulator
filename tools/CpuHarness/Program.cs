using System.Text.Json;

namespace VibeEmulator.CpuHarness;

/// <summary>
/// Wejście narzędzia: parsuje opcje, wczytuje obraz i wypisuje wynik.
/// Narzędzie jest diagnostyczne — wolno mu pisać do <c>Console</c>.
/// </summary>
public static class Program
{
    private static readonly JsonWriterOptions JsonOptions = new() { Indented = true };

    public static int Main(string[] args)
    {
        var options = HarnessOptions.Parse(args);

        if (options.Help)
        {
            PrintHelp();
            return 0;
        }

        if (options.Error is not null)
        {
            Console.Error.WriteLine($"błąd: {options.Error}");
            Console.Error.WriteLine("Użyj --help, aby zobaczyć dostępne opcje.");
            return 4;
        }

        if (options.InesPath is not null)
        {
            return RunNes(options);
        }

        byte[] image;
        try
        {
            image = File.ReadAllBytes(options.ImagePath!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"błąd: nie można wczytać „{options.ImagePath}”: {ex.Message}");
            return 4;
        }

        HarnessResult result;
        try
        {
            result = HarnessRunner.Run(image, options);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"błąd: {ex.Message}");
            return 4;
        }

        if (options.Json)
        {
            WriteJson(result, options);
        }
        else
        {
            WriteText(result, options);
        }

        return result.ExitCode;
    }

    private static int RunNes(HarnessOptions options)
    {
        InesImage rom;
        try
        {
            rom = InesImage.Load(File.ReadAllBytes(options.InesPath!));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"błąd: nie można wczytać „{options.InesPath}”: {ex.Message}");
            return 4;
        }

        ushort entryPc = options.PcSpecified ? options.Pc : new NesBus(rom).ResetVector;

        if (options.NestestLogPath is not null)
        {
            List<NestestLine> log;
            try
            {
                log = NestestLine.Parse(File.ReadAllText(options.NestestLogPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"błąd: nie można wczytać logu „{options.NestestLogPath}”: {ex.Message}");
                return 4;
            }

            (NestestComparison comparison, HarnessResult nestestResult) =
                NestestRunner.Run(rom, log, entryPc, options.LimitCycles);
            WriteNestest(comparison, nestestResult, options);
            return nestestResult.ExitCode;
        }

        (HarnessResult result, string text, NesBus bus) = BlarggRunner.Run(rom, entryPc, options.LimitCycles);
        WriteBlargg(result, text, options);

        if (options.Dump is (ushort dumpAddress, int dumpLength))
        {
            WriteDump(bus, dumpAddress, dumpLength);
        }

        return result.ExitCode;
    }

    private static void WriteNestest(NestestComparison comparison, HarnessResult result, HarnessOptions options)
    {
        Console.WriteLine($"obraz:        {options.InesPath}");
        Console.WriteLine($"wynik:        {(comparison.FirstMismatch is null ? "pass (zgodny z logiem)" : "fail")}");
        Console.WriteLine($"linie logu:   {comparison.Matched} / {comparison.Total}");
        Console.WriteLine($"cykle:        {result.Cycles}");
        Console.WriteLine($"instrukcje:   {result.Instructions}");

        if (comparison.FirstMismatch is not null)
        {
            Console.WriteLine($"różnica:      {comparison.FirstMismatch}");
        }

        Console.WriteLine($"zapisy I/O:   {result.RegisterWrites}");
    }

    private static void WriteBlargg(HarnessResult result, string text, HarnessOptions options)
    {
        Console.WriteLine($"obraz:        {options.InesPath}");
        Console.WriteLine($"wynik:        {(result.Outcome == HarnessOutcome.Pass ? "pass (kod 0)" : "fail")}");
        Console.WriteLine($"kod statusu:  ${result.ReportByte:X2}");
        Console.WriteLine($"cykle:        {result.Cycles}");
        Console.WriteLine($"instrukcje:   {result.Instructions}");

        if (text.Length > 0)
        {
            Console.WriteLine("raport:");
            Console.WriteLine(text);
        }
    }

    private static void WriteDump(NesBus bus, ushort address, int length)
    {
        Console.WriteLine($"zrzut ${address:X4}, {length} B:");
        for (int offset = 0; offset < length; offset += 16)
        {
            var hex = new System.Text.StringBuilder();
            var ascii = new System.Text.StringBuilder();

            for (int index = 0; index < 16 && offset + index < length; index++)
            {
                ushort current = unchecked((ushort)(address + offset + index));
                byte value = bus.Peek(current);
                hex.Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
                ascii.Append(value is >= 0x20 and < 0x7F ? (char)value : '.');
            }

            Console.WriteLine($"  ${address + offset:X4}: {hex.ToString(),-48} {ascii}");
        }
    }

    private static void WriteJson(HarnessResult result, HarnessOptions options)
    {
        using var stream = Console.OpenStandardOutput();
        using var writer = new Utf8JsonWriter(stream, JsonOptions);

        writer.WriteStartObject();
        writer.WriteString("image", options.ImagePath);
        writer.WriteString("variant", options.Variant);
        writer.WriteString("outcome", result.Outcome.ToString().ToLowerInvariant());
        writer.WriteString("pc", $"0x{result.Pc:X4}");
        writer.WriteNumber("cycles", result.Cycles);
        writer.WriteNumber("instructions", result.Instructions);

        if (result.ReportByte is byte report)
        {
            writer.WriteNumber("reportByte", report);
        }

        writer.WriteNumber("registerWrites", result.RegisterWrites);
        writer.WriteEndObject();
        writer.Flush();
        stream.WriteByte((byte)'\n');
    }

    private static void WriteText(HarnessResult result, HarnessOptions options)
    {
        Console.WriteLine($"obraz:        {options.ImagePath}");
        Console.WriteLine($"wariant:      {options.Variant}");
        Console.WriteLine($"wynik:        {OutcomeText(result.Outcome)}");
        Console.WriteLine($"PC:           ${result.Pc:X4}");
        Console.WriteLine($"cykle:        {result.Cycles}");
        Console.WriteLine($"instrukcje:   {result.Instructions}");

        if (result.ReportByte is byte report)
        {
            Console.WriteLine($"bajt raportu: ${report:X2} ({(report == 0 ? "sukces" : "błąd")})");
        }

        if (options.WatchRegister is not null)
        {
            Console.WriteLine($"zapisy rejestru ${options.WatchRegister:X4}: {result.RegisterWrites}");
        }
    }

    private static string OutcomeText(HarnessOutcome outcome) => outcome switch
    {
        HarnessOutcome.Pass => "pass (pętla PC pod adresem sukcesu)",
        HarnessOutcome.Fail => "fail (pętla PC pod innym adresem)",
        HarnessOutcome.Limit => "limit (przekroczony limit cykli)",
        _ => "jam (rdzeń zatrzymany przez KIL/JAM)",
    };

    private static void PrintHelp()
    {
        Console.WriteLine("""
            VibeEmulator.CpuHarness — uruchamia rdzeń cyklowy na obrazie pamięci.

            Użycie:
              CpuHarness --image <plik> [opcje]

            Opcje:
              --image <plik>        obraz pamięci (wymagane); 64 KB dla testów Dormanna
              --origin <adres>      adres wczytania obrazu (domyślnie 0x0000)
              --pc <adres>          początkowy PC (obraz: 0x0400; NES: wektor resetu z ROM)
              --success-pc <adres>  adres pętli sukcesu; brak = każda pętla raportowana bez oceny
              --limit-cycles <n>    limit cykli (domyślnie 200000000)
              --variant <nazwa>     nmos (domyślny) albo c02
              --watch-register <a>  zapisy pod adres sterują IRQ (bit 0) i NMI (bit 1)
              --report-byte <adres> odczytaj bajt wyniku po zakończeniu (np. ERROR = 0)
              --ines <plik.nes>     tryb NES: NROM (mapper 0) + RAM $6000, bez PPU/APU
              --log <nestest.log>   porównaj stan po każdej instrukcji z logiem wzorcowym
              --blargg              czekaj na status pod $6000 (protokół instr_test-v5)
              --dump <adres>:<n>    zrzut pamięci po przebiegu, np. 0x6004:128
              --format <text|json>  format wyniku (domyślnie text)
              --help                ta pomoc

            Kody wyjścia:
              0 = pass, 2 = fail/jam, 3 = limit cykli, 4 = błąd wejścia

            Koniec testu: pętla PC, czyli instrukcja skacząca sama do siebie (jmp *) —
            konwencja testów Klausa Dormanna.
            """);
    }
}
