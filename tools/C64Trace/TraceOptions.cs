using System.Globalization;

namespace C64Trace;

/// <summary>Ustawienie rejestru: nazwa (A, X, Y, SP) i wartość.</summary>
internal sealed record RegisterAssignment(string Name, byte Value);

/// <summary>Zapis do magistrali wykonany przed startem (--poke).</summary>
internal sealed record MemoryWrite(ushort Address, byte Value);

/// <summary>Argumenty wiersza poleceń tracera.</summary>
internal sealed record TraceOptions
{
    /// <summary>Nazwa scenariusza własnego programu (zamiast nazwy wbudowanej).</summary>
    public const string CustomScenario = "custom";

    /// <summary>Wbudowane scenariusze; <c>count</c> to pomiar bez śladu.</summary>
    public static readonly IReadOnlyList<string> BuiltInScenarios = ["demo", "rmw", "badline", "count"];

    /// <summary>Maksymalna liczba cykli akceptowana w --cycles.</summary>
    public const long MaxCycles = 1_000_000;

    /// <summary>Domyślny limit cykli dla własnego programu (bez --cycles, --until, --instr).</summary>
    public const int DefaultCustomCycles = 1000;

    /// <summary>Domyślny adres ładowania i startowy PC własnego programu.</summary>
    public const ushort DefaultOrigin = 0x0200;

    public bool ShowHelp { get; init; }

    public string Scenario { get; init; } = "demo";

    /// <summary>Prawda, gdy program podano przez --hex lub --load.</summary>
    public bool IsCustom => Hex is not null || LoadPath is not null;

    /// <summary>Pierwszy cykl do wyświetlenia (włącznie).</summary>
    public long From { get; init; }

    /// <summary>Ostatni cykl do wyświetlenia i symulacji (włącznie); 0 = bez ograniczenia.</summary>
    public long To { get; init; }

    /// <summary>Plik wyjściowy zrzutu; brak = brak zrzutu.</summary>
    public string? DumpPath { get; init; }

    /// <summary>Format stdout i pliku zrzutu: <c>text</c>, <c>vice</c> lub <c>json</c>.</summary>
    public string Format { get; init; } = "text";

    /// <summary>Bajty z --hex.</summary>
    public byte[]? Hex { get; init; }

    /// <summary>Ścieżka pliku z --load.</summary>
    public string? LoadPath { get; init; }

    /// <summary>Adres ładowania z --org.</summary>
    public ushort? Org { get; init; }

    public IReadOnlyList<RegisterAssignment> Registers { get; init; } = [];

    public IReadOnlyList<MemoryWrite> Pokes { get; init; } = [];

    public IReadOnlyList<StopCondition> Until { get; init; } = [];

    /// <summary>Maksymalna liczba cykli z --cycles.</summary>
    public long? Cycles { get; init; }

    /// <summary>Liczba ukończonych instrukcji z --instr.</summary>
    public long? Instructions { get; init; }

