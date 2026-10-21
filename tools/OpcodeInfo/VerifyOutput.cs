using System.Text.Json;

namespace OpcodeInfo;

/// <summary>Serializacja raportu <c>--verify-code</c>.</summary>
internal static class VerifyOutput
{
    public static void Write(Stream output, VerificationReport report, IReadOnlyList<string> sources, QueryOptions options)
    {
        using (Utf8JsonWriter writer = new(output, new JsonWriterOptions { Indented = options.Pretty }))
        {
            writer.WriteStartObject();
            writer.WriteString("mode", "verify-code");

            writer.WriteStartArray("sources");
            foreach (string source in sources)
            {
                writer.WriteStringValue(source);
            }

            writer.WriteEndArray();

            writer.WriteBoolean("consistent", report.IsConsistent);

            writer.WriteStartObject("counts");
            writer.WriteNumber("attributes", report.AttributeCount);
            writer.WriteNumber("csvRows", report.CsvRowCount);
            writer.WriteNumber("matchedCsvRows", report.MatchedCsvRows);
            writer.WriteNumber("cyclesChecked", report.CyclesChecked);
            writer.WriteNumber("mismatched", report.Mismatches.Count);
            writer.WriteNumber("missingInCsv", report.MissingInCsv.Count);
            writer.WriteNumber("missingInCode", report.MissingInCode.Count);
            writer.WriteEndObject();

            writer.WriteStartObject("missingInCodeByClass");
            foreach (KeyValuePair<string, int> pair in report.MissingInCodeByClass.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteNumber(pair.Key, pair.Value);
            }

            writer.WriteEndObject();

            writer.WriteStartArray("mismatches");
            foreach (CodeMismatch mismatch in report.Mismatches)
            {
                writer.WriteStartObject();
                writer.WriteString("set", mismatch.Set);
                writer.WriteString("opcode", mismatch.Opcode);
                writer.WriteString("member", mismatch.Member);
                writer.WriteString("codeMnemonic", mismatch.CodeMnemonic);
                writer.WriteString("csvMnemonic", mismatch.CsvMnemonic);
                writer.WriteString("codeMode", mismatch.CodeMode);
                writer.WriteString("csvMode", mismatch.CsvMode);
                writer.WriteNumber("codeBytes", mismatch.CodeBytes);
                writer.WriteNumber("csvBytes", mismatch.CsvBytes);
                writer.WriteNumber("codeBaseCycles", mismatch.CodeBaseCycles);

                if (mismatch.CsvBaseCycles is int csvBaseCycles)
                {
                    writer.WriteNumber("csvBaseCycles", csvBaseCycles);
                }
                else
                {
                    writer.WriteNull("csvBaseCycles");
                }

                writer.WriteString("csvClass", mismatch.CsvClass);

                writer.WriteStartArray("differences");
                foreach (string difference in mismatch.Differences)
                {
                    writer.WriteStringValue(difference);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("missingInCsv");
            foreach (MissingInCsv missing in report.MissingInCsv)
            {
                writer.WriteStartObject();
                writer.WriteString("set", missing.Set);
                writer.WriteString("opcode", missing.Opcode);
                writer.WriteString("mnemonic", missing.Mnemonic);
                writer.WriteString("mode", missing.Mode);
                writer.WriteString("member", missing.Member);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            // Pełna lista brakujących w kodzie jest długa (setki wierszy) — tylko na żądanie.
            if (options.IncludeMissing)
            {
                writer.WriteStartArray("missingInCode");
                foreach (OpcodeRow row in report.MissingInCode)
                {
                    writer.WriteStartObject();
                    writer.WriteString("set", row.Set);
                    writer.WriteString("opcode", row.Opcode.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                    writer.WriteString("class", row.Class);
                    writer.WriteString("mnemonic", row.Mnemonic);
                    writer.WriteString("mode", row.Mode);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        output.WriteByte((byte)'\n');
        output.Flush();
    }
}
