namespace VibeEmulator.Memory.Tests;

public class FlatAddressMemoryTests
{
    [Theory]
    [InlineData(0x00000)]
    [InlineData(0x0FFFF)]
    [InlineData(0x10000)]
    [InlineData(0xFFFFF)]
    public void Write_ThenRead_UsesFullAddress(uint address)
    {
        var memory = new FlatAddressMemory(0x100000);

        memory.Write(address, 0x5A);

        Assert.Equal((byte)0x5A, memory.Read(address));
    }

    [Fact]
    public void Read_AddressOutsideConfiguredSize_Throws()
    {
        var memory = new FlatAddressMemory(0x100000);

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Read(0x100000));
    }

    [Fact]
    public void Load_CopiesBytesAtPhysicalAddress()
    {
        var memory = new FlatAddressMemory(0x100000);

        memory.Load(0xFFFFE, [0x12, 0x34]);

        Assert.Equal((byte)0x12, memory.Read(0xFFFFE));
        Assert.Equal((byte)0x34, memory.Read(0xFFFFF));
    }
}