    public static TraceOptions Parse(string[] args, out string? error)
    {
        error = null;

        string? scenarioArg = null;
        long from = 0;
        long to = 0;
        string? dumpPath = null;
        string format = "text";
        byte[]? hex = null;
        string? loadPath = null;
        ushort? org = null;
        var registers = new List<RegisterAssignment>();
        var pokes = new List<MemoryWrite>();
        var until = new List<StopCondition>();
        long? cycles = null;
        long? instructions = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--from" or "-f":
                    if (!TryNext(args, ref i, out string fromText))
                    {
                        return Fail("Brak wartości po --from.", out error);
                    }

                    if (!long.TryParse(fromText, out from) || from < 0)
                    {
                        return Fail($"--from wymaga liczby cykli >= 0 (otrzymano '{fromText}').", out error);
                    }

                    break;

                case "--to" or "-t":
                    if (!TryNext(args, ref i, out string toText))
                    {
                        return Fail("Brak wartości po --to.", out error);
                    }

                    if (!long.TryParse(toText, out to) || to < 0)
                    {
                        return Fail($"--to wymaga liczby cykli >= 0 (otrzymano '{toText}').", out error);
                    }

                    break;

                case "--dump" or "-d":
                    if (!TryNext(args, ref i, out string path))
                    {
                        return Fail("Brak ścieżki po --dump.", out error);
                    }

                    dumpPath = path;
                    break;

                case "--format":
                    if (!TryNext(args, ref i, out string formatText))
                    {
                        return Fail("Brak wartości po --format.", out error);
                    }

                    if (!TraceFormatter.IsKnownFormat(formatText))
                    {
                        return Fail($"--format dopuszcza 'text', 'vice' lub 'json' (otrzymano '{formatText}').", out error);
                    }

                    format = formatText;
                    break;

                case "--hex":
                    if (!TryNext(args, ref i, out string hexText))
                    {
                        return Fail("Brak bajtów po --hex.", out error);
                    }

                    if (!TryParseHexBytes(hexText, out hex, out string? badToken))
                    {
                        return Fail(
                            badToken is null
                                ? "--hex wymaga co najmniej jednego bajtu."
                                : $"--hex wymaga poprawnych bajtów hex (otrzymano '{badToken}')",
                            out error);
                    }

                    break;

                case "--load":
                    if (!TryNext(args, ref i, out string loadText))
                    {
                        return Fail("Brak ścieżki po --load.", out error);
                    }

                    loadPath = loadText;
                    break;

                case "--org":
                    if (!TryNext(args, ref i, out string orgText))
                    {
                        return Fail("Brak adresu po --org.", out error);
                    }

                    if (!TryParseHex(orgText, 4, 0xFFFF, out int orgValue))
                    {
                        return Fail($"--org wymaga adresu hex 0000..FFFF (otrzymano '{orgText}').", out error);
                    }

                    org = (ushort)orgValue;
                    break;

                case "--reg":
                    if (!TryNext(args, ref i, out string regText))
                    {
                        return Fail("Brak przypisania po --reg.", out error);
                    }

                    RegisterAssignment? register = ParseRegister(regText);

                    if (register is null)
                    {
                        return Fail(
                            $"--reg wymaga NAZWA=WARTOŚĆ, np. A=7F (dozwolone: A, X, Y, SP; otrzymano '{regText}').",
                            out error);
                    }

                    registers.Add(register);
                    break;

                case "--poke":
                    if (!TryNext(args, ref i, out string pokeText))
                    {
                        return Fail("Brak przypisania po --poke.", out error);
                    }

                    MemoryWrite? poke = ParsePoke(pokeText);

                    if (poke is null)
                    {
                        return Fail(
                            $"--poke wymaga ADRES=WARTOŚĆ w hex, np. D011=18 (otrzymano '{pokeText}').",
                            out error);
                    }

                    pokes.Add(poke);
                    break;

                case "--until":
                    if (!TryNext(args, ref i, out string untilText))
                    {
                        return Fail("Brak warunku po --until.", out error);
                    }

                    StopCondition? condition = StopCondition.Parse(untilText, out string? conditionError);

                    if (condition is null)
                    {
                        return Fail(conditionError ?? "Niepoprawny warunek --until.", out error);
                    }

                    until.Add(condition);
                    break;

                case "--cycles":
                    if (!TryNext(args, ref i, out string cyclesText))
                    {
                        return Fail("Brak liczby po --cycles.", out error);
                    }

                    if (!long.TryParse(cyclesText, out long cyclesValue) || cyclesValue < 1 || cyclesValue > MaxCycles)
                    {
                        return Fail(
                            $"--cycles wymaga liczby 1..{MaxCycles} (otrzymano '{cyclesText}').",
                            out error);
                    }

                    cycles = cyclesValue;
                    break;

                case "--instr":
                    if (!TryNext(args, ref i, out string instrText))
                    {
                        return Fail("Brak liczby po --instr.", out error);
                    }

                    if (!long.TryParse(instrText, out long instrValue) || instrValue < 1)
                    {
                        return Fail($"--instr wymaga liczby instrukcji >= 1 (otrzymano '{instrText}').", out error);
                    }

                    instructions = instrValue;
                    break;

                case "--help" or "-h":
                    return new TraceOptions { ShowHelp = true };

                default:
                    if (arg.StartsWith('-'))
                    {
                        return Fail($"Nieznana opcja '{arg}'.", out error);
                    }

                    scenarioArg = arg.ToLowerInvariant();
                    break;
            }
        }

        if (to != 0 && to < from)
        {
            return Fail($"--to ({to}) jest mniejsze niż --from ({from}).", out error);
        }

        if (hex is not null && loadPath is not null)
        {
            return Fail("Podaj --hex albo --load, nie oba naraz.", out error);
        }

        bool custom = hex is not null || loadPath is not null;

        if (custom)
        {
            if (scenarioArg is not null)
            {
                return Fail($"--hex/--load nie łączy się z nazwą scenariusza '{scenarioArg}'.", out error);
            }
        }
        else
        {
            if (org is not null)
            {
                return Fail("--org działa tylko z własnym programem (--hex lub --load).", out error);
            }

            string scenario = scenarioArg ?? "demo";

            if (scenario == CustomScenario)
            {
                return Fail("scenariusz 'custom' wymaga --hex albo --load.", out error);
            }

            if (!BuiltInScenarios.Contains(scenario))
            {
                return Fail(
                    $"nieznany scenariusz '{scenario}'. Dostępne: {string.Join(", ", BuiltInScenarios)}.",
                    out error);
            }
        }

        return new TraceOptions
        {
            Scenario = scenarioArg ?? "demo",
            From = from,
            To = to,
            DumpPath = dumpPath,
            Format = format,
            Hex = hex,
            LoadPath = loadPath,
            Org = org,
            Registers = registers,
            Pokes = pokes,
            Until = until,
            Cycles = cycles,
            Instructions = instructions,
        };

        static TraceOptions Fail(string message, out string? error)
        {
            error = message;
            return new TraceOptions();
        }

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

    /// <summary>
    /// Parsuje liczbę hex z prefiksem <c>$</c>, <c>0x</c> lub bez prefiksu.
    /// Zwraca false, gdy tekst nie jest hex, ma za dużo cyfr albo wartość przekracza <paramref name="maxValue"/>.
    /// </summary>
    internal static bool TryParseHex(string text, int maxDigits, int maxValue, out int value)
    {
        value = 0;
        string digits = text;

        if (digits.StartsWith('$'))
        {
            digits = digits[1..];
        }
        else if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            digits = digits[2..];
        }

        if (digits.Length == 0 || digits.Length > maxDigits)
        {
            return false;
        }

        foreach (char c in digits)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        value = int.Parse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return value <= maxValue;
    }

    /// <summary>Bajty oddzielone spacją lub przecinkiem. Przy błędzie zwraca pierwszy zły znacznik.</summary>
    private static bool TryParseHexBytes(string text, out byte[]? bytes, out string? badToken)
    {
        string[] tokens = text.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
        badToken = null;
        bytes = null;

        if (tokens.Length == 0)
        {
            return false;
        }

        var result = new byte[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!TryParseHex(tokens[i], 2, 0xFF, out int value))
            {
                badToken = tokens[i];
                return false;
            }

            result[i] = (byte)value;
        }

        bytes = result;
        return true;
    }

    /// <summary>Parsuje <c>A=7F</c>; null przy błędzie.</summary>
    private static RegisterAssignment? ParseRegister(string text)
    {
        int eq = text.IndexOf('=');

        if (eq <= 0)
        {
            return null;
        }

        string name = text[..eq].Trim().ToUpperInvariant();

        if (name is not ("A" or "X" or "Y" or "SP"))
        {
            return null;
        }

        return TryParseHex(text[(eq + 1)..], 2, 0xFF, out int value)
            ? new RegisterAssignment(name, (byte)value)
            : null;
    }

    /// <summary>Parsuje <c>D011=18</c>; null przy błędzie.</summary>
    private static MemoryWrite? ParsePoke(string text)
    {
        int eq = text.IndexOf('=');

        if (eq <= 0)
        {
            return null;
        }

        if (!TryParseHex(text[..eq], 4, 0xFFFF, out int address)
            || !TryParseHex(text[(eq + 1)..], 2, 0xFF, out int value))
        {
            return null;
        }

        return new MemoryWrite((ushort)address, (byte)value);
    }

    public const string Usage = """
        Tracer maszyny C64 — wykonuje program cykl po cyklu.

        Użycie:
          dotnet run -c Release --project tools/C64Trace -- [scenariusz | --hex B | --load P] [opcje]

        Scenariusze wbudowane:
          demo      LDA/STA/INC/JMP w pętli (domyślnie)
          rmw       INC abs — pokazuje dwa zapisy
          badline   linia z badline — pokazuje BA/AEC i kradzież cykli
          count     pomiar kosztu klatki, bez śladu

        Własny program (zamiast nazwy scenariusza; nazwa wewnętrzna: custom):
          --hex <bajty>            bajty hex, np. "A9 7F 8D 00 03" (spacja lub przecinek; $ i 0x dozwolone)
          --load <plik>            plik binarny; dla .prg pierwsze 2 bajty to adres ładowania
          --org <adres>            adres ładowania i startowy PC (domyślnie 0200; .prg: adres z pliku)

        Ustawienia (dowolny scenariusz):
          --reg A=7F               rejestr po Reset; nazwy A, X, Y, SP (można powtarzać)
          --poke ADRES=WARTOŚĆ     zapis do magistrali przed startem, np. --poke D011=18 (można powtarzać)

        Warunki zatrzymania:
          --cycles <N>             maksymalna liczba cykli maszyny (1..1000000)
                                   własny program domyślnie 1000 cykli
          --instr <N>              zatrzymaj po N ukończonych instrukcjach
          --until <warunek>        pc=ADRES | write=ADRES | read=ADRES | jam | raster=LINIA:CYKL
                                   (można powtarzać; wiersz, w którym warunek zaszedł, jest w śladzie)

        Opcje wyświetlania:
          --from <cykl>, -f <cykl>   pierwszy wyświetlany cykl (włącznie)
          --to <cykl>, -t <cykl>     ostatni wyświetlany i symulowany cykl (włącznie)
          --dump <plik>, -d <plik>   zapisz ślad do pliku (format jak --format)
          --format text|vice|json    format stdout i pliku zrzutu (domyślnie text)
          --help, -h                 ta pomoc

        Powody zatrzymania (linia "Zatrzymano" / pole "stop" w JSON):
          end            koniec scenariusza wbudowanego
          cycle-limit    osiągnięto limit cykli (--cycles, --to, własny program)
          instr-limit    ukończono N instrukcji (--instr)
          until:<warunek> spełniony warunek --until
          jam            CPU zawiesił się (JAM)

        Kody wyjścia:
          0  OK (także przy --until jam)
          2  błąd użycia
          3  CPU zawiesił się (JAM) i nie zażądano --until jam

        Przykłady:
          ... -- demo --instr 3
          ... -- --hex "A9 7F 8D 00 03 4C 00 02" --until write=0300
          ... -- badline --from 3030 --to 3060
          ... -- --hex "A9 7F 8D 00 03" --cycles 6 --format json
          ... -- --load gra.prg --org 0801 --until jam --dump slad.json --format json
        """;
}
