using VibeEmulator.Core.C64;

namespace VibeEmulator.Core.Tests;

public class CompositeBusObserverTests
{
    [Fact]
    public void Add_Null_ThrowsArgumentNullException()
    {
        var composite = new CompositeBusObserver();

        Assert.Throws<ArgumentNullException>(() => composite.Add(null!));
    }

    [Fact]
    public void Add_SameObserverTwice_IsIgnored()
    {
        var composite = new CompositeBusObserver();
        var log = new List<string>();
        var observer = new NamedObserver("A", log);

        composite.Add(observer);
        composite.Add(observer);
        composite.OnAccess(false, 0x0200, 0x11);

        Assert.Equal(1, composite.Count);
        Assert.Equal("A", string.Join(",", log));
    }

    [Fact]
    public void OnAccess_ThreeObservers_CallsInAddOrder()
    {
        var composite = new CompositeBusObserver();
        var log = new List<string>();
        composite.Add(new NamedObserver("A", log));
        composite.Add(new NamedObserver("B", log));
        composite.Add(new NamedObserver("C", log));

        composite.OnAccess(true, 0x0300, 0x42);

        Assert.Equal("A,B,C", string.Join(",", log));
    }

    [Fact]
    public void Remove_PresentObserver_ReturnsTrueThenFalse()
    {
        var composite = new CompositeBusObserver();
        var log = new List<string>();
        var a = new NamedObserver("A", log);
        composite.Add(a);
        composite.Add(new NamedObserver("B", log));

        Assert.True(composite.Remove(a));
        Assert.False(composite.Remove(a));
        Assert.Equal(1, composite.Count);

        composite.OnAccess(false, 0x0000, 0x00);

        Assert.Equal("B", string.Join(",", log));
    }

    [Fact]
    public void Remove_ObserverDuringIteration_DoesNotBreakCurrentPass()
    {
        var composite = new CompositeBusObserver();
        var log = new List<string>();
        var b = new NamedObserver("B", log);
        composite.Add(new RemovingObserver("A", log, composite, b));
        composite.Add(b);

        composite.OnAccess(false, 0x0000, 0x00);

        Assert.Equal("A,B", string.Join(",", log));
        Assert.Equal(1, composite.Count);

        composite.OnAccess(false, 0x0000, 0x00);

        Assert.Equal("A,B,A", string.Join(",", log));
    }

    [Fact]
    public void OnAccess_TwoObservers_DoesNotAllocate()
    {
        var composite = new CompositeBusObserver();
        var first = new CountingObserver();
        var second = new CountingObserver();
        composite.Add(first);
        composite.Add(second);

        for (int i = 0; i < 1000; i++)
        {
            composite.OnAccess(false, 0x0200, 0x00);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 10_000; i++)
        {
            composite.OnAccess(i % 2 == 0, (ushort)i, (byte)i);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
        Assert.Equal(11_000, first.Count);
        Assert.Equal(11_000, second.Count);
    }

    [Fact]
    public void CycleExactMachine_BothObserversSeeSameAccesses()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Bus.LoadRam(0x0200, [0xA9, 0x42, 0x8D, 0x00, 0x03]); // LDA #$42; STA $0300
        var first = new TraceObserver();
        var second = new TraceObserver();
        var composite = new CompositeBusObserver();
        composite.Add(first);
        composite.Add(second);
        machine.Bus.Observer = composite;
        machine.Reset(0x0200);

        for (int i = 0; i < 6; i++)
        {
            machine.Tick();
        }

        (bool Write, ushort Address, byte Value)[] expected =
        [
            (false, 0x0200, 0xA9),
            (false, 0x0201, 0x42),
            (false, 0x0202, 0x8D),
            (false, 0x0203, 0x00),
            (false, 0x0204, 0x03),
            (true, 0x0300, 0x42),
        ];

        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    private sealed class NamedObserver(string name, List<string> log) : IBusObserver
    {
        public void OnAccess(bool write, ushort address, byte value) => log.Add(name);
    }

    private sealed class RemovingObserver(string name, List<string> log, CompositeBusObserver composite, IBusObserver target)
        : IBusObserver
    {
        public void OnAccess(bool write, ushort address, byte value)
        {
            log.Add(name);
            composite.Remove(target);
        }
    }

    private sealed class CountingObserver : IBusObserver
    {
        public int Count { get; private set; }

        public void OnAccess(bool write, ushort address, byte value) => Count++;
    }

    private sealed class TraceObserver : IBusObserver
    {
        public List<(bool Write, ushort Address, byte Value)> Events { get; } = [];

        public void OnAccess(bool write, ushort address, byte value) => Events.Add((write, address, value));
    }
}
