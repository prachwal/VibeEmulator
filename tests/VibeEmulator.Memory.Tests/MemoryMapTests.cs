namespace VibeEmulator.Memory.Tests;

public class MemoryMapTests
{
    [Fact]
    public void Map_OverlappingRegions_ThrowsInvalidOperationException()
    {
        var first = new FlatMemory(0x100);
        var second = new FlatMemory(0x100);

        Assert.Throws<InvalidOperationException>(() => MemoryMap.CreateBuilder()
            .Map(0x0000, 0x00FF, first)
            .Map(0x00F0, 0x01FF, second));
    }

    [Fact]
    public void Read_UnmappedAddress_ThrowsInvalidOperationException()
    {
        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x00FF, new FlatMemory(0x100))
            .Build();

        Assert.Throws<InvalidOperationException>(() => map.Read(0x0200));
    }

    [Fact]
    public void Write_TwoRegions_RoutesEachAddressToItsDeviceWithOffset()
    {
        var low = new FlatMemory(0x100);
        var high = new FlatMemory(0x100);
        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x00FF, low)
            .Map(0x0100, 0x01FF, high)
            .Build();

        map.Write(0x0010, 0x11);
        map.Write(0x0110, 0x22);

        Assert.Equal((byte)0x11, low.Read(0x0010));
        Assert.Equal((byte)0x22, high.Read(0x0010));
        Assert.Equal((byte)0x22, map.Read(0x0110));
    }
}
