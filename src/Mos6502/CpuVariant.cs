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
    public CpuVariant(string name, IReadOnlyList<CpuOpcode> opcodes, RmwStyle rmw, IndexedDummyRead indexedDummy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(opcodes);

        Name = name;
        Rmw = rmw;
        IndexedDummy = indexedDummy;
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

    /// <summary>
    /// Wpis wariantu dla bajtu opkodu; <c>null</c>, gdy wariant go nie obsługuje.
    /// Ścieżka diagnostyczna — nie używaj na gorącej ścieżce <c>Tick</c>.
    /// </summary>
    public CpuOpcode? Describe(byte opcode) => _byOpcode[opcode];
}

/// <summary>Gotowe warianty rodziny 6502.</summary>
public static class CpuVariants
{
    // Tabela NMOS: 31 wpisów z BRK (00) i 26 wpisów etapu 2a. Każdy wpis zweryfikowany wektorami SingleStepTests.
    private static readonly CpuOpcode[] Nmos6502Opcodes =
    [
        new(0xA9, "LDA", AddressingMode.Immediate, OpKind.Read, CpuAction.Lda),
        new(0xA5, "LDA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lda),
        new(0xB5, "LDA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Lda),
        new(0xAD, "LDA", AddressingMode.Absolute, OpKind.Read, CpuAction.Lda),
        new(0xBD, "LDA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Lda),
        new(0xB9, "LDA", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Lda),
        new(0xA1, "LDA", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Lda),
        new(0xB1, "LDA", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Lda),
        new(0xA2, "LDX", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldx),
        new(0xA6, "LDX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldx),
        new(0xB6, "LDX", AddressingMode.ZeroPageY, OpKind.Read, CpuAction.Ldx),
        new(0xAE, "LDX", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldx),
        new(0xBE, "LDX", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Ldx),
        new(0xA0, "LDY", AddressingMode.Immediate, OpKind.Read, CpuAction.Ldy),
        new(0xA4, "LDY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ldy),
        new(0xB4, "LDY", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Ldy),
        new(0xAC, "LDY", AddressingMode.Absolute, OpKind.Read, CpuAction.Ldy),
        new(0xBC, "LDY", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Ldy),
        new(0x85, "STA", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sta),
        new(0x95, "STA", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sta),
        new(0x8D, "STA", AddressingMode.Absolute, OpKind.Write, CpuAction.Sta),
        new(0x9D, "STA", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Sta),
        new(0x99, "STA", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Sta),
        new(0x81, "STA", AddressingMode.IndexedIndirect, OpKind.Write, CpuAction.Sta),
        new(0x91, "STA", AddressingMode.IndirectIndexed, OpKind.Write, CpuAction.Sta),
        new(0x86, "STX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stx),
        new(0x96, "STX", AddressingMode.ZeroPageY, OpKind.Write, CpuAction.Stx),
        new(0x8E, "STX", AddressingMode.Absolute, OpKind.Write, CpuAction.Stx),
        new(0x84, "STY", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sty),
        new(0x94, "STY", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Sty),
        new(0x8C, "STY", AddressingMode.Absolute, OpKind.Write, CpuAction.Sty),
        new(0x69, "ADC", AddressingMode.Immediate, OpKind.Read, CpuAction.Adc),
        new(0xE9, "SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc),
        new(0xE6, "INC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Inc),
        new(0xEE, "INC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Inc),
        new(0xC6, "DEC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dec),
        new(0xCE, "DEC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dec),
        new(0xE8, "INX", AddressingMode.Implied, OpKind.Implied, CpuAction.Inx, 1),
        new(0xCA, "DEX", AddressingMode.Implied, OpKind.Implied, CpuAction.Dex, 1),
        new(0xC8, "INY", AddressingMode.Implied, OpKind.Implied, CpuAction.Iny, 1),
        new(0x88, "DEY", AddressingMode.Implied, OpKind.Implied, CpuAction.Dey, 1),
        new(0xAA, "TAX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tax, 1),
        new(0xA8, "TAY", AddressingMode.Implied, OpKind.Implied, CpuAction.Tay, 1),
        new(0x8A, "TXA", AddressingMode.Implied, OpKind.Implied, CpuAction.Txa, 1),
        new(0x98, "TYA", AddressingMode.Implied, OpKind.Implied, CpuAction.Tya, 1),
        new(0xBA, "TSX", AddressingMode.Implied, OpKind.Implied, CpuAction.Tsx, 1),
        new(0x9A, "TXS", AddressingMode.Implied, OpKind.Implied, CpuAction.Txs, 1),
        new(0x18, "CLC", AddressingMode.Implied, OpKind.Implied, CpuAction.Clc, 1),
        new(0x38, "SEC", AddressingMode.Implied, OpKind.Implied, CpuAction.Sec, 1),
        new(0x58, "CLI", AddressingMode.Implied, OpKind.Implied, CpuAction.Cli, 1),
        new(0x78, "SEI", AddressingMode.Implied, OpKind.Implied, CpuAction.Sei, 1),
        new(0xB8, "CLV", AddressingMode.Implied, OpKind.Implied, CpuAction.Clv, 1),
        new(0xD8, "CLD", AddressingMode.Implied, OpKind.Implied, CpuAction.Cld, 1),
        new(0xF8, "SED", AddressingMode.Implied, OpKind.Implied, CpuAction.Sed, 1),
        new(0xEA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1),
        new(0x00, "BRK", AddressingMode.Implied, OpKind.Implied, CpuAction.Brk, 6),
        new(0x4C, "JMP", AddressingMode.Absolute, OpKind.Jump, CpuAction.Jmp),
    ];

    // Dodatki 65C02 względem NMOS (6 wpisów; tabela NMOS dziedziczona bez powielania).
    private static readonly CpuOpcode[] Cmos65C02Additions =
    [
        new(0x64, "STZ", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stz),
        new(0x74, "STZ", AddressingMode.ZeroPageX, OpKind.Write, CpuAction.Stz),
        new(0x9C, "STZ", AddressingMode.Absolute, OpKind.Write, CpuAction.Stz),
        new(0x9E, "STZ", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Stz),
        new(0x1A, "INC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.IncA),
        new(0x3A, "DEC", AddressingMode.Implied, OpKind.Accumulator, CpuAction.DecA),
    ];

    /// <summary>NMOS 6502: dzisiejsze zachowanie rdzenia, RMW z dwoma zapisami. Domyślny wariant.</summary>
    public static CpuVariant Nmos6502 { get; } =
        new("NMOS 6502", Nmos6502Opcodes, RmwStyle.DoubleWrite, IndexedDummyRead.GhostByte);

    /// <summary>CMOS 65C02: tabela NMOS + dodatki, RMW z dwoma odczytami, dummy read na bajcie operandu.</summary>
    public static CpuVariant Cmos65C02 { get; } =
        new("CMOS 65C02", [.. Nmos6502Opcodes, .. Cmos65C02Additions], RmwStyle.DoubleRead, IndexedDummyRead.LastOperandByte);
}
