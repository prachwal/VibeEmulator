using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.C64;

/// <summary>
/// Koszt cyklowy instrukcji dla adaptera nad modelem atomowym.
/// To przybliżenie — prawdziwe cykle wynikają z liczby dostępów do magistrali,
/// a nie z tabeli. Wartości zgodne z tabelą produkcyjną 6510 dla trybów bez
/// przekroczenia strony.
/// </summary>
public static class CycleCostTable
{
    public static int For(in Instruction instruction)
    {
        // Instrukcje o stałym, nietypowym koszcie.
        return instruction.Mnemonic switch
        {
            "BRK" => 7,
            "JMP" => 3,
            "NOP" => 2,
            _ => instruction.Mode switch
            {
                AddressingMode.Implied => 2,
                AddressingMode.Immediate => 2,
                AddressingMode.ZeroPage => 3,
                AddressingMode.ZeroPageX => 4,
                AddressingMode.Absolute => 4,
                AddressingMode.AbsoluteX => 4,
                AddressingMode.Indirect => 5,
                _ => 2,
            },
        };
    }
}
