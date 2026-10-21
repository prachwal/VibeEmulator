namespace OpcodeInfo;

/// <summary>Niezgodność opkodu obecnego i w adnotacji, i w CSV.</summary>
internal sealed record CodeMismatch(
    string Set,
    string Opcode,
    string Member,
    string CodeMnemonic,
    string CsvMnemonic,
    string CodeMode,
    string CsvMode,
    int CodeBytes,
    int CsvBytes,
    int CodeBaseCycles,
    int? CsvBaseCycles,
    string CsvClass,
    IReadOnlyList<string> Differences);

/// <summary>Opkod zarejestrowany w kodzie, którego nie ma w tabeli CSV.</summary>
internal sealed record MissingInCsv(string Set, string Opcode, string Mnemonic, string Mode, string Member);

/// <summary>Wynik porównania adnotacji w kodzie z tabelami CSV.</summary>
internal sealed record VerificationReport(
    int AttributeCount,
    int CsvRowCount,
    int MatchedCsvRows,
    int CyclesChecked,
    IReadOnlyList<CodeMismatch> Mismatches,
    IReadOnlyList<MissingInCsv> MissingInCsv,
    IReadOnlyList<OpcodeRow> MissingInCode,
    IReadOnlyDictionary<string, int> MissingInCodeByClass)
{
    /// <summary>Zgodność wymaga braku niezgodności i braku adnotacji spoza CSV.</summary>
    public bool IsConsistent => Mismatches.Count == 0 && MissingInCsv.Count == 0;
}

