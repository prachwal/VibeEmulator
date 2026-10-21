using VibeEmulator.Core.C64;

namespace VibeEmulator.Core.Tests;

public class C64MachineTests
{
    [Fact]
    public void Frame_HasExactly19656Cycles()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        machine.RunFrame();

        Assert.Equal(19_656, machine.TotalCycles);
        Assert.Equal(1, machine.Frames);
    }

    [Fact]
    public void Frame_ResetsRasterToLineZero()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        machine.RunFrame();

        Assert.Equal(0, machine.Vic.Line);
        Assert.Equal(0, machine.Vic.Cycle);
    }

    [Fact]
    public void Raster_AdvancesOneLinePerCycleLine()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        for (int i = 0; i < VicII.CyclesPerLine; i++)
        {
            machine.Tick();
        }

        Assert.Equal(1, machine.Vic.Line);
        Assert.Equal(0, machine.Vic.Cycle);
    }

    [Fact]
    public void BadLine_StealsExactlyFortyCycles()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        // DEN=1, RSEL=1, YSCROLL=3 → badline na liniach 0x30..0xF7 co 8.
        machine.Vic.Write(0xD011, 0x1B);

        machine.RunFrame();

        // Linie 0x30..0xF7 (200 linii), co ósma → 25 badline'ów × 40 cykli.
        long stolen = machine.TotalCycles - machine.Cpu.Cycles;

        Assert.Equal(25 * 40, stolen);
    }

    [Fact]
    public void NoBadLine_WhenDisplayDisabled()
    {
        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset();

        machine.Vic.Write(0xD011, 0x0B); // DEN=0

        machine.RunFrame();

        Assert.Equal(machine.TotalCycles, machine.Cpu.Cycles);
    }

    [Fact]
    public void IsBadLine_DetectsOnlyMatchingScrollLine()
    {
        var vic = new VicII();
        vic.Write(0xD011, 0x1B); // YSCROLL = 3

        Assert.True(vic.IsBadLine(0x33));
        Assert.False(vic.IsBadLine(0x32));
        Assert.False(vic.IsBadLine(0x28)); // przed zakresem
        Assert.False(vic.IsBadLine(0xF8)); // za zakresem
    }

    [Fact]
    public void Aec_BlocksCpuOnlyInStealingWindow()
    {
        var vic = new VicII();
        vic.Write(0xD011, 0x1B);

        // Przewiń do linii 0x33 (badline), cykl 0.
        int target = 0x33 * VicII.CyclesPerLine;

        for (int i = 0; i < target; i++)
        {
            vic.Tick();
        }

        var blocked = new List<int>();

        for (int cycle = 0; cycle < VicII.CyclesPerLine; cycle++)
        {
            VicTickResult result = vic.Tick();

            if (!result.Aec)
            {
                blocked.Add(result.Cycle);
            }
        }

        Assert.Equal(40, blocked.Count);
        Assert.Equal(15, blocked[0]);
        Assert.Equal(54, blocked[^1]);
    }
}
