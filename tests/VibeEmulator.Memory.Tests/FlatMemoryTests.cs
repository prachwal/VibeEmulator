namespace VibeEmulator.Memory.Tests;

public class FlatMemoryTests
{
    [Fact]
    public void Write_ThenRead_ReturnsWrittenValue()
    {
        var memory = new FlatMemory();

        memory.Write(0x1234, 0x5A);

        Assert.Equal((byte)0x5A, memory.Read(0x1234));
    }

    [Fact]
    public void Load_CopiesProgramAtAddress()
    {
        var memory = new FlatMemory();

        memory.Load(0x0200, [0xA9, 0x42]);

        Assert.Equal(0x10000, memory.Size);
        Assert.Equal((byte)0xA9, memory.Read(0x0200));
        Assert.Equal((byte)0x42, memory.Read(0x0201));
        Assert.Equal((byte)0x00, memory.Read(0x0202));
    }
}
