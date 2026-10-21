using System.Text;

namespace OpcodeInfo;

/// <summary>
/// Minimalny czytnik CSV zgodny z RFC 4180: cudzysłowy wokół pól, <c>""</c> jako escaped cudzysłów,
/// przecinki i znaki nowej linii wewnątrz pól. Obsługuje CRLF i BOM.
/// </summary>
internal static class Csv
{
    public static List<string[]> Parse(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        List<string[]> rows = [];
        List<string> fields = [];
        StringBuilder field = new();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    // Początek pola cytowanego; ewentualne wcześniejsze znaki zostają (tolerancja wejścia).
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    // Koniec wiersza obsługuje '\n'; samotny '\r' pomijamy.
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    rows.Add([.. fields]);
                    fields.Clear();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add([.. fields]);
        }

        return rows;
    }
}
