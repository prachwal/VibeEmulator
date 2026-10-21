namespace OpcodeInfo;

/// <summary>Tabela opkodów wczytana z jednego pliku CSV.</summary>
internal sealed class OpcodeTable
{
    /// <summary>Kolumny wymagane przez narzędzie; nagłówek jest mapowany po nazwach, nie po pozycji.</summary>
    private static readonly string[] RequiredColumns =
    [
        "opcode", "set", "klasa", "mnemonik", "aliasy", "tryb", "bajty",
        "cykle_bazowe", "dodatkowe", "wektory_cykle", "wektory_n", "masswerk", "datasheet_wdc", "uwagi",
    ];

    private OpcodeTable(string source, IReadOnlyList<OpcodeRow> rows)
    {
        Source = source;
        Rows = rows;
    }

    /// <summary>Ścieżka pliku w formie pokazywanej w JSON (względna do katalogu bieżącego, gdy to możliwe).</summary>
    public string Source { get; }

    public IReadOnlyList<OpcodeRow> Rows { get; }

    public static OpcodeTable Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"plik CSV nie istnieje: {path}");
        }

        List<string[]> records = Csv.Parse(File.ReadAllText(path));
        if (records.Count == 0)
        {
            throw new InvalidDataException($"pusty plik CSV: {path}");
        }

        Dictionary<string, int> columns = new(StringComparer.OrdinalIgnoreCase);
        string[] header = records[0];
        for (int i = 0; i < header.Length; i++)
        {
            columns[header[i].Trim()] = i;
        }

        foreach (string column in RequiredColumns)
        {
            if (!columns.ContainsKey(column))
            {
                throw new InvalidDataException($"{path}: brak kolumny \"{column}\" w nagłówku");
            }
        }

        string source = Paths.Display(path);
        List<OpcodeRow> rows = new(records.Count - 1);

        for (int i = 1; i < records.Count; i++)
        {
            string[] fields = records[i];

            // Puste linie (np. końcowy newline) pomijamy.
            bool empty = true;
            foreach (string field in fields)
            {
                if (field.Trim().Length != 0)
                {
                    empty = false;
                    break;
                }
            }

            if (empty)
            {
                continue;
            }

            if (fields.Length != header.Length)
            {
                throw new InvalidDataException($"{source}: wiersz {i}: oczekiwano {header.Length} pól, jest {fields.Length}");
            }

            rows.Add(OpcodeRow.Create(fields, columns, source, i));
        }

        return new OpcodeTable(source, rows);
    }
}
