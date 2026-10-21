namespace VibeEmulator.Memory;

/// <summary>Magistrala pamięci. Pozwala podstawić płaską pamięć, mapę lub urządzenie.</summary>
public interface IMemoryBus
{
    byte Read(ushort address);

    void Write(ushort address, byte value);
}

/// <summary>Płaska pamięć 64 KB — najprostsza implementacja <see cref="IMemoryBus"/>.</summary>
public sealed class FlatMemory : IMemoryBus
{
    private readonly byte[] _data;

    public FlatMemory(int size = 0x10000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _data = new byte[size];
    }

    public int Size => _data.Length;

    public ReadOnlySpan<byte> Span => _data;

    public byte Read(ushort address) => _data[address];

    public void Write(ushort address, byte value) => _data[address] = value;

    public void Load(ushort address, ReadOnlySpan<byte> program) => program.CopyTo(_data.AsSpan(address));
}
