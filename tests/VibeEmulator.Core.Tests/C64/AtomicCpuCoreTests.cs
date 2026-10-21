using VibeEmulator.Core.C64;

namespace VibeEmulator.Core.Tests;

public class AtomicCpuCoreTests
{
    [Fact]
    public void RunsProgram_UsingExistingCpuModel()
    {
        C64Machine machine = C64Machine.CreateAtomic();
        machine.Reset(0x0200);

        machine.Bus.LoadRam(0x0200, [0xA9, 0x10, 0x8D, 0x00, 0x03, 0x00]);

        // LDA #$10 (2 cykle) + STA $0300 (4 cykle).
        for (int i = 0; i < 6; i++)
        {
            machine.Tick();
        }

        Assert.Equal((byte)0x10, machine.Cpu.State.A);
        Assert.Equal((byte)0x10, machine.Bus.Read(0x0300));
    }

    [Fact]
    public void PendingCycles_CountDownWithinInstruction()
    {
        C64Machine machine = C64Machine.CreateAtomic();
        machine.Reset(0x0200);
        machine.Bus.LoadRam(0x0200, [0xAD, 0x00, 0x03]); // LDA abs = 4 cykle

        machine.Tick(); // wykonanie instrukcji, 3 cykle pozostają
        Assert.Equal(3, machine.Cpu.Pending);

        machine.Tick();
        Assert.Equal(2, machine.Cpu.Pending);

        machine.Tick();
        machine.Tick();

        Assert.Equal(0, machine.Cpu.Pending);
    }

    [Fact]
    public void UnknownOpcode_JamsInsteadOfThrowing()
    {
        C64Machine machine = C64Machine.CreateAtomic();
        machine.Reset(0x0200);
        machine.Bus.LoadRam(0x0200, [0xFF]);

        machine.Tick();

        Assert.True(machine.Cpu.Jammed);
    }

    [Fact]
    public void AtomicCore_DoesNotSeeCycleStealing_InBusEvents()
    {
        C64Machine machine = C64Machine.CreateAtomic();
        machine.Reset(0x0200);
        machine.Vic.Write(0xD011, 0x1B); // badlines aktywne

        machine.Bus.LoadRam(0x0200, [0xA9, 0x01, 0x4C, 0x00, 0x02]); // LDA #1; JMP $0200
        machine.RunFrame();

        // Cykle ukradzione są liczone przez maszynę, ale nie zmieniają liczby
        // dostępów do magistrali — to właśnie różnica wobec rdzenia cyklowego.
        long stolen = machine.TotalCycles - machine.Cpu.Cycles;

        Assert.Equal(25 * 40, stolen);
    }
}
