namespace VibeEmulator.Memory;

/// <summary>Płaska pamięć adresowana przez 32-bitowy adres fizyczny.</summary>
public sealed class FlatAddressMemory : IAddressBus
{
    private readonly byte[] _data;

    public FlatAddressMemory(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        _data = new byte[size];
    }

    public int Size => _data.Length;

    public byte Read(uint address)
    {
        ValidateAddress(address);
        return _data[(int)address];
    }

    public void Write(uint address, byte value)
    {
        ValidateAddress(address);
        _data[(int)address] = value;
    }

    public void Load(uint address, ReadOnlySpan<byte> data)
    {
        if (address > int.MaxValue || address + (uint)data.Length > (uint)_data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(address));
        }

        data.CopyTo(_data.AsSpan((int)address));
    }

    private void ValidateAddress(uint address)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(address, (uint)_data.Length);
    }
}
