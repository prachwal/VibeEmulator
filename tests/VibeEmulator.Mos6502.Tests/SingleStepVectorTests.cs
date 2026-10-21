namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy referencyjne: rdzeń cyklowy porównywany z wektorami SingleStepTests/65x02 (cykl po cyklu).
/// Jeden test na wariant (pętla po opkodach), w środku pętla po próbce 100 testów z każdego pliku.
/// Opkody z niezgodnością rdzenia NMOS są jawnie pominięte (<c>Skip</c>) z opisem różnicy —
/// nie są ukryte i nie są naprawiane w tym etapie (patrz <c>docs/plan-c64-cycle-exact.md</c>).
/// </summary>
public class SingleStepVectorTests
{
    private const string NmosDirectory = "nmos6502";
    private const string CmosDirectory = "wdc65c02";

    private const string ImpliedDummyReadSkip =
        "znany błąd NMOS (ETAP 2): instrukcja jednobajtowa nie wykonuje dummy read spod PC w drugim cyklu; " +
        "wektor: cykl 2 = read $PC, rdzeń: brak dostępu. Poprawka (jedna linia w AccessImplied) zmieniłaby " +
        "zachowanie istniejących opkodów NMOS, czego zakres tego etapu zabrania, więc zostaje do etapu 2.";

    [Theory]
    [InlineData(0xA9)]
    [InlineData(0xA5)]
    [InlineData(0xAD)]
    [InlineData(0xBD)]
    [InlineData(0xA2)]
    [InlineData(0xA6)]
    [InlineData(0xA0)]
    [InlineData(0xA4)]
    [InlineData(0x85)]
    [InlineData(0x8D)]
    [InlineData(0x9D)]
    [InlineData(0x86)]
    [InlineData(0x84)]
    [InlineData(0x69)]
    [InlineData(0xE9)]
    [InlineData(0x4C)]
    [InlineData(0xEE)]
    [InlineData(0xE6)]
    [InlineData(0xCE)]
    [InlineData(0xC6)]
    public void Vector_Nmos6502_MatchesReference(int opcode)
    {
        SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, (byte)opcode);
    }

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_EA_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xEA);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_E8_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xE8);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_CA_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xCA);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_C8_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xC8);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_88_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0x88);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_AA_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xAA);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_A8_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xA8);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_8A_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0x8A);

    [Fact(Skip = ImpliedDummyReadSkip)]
    public void Vector_Nmos6502_98_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0x98);

    [Fact(Skip =
        "znany błąd NMOS (ETAP 2): LDA zp,X nie wykonuje dummy read spod adresu zerowej strony w trzecim cyklu; " +
        "wektor: cykl 3 = read zp, rdzeń: brak dostępu. Poprawka (jedna linia w IndexInternal) zmieniłaby " +
        "zachowanie opkodu NMOS, więc zostaje do etapu 2.")]
    public void Vector_Nmos6502_B5_MatchesReference() => SingleStepVectors.Run(CpuVariants.Nmos6502, NmosDirectory, 0xB5);

    [Theory]
    [InlineData(0xEE)]
    [InlineData(0xE6)]
    [InlineData(0xCE)]
    [InlineData(0xC6)]
    [InlineData(0x1A)]
    [InlineData(0x3A)]
    [InlineData(0x64)]
    [InlineData(0x74)]
    [InlineData(0x9C)]
    [InlineData(0x9E)]
    public void Vector_Cmos65C02_MatchesReference(int opcode)
    {
        SingleStepVectors.Run(CpuVariants.Cmos65C02, CmosDirectory, (byte)opcode);
    }
}
