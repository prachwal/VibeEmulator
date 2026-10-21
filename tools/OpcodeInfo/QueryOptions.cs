using System.Globalization;

namespace OpcodeInfo;

/// <summary>
/// Filtry zapytania: pojedynczy opkod, lista/ zakres opkodów, mnemonik, tryb adresowania, klasa i zestaw.
/// Filtry łączą się spójnikiem AND; brak filtra danego rodzaju = brak ograniczenia.
/// </summary>
internal sealed class QueryOptions
{
    public const string Usage =
        """
        OpcodeInfo — informacje o opkodach 6502/65C02 z tabel CSV jako JSON.

        Użycie:
          dotnet run -c Release --project tools/OpcodeInfo -- [opcje]

        Wybór (bez filtrów = wszystkie opkody):
          --opcode <spec>      jeden opkod lub grupa: A9 | A9,EA | 00-0F | A9,00-0F
          --mnemonic <lista>   mnemonic lub alias, np. LDA,ADC
          --mode <lista>       tryb: imm zp zpx zpy abs abx aby izx izy izp ind rel zpr iax,
                               a także impl/acc dla trybu niejawnego (puste pole w CSV)
          --class <lista>      klasa, np. official, unofficial, KIL, nowy-65C02, undefined-NOP;
                               gwiazdka na końcu dopasowuje prefiks (np. official*)
          --set <lista>        zestaw: nmos, c02

        Źródło danych:
          --file <ścieżka>     plik CSV (można powtarzać); domyślnie docs/cpu/opcodes/*.csv
          --csv-dir <ścieżka>  katalog z plikami CSV

        Wyjście:
          --pretty             JSON z wcięciami (domyślnie jedna linia)
          --verify-code        porównaj adnotacje [Opcode] w kodzie z tabelami CSV
          --include-missing    z --verify-code: dołącz pełną listę opkodów spoza kodu
          -h, --help           ta pomoc

        Przykłady:
          … --opcode A9
          … --opcode 00-0F --set nmos --pretty
          … --opcode A9,EA --set c02
          … --mnemonic LDA --mode abs,abx
          … --class unofficial --set nmos
          … --verify-code --pretty

        Tryb --verify-code czyta refleksją adnotacje [Opcode] z zestawu VibeEmulator.Mos6502
        i porównuje mnemonic, tryb adresowania, długość instrukcji oraz cykle bazowe z CSV.
        Cykle są wyprowadzane z faz rdzenia (tryb + rodzaj + cykle wewnętrzne), nie z CSV.
        Filtry wyboru (--opcode, --mnemonic, --mode, --class, --set) w tym trybie nie działają.

        Kody wyjścia: 0 = OK (także 0 trafień), 1 = niezgodności kodu z CSV (tylko --verify-code),
        2 = błąd użycia lub danych.
        """;

    private readonly HashSet<byte>? _opcodes;
    private readonly HashSet<string>? _mnemonics;
    private readonly HashSet<string>? _modes;
    private readonly List<ClassNameFilter>? _classes;
    private readonly HashSet<string>? _sets;
    private readonly List<string> _files = [];

    private QueryOptions(
        string? opcodeSpec,
        string? mnemonicSpec,
        string? modeSpec,
        string? classSpec,
        string? setSpec,
        HashSet<byte>? opcodes,
        HashSet<string>? mnemonics,
        HashSet<string>? modes,
        List<ClassNameFilter>? classes,
        HashSet<string>? sets)
    {
        OpcodeSpec = opcodeSpec;
        MnemonicSpec = mnemonicSpec;
        ModeSpec = modeSpec;
        ClassSpec = classSpec;
        SetSpec = setSpec;
        _opcodes = opcodes;
        _mnemonics = mnemonics;
        _modes = modes;
        _classes = classes;
        _sets = sets;
    }

    public bool ShowHelp { get; private init; }

    public bool Pretty { get; private init; }

    /// <summary>Tryb <c>--verify-code</c>: porównanie adnotacji w kodzie z CSV zamiast listy opkodów.</summary>
    public bool VerifyCode { get; private init; }

    /// <summary><c>--include-missing</c>: pełna lista opkodów z CSV nieobecnych w kodzie.</summary>
    public bool IncludeMissing { get; private init; }

    public string? OpcodeSpec { get; }

    public string? MnemonicSpec { get; }

    public string? ModeSpec { get; }

    public string? ClassSpec { get; }

    public string? SetSpec { get; }

    public string? CsvDirectory { get; private init; }