/// <summary>
/// Porównuje adnotacje <c>[Opcode]</c> z wierszami tabel CSV. Zestaw <c>c02</c> dziedziczy tabelę NMOS
/// (tak jak <c>CpuVariants.Cmos65C02</c> = NMOS + dodatki), więc adnotacja NMOS pokrywa też wiersz c02.
/// </summary>
internal static class CodeVerification
{
    public static VerificationReport Compare(IReadOnlyList<OpcodeRow> csvRows, IReadOnlyList<CodeOpcode> code)
    {
        Dictionary<string, CodeOpcode> byKey = new(StringComparer.Ordinal);
        foreach (CodeOpcode opcode in code)
        {
            byKey[opcode.Key] = opcode;
        }

        List<CodeMismatch> mismatches = [];
        List<OpcodeRow> missingInCode = [];

        // Adnotacja musi mieć wiersz we własnym zestawie — inaczej zniknięcie wiersza z CSV NMOS
        // zostałoby zamaskowane przez dziedziczenie z wiersza 65C02.
        HashSet<string> csvKeys = new(StringComparer.Ordinal);
        foreach (OpcodeRow row in csvRows)
        {
            csvKeys.Add($"{row.Set}:{row.Opcode:X2}");
        }

        int matchedRows = 0;
        int cyclesChecked = 0;

        foreach (OpcodeRow row in csvRows)
        {
            CodeOpcode? codeOpcode = Find(byKey, row);
            if (codeOpcode is null)
            {
                missingInCode.Add(row);
                continue;
            }

            matchedRows++;

            List<string> differences = [];
            string csvMode = NormalizeMode(row.Mode);

            if (!string.Equals(codeOpcode.Mnemonic, row.Mnemonic, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add("mnemonic");
            }

            if (!string.Equals(codeOpcode.Mode, csvMode, StringComparison.Ordinal))
            {
                differences.Add("mode");
            }

            if (codeOpcode.Bytes != row.Bytes)
            {
                differences.Add("bytes");
            }

            if (row.BaseCycles is int csvBaseCycles)
            {
                cyclesChecked++;

                // Wiersz c02 dziedziczony z NMOS: formuła liczy koszt dla zestawu wiersza, nie adnotacji.
                // (RMW w abs,X: NMOS płaci dummy read, 65C02 nie — wektory i kolumna `dodatkowe` w CSV.)
                // (JMP (ind.): NMOS 5, 65C02 6 z fixupem — wektory wdc65c02.)
                int expected = codeOpcode.ExpectedBaseCycles;
                if (row.Set != codeOpcode.Set
                    && ((codeOpcode.Mode is "abx" or "aby"
                        && codeOpcode.Mnemonic is "ASL" or "LSR" or "ROL" or "ROR")
                        || (codeOpcode.Mode == "ind" && codeOpcode.Mnemonic == "JMP")))
                {
                    expected = CodeAnnotations.ExpectedBaseCyclesForSet(codeOpcode, row.Set);
                }

                if (expected != csvBaseCycles)
                {
                    // Etap I: $5C to jedyna **udokumentowana** rozbieżność datasheet ↔ wektory
                    // (datasheet WDC/Oxyron 8 cykli, wektory 4; opis w kolumnie `uwagi` CSV
                    // i w docs/cpu/opcodes.md). Rdzeń liczy 4 zgodnie z wektorami, więc CSV
                    // (źródło dla tej bramki) różni się o 4 — wyjątek jest jawny, nie wyciszony.
                    bool documentedDifference = row.Set == "c02" && row.Opcode == 0x5C && expected == 4 && csvBaseCycles == 8;

                    if (!documentedDifference)
                    {
                        differences.Add("cycles");
                    }
                }
            }

            if (!codeOpcode.FieldMatchesAttribute)
            {
                differences.Add("fieldValue");
            }

            if (differences.Count > 0)
            {
                mismatches.Add(new CodeMismatch(
                    Set: row.Set,
                    Opcode: row.Opcode.ToString("X2", System.Globalization.CultureInfo.InvariantCulture),
                    Member: codeOpcode.Member,
                    CodeMnemonic: codeOpcode.Mnemonic,
                    CsvMnemonic: row.Mnemonic,
                    CodeMode: codeOpcode.Mode,
                    CsvMode: csvMode,
                    CodeBytes: codeOpcode.Bytes,
                    CsvBytes: row.Bytes,
                    CodeBaseCycles: codeOpcode.ExpectedBaseCycles,
                    CsvBaseCycles: row.BaseCycles,
                    CsvClass: row.Class,
                    Differences: differences));
            }
        }

        List<MissingInCsv> missingInCsv = [];
        foreach (CodeOpcode opcode in code)
        {
            if (!csvKeys.Contains(opcode.Key))
            {
                missingInCsv.Add(new MissingInCsv(
                    opcode.Set,
                    opcode.Opcode.ToString("X2", System.Globalization.CultureInfo.InvariantCulture),
                    opcode.Mnemonic,
                    opcode.Mode,
                    opcode.Member));
            }
        }

        Dictionary<string, int> byClass = new(StringComparer.Ordinal);
        foreach (OpcodeRow row in missingInCode)
        {
            byClass[row.Class] = byClass.GetValueOrDefault(row.Class) + 1;
        }

        return new VerificationReport(
            AttributeCount: code.Count,
            CsvRowCount: csvRows.Count,
            MatchedCsvRows: matchedRows,
            CyclesChecked: cyclesChecked,
            Mismatches: mismatches,
            MissingInCsv: missingInCsv,
            MissingInCode: missingInCode,
            MissingInCodeByClass: byClass);
    }

    /// <summary>CSV w trybie niejawnym używa pustego pola, a dla KIL bywa `—` — oba znaczą „brak operandu”.</summary>
    internal static string NormalizeMode(string mode) => mode is "—" ? string.Empty : mode;

    private static CodeOpcode? Find(Dictionary<string, CodeOpcode> byKey, OpcodeRow row)
    {
        if (byKey.TryGetValue($"{row.Set}:{row.Opcode:X2}", out CodeOpcode? direct))
        {
            return direct;
        }

        // Wariant 65C02 dziedziczy tabelę NMOS bez powielania wpisów.
        return row.Set == "c02" && byKey.TryGetValue($"nmos:{row.Opcode:X2}", out CodeOpcode? inherited)
            ? inherited
            : null;
    }
}
