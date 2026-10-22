namespace VibeEmulator.Memory;

/// <summary>
/// Magistrala portów I/O (przestrzeń portów Z80, 16-bitowy adres portu).
/// Analogiczna do <see cref="IMemoryBus"/>, ale dla przestrzeni I/O.
/// </summary>
public interface IPortBus
{
    byte Input(ushort port);
    void Output(ushort port, byte value);
}

/// <summary>Płaska przestrzeń portów (256 lub 65536 portów) do testów i harnessów.</summary>
public sealed class FlatPorts : IPortBus
{
    private readonly byte[] _ports;

    public FlatPorts(int size = 0x10000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);
        _ports = new byte[size];
    }

    public byte Input(ushort port) => _ports[port % _ports.Length];

    public void Output(ushort port, byte value) => _ports[port % _ports.Length] = value;

    public Span<byte> Ports => _ports;
}
