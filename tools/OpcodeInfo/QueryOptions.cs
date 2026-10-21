using System.Globalization;

namespace OpcodeInfo;

internal sealed partial class QueryOptions
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

}
