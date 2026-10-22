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
            AddressingMode.Relative => "rel",
            AddressingMode.IndirectZeroPage => "izp",
            AddressingMode.ZeroPageRelative => "zpr",
            AddressingMode.IndirectAbsoluteX => "iax",
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

        // Etap E: JSR/RTS/RTI (Subroutine) nie mają własnej długości — wynika z trybu jak zwykle.
        if (attribute.Kind == OpKind.Subroutine)
        {
            return attribute.Mode == AddressingMode.Absolute ? 3 : 1;
        }

        return attribute.Mode switch
        {
            AddressingMode.Implied => 1,
            AddressingMode.Immediate
                or AddressingMode.ZeroPage
                or AddressingMode.ZeroPageX
                or AddressingMode.ZeroPageY
                or AddressingMode.IndexedIndirect
                or AddressingMode.IndirectIndexed
                or AddressingMode.Relative
                or AddressingMode.IndirectZeroPage => 2,
            AddressingMode.Absolute
                or AddressingMode.AbsoluteX
                or AddressingMode.AbsoluteY
                or AddressingMode.Indirect
                or AddressingMode.ZeroPageRelative
                or AddressingMode.IndirectAbsoluteX => 3,
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
    internal static int ExpectedBaseCycles(OpcodeAttribute attribute) =>
        ExpectedBaseCycles(attribute.Kind, attribute.Mode, attribute.InternalCycles, attribute.CsvSet, attribute.Action);

    /// <summary>
    /// Koszt dla zestawu wiersza CSV, gdy adnotacja pochodzi z innego zestawu (dziedziczenie NMOS → c02).
    /// Jedyna różnica zależna od zestawu: RMW w <c>abs,X</c>/<c>abs,Y</c> płaci dummy read w NMOS, nie w 65C02.
    /// </summary>
    /// <summary>
    /// Koszt dla zestawu wiersza CSV, gdy adnotacja pochodzi z innego zestawu (dziedziczenie NMOS → c02).
    /// Różnica zależna od zestawu: przesunięcia/rotacje (ASL/LSR/ROL/ROR) w <c>abs,X</c> płacą dummy read
    /// w NMOS (7 cykli), nie w 65C02 (6 cykli + opcjonalny +1 przy przekroczeniu strony).
    /// INC/DEC w <c>abs,X</c> mają 7 cykli w obu zestawach — korekta ich nie dotyczy, więc wywołujący
    /// ogranicza ją do mnemoników ASL/LSR/ROL/ROR.
    /// </summary>
    internal static int ExpectedBaseCyclesForSet(CodeOpcode opcode, string set)
    {
        // Rekord nie przechowuje rodzaju, więc różnicę liczymy po trybie i mnemoniku u wywołującego.
        if (opcode.Set == "nmos" && set == "c02" && opcode.Mode is "abx" or "aby")
        {
            return opcode.ExpectedBaseCycles - 1;
        }

        // Etap E: JMP (ind.) w 65C02 ma 6 cykli (fixup), w NMOS 5 — adnotacja NMOS liczy 5.
        if (opcode.Set == "nmos" && set == "c02" && opcode.Mode == "ind")
        {
            return opcode.ExpectedBaseCycles + 1;
        }

        return opcode.ExpectedBaseCycles;
    }

    private static int ExpectedBaseCycles(OpKind kind, AddressingMode mode, int internalCycles, string csvSet, CpuAction action)
    {
        const int fetch = 1;

        switch (kind)
        {
            case OpKind.Implied:
                // ImpliedRead (zawsze) + pozostałe cykle wewnętrzne; InternalCycles liczy całość.
                return fetch + Math.Max(1, internalCycles);

            case OpKind.Accumulator:
                return fetch + 1;

            case OpKind.Jump:
                // JMP abs: fetch + 2 bajty operandu. JMP (ind.): fetch + 2 bajty wskaźnika
                // + low + high (5 w NMOS); 65C02 dokłada fixup przy wrapie (6) — patrz korekta niżej.
                // JMP (abs,X): fetch + 2 bajty + dummy + low + high = 6 (wektory wdc65c02).
                return fetch + 2 + (mode == AddressingMode.Indirect ? 2 : mode == AddressingMode.IndirectAbsoluteX ? 3 : 0);

            case OpKind.Subroutine:
                // Etap E: JSR/RTS/RTI mają po 6 cykli (fetch + 5 faz). RTS/RTI są Implied (1 bajt),
                // JSR jest Absolute (3 bajty), ale liczba faz jest ta sama — formuła nie zależy od trybu.
                return fetch + 5;

            case OpKind.Interrupt:
                // Etap F: BRK = fetch + sygnatura + 3 pushy + 2 odczyty wektora = 7 (wektory 00.json).
                return fetch + 6;

            case OpKind.Branch:
                // Etap C: minimum to fetch + operand (warunek fałszywy). Skok w tej samej stronie
                // dokłada BranchTaken (dummy read nie występuje), a przejście strony — BranchCrossed.
                // CSV podaje cykle_bazowe: 2, więc formuła liczy minimum; +1/+2 są w kolumnie `dodatkowe`.
                // Etap H: BRA skacze zawsze — minimum to fetch + operand + dummy PC = 3 (CSV: 3).
                return action == CpuAction.Bra ? fetch + 2 : fetch + 1;

            case OpKind.Push:
                // Etap D: fetch + StackDummy (dummy read spod PC) + Push (zapis na stos).
                return fetch + 2;

            case OpKind.Pull:
                // Etap D: fetch + StackDummy + PullDummy (dummy read stosu) + Pull (odczyt stosu).
                return fetch + 3;

            case OpKind.BitBranch:
                // Etap H: BBR/BBS = fetch + zp + dummy zp + read zp + rel = 5; +1/+2 przy skoku
                // (kolumna `dodatkowe` w CSV). Brak skoku = 5, skok = 6, skok z przekroczeniem = 7.
                return fetch + 4;

            case OpKind.NopByte:
                // Etap I: jednobajtowy NOP 65C02 — tylko fetch (wektory wdc65c02 pokazują 1 cykl).
                return fetch;

            case OpKind.Read:
            case OpKind.Write:
            case OpKind.ReadModifyWrite:
                int addressing = mode switch
                {
                    AddressingMode.Immediate => 1,
                    AddressingMode.ZeroPage => 2,
                    AddressingMode.ZeroPageX or AddressingMode.ZeroPageY => 3,
                    AddressingMode.Absolute => 3,
                    AddressingMode.AbsoluteX or AddressingMode.AbsoluteY => 3,
                    AddressingMode.IndexedIndirect => 5,
                    AddressingMode.IndirectIndexed => 4,
                    AddressingMode.IndirectZeroPage => 4,
                    _ => 0,
                };

                // RMW dokłada dwie fazy po odczycie: RmwOld (zapis starej wartości albo ponowny odczyt)
                // i RmwNew (zapis nowej). Zapis zwykły nie ma dodatkowych faz poza dummy read przy indeksowaniu.
                int modify = kind == OpKind.ReadModifyWrite ? 2 : 0;
                int writeDummy = kind == OpKind.Write
                    && mode is AddressingMode.AbsoluteX or AddressingMode.AbsoluteY or AddressingMode.IndirectIndexed
                    ? 1
                    : 0;

                // RMW w abs,X zawsze płaci dummy read (jak zapis): odczyt nie ma warunkowego +1,
                // więc bazowy koszt zawiera stały cykl. Dla zp,X dummy read jest już w fazie IndexInternal.
                // Wyjątek: 65C02 (zestaw c02) nie płaci dummy read dla RMW w abs,X — wektory pokazują 6/7
                // zamiast 7, a kolumna `dodatkowe` w CSV mówi o +1 przy przekroczeniu strony.
                int rmwIndexedDummy = kind == OpKind.ReadModifyWrite
                    && mode is AddressingMode.AbsoluteX or AddressingMode.AbsoluteY
                    && csvSet != "c02"
                    ? 1
                    : 0;

                return fetch + addressing + modify + writeDummy + rmwIndexedDummy;

            default:
                return 0;
        }
    }
}
