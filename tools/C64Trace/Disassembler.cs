namespace C64Trace;

/// <summary>Dekoder opkodów obsługiwanych przez rdzeń — tylko do wyświetlania śladu.</summary>
internal static class Disassembler
{
    internal interface IMemoryReader
    {
        byte Peek(ushort address);
    }

    public static string Decode(IMemoryReader memory, ushort pc, out int length)
    {
        byte opcode = memory.Peek(pc);

        (string mnemonic, int bytes) = opcode switch
        {
            0xA9 => ("LDA #", 2),
            0xA5 => ("LDA zp", 2),
            0xB5 => ("LDA zp,X", 2),
            0xAD => ("LDA abs", 3),
            0xBD => ("LDA abs,X", 3),
            0xA2 => ("LDX #", 2),
            0xA6 => ("LDX zp", 2),
            0xA0 => ("LDY #", 2),
            0xA4 => ("LDY zp", 2),
            0x85 => ("STA zp", 2),
            0x8D => ("STA abs", 3),
            0x9D => ("STA abs,X", 3),
            0x86 => ("STX zp", 2),
            0x84 => ("STY zp", 2),
            0x69 => ("ADC #", 2),
            0xE9 => ("SBC #", 2),
            0xE6 => ("INC zp", 2),
            0xEE => ("INC abs", 3),
            0xC6 => ("DEC zp", 2),
            0xCE => ("DEC abs", 3),
            0xE8 => ("INX", 1),
            0xCA => ("DEX", 1),
            0xC8 => ("INY", 1),
            0x88 => ("DEY", 1),
            0xAA => ("TAX", 1),
            0xA8 => ("TAY", 1),
            0x8A => ("TXA", 1),
            0x98 => ("TYA", 1),
            0xEA => ("NOP", 1),
            0x00 => ("BRK", 1),
            0x4C => ("JMP", 3),
            _ => ($"???", 1),
        };

        length = bytes;

        string operand = bytes switch
        {
            2 => $"${memory.Peek((ushort)(pc + 1)):X2}",
            3 => $"${memory.Peek((ushort)(pc + 2)):X2}{memory.Peek((ushort)(pc + 1)):X2}",
            _ => string.Empty,
        };

        return operand.Length == 0
            ? mnemonic
            : mnemonic.EndsWith('#') || mnemonic.EndsWith(',') || mnemonic.EndsWith('(')
                ? $"{mnemonic}{operand}"
                : $"{mnemonic} {operand}";
    }
}
