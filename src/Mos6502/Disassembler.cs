using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Dekoder opkodów rdzenia do tekstu — ścieżka diagnostyczna (tracer, debugger, MCP).
/// Tekst powstaje z tablicy <see cref="StateMachineCpuCore.Describe"/>, nie z osobnej listy opkodów,
/// więc nowy opkod dodany do tablicy rdzenia trafia tu automatycznie.
/// </summary>
/// <remarks>
/// Nie jest na ścieżce <c>Tick</c> — alokuje tekst i może wywołać odczyty przez <c>peek</c>.
/// Format: <c>LDA #$7F</c>, <c>LDA zp $10</c>, <c>LDA zp,X $10</c>, <c>LDX zp,Y $10</c>, <c>LDA abs $1234</c>,
/// <c>LDA abs,X $1234</c>, <c>LDA abs,Y $1234</c>, <c>LDA (zp,X) $10</c>, <c>LDA (zp),Y $10</c>,
/// <c>JMP $0200</c>, <c>INX</c>, <c>CLC</c>. Nieznany opkod: <c>???</c>, długość 1.
/// </remarks>
public static class Disassembler
{
    private const string Unknown = "???";

    /// <summary>
    /// Dekoduje instrukcję spod <paramref name="pc"/>, czytając bajty przez <paramref name="peek"/>.
    /// </summary>
    /// <param name="peek">Odczyt bez skutków ubocznych (np. <see cref="IPeekableBus.Peek"/>).</param>
    /// <param name="pc">Adres pierwszego bajtu instrukcji.</param>
    /// <param name="length">Długość instrukcji w bajtach (1–3).</param>
    public static string Decode(Func<ushort, byte> peek, ushort pc, out int length) =>
        Decode(CpuVariants.Nmos6502, peek, pc, out length);

    /// <summary>
    /// Dekoduje instrukcję z magistrali. Używa <see cref="IPeekableBus.Peek"/>, więc widzi to, co widzi
    /// procesor w bieżącym stanie bankowania, bez zmiany stanu magistrali.
    /// </summary>
    public static string Decode(IPeekableBus bus, ushort pc, out int length)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return Decode(bus.Peek, pc, out length);
    }

    /// <summary>Dekoduje instrukcję według tablicy wskazanego wariantu (np. <see cref="CpuVariants.Cmos65C02"/>).</summary>
    public static string Decode(CpuVariant variant, Func<ushort, byte> peek, ushort pc, out int length)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(peek);

        CpuOpcode? entry = variant.Describe(peek(pc));

        if (entry is not { } opcode)
        {
            length = 1;
            return Unknown;
        }

        length = LengthOf(opcode.Mode);

        return Format(opcode, peek, pc);
    }

    /// <summary>Wariant z busem peekowalnym; patrz <see cref="Decode(CpuVariant, Func{ushort, byte}, ushort, out int)"/>.</summary>
    public static string Decode(CpuVariant variant, IPeekableBus bus, ushort pc, out int length)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return Decode(variant, bus.Peek, pc, out length);
    }

    private static int LengthOf(AddressingMode mode) => mode switch
    {
        AddressingMode.Immediate
            or AddressingMode.ZeroPage
            or AddressingMode.ZeroPageX
            or AddressingMode.ZeroPageY
            or AddressingMode.IndexedIndirect
            or AddressingMode.IndirectIndexed
            or AddressingMode.Relative => 2,
        AddressingMode.Absolute
            or AddressingMode.AbsoluteX
            or AddressingMode.AbsoluteY
            or AddressingMode.Indirect => 3,
        _ => 1,
    };

    private static string Format(CpuOpcode opcode, Func<ushort, byte> peek, ushort pc)
    {
        string mnemonic = opcode.Mnemonic;
        byte low;
        byte high;

        switch (opcode.Mode)
        {
            case AddressingMode.Immediate:
                return $"{mnemonic} #${peek(Next(pc, 1)):X2}";

            case AddressingMode.ZeroPage:
                return $"{mnemonic} zp ${peek(Next(pc, 1)):X2}";

            case AddressingMode.ZeroPageX:
                return $"{mnemonic} zp,X ${peek(Next(pc, 1)):X2}";

            case AddressingMode.ZeroPageY:
                return $"{mnemonic} zp,Y ${peek(Next(pc, 1)):X2}";

            case AddressingMode.IndexedIndirect:
                return $"{mnemonic} (zp,X) ${peek(Next(pc, 1)):X2}";

            case AddressingMode.IndirectIndexed:
                return $"{mnemonic} (zp),Y ${peek(Next(pc, 1)):X2}";

            case AddressingMode.Absolute:
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));

                // JMP nie ma w tekście nazwy trybu — w przeciwieństwie do pozostałych instrukcji pamięciowych.
                // Dotyczy też JSR (abs) i JMP (ind.): sam adres, bez `abs` ani `ind`.
                return opcode.Kind is OpKind.Jump or OpKind.Subroutine
                    ? $"{mnemonic} ${Word(low, high):X4}"
                    : $"{mnemonic} abs ${Word(low, high):X4}";

            case AddressingMode.AbsoluteX:
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));
                return $"{mnemonic} abs,X ${Word(low, high):X4}";

            case AddressingMode.AbsoluteY:
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));
                return $"{mnemonic} abs,Y ${Word(low, high):X4}";

            case AddressingMode.Relative:
                // Etap C: rozgałęzienia pokazują offset ze znakiem, np. `BNE rel $FB`.
                return $"{mnemonic} rel ${peek(Next(pc, 1)):X2}";

            case AddressingMode.Indirect:
                // Etap E: JMP (ind.) pokazuje wskaźnik, np. `JMP ($10FF)`.
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));
                return $"{mnemonic} (${Word(low, high):X4})";

            default:
                // Akumulator (INC A / DEC A 65C02) ma w tekście jawny operand A.
                return opcode.Kind == OpKind.Accumulator ? $"{mnemonic} A" : mnemonic;
        }
    }

    private static ushort Next(ushort pc, int offset) => unchecked((ushort)(pc + offset));

    private static ushort Word(byte low, byte high) => (ushort)(low | (high << 8));
}
