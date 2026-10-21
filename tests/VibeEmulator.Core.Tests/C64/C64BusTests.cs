using VibeEmulator.Core.C64;

namespace VibeEmulator.Core.Tests;

public class C64BusTests
{
    [Fact]
    public void DefaultBanking_EnablesBasicAndKernalRom()
    {
        var port = new ProcessorPort();

        Assert.True(port.BasicRomEnabled);
        Assert.True(port.KernalRomEnabled);
        Assert.True(port.IoVisible);
    }

    [Fact]
    public void BankSwitch_TakesThreeCyclesToSettle()
    {
        var port = new ProcessorPort();

        port.WriteData(0x34); // LORAM=0, HIRAM=0, CHAREN=1

        Assert.True(port.BasicRomEnabled);
        Assert.True(port.KernalRomEnabled);

        port.Tick();
        port.Tick();

        Assert.True(port.BasicRomEnabled);

        port.Tick();

        Assert.False(port.BasicRomEnabled);
        Assert.False(port.KernalRomEnabled);
    }

    [Fact]
    public void ReadData_MixesOutputAndInputPins()
    {
        var port = new ProcessorPort
        {
            Direction = 0x00,
            Data = 0xFF,
        };

        // Cały port jako wejście → piny wejściowe 0x17.
        Assert.Equal(0x17, port.ReadData());
    }

    [Fact]
    public void Bus_ReadsBasicRomWhenEnabled()
    {
        byte[] basic = new byte[0x2000];
        basic[0] = 0xAA;
        C64Machine machine = C64Machine.CreateCycleExact(basic, null, null);

        Assert.Equal((byte)0xAA, machine.Bus.Read(0xA000));
    }

    [Fact]
    public void Bus_ReadsRamAtBasicRegionWhenRomDisabled()
    {
        byte[] basic = new byte[0x2000];
        basic[0] = 0xAA;
        C64Machine machine = C64Machine.CreateCycleExact(basic, null, null);

        machine.Bus.Write(0xA000, 0x55); // zapis do ROM ignorowany
        Assert.Equal((byte)0xAA, machine.Bus.Read(0xA000));

        machine.Bus.Port.WriteData(0x34);
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();

        machine.Bus.Write(0xA000, 0x55);
        Assert.Equal((byte)0x55, machine.Bus.Read(0xA000));
    }

    [Fact]
    public void Bus_CharRomVisibleWhenBothRomsAndIoDisabled()
    {
        byte[] charRom = new byte[0x1000];
        charRom[0] = 0x11;
        C64Machine machine = C64Machine.CreateCycleExact(null, null, charRom);

        machine.Bus.Port.WriteData(0x30); // LORAM=0, HIRAM=0, CHAREN=0
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();

        Assert.Equal((byte)0x11, machine.Bus.Read(0x1000));
    }

    [Fact]
    public void Bus_VicRegisters_AreMirroredEverySixtyFourBytes()
    {
        C64Machine machine = C64Machine.CreateCycleExact();

        machine.Bus.Write(0xD020, 0x0F);

        Assert.Equal((byte)0x0F, machine.Bus.Read(0xD020));
        Assert.Equal((byte)0x0F, machine.Bus.Read(0xD060));
        Assert.Equal((byte)0x0F, machine.Bus.Read(0xD3E0));
    }

    [Fact]
    public void Bus_ReadsRasterFromD012()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        for (int i = 0; i < VicII.CyclesPerLine; i++)
        {
            machine.Tick();
        }

        Assert.Equal(1, machine.Bus.Read(0xD012));
    }

    [Fact]
    public void OpenBus_ReturnsLastValueOnBus()
    {
        C64Machine machine = C64Machine.CreateCycleExact();

        machine.Bus.Write(0x1000, 0x42);
        Assert.Equal((byte)0x42, machine.Bus.Read(0x1000));
        Assert.Equal((byte)0x42, machine.Bus.OpenBus);

        // $DF00 nie jest obsłużone → open bus, nie zero.
        Assert.Equal((byte)0x42, machine.Bus.Read(0xDF00));
    }

    [Fact]
    public void Sid_Registers_AreMirroredEveryThirtyTwoBytes()
    {
        C64Machine machine = C64Machine.CreateCycleExact();

        machine.Bus.Write(0xD400, 0x12);

        Assert.Equal((byte)0x12, machine.Bus.Read(0xD400));
        Assert.Equal((byte)0x12, machine.Bus.Read(0xD420));
    }
}
