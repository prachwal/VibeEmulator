using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086PortBusTests
{
    [Fact]
    public void Input_WhenPortIsUnconnected_ReturnsOpenBusValue()
    {
        var ports = new Intel8086PortBus();

        Assert.Equal(byte.MaxValue, ports.Input(0x1234));
    }

    [Fact]
    public void ConnectedPortBus_ReceivesInputAndOutput()
    {
        var flatPorts = new FlatPorts();
        flatPorts.Ports.Fill(0xFF);
        var ports = new Intel8086PortBus(flatPorts);

        ports.Output(0x0123, 0x5A);

        Assert.Equal((byte)0x5A, ports.Input(0x0123));
    }
}
