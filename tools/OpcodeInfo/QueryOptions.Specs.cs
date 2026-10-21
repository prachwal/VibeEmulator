using System.Globalization;

namespace OpcodeInfo;

internal sealed partial class QueryOptions
{
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
