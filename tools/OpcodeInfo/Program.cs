using OpcodeInfo;

// Narzędzie diagnostyczne: wybiera opkody z tabel CSV (docs/cpu/opcodes) i wypisuje je jako JSON.
// Tryb --verify-code porównuje tabele z adnotacjami [Opcode] w kodzie (refleksja).
// Użycie: dotnet run -c Release --project tools/OpcodeInfo -- --help
// Kody wyjścia: 0 = OK (także brak trafień), 1 = niezgodności kodu z CSV, 2 = błąd użycia, danych lub I/O.

const int ExitMismatch = 1;
const int ExitUsage = 2;

if (!QueryOptions.TryParse(args, out QueryOptions options, out string? error))
{
    Console.Error.WriteLine($"error: {error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(QueryOptions.Usage);
    return ExitUsage;
}

if (options.ShowHelp)
{
    Console.WriteLine(QueryOptions.Usage);
    return 0;
}

IReadOnlyList<string> files = options.ResolveFiles(out error);
if (error is not null)
{
    Console.Error.WriteLine($"error: {error}");
    return ExitUsage;
}

List<OpcodeTable> tables = [];
HashSet<string> loaded = new(StringComparer.Ordinal);

try
{
    foreach (string file in files)
    {
        // Ten sam plik podany dwa razy (np. przez --file i domyślny katalog) nie dubluje wierszy.
        string fullPath = Path.GetFullPath(file);
        if (loaded.Add(fullPath))
        {
            tables.Add(OpcodeTable.Load(fullPath));
        }
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return ExitUsage;
}

List<OpcodeRow> rows = [];
foreach (OpcodeTable table in tables)
{
    foreach (OpcodeRow row in table.Rows)
    {
        if (options.Matches(row))
        {
            rows.Add(row);
        }
    }
}

List<string> sources = [];
foreach (OpcodeTable table in tables)
{
    sources.Add(table.Source);
}

Stream output = Console.OpenStandardOutput();

// Tryb weryfikacji: adnotacje [Opcode] w kodzie vs pełne tabele CSV (filtry wyboru nie działają).
if (options.VerifyCode)
{
    List<OpcodeRow> allRows = [];
    foreach (OpcodeTable table in tables)
    {
        allRows.AddRange(table.Rows);
    }

    VerificationReport report = CodeVerification.Compare(allRows, CodeAnnotations.Extract());
    VerifyOutput.Write(output, report, sources, options);

    return report.IsConsistent ? 0 : ExitMismatch;
}

// Kolejność deterministyczna: zestaw (nmos, c02, potem inne), następnie numer opkodu.
rows.Sort(static (left, right) =>
{
    int bySet = SetRank(left.Set).CompareTo(SetRank(right.Set));
    if (bySet != 0)
    {
        return bySet;
    }

    int byName = string.CompareOrdinal(left.Set, right.Set);
    return byName != 0 ? byName : left.Opcode.CompareTo(right.Opcode);
});

JsonOutput.Write(output, rows, sources, options);
return 0;

static int SetRank(string set) => set switch
{
    "nmos" => 0,
    "c02" => 1,
    _ => 2,
};
