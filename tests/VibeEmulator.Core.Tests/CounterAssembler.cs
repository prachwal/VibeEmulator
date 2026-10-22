using System.Globalization;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.Tests;

/// <summary>Minimalny assembler przykładu: etykiety, implied, immediate, absolute i BNE relative.</summary>
internal static class CounterAssembler
{
    public static byte[] Compile(string source, CpuVariant variant, ushort origin)
    {
        string[] lines = source.Split('\n').Select(line => line.Split(';')[0].Trim())
            .Where(line => line.Length != 0).ToArray();
        var labels = new Dictionary<string, int>(StringComparer.Ordinal);
        int pc = origin;
        foreach (string line in lines)
        {
            if (line.EndsWith(':'))
            {
                labels.Add(line[..^1], pc);
            }
            else
            {
                var (_, _, mode) = Parse(line);
                pc += mode == AddressingMode.Implied ? 1 : mode == AddressingMode.Absolute ? 3 : 2;
            }
        }

        var bytes = new List<byte>();
        foreach (string line in lines.Where(line => !line.EndsWith(':')))
        {
            var (mnemonic, operand, mode) = Parse(line);
            CpuOpcode opcode = variant.Opcodes.Single(entry => entry.Mnemonic == mnemonic && entry.Mode == mode);
            bytes.Add(opcode.Opcode);
            if (mode == AddressingMode.Implied)
            {
                continue;
            }

            if (mode == AddressingMode.Relative)
            {
                int offset = labels[operand] - (origin + bytes.Count + 1);
                bytes.Add(unchecked((byte)checked((sbyte)offset)));
            }
            else
            {
                ushort value = ushort.Parse(operand.TrimStart('#', '$'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                bytes.Add(mode == AddressingMode.Immediate ? checked((byte)value) : (byte)(value & 0xFF));
                if (mode == AddressingMode.Absolute)
                {
                    bytes.Add((byte)(value >> 8));
                }
            }
        }

        return bytes.ToArray();
    }

    private static (string Mnemonic, string Operand, AddressingMode Mode) Parse(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], string.Empty, AddressingMode.Implied);
        }

        if (parts.Length != 2)
        {
            throw new FormatException($"Nieobsługiwana instrukcja przykładu: {line}");
        }

        AddressingMode mode = parts[0] == "BNE" ? AddressingMode.Relative
            : parts[1].StartsWith('#') ? AddressingMode.Immediate : AddressingMode.Absolute;
        return (parts[0], parts[1], mode);
    }
}
