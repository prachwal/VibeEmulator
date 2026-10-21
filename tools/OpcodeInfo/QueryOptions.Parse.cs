using System.Globalization;

namespace OpcodeInfo;

internal sealed partial class QueryOptions
{
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
}
