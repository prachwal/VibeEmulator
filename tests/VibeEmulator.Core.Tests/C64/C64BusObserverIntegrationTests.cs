using VibeEmulator.Core.C64;
using VibeEmulator.Memory;

namespace VibeEmulator.Core.Tests;

/// <summary>Integracja <see cref="CompositeBusObserver"/> z maszyną C64 (magistrala + rdzeń cyklowy).</summary>
public class C64BusObserverIntegrationTests
{
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

    private sealed class TraceObserver : IBusObserver
    {
        public List<(bool Write, ushort Address, byte Value)> Events { get; } = [];

        public void OnAccess(bool write, ushort address, byte value) => Events.Add((write, address, value));
    }
}
