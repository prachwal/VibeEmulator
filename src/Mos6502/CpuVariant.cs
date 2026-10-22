namespace VibeEmulator.Mos6502;

/// <summary>
/// Styl sekwencji dostępów read-modify-write (INC/DEC w trybie zero page i absolute).
/// </summary>
public enum RmwStyle
{
    /// <summary>NMOS: odczyt → zapis starej wartości → zapis nowej.</summary>
    DoubleWrite,

    /// <summary>CMOS 65C02: odczyt → ponowny odczyt tego samego adresu → zapis nowej.</summary>
    DoubleRead,
}

/// <summary>
/// Adres dummy read w cyklu indeksowania <c>abs,X</c>, <c>abs,Y</c> i <c>(zp),Y</c> — wariant NMOS i 65C02
/// różnią się tylko tym adresem (wektory SingleStepTests/65x02, cykl 4 lub 5 zależnie od przekroczenia strony).
/// Kiedy dummy read występuje, jest ten sam w obu wariantach: odczyt tylko przy przekroczeniu strony,
/// zapis zawsze.
/// </summary>
public enum IndexedDummyRead
{
    /// <summary>NMOS: bajt spod adresu bez korekty strony, <c>(base &amp; 0xFF00) | ((base + indeks) &amp; 0xFF)</c>.</summary>
    GhostByte,

    /// <summary>65C02: ostatni pobrany bajt operandu, czyli <c>PC - 1</c> w chwili dummy read.</summary>
    LastOperandByte,
}

/// <summary>
/// Zawijanie high-byte przy <c>JMP (ind.)</c>, gdy wskaźnik kończy stronę (<c>xxFF</c>).
/// Jedyna różnica wariantu, która zmienia zachowanie (nie tylko tabelę): NMOS czyta z błędem strony.
/// </summary>
public enum JmpIndirectWrap
{
    /// <summary>NMOS: high-byte spod <c>(ptr &amp; 0xFF00) | ((ptr + 1) &amp; 0xFF)</c> (błąd strony).</summary>
    PageWrap,

    /// <summary>65C02: high-byte spod <c>ptr + 1</c> (poprawnie), kosztem dodatkowego cyklu.</summary>
    NoWrap,
}

/// <summary>
/// Wariant rodziny 6502 (NMOS 6502, CMOS 65C02, a w przyszłości 6510): nazwa, <b>pełna</b> tabela
/// opkodów wariantu, styl RMW i adres dummy read przy indeksowaniu. Tabela wariantu jest źródłem prawdy dla rdzenia
/// cyklowego (<see cref="StateMachineCpuCore"/>) i dekodera (<see cref="Disassembler"/>) tego wariantu.
/// </summary>
public sealed class CpuVariant
{
    private readonly CpuOpcode?[] _byOpcode = new CpuOpcode?[256];

    /// <param name="name">Nazwa wariantu do diagnostyki, np. <c>NMOS 6502</c>.</param>
    /// <param name="opcodes">Pełna tabela wariantu; każdy bajt opkodu może wystąpić tylko raz.</param>
    /// <param name="rmw">Styl sekwencji RMW.</param>
    /// <param name="indexedDummy">Adres dummy read przy indeksowaniu <c>abs,X</c>/<c>abs,Y</c>/<c>(zp),Y</c>.</param>
    /// <param name="jmpWrap">Zawijanie high-byte przy <c>JMP (ind.)</c> (domyślnie błąd strony NMOS).</param>
    public CpuVariant(string name, IReadOnlyList<CpuOpcode> opcodes, RmwStyle rmw, IndexedDummyRead indexedDummy, JmpIndirectWrap jmpWrap = JmpIndirectWrap.PageWrap)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(opcodes);

        Name = name;
        Rmw = rmw;
        IndexedDummy = indexedDummy;
        JmpWrap = jmpWrap;
        Opcodes = [.. opcodes];

        foreach (CpuOpcode entry in Opcodes)
        {
            if (_byOpcode[entry.Opcode] is { } existing)
            {
                throw new ArgumentException(
                    $"Opcode 0x{entry.Opcode:X2} ({entry.Mnemonic}) jest już zdefiniowany jako {existing.Mnemonic} w wariancie {name}.",
                    nameof(opcodes));
            }

            _byOpcode[entry.Opcode] = entry;
        }
    }

    public string Name { get; }

    /// <summary>Pełna tabela wariantu (nie różnica względem innego wariantu).</summary>
    public IReadOnlyList<CpuOpcode> Opcodes { get; }

    public RmwStyle Rmw { get; }

    /// <summary>Adres dummy read przy indeksowaniu abs,X / abs,Y / (zp),Y.</summary>
    public IndexedDummyRead IndexedDummy { get; }

    /// <summary>Zawijanie high-byte przy <c>JMP (ind.)</c> (błąd strony tylko w NMOS).</summary>
    public JmpIndirectWrap JmpWrap { get; }

    /// <summary>
    /// Wpis wariantu dla bajtu opkodu; <c>null</c>, gdy wariant go nie obsługuje.
    /// Ścieżka diagnostyczna — nie używaj na gorącej ścieżce <c>Tick</c>.
    /// </summary>
    public CpuOpcode? Describe(byte opcode) => _byOpcode[opcode];
}

/// <summary>Gotowe warianty rodziny 6502.</summary>
public static class CpuVariants
{
    // Tabele pochodzą z adnotowanego katalogu (OpcodeCatalog). Adnotacja opisuje ten sam opkod
    // co wpis tabeli i służy weryfikacji z docs/cpu/opcodes/*.csv: tools/OpcodeInfo --verify-code.
    private static readonly CpuOpcode[] Nmos6502Opcodes = OpcodeCatalog.Nmos.Opcodes;
    private static readonly CpuOpcode[] Cmos65C02Additions = OpcodeCatalog.Cmos.Additions;

    /// <summary>NMOS 6502: dzisiejsze zachowanie rdzenia, RMW z dwoma zapisami. Domyślny wariant.</summary>
    public static CpuVariant Nmos6502 { get; } =
        new("NMOS 6502", Nmos6502Opcodes, RmwStyle.DoubleWrite, IndexedDummyRead.GhostByte);

    /// <summary>CMOS 65C02: tabela NMOS + dodatki, RMW z dwoma odczytami, dummy read na bajcie operandu.</summary>
    public static CpuVariant Cmos65C02 { get; } =
        new("CMOS 65C02", [.. Nmos6502Opcodes, .. Cmos65C02Additions], RmwStyle.DoubleRead, IndexedDummyRead.LastOperandByte, JmpIndirectWrap.NoWrap);
}
