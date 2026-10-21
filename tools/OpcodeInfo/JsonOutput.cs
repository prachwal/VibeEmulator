using System.Globalization;
using System.Text.Json;

namespace OpcodeInfo;

/// <summary>Serializacja wyniku zapytania do JSON (jeden dokument, UTF-8, opcjonalne wcięcia).</summary>
internal static class JsonOutput
{
    public static void Write(
        Stream output,
        IReadOnlyList<OpcodeRow> rows,
        IReadOnlyList<string> sources,
        QueryOptions options)
    {
        using (Utf8JsonWriter writer = new(output, new JsonWriterOptions { Indented = options.Pretty }))
        {
            writer.WriteStartObject();

            writer.WriteStartObject("filter");
            writer.WriteString("opcode", options.OpcodeSpec);
            writer.WriteString("mnemonic", options.MnemonicSpec);
            writer.WriteString("mode", options.ModeSpec);
            writer.WriteString("class", options.ClassSpec);
            writer.WriteString("set", options.SetSpec);
            writer.WriteEndObject();

            writer.WriteStartArray("sources");
            foreach (string source in sources)
            {
                writer.WriteStringValue(source);
            }

            writer.WriteEndArray();

            writer.WriteNumber("count", rows.Count);

            writer.WriteStartArray("opcodes");
            foreach (OpcodeRow row in rows)
            {
                WriteRow(writer, row);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
            writer.Flush();
        }

        // Nowa linia po dokumencie: wygodne dla powłoki i narzędzi czytających stdout liniami.
        output.WriteByte((byte)'\n');
        output.Flush();
    }

    private static void WriteRow(Utf8JsonWriter writer, OpcodeRow row)
    {
        writer.WriteStartObject();

        writer.WriteString("opcode", row.Opcode.ToString("X2", CultureInfo.InvariantCulture));
        writer.WriteString("set", row.Set);
        writer.WriteString("class", row.Class);
        writer.WriteString("mnemonic", row.Mnemonic);

        writer.WriteStartArray("aliases");
        foreach (string alias in row.Aliases)
        {
            writer.WriteStringValue(alias);
        }

        writer.WriteEndArray();

        // Puste pole trybu w CSV = tryb niejawny lub akumulator (opis w docs/cpu/opcodes.md).
        writer.WriteString("mode", row.Mode);
        writer.WriteNumber("bytes", row.Bytes);

        if (row.BaseCycles is int baseCycles)
        {
            writer.WriteNumber("baseCycles", baseCycles);
        }
        else
        {
            writer.WriteNull("baseCycles");
        }

        writer.WriteString("additionalCycles", row.AdditionalCycles);
        writer.WriteString("vectorCycles", row.VectorCycles);

        writer.WriteStartArray("vectorCycleValues");
        foreach (int value in row.VectorCycleValues)
        {
            writer.WriteNumberValue(value);
        }

        writer.WriteEndArray();

        if (row.VectorSamples is int samples)
        {
            writer.WriteNumber("vectorSamples", samples);
        }
        else
        {
            writer.WriteNull("vectorSamples");
        }

        writer.WriteString("masswerk", row.Masswerk);
        writer.WriteString("wdcDatasheet", row.WdcDatasheet);
        writer.WriteString("notes", row.Notes);
        writer.WriteString("source", row.Source);

        writer.WriteEndObject();
    }
}
