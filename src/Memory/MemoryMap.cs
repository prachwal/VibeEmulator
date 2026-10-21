namespace VibeEmulator.Memory;

/// <summary>
/// Kompozycja magistral: adresy kierowane do regionów (RAM, ROM, I/O).
/// Odpowiednik dekodera adresów w prawdziwym sprzęcie.
/// </summary>
public sealed class MemoryMap : IMemoryBus
{
    private readonly Region[] _regions;

    internal MemoryMap(Region[] regions) => _regions = regions;

    public static MemoryMapBuilder CreateBuilder() => new();

    public byte Read(ushort address)
    {
        foreach (Region region in _regions)
        {
            if (region.Contains(address))
            {
                return region.Device.Read((ushort)(address - region.Start));
            }
        }

        throw new InvalidOperationException($"Unmapped read at 0x{address:X4}.");
    }

    public void Write(ushort address, byte value)
    {
        foreach (Region region in _regions)
        {
            if (region.Contains(address))
            {
                region.Device.Write((ushort)(address - region.Start), value);
                return;
            }
        }

        throw new InvalidOperationException($"Unmapped write at 0x{address:X4}.");
    }

    internal readonly struct Region
    {
        public Region(ushort start, ushort end, IMemoryBus device)
        {
            Start = start;
            End = end;
            Device = device;
        }

        public ushort Start { get; }

        public ushort End { get; }

        public IMemoryBus Device { get; }

        public bool Contains(ushort address) => address >= Start && address <= End;

        public bool Overlaps(ushort start, ushort end) => start <= End && end >= Start;
    }
}

public sealed class MemoryMapBuilder
{
    private readonly List<MemoryMap.Region> _regions = [];

    public MemoryMapBuilder Map(ushort start, ushort end, IMemoryBus device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (end < start)
        {
            throw new ArgumentException($"Region end 0x{end:X4} is below start 0x{start:X4}.", nameof(end));
        }

        foreach (MemoryMap.Region existing in _regions)
        {
            if (existing.Overlaps(start, end))
            {
                throw new InvalidOperationException(
                    $"Region 0x{start:X4}-0x{end:X4} overlaps 0x{existing.Start:X4}-0x{existing.End:X4}.");
            }
        }

        _regions.Add(new MemoryMap.Region(start, end, device));
        return this;
    }

    public MemoryMap Build() => new([.. _regions]);
}
