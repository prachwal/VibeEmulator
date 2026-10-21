using System.Reflection;
using VibeEmulator.Mos6502;

namespace OpcodeInfo;

/// <summary>Opkod odczytany refleksją z adnotacji <see cref="OpcodeAttribute"/>.</summary>
internal sealed record CodeOpcode(
    byte Opcode,
    string Set,
    string Mnemonic,
    string Mode,
    int Bytes,
    int ExpectedBaseCycles,
    string Member,
    bool FieldMatchesAttribute)
{
    /// <summary>Klucz porównania z CSV: zestaw i bajt opkodu.</summary>
    public string Key => $"{Set}:{Opcode:X2}";
}

/// <summary>
/// Wydobywa adnotacje opkodów z zestawu <c>VibeEmulator.Mos6502</c>. Czytane są wyłącznie
/// publiczne pola statyczne z <see cref="OpcodeAttribute"/>; wartości pól są porównywane
/// z adnotacją, żeby wykryć rozjazd metadanych i tabeli.
/// </summary>
internal static class CodeAnnotations
{
    public static IReadOnlyList<CodeOpcode> Extract()
    {
        List<CodeOpcode> opcodes = [];

        foreach (Type type in typeof(OpcodeAttribute).Assembly.GetTypes())
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetCustomAttribute<OpcodeAttribute>() is not { } attribute)
                {
                    continue;
                }

                bool matches = field.GetValue(null) is CpuOpcode value && value == attribute.ToOpcode();

                opcodes.Add(new CodeOpcode(
                    Opcode: attribute.Opcode,
                    Set: attribute.CsvSet,
                    Mnemonic: attribute.Mnemonic,
                    Mode: CsvMode(attribute),
                    Bytes: ExpectedBytes(attribute),
                    ExpectedBaseCycles: ExpectedBaseCycles(attribute),
                    Member: $"{type.FullName}.{field.Name}",
                    FieldMatchesAttribute: matches));
            }
        }

        opcodes.Sort(static (left, right) =>
        {
            int bySet = string.CompareOrdinal(left.Set, right.Set);
            return bySet != 0 ? bySet : left.Opcode.CompareTo(right.Opcode);
        });

        return opcodes;
    }

    /// <summary>Tryb w konwencji kolumny <c>tryb</c> w CSV; puste pole dla trybu niejawnego i akumulatora.</summary>
    private static string CsvMode(OpcodeAttribute attribute)
    {
        if (attribute.Kind == OpKind.Accumulator)
        {
            return string.Empty;
        }

        return attribute.Mode switch
        {
            AddressingMode.Implied => string.Empty,
            AddressingMode.Immediate => "imm",
            AddressingMode.ZeroPage => "zp",
            AddressingMode.ZeroPageX => "zpx",
            AddressingMode.ZeroPageY => "zpy",
            AddressingMode.Absolute => "abs",
            AddressingMode.AbsoluteX => "abx",
            AddressingMode.AbsoluteY => "aby",
            AddressingMode.Indirect => "ind",
            AddressingMode.IndexedIndirect => "izx",
            AddressingMode.IndirectIndexed => "izy",
            _ => attribute.Mode.ToString().ToLowerInvariant(),
        };
    }

    /// <summary>Długość instrukcji wynikająca z trybu adresowania — porównywana z kolumną <c>bajty</c>.</summary>
    private static int ExpectedBytes(OpcodeAttribute attribute)
    {
        if (attribute.Kind == OpKind.Accumulator)
        {
            return 1;
        }

        return attribute.Mode switch
        {
            AddressingMode.Implied => 1,
            AddressingMode.Immediate
                or AddressingMode.ZeroPage
                or AddressingMode.ZeroPageX
                or AddressingMode.ZeroPageY
                or AddressingMode.IndexedIndirect
                or AddressingMode.IndirectIndexed => 2,
            AddressingMode.Absolute
                or AddressingMode.AbsoluteX
                or AddressingMode.AbsoluteY
                or AddressingMode.Indirect => 3,
            _ => 0,
        };
    }

    /// <summary>
    /// Cykle bazowe wynikające z faz rdzenia cyklowego (<c>StateMachineCpuCore</c>): każda faza to jeden cykl,
    /// a liczba faz zależy tylko od rodzaju instrukcji i trybu adresowania. To nie jest kopia kolumny
    /// <c>cykle_bazowe</c> z CSV — porównanie wykrywa wpis tabeli, którego tryb lub rodzaj daje inny koszt niż
    /// dokumentacja. Wartość bazowa nie zawiera cyklu dodatkowego przy przekroczeniu strony.
    /// </summary>
    /// <remarks>
    /// Fazowanie w rdzeniu: <c>Fetch</c> (1), potem dla odczytu/zapisu/RMW fazy operandu i dostępu,
    /// dla <c>Implied</c> jedno <c>ImpliedRead</c> i <c>InternalCycles − 1</c> faz <c>Internal</c>,
    /// dla <c>Jump</c> dwa bajty operandu bez dostępu do adresu. RMW dokłada <c>RmwOld</c> i <c>RmwNew</c>,
    /// a zapis w <c>abs,X</c>/<c>abs,Y</c>/<c>(zp),Y</c> — obowiązujący dummy read.
    /// </remarks>
    internal static int ExpectedBaseCycles(OpcodeAttribute attribute)
    {
        const int fetch = 1;

        switch (attribute.Kind)
        {
            case OpKind.Implied:
                // ImpliedRead (zawsze) + pozostałe cykle wewnętrzne; InternalCycles liczy całość.
                return fetch + Math.Max(1, attribute.InternalCycles);

            case OpKind.Accumulator:
                return fetch + 1;

            case OpKind.Jump:
                return fetch + 2;

            case OpKind.Read:
            case OpKind.Write:
            case OpKind.ReadModifyWrite:
                int addressing = attribute.Mode switch
                {
                    AddressingMode.Immediate => 1,
                    AddressingMode.ZeroPage => 2,
                    AddressingMode.ZeroPageX or AddressingMode.ZeroPageY => 3,
                    AddressingMode.Absolute => 3,
                    AddressingMode.AbsoluteX or AddressingMode.AbsoluteY => 3,
                    AddressingMode.IndexedIndirect => 5,
                    AddressingMode.IndirectIndexed => 4,
                    _ => 0,
                };

                // RMW dokłada dwie fazy po odczycie: RmwOld (zapis starej wartości albo ponowny odczyt)
                // i RmwNew (zapis nowej). Zapis zwykły nie ma dodatkowych faz poza dummy read przy indeksowaniu.
                int modify = attribute.Kind == OpKind.ReadModifyWrite ? 2 : 0;
                int writeDummy = attribute.Kind == OpKind.Write
                    && attribute.Mode is AddressingMode.AbsoluteX or AddressingMode.AbsoluteY or AddressingMode.IndirectIndexed
                    ? 1
                    : 0;

                return fetch + addressing + modify + writeDummy;

            default:
                return 0;
        }
    }
}
