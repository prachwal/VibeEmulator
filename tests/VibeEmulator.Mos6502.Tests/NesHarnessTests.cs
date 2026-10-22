using VibeEmulator.CpuHarness;

namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Testy warstwy NES harnessu (etap Q) na danych budowanych w pamięci — repozytorium nie trzyma ROM-ów,
/// więc sprawdzamy parsowanie iNES, mapowanie pamięci NROM, protokół statusu i parser logu `nestest`.
/// </summary>
public class NesHarnessTests
{
    /// <summary>Minimalny nagłówek iNES + PRG (bez CHR) — tyle wystarcza NROM-128.</summary>
    private static byte[] NesRom(int prgBanks, int mapper = 0)
    {
        byte[] rom = new byte[16 + (prgBanks * 16384)];
        rom[0] = (byte)'N';
        rom[1] = (byte)'E';
        rom[2] = (byte)'S';
        rom[3] = 0x1A;
        rom[4] = (byte)prgBanks;
        rom[5] = 0;
        rom[6] = (byte)(mapper << 4);
        rom[7] = 0;
        return rom;
    }

    [Fact]
    public void InesImage_RejectsFileWithoutHeader()
    {
        Assert.Throws<ArgumentException>(() => InesImage.Load(new byte[32]));
    }

    [Fact]
    public void InesImage_RejectsNonNromMapper()
    {
        byte[] rom = NesRom(prgBanks: 1, mapper: 1);

        ArgumentException error = Assert.Throws<ArgumentException>(() => InesImage.Load(rom));

        Assert.Contains("Mapper 1", error.Message);
    }

    [Fact]
    public void InesImage_ReadsPrgAndFlags()
    {
        byte[] rom = NesRom(prgBanks: 1);
        rom[16] = 0xAA;
        rom[^1] = 0xBB;

        var image = InesImage.Load(rom);

        Assert.Equal(0x4000, image.PrgRom.Length);
        Assert.Equal((byte)0xAA, image.PrgRom[0]);
        Assert.Equal((byte)0xBB, image.PrgRom[^1]);
        Assert.Equal(0, image.Mapper);
    }

    [Fact]
    public void NesBus_MirrorsTwoKilobytesOfRam()
    {
        var bus = new NesBus(InesImage.Load(NesRom(prgBanks: 1)));

        bus.Write(0x0000, 0x42);

        Assert.Equal((byte)0x42, bus.Read(0x0800));
        Assert.Equal((byte)0x42, bus.Read(0x1000));
        Assert.Equal((byte)0x42, bus.Read(0x1800));
    }

    [Fact]
    public void NesBus_IgnoresIoWritesButCountsThem()
    {
        var bus = new NesBus(InesImage.Load(NesRom(prgBanks: 1)));

        bus.Write(0x4015, 0x0F);
        bus.Write(0x2000, 0x80);

        Assert.Equal(2, bus.IoWrites);
        Assert.Equal((byte)0x00, bus.Read(0x4015));
    }

    [Fact]
    public void NesBus_MirrorsNrom128IntoUpperHalf()
    {
        byte[] rom = NesRom(prgBanks: 1);
        rom[16] = 0x7E;

        var bus = new NesBus(InesImage.Load(rom));

        Assert.Equal((byte)0x7E, bus.Read(0x8000));
        Assert.Equal((byte)0x7E, bus.Read(0xC000));
    }

    [Fact]
    public void NesBus_ReadsResetVectorFromPrg()
    {
        byte[] rom = NesRom(prgBanks: 1);
        // PRG 16 KB jest mapowane pod $8000 i $C000, więc wektor $FFFC leży pod offsetem $3FFC.
        rom[16 + 0x3FFC] = 0x83;
        rom[16 + 0x3FFD] = 0xE6;

        var bus = new NesBus(InesImage.Load(rom));

        Assert.Equal((ushort)0xE683, bus.ResetVector);
    }

    [Fact]
    public void NestestLine_ParsesLogLine()
    {
        const string line = "C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7";

        List<NestestLine> parsed = NestestLine.Parse(line);

        NestestLine entry = Assert.Single(parsed);
        Assert.Equal((ushort)0xC000, entry.Pc);
        Assert.Equal((byte)0x24, entry.P);
        Assert.Equal((byte)0xFD, entry.Sp);
        Assert.Equal(7, entry.Cycles);
    }

    [Fact]
    public void NestestLine_SkipsLinesWithoutState()
    {
        Assert.Empty(NestestLine.Parse("nagłówek\n\n---\n"));
    }

    [Fact]
    public void NestestLine_CycleOffsetMatchesFirstLogEntry()
    {
        // Pierwsza linia nestest.log ma CYC:7, a rdzeń startuje od 0 — offset jest stały dla całego logu.
        Assert.Equal(7, NestestLine.CycleOffset);
    }
}
