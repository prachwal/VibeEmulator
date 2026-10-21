using System.Globalization;

namespace OpcodeInfo;

/// <summary>Jeden wiersz tabeli opkodów (<c>docs/cpu/opcodes/*.csv</c>) w postaci typowanej.</summary>
internal sealed record OpcodeRow(
    byte Opcode,
    string Set,
    string Class,
    string Mnemonic,
    IReadOnlyList<string> Aliases,
    string Mode,
    int Bytes,
    int? BaseCycles,
    string? AdditionalCycles,
    string? VectorCycles,
    IReadOnlyList<int> VectorCycleValues,
    int? VectorSamples,
    string? Masswerk,
    string? WdcDatasheet,
    string? Notes,
    string Source)
{
    /// <summary>Buduje wiersz z pól CSV; nazwy kolumn pochodzą z nagłówka pliku.</summary>
    public static OpcodeRow Create(string[] fields, IReadOnlyDictionary<string, int> columns, string source, int row)
    {
        string Field(string name) => fields[columns[name]].Trim();
        string? Optional(string name) => NullIfEmpty(Field(name));

        return new OpcodeRow(
            Opcode: ParseOpcode(Field("opcode"), source, row),
            Set: Field("set"),
            Class: Field("klasa"),
            Mnemonic: Field("mnemonik"),
            Aliases: SplitAliases(Field("aliasy")),
            Mode: Field("tryb"),
            Bytes: ParseRequiredInt(Field("bajty"), "bajty", source, row),
            BaseCycles: ParseOptionalInt(Field("cykle_bazowe"), "cykle_bazowe", source, row),
            AdditionalCycles: Optional("dodatkowe"),
            VectorCycles: Optional("wektory_cykle"),
            VectorCycleValues: ParseCycleValues(Field("wektory_cykle")),
            VectorSamples: ParseOptionalInt(Field("wektory_n"), "wektory_n", source, row),
            Masswerk: Optional("masswerk"),
            WdcDatasheet: Optional("datasheet_wdc"),
            Notes: Optional("uwagi"),
            Source: source);
    }

    private static byte ParseOpcode(string text, string source, int row)
    {
        if (text.Length != 2 || !byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
        {
            throw new InvalidDataException($"{source}: wiersz {row}: niepoprawny opkod \"{text}\" (oczekiwano dwóch cyfr hex)");
        }

        return value;
    }

    private static int ParseRequiredInt(string text, string column, string source, int row)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidDataException($"{source}: wiersz {row}: kolumna \"{column}\" nie jest liczbą: \"{text}\"");
        }

        return value;
    }

    private static int? ParseOptionalInt(string text, string column, string source, int row)
        => text.Length == 0 ? null : ParseRequiredInt(text, column, source, row);

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    private static string[] SplitAliases(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        return text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>Cykle z wektorów bywają zakresem lub listą wariantów (<c>4/5/6</c>); surowa wartość zostaje w JSON.</summary>
    private static List<int> ParseCycleValues(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        List<int> values = [];
        foreach (string part in text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                // Wartość opisowa (np. „brak”) — zostaje tylko w polu tekstowym.
                return [];
            }

            values.Add(value);
        }

        return values;
    }
}
