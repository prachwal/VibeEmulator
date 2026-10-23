using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086;

/// <summary>Adapter portów 8086; niepodłączone wejście zwraca wartość magistrali z próbek SST.</summary>
public sealed class Intel8086PortBus(IPortBus? connectedBus = null) : IPortBus
{
    private readonly IPortBus? _connectedBus = connectedBus;

    public byte Input(ushort port) => _connectedBus?.Input(port) ?? byte.MaxValue;

    public void Output(ushort port, byte value) => _connectedBus?.Output(port, value);
}
