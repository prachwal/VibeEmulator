using VibeEmulator.Core.C64;

namespace VibeEmulator.Core.Tests;

public class C64BusPeekTests
{
    [Fact]
    public void Peek_RamAddress_ReturnsSameValueAsRead()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Bus.LoadRam(0x0300, [0x42]);

        Assert.Equal((byte)0x42, machine.Bus.Peek(0x0300));
        Assert.Equal((byte)0x42, machine.Bus.Read(0x0300));
    }

    [Fact]
    public void Peek_KernalAddress_ReturnsSameValueAsRead()
    {
        byte[] kernal = new byte[0x2000];
        kernal[0x0100] = 0xEA; // $E100
        C64Machine machine = C64Machine.CreateCycleExact(null, kernal, null);

        Assert.Equal((byte)0xEA, machine.Bus.Peek(0xE100));
        Assert.Equal((byte)0xEA, machine.Bus.Read(0xE100));
    }

    [Fact]
    public void Peek_BasicRomWhenEnabled_ReturnsRomAndRamWhenDisabled()
    {
        byte[] basic = new byte[0x2000];
        basic[0] = 0xAA;
        C64Machine machine = C64Machine.CreateCycleExact(basic, null, null);
        machine.Bus.LoadRam(0xA000, [0x55]);

        Assert.Equal((byte)0xAA, machine.Bus.Peek(0xA000));

        machine.Bus.Port.WriteData(0x34); // LORAM=0 — BASIC ROM znika po 3 cyklach
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();
        machine.Bus.Port.Tick();

        Assert.Equal((byte)0x55, machine.Bus.Peek(0xA000));
    }

    [Fact]
    public void Peek_VicRegister_ReturnsSameValueAsRead()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Bus.Write(0xD020, 0x0F);

        Assert.Equal((byte)0x0F, machine.Bus.Peek(0xD020));
        Assert.Equal((byte)0x0F, machine.Bus.Read(0xD020));
    }

    [Fact]
    public void Peek_AnyAddress_DoesNotChangeOpenBus()
    {
        byte[] kernal = new byte[0x2000];
        kernal[0x0100] = 0xEA;
        C64Machine machine = C64Machine.CreateCycleExact(null, kernal, null);
        machine.Bus.LoadRam(0x0300, [0x42]);
        _ = machine.Bus.Read(0x0300);

        _ = machine.Bus.Peek(0xE100);

        Assert.Equal((byte)0x42, machine.Bus.OpenBus);

        // Kontrola: zwykły odczyt tego samego adresu zmienia OpenBus.
        _ = machine.Bus.Read(0xE100);

        Assert.Equal((byte)0xEA, machine.Bus.OpenBus);
    }

    [Fact]
    public void Peek_AnyAddress_DoesNotInvokeObserver()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        var observer = new CountingObserver();
        machine.Bus.Observer = observer;

        _ = machine.Bus.Peek(0x0300);

        Assert.Equal(0, observer.Count);

        _ = machine.Bus.Read(0x0300);

        Assert.Equal(1, observer.Count);
    }

    private sealed class CountingObserver : IBusObserver
    {
        public int Count { get; private set; }

        public void OnAccess(bool write, ushort address, byte value) => Count++;
    }
}
