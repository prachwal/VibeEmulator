using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.C64;

/// <summary>
/// Dekoder opkodów rdzenia do tekstu — ścieżka diagnostyczna (tracer, debugger, MCP).
/// Tekst powstaje z tablicy <see cref="StateMachineCpuCore.Describe"/>, nie z osobnej listy opkodów,
/// więc nowy opkod dodany do tablicy rdzenia trafia tu automatycznie.
/// </summary>
/// <remarks>
/// Nie jest na ścieżce <c>Tick</c> — alokuje tekst i może wywołać odczyty przez <c>peek</c>.
/// Format: <c>LDA #$7F</c>, <c>LDA zp $10</c>, <c>LDA zp,X $10</c>, <c>LDA abs $1234</c>,
/// <c>LDA abs,X $1234</c>, <c>JMP $0200</c>, <c>INX</c>. Nieznany opkod: <c>???</c>, długość 1.
/// </remarks>
public static class Disassembler
{
    private const string Unknown = "???";

    /// <summary>
    /// Dekoduje instrukcję spod <paramref name="pc"/>, czytając bajty przez <paramref name="peek"/>.
    /// </summary>
    /// <param name="peek">Odczyt bez skutków ubocznych (np. <see cref="C64Bus.Peek"/>).</param>
    /// <param name="pc">Adres pierwszego bajtu instrukcji.</param>
    /// <param name="length">Długość instrukcji w bajtach (1–3).</param>
    public static string Decode(Func<ushort, byte> peek, ushort pc, out int length)
    {
        ArgumentNullException.ThrowIfNull(peek);

        CpuOpcode? entry = StateMachineCpuCore.Describe(peek(pc));

        if (entry is not { } opcode)
        {
            length = 1;
            return Unknown;
        }

        length = LengthOf(opcode.Mode);

        return Format(opcode, peek, pc);
    }

    /// <summary>
    /// Dekoduje instrukcję z magistrali. Używa <see cref="C64Bus.Peek"/>, więc widzi ROM-y i I/O
    /// zgodnie z bieżącym bankowaniem, bez zmiany stanu magistrali.
    /// </summary>
    public static string Decode(C64Bus bus, ushort pc, out int length)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return Decode(bus.Peek, pc, out length);
    }

    private static int LengthOf(AddressingMode mode) => mode switch
    {
        AddressingMode.Immediate or AddressingMode.ZeroPage or AddressingMode.ZeroPageX => 2,
        AddressingMode.Absolute or AddressingMode.AbsoluteX => 3,
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

            case AddressingMode.Absolute:
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));

                // JMP nie ma w tekście nazwy trybu — w przeciwieństwie do pozostałych instrukcji pamięciowych.
                return opcode.Kind == OpKind.Jump
                    ? $"{mnemonic} ${Word(low, high):X4}"
                    : $"{mnemonic} abs ${Word(low, high):X4}";

            case AddressingMode.AbsoluteX:
                low = peek(Next(pc, 1));
                high = peek(Next(pc, 2));
                return $"{mnemonic} abs,X ${Word(low, high):X4}";

            default:
                return mnemonic;
        }
    }

    private static ushort Next(ushort pc, int offset) => unchecked((ushort)(pc + offset));

    private static ushort Word(byte low, byte high) => (ushort)(low | (high << 8));
}
