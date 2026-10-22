namespace VibeEmulator.CpuHarness;

/// <summary>
/// Obraz iNES (mapowanie NROM, mapper 0) — minimalne wczytanie potrzebne testom `nestest` i Blargga.
/// Inne mappery są odrzucane: bez PPU/MMC nie ma czego emulować, a zgadywanie mapowania byłoby
/// udawaniem weryfikacji.
/// </summary>
public sealed class InesImage
{
    /// <summary>Rozmiar nagłówka iNES.</summary>
    public const int HeaderSize = 16;

    private InesImage(byte[] prgRom, byte[] chrRom, int mapper, bool mirroring, bool trainer)
    {
        PrgRom = prgRom;
        ChrRom = chrRom;
        Mapper = mapper;
        VerticalMirroring = mirroring;
        HasTrainer = trainer;
    }

    /// <summary>Pamięć programu (16 KB lub 32 KB).</summary>
    public byte[] PrgRom { get; }

    /// <summary>Pamięć znaków (nieużywana przez testy CPU, zachowana dla kompletności).</summary>
    public byte[] ChrRom { get; }

    public int Mapper { get; }

    /// <summary>Mirroring wertykalny (bit 0 flag 6); horyzontalny, gdy <c>false</c>.</summary>
    public bool VerticalMirroring { get; }

    public bool HasTrainer { get; }

    /// <summary>
    /// Wczytuje plik iNES. Obsługiwany jest wyłącznie mapper 0 (NROM) — testy `nestest` i Blargga go używają.
    /// </summary>
    /// <exception cref="ArgumentException">Zły nagłówek, nieobsługiwany mapper albo brak danych.</exception>
    public static InesImage Load(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize || data[0] != 'N' || data[1] != 'E' || data[2] != 'S' || data[3] != 0x1A)
        {
            throw new ArgumentException("Brak nagłówka iNES („NES\\x1A”).", nameof(data));
        }

        int prgBanks = data[4];
        int chrBanks = data[5];
        bool trainer = (data[6] & 0x04) != 0;
        int mapper = (data[6] >> 4) | (data[7] & 0xF0);

        if (mapper != 0)
        {
            throw new ArgumentException($"Mapper {mapper} nie jest obsługiwany (tylko NROM/mapper 0).", nameof(data));
        }

        if (prgBanks is not (1 or 2))
        {
            throw new ArgumentException($"NROM wymaga 1 lub 2 banków PRG, plik ma {prgBanks}.", nameof(data));
        }

        int offset = HeaderSize + (trainer ? 512 : 0);
        int prgSize = prgBanks * 16384;
        int chrSize = chrBanks * 8192;

        if (data.Length < offset + prgSize + chrSize)
        {
            throw new ArgumentException("Plik jest krótszy niż deklaruje nagłówek.", nameof(data));
        }

        return new InesImage(
            data.Slice(offset, prgSize).ToArray(),
            chrSize == 0 ? [] : data.Slice(offset + prgSize, chrSize).ToArray(),
            mapper,
            (data[6] & 0x01) != 0,
            trainer);
    }
}
