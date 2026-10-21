namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy referencyjne: rdzeń cyklowy porównywany z wektorami SingleStepTests/65x02 (cykl po cyklu).
/// Jeden test na opkod i wariant; w repo leżą pierwsze 100 testów z każdego pliku (patrz <c>Vectors/README.md</c>).
/// Bez pominięć (<c>Skip</c>) — jedyny wyjątek od porównania to tryb dziesiętny ADC/SBC w <see cref="SingleStepVectors"/>.
/// </summary>
public class SingleStepVectorTests
{
    private const string NmosDirectory = "nmos6502";
    private const string CmosDirectory = "wdc65c02";

    /// <summary>Opkody wariantu NMOS z wektorami (BRK, <c>00</c>, nie jest walidowany wektorami).</summary>
    public static TheoryData<int> NmosOpcodes => ToTheoryData(
        0xA9, 0xA5, 0xB5, 0xAD, 0xBD, 0xB9, 0xA1, 0xB1,
        0xA2, 0xA6, 0xB6, 0xAE, 0xBE,
        0xA0, 0xA4, 0xB4, 0xAC, 0xBC,
        0x85, 0x95, 0x8D, 0x9D, 0x99, 0x81, 0x91,
        0x86, 0x96, 0x8E,
        0x84, 0x94, 0x8C,
        0x69, 0xE9, 0xE6, 0xEE, 0xC6, 0xCE,
        0xE8, 0xCA, 0xC8, 0x88, 0xAA, 0xA8, 0x8A, 0x98, 0xBA, 0x9A,
        0x18, 0x38, 0x58, 0x78, 0xB8, 0xD8, 0xF8,
        0xEA, 0x4C);

    /// <summary>Opkody wariantu 65C02 z wektorami: dziedziczone z NMOS oraz dodatki STZ/INC A/DEC A.</summary>
    public static TheoryData<int> CmosOpcodes => ToTheoryData(
        0x1A, 0x3A, 0x64, 0x74, 0x9C, 0x9E, 0xC6, 0xCE, 0xE6, 0xEE,
        0xA9, 0xA5, 0xB5, 0xAD, 0xBD, 0xB9, 0xA1, 0xB1,
        0xA2, 0xA6, 0xB6, 0xAE, 0xBE,
        0xA0, 0xA4, 0xB4, 0xAC, 0xBC,
        0x85, 0x95, 0x8D, 0x9D, 0x99, 0x81, 0x91,
        0x86, 0x96, 0x8E,
        0x84, 0x94, 0x8C,
        0x69, 0xE9,
        0xE8, 0xCA, 0xC8, 0x88, 0xAA, 0xA8, 0x8A, 0x98, 0xBA, 0x9A,
        0x18, 0x38, 0x58, 0x78, 0xB8, 0xD8, 0xF8,
        0xEA, 0x4C);

    [Theory]
    [MemberData(nameof(NmosOpcodes))]
    public void Vector_Nmos6502_MatchesReference(int opcode)
    {
        SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, (byte)opcode);
    }

    [Theory]
    [MemberData(nameof(CmosOpcodes))]
    public void Vector_Cmos65C02_MatchesReference(int opcode)
    {
        SingleStepVectors.Run(CpuVariants.Cmos65C02, CmosDirectory, (byte)opcode);
    }

    private static TheoryData<int> ToTheoryData(params int[] opcodes)
    {
        var data = new TheoryData<int>();

        foreach (int opcode in opcodes)
        {
            data.Add(opcode);
        }

        return data;
    }
}