    public static bool TryParse(string[] args, out QueryOptions options, out string? error)
    {
        options = new QueryOptions(null, null, null, null, null, null, null, null, null, null);
        error = null;

        bool showHelp = false;
        bool pretty = false;
        bool verifyCode = false;
        bool includeMissing = false;
        string? opcodeSpec = null;
        string? mnemonicSpec = null;
        string? modeSpec = null;
        string? classSpec = null;
        string? setSpec = null;
        string? csvDirectory = null;
        List<string> files = [];
        List<string> errors = [];

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            string? inlineValue = null;

            int equals = argument.IndexOf('=', StringComparison.Ordinal);
            if (argument.StartsWith("--", StringComparison.Ordinal) && equals > 2)
            {
                inlineValue = argument[(equals + 1)..];
                argument = argument[..equals];
            }

            switch (argument)
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--pretty":
                    pretty = true;
                    break;
                case "--verify-code":
                    verifyCode = true;
                    break;
                case "--include-missing":
                    includeMissing = true;
                    break;
                case "--opcode":
                case "--mnemonic":
                case "--mode":
                case "--class":
                case "--set":
                case "--file":
                case "--csv-dir":
                    if (!TryTakeValue(argument, inlineValue, args, ref i, out string? value, out string? valueError))
                    {
                        errors.Add(valueError ?? $"brak wartości dla {argument}");
                        break;
                    }

                    switch (argument)
                    {
                        case "--opcode":
                            opcodeSpec = value;
                            break;
                        case "--mnemonic":
                            mnemonicSpec = value;
                            break;
                        case "--mode":
                            modeSpec = value;
                            break;
                        case "--class":
                            classSpec = value;
                            break;
                        case "--set":
                            setSpec = value;
                            break;
                        case "--file":
                            files.Add(value!);
                            break;
                        default:
                            csvDirectory = value;
                            break;
                    }

                    break;
                default:
                    errors.Add($"nieznany argument: {argument}");
                    break;
            }
        }

        if (showHelp)
        {
            options = new QueryOptions(null, null, null, null, null, null, null, null, null, null)
            {
                ShowHelp = true,
            };
            return true;
        }

        if (errors.Count > 0)
        {
            error = string.Join(Environment.NewLine, errors);
            return false;
        }

        HashSet<byte>? opcodes = null;
        if (opcodeSpec is not null && !TryParseOpcodes(opcodeSpec, out opcodes, out error))
        {
            return false;
        }

        HashSet<string>? mnemonics = null;
        if (mnemonicSpec is not null)
        {
            mnemonics = SplitList(mnemonicSpec, StringComparer.OrdinalIgnoreCase);
            if (mnemonics.Count == 0)
            {
                error = "brak wartości w --mnemonic";
                return false;
            }
        }

        HashSet<string>? modes = null;
        if (modeSpec is not null && !TryParseModes(modeSpec, out modes, out error))
        {
            return false;
        }

        List<ClassNameFilter>? classes = null;
        if (classSpec is not null)
        {
            classes = [];
            foreach (string item in SplitList(classSpec, StringComparer.OrdinalIgnoreCase))
            {
                classes.Add(new ClassNameFilter(item));
            }

            if (classes.Count == 0)
            {
                error = "brak wartości w --class";
                return false;
            }
        }

        HashSet<string>? sets = null;
        if (setSpec is not null)
        {
            sets = SplitList(setSpec, StringComparer.OrdinalIgnoreCase);
            if (sets.Count == 0)
            {
                error = "brak wartości w --set";
                return false;
            }
        }

        options = new QueryOptions(opcodeSpec, mnemonicSpec, modeSpec, classSpec, setSpec, opcodes, mnemonics, modes, classes, sets)
        {
            Pretty = pretty,
            VerifyCode = verifyCode,
            IncludeMissing = includeMissing,
            CsvDirectory = csvDirectory,
        };

        foreach (string file in files)
        {
            options._files.Add(file);
        }

        return true;
    }

    /// <summary>Czy wiersz przechodzi wszystkie podane filtry.</summary>
    public bool Matches(OpcodeRow row)
    {
        if (_opcodes is not null && !_opcodes.Contains(row.Opcode))
        {
            return false;
        }

        if (_mnemonics is not null && !_mnemonics.Contains(row.Mnemonic) && !HasAlias(row))
        {
            return false;
        }

        if (_modes is not null && !_modes.Contains(row.Mode))
        {
            return false;
        }

        if (_classes is not null && !MatchesClass(row.Class))
        {
            return false;
        }

        if (_sets is not null && !_sets.Contains(row.Set))
        {
            return false;
        }

        return true;
    }

    /// <summary>Lista plików do wczytania: jawne <c>--file</c> albo wszystkie <c>*.csv</c> z katalogu danych.</summary>
    public IReadOnlyList<string> ResolveFiles(out string? error)
    {
        error = null;

        if (_files.Count > 0)
        {
            return _files;
        }

        string directory = CsvDirectory ?? DefaultCsvDirectory();
        if (!Directory.Exists(directory))
        {
            error = $"katalog CSV nie istnieje: {directory} (użyj --file lub --csv-dir)";
            return [];
        }

        string[] found = Directory.GetFiles(directory, "*.csv");
        Array.Sort(found, StringComparer.Ordinal);

        if (found.Length == 0)
        {
            error = $"brak plików CSV w katalogu: {directory}";
            return [];
        }

        return found;
    }

    private static string DefaultCsvDirectory()
    {
        string? root = Paths.FindRepositoryRoot();
        return root is null
            ? Path.Combine(Environment.CurrentDirectory, "docs", "cpu", "opcodes")
            : Path.Combine(root, "docs", "cpu", "opcodes");
    }

    private bool HasAlias(OpcodeRow row)
    {
        foreach (string alias in row.Aliases)
        {
            if (_mnemonics!.Contains(alias))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesClass(string value)
    {
        foreach (ClassNameFilter filter in _classes!)
        {
            if (filter.Matches(value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryTakeValue(
        string name,
        string? inlineValue,
        string[] args,
        ref int index,
        out string? value,
        out string? error)
    {
        error = null;

        if (inlineValue is not null)
        {
            value = inlineValue;
            return true;
        }

        if (index + 1 >= args.Length)
        {
            value = null;
            error = $"brak wartości dla {name}";
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static HashSet<string> SplitList(string text, StringComparer comparer)
    {
        HashSet<string> values = new(comparer);
        foreach (string item in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            values.Add(item);
        }

        return values;
    }

    private static bool TryParseModes(string text, out HashSet<string>? modes, out string? error)
    {
        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        error = null;

        foreach (string item in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string mode = item.ToLowerInvariant();
            switch (mode)
            {
                case "impl":
                case "imp":
                case "acc":
                case "none":
                    values.Add(string.Empty);
                    break;
                default:
                    values.Add(mode);
                    break;
            }
        }

        if (values.Count == 0)
        {
            modes = null;
            error = "brak wartości w --mode";
            return false;
        }

        modes = values;
        return true;
    }

    /// <summary>
    /// Spec opkodów: pojedyncze bajty hex, zakresy (<c>00-0F</c>) i listy po przecinku.
    /// Hex akceptujemy w obu rejestrach (<c>a9</c> = <c>A9</c>).
    /// </summary>
    private static bool TryParseOpcodes(string text, out HashSet<byte>? opcodes, out string? error)
    {
        HashSet<byte> values = [];
        error = null;

        foreach (string item in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string spec = item.Trim();
            int dash = spec.IndexOf('-', StringComparison.Ordinal);

            if (dash < 0)
            {
                if (!TryParseByte(spec, out byte single))
                {
                    error = $"niepoprawny opkod: \"{item}\" (oczekiwano hex, np. A9 lub 00-0F)";
                    opcodes = null;
                    return false;
                }

                values.Add(single);
                continue;
            }

            string from = spec[..dash];
            string to = spec[(dash + 1)..];

            if (!TryParseByte(from, out byte start) || !TryParseByte(to, out byte end) || start > end)
            {
                error = $"niepoprawny zakres opkodów: \"{item}\" (oczekiwano np. 00-0F)";
                opcodes = null;
                return false;
            }

            for (int value = start; value <= end; value++)
            {
                values.Add((byte)value);
            }
        }

        if (values.Count == 0)
        {
            opcodes = null;
            error = "brak wartości w --opcode";
            return false;
        }

        opcodes = values;
        return true;
    }

    private static bool TryParseByte(string text, out byte value)
    {
        string trimmed = text.Trim();
        value = 0;

        return trimmed.Length is > 0 and <= 2
            && byte.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Klasa docelowa; <c>official*</c> dopasowuje prefiks, wartość bez gwiazdki wymaga równości.</summary>
    private sealed class ClassNameFilter
    {
        private readonly string _value;
        private readonly bool _prefix;

        public ClassNameFilter(string value)
        {
            _prefix = value.EndsWith('*');
            _value = _prefix ? value[..^1] : value;
        }

        public bool Matches(string candidate)
        {
            return _prefix
                ? candidate.StartsWith(_value, StringComparison.OrdinalIgnoreCase)
                : string.Equals(candidate, _value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
