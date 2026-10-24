using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Intel8086VectorRunner;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

public class Intel8086VectorTests
{
    public static IEnumerable<object[]> ActiveVectors()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Vectors");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string path = Path.Combine(directory, entry.GetProperty("file").GetString()!);
            ushort flagsMask = entry.TryGetProperty("flagsMask", out JsonElement mask)
                ? mask.GetUInt16()
                : ushort.MaxValue;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonElement vector in document.RootElement.EnumerateArray())
            {
                yield return [vector.Clone(), flagsMask];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ActiveVectors))]
    public void Implemented_Vector_MatchesStateQueueAndTStates(JsonElement vector, ushort flagsMask)
    {
        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, flagsMask: flagsMask);

        Assert.True(result.Passed, result.Difference);
        Assert.Equal(vector.GetProperty("cycles").GetArrayLength(), result.TStates);
    }

    [Fact]
    public void Nop_Vector_ExceedingTStateLimit_FailsWithCaseIdentity()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Vectors", "90.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement vector = document.RootElement[0];

        VectorRunResult result = Intel8086VectorRunner.Intel8086VectorRunner.Run(vector, 2);

        Assert.False(result.Passed);
        Assert.Equal(VectorRunStatus.TimedOut, result.Status);
        Assert.Contains("test_num=0", result.Difference);
        Assert.Contains("T-states", result.Difference);
    }

    [Fact]
    public void Reset_SetsTestProgramCounterAndClearsPrefetchQueue()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));

        cpu.Reset(0x1234);

        Assert.Equal((ushort)0, cpu.State.CS);
        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Empty(cpu.State.PendingQueue.ToArray());
        Assert.Equal(0, cpu.Pending);
        Assert.Equal(0L, cpu.Cycles);
    }

    [Fact]
    public void ResetHardware_SetsDocumentedResetVectorAndClearsQueue()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.AX = 0x1234;
        cpu.State.CS = 0x4321;
        cpu.State.IP = 0x5678;
        cpu.State.Flags = 0xFFFF;
        cpu.State.QueueCount = 1;
        cpu.State.Queue[0] = 0x90;

        cpu.ResetHardware();

        Assert.Equal((ushort)0xFFFF, cpu.State.CS);
        Assert.Equal((ushort)0, cpu.State.IP);
        Assert.Equal((ushort)0, cpu.State.DS);
        Assert.Equal((ushort)0, cpu.State.SS);
        Assert.Equal((ushort)0, cpu.State.ES);
        Assert.Equal((ushort)0, cpu.State.Flags);
        Assert.Empty(cpu.State.PendingQueue.ToArray());
        Assert.Equal(0xFFFF0u, cpu.PhysicalInstructionAddress);
        Assert.Equal(0L, cpu.Cycles);
    }

    [Fact]
    public void ResetHardware_ClearsInFlightInstructionAndLockSignal()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.Queue = [0xF0, 0x90, 0x90, 0, 0, 0];
        cpu.State.QueueCount = 3;
        cpu.Tick();

        Assert.True(cpu.LastBusCycle.LockActive);
        Assert.True(cpu.Pending > 0);

        cpu.ResetHardware();

        Assert.Equal(0, cpu.Pending);
        Assert.False(cpu.LastBusCycle.LockActive);
        Assert.Equal(0L, cpu.Cycles);
        Assert.Equal(0xFFFF0u, cpu.PhysicalInstructionAddress);
    }

    [Fact]
    public void ResetPin_FiveClockPulseAbortsInstructionAndFetchesFromResetVector()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0xFFFF0, 0x90);
        memory.Write(0xFFFF1, 0x90);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.Tick();
        Assert.True(cpu.Pending > 0);

        cpu.ResetLevel = true;
        cpu.Tick();
        Assert.Equal("PASV", cpu.LastBusCycle.BusStatus);
        for (int cycle = 1; cycle < 5; cycle++)
        {
            cpu.Tick();
            Assert.Equal("---", cpu.LastBusCycle.BusStatus);
        }

        Assert.Equal(0, cpu.Pending);
        Assert.Equal((ushort)0xFFFF, cpu.State.CS);
        Assert.Equal((ushort)0, cpu.State.IP);
        Assert.Equal((ushort)0, cpu.State.Flags);
        Assert.Equal(6, cpu.Cycles);

        cpu.ResetLevel = false;
        for (int cycle = 0; cycle < 10; cycle++)
        {
            cpu.Tick();
            Assert.NotEqual("CODE", cpu.LastBusCycle.BusStatus);
        }

        Assert.Equal(16, cpu.Cycles);
        cpu.Tick();
        Assert.Equal("CODE", cpu.LastBusCycle.BusStatus);
        Assert.Equal("T1", cpu.LastBusCycle.TState);
        Assert.Equal(0xFFFF0u, cpu.LastBusCycle.MultiplexedBus);
        Assert.Equal(1, cpu.LastBusCycle.PinBits);
        Assert.Equal((ushort)0, cpu.State.QueueCount);

        cpu.Tick();
        Assert.Equal("T2", cpu.LastBusCycle.TState);
        cpu.Tick();
        Assert.Equal("T3", cpu.LastBusCycle.TState);
        Assert.Equal((ushort)0x9090, cpu.LastBusCycle.Data);
        Assert.Equal(2, cpu.State.QueueCount);
        Assert.Equal((byte)0x90, cpu.State.Queue[0]);
        Assert.Equal((byte)0x90, cpu.State.Queue[1]);
        cpu.Tick();
        Assert.Equal("T4", cpu.LastBusCycle.TState);
    }

    [Theory]
    [InlineData(0x1000, 50)]
    [InlineData(0x1001, 62)]
    public void Nmi_RisingEdgeIsServicedAfterInstructionAndUsesTypeTwoVector(ushort initialSp, int expectedInterruptClocks)
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(8, 0x34);
        memory.Write(9, 0x12);
        memory.Write(10, 0x78);
        memory.Write(11, 0x56);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = initialSp;
        cpu.State.Flags = 0x0002;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.NmiLevel = true;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal(initialSp, cpu.State.SP);

        List<Intel8086BusCycle> interruptCycles = [];
        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            interruptCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Equal((ushort)0x5678, cpu.State.CS);
        ushort frameSp = unchecked((ushort)(initialSp - 6));
        Assert.Equal(frameSp, cpu.State.SP);
        Assert.Equal(expectedInterruptClocks, interruptCycles.Count);
        Assert.Equal((ushort)0x0000, cpu.State.Flags & 0x0300);
        Assert.Equal((byte)0x21, memory.Read(frameSp));
        Assert.Equal((byte)0x00, memory.Read(unchecked((ushort)(frameSp + 1))));
        Assert.Equal((byte)0x00, memory.Read(unchecked((ushort)(frameSp + 2))));
        Assert.Equal((byte)0x01, memory.Read(unchecked((ushort)(frameSp + 3))));
        Assert.Equal((byte)0x02, memory.Read(unchecked((ushort)(frameSp + 4))));
        Assert.Equal((byte)0x00, memory.Read(unchecked((ushort)(frameSp + 5))));
        Assert.DoesNotContain(interruptCycles, cycle => cycle.BusStatus == "INTA");
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 8);
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 10);

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x1235, cpu.State.IP);
        Assert.Equal(frameSp, cpu.State.SP);
    }

    [Theory]
    [InlineData(0x1000, 61)]
    [InlineData(0x1001, 73)]
    public void Intr_WhenIfIsSetUsesTwoAcknowledgeCyclesAndExternalVector(ushort initialSp, int expectedInterruptClocks)
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x80, 0x45);
        memory.Write(0x81, 0x23);
        memory.Write(0x82, 0x89);
        memory.Write(0x83, 0x67);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = initialSp;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;
        cpu.IrqVector = 0x20;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal(initialSp, cpu.State.SP);

        List<Intel8086BusCycle> interruptCycles = [];
        for (int cycle = 0; cycle < 100 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            interruptCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x2345, cpu.State.IP);
        Assert.Equal((ushort)0x6789, cpu.State.CS);
        ushort frameSp = unchecked((ushort)(initialSp - 6));
        Assert.Equal(frameSp, cpu.State.SP);
        Assert.Equal(expectedInterruptClocks, interruptCycles.Count);
        Assert.Equal((ushort)0x0000, cpu.State.Flags & 0x0300);
        Assert.Equal((byte)0x21, memory.Read(frameSp));
        Assert.Equal((byte)0x00, memory.Read(unchecked((ushort)(frameSp + 1))));
        Assert.Equal((byte)0x00, memory.Read(unchecked((ushort)(frameSp + 2))));
        Assert.Equal((byte)0x01, memory.Read(unchecked((ushort)(frameSp + 3))));
        Assert.Equal((byte)0x02, memory.Read(unchecked((ushort)(frameSp + 4))));
        Assert.Equal((byte)0x02, memory.Read(unchecked((ushort)(frameSp + 5))));

        Intel8086BusCycle[] intaCycles = [.. interruptCycles.Where(cycle => cycle.BusStatus == "INTA")];
        Assert.Equal(6, intaCycles.Length);
        Assert.Equal(["T2", "T3", "T4", "T2", "T3", "T4"], intaCycles.Select(cycle => cycle.TState));
        Assert.Equal((ushort)0x20, intaCycles[4].Data);
        Assert.True(intaCycles[0].LockActive);
        Assert.True(intaCycles[3].LockActive);
        Assert.False(intaCycles[4].LockActive);
        Assert.Equal(
            [
                ("PASV", "T1", false),
                ("INTA", "T2", true),
                ("INTA", "T3", true),
                ("INTA", "T4", true),
                ("PASV", "T1", true),
                ("PASV", "T2", true),
                ("PASV", "T3", true),
                ("PASV", "T1", true),
                ("INTA", "T2", true),
                ("INTA", "T3", false),
                ("INTA", "T4", false),
            ],
            interruptCycles
                .Skip(2)
                .Take(11)
                .Select(cycle => (cycle.BusStatus, cycle.TState, cycle.LockActive)));
        int firstAcknowledgeT4 = interruptCycles.FindIndex(cycle => cycle.BusStatus == "INTA" && cycle.TState == "T4");
        int secondAcknowledgeT2 = interruptCycles.FindIndex(
            firstAcknowledgeT4 + 1,
            cycle => cycle.BusStatus == "INTA" && cycle.TState == "T2");
        Intel8086BusCycle[] interveningCycles = [.. interruptCycles.Skip(firstAcknowledgeT4 + 1).Take(secondAcknowledgeT2 - firstAcknowledgeT4 - 1)];
        Assert.Equal(["T1", "T2", "T3", "T1"], interveningCycles.Select(cycle => cycle.TState));
        Assert.All(interveningCycles, cycle => Assert.Equal("PASV", cycle.BusStatus));
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 0x80);
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 0x82);
    }

    [Fact]
    public void Intr_WhenIfIsClearDoesNotStartAcknowledge()
    {
        var memory = new FlatAddressMemory(0x100000);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0002;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;
        cpu.IrqVector = 0x20;

        List<Intel8086BusCycle> cycles = [];
        for (int cycle = 0; cycle < 9; cycle++)
        {
            cpu.Tick();
            cycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x0023, cpu.State.IP);
        Assert.Equal((ushort)0x1000, cpu.State.SP);
        Assert.DoesNotContain(cycles, cycle => cycle.BusStatus == "INTA");
    }

    [Fact]
    public void Intr_PendingAtStiIsDeferredUntilFollowingInstructionCompletes()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x80, 0x45);
        memory.Write(0x81, 0x23);
        memory.Write(0x82, 0x89);
        memory.Write(0x83, 0x67);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0002;
        cpu.State.Queue = [0xFB, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;
        cpu.IrqVector = 0x20;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.NotEqual((ushort)0, cpu.State.Flags & 0x0200);
        Assert.Equal((ushort)0x1000, cpu.State.SP);

        List<Intel8086BusCycle> followingInstructionCycles = [];
        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            followingInstructionCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x0022, cpu.State.IP);
        Assert.Equal((ushort)0x1000, cpu.State.SP);
        Assert.DoesNotContain(followingInstructionCycles, cycle => cycle.BusStatus == "INTA");

        cpu.Tick();
        Assert.True(cpu.Pending > 0);
        Assert.Equal("PASV", cpu.LastBusCycle.BusStatus);
    }

    [Fact]
    public void Nmi_HasPriorityOverIntr()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(8, 0x34);
        memory.Write(9, 0x12);
        memory.Write(10, 0x78);
        memory.Write(11, 0x56);
        memory.Write(0x80, 0xCD);
        memory.Write(0x81, 0xAB);
        memory.Write(0x82, 0xEF);
        memory.Write(0x83, 0xBE);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;
        cpu.IrqVector = 0x20;
        cpu.NmiLevel = true;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        List<Intel8086BusCycle> interruptCycles = [];
        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            interruptCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Equal((ushort)0x5678, cpu.State.CS);
        Assert.DoesNotContain(interruptCycles, cycle => cycle.BusStatus == "INTA");
    }

    [Fact]
    public void TrapFlag_SingleStepsAfterInstructionAndStacksTypeOneFrame()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(4, 0x34);
        memory.Write(5, 0x12);
        memory.Write(6, 0x78);
        memory.Write(7, 0x56);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0102;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal((ushort)0x1000, cpu.State.SP);

        List<Intel8086BusCycle> trapCycles = [];
        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            trapCycles.Add(cpu.LastBusCycle);
        }

        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Equal((ushort)0x5678, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
        Assert.Equal((byte)0x21, memory.Read(0x0FFA));
        Assert.Equal((byte)0x00, memory.Read(0x0FFB));
        Assert.Equal((byte)0x00, memory.Read(0x0FFC));
        Assert.Equal((byte)0x01, memory.Read(0x0FFD));
        Assert.Equal((byte)0x02, memory.Read(0x0FFE));
        Assert.Equal((byte)0x01, memory.Read(0x0FFF));
        Assert.Equal((ushort)0, cpu.State.Flags & 0x0300);
        Assert.DoesNotContain(trapCycles, cycle => cycle.BusStatus == "INTA");
        Assert.Contains(trapCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 4);
        Assert.Contains(trapCycles, cycle => cycle.BusStatus == "MEMR" && cycle.MultiplexedBus == 6);

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x1235, cpu.State.IP);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
    }

    [Fact]
    public void TrapFlag_WhenNmiArrivesEntersNmiThenTrapBeforeHandlerInstruction()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(4, 0x34);
        memory.Write(5, 0x12);
        memory.Write(6, 0x78);
        memory.Write(7, 0x56);
        memory.Write(8, 0x00);
        memory.Write(9, 0x40);
        memory.Write(10, 0x00);
        memory.Write(11, 0x30);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0102;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.NmiLevel = true;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x4000, cpu.State.IP);
        Assert.Equal((ushort)0x3000, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);

        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Equal((ushort)0x5678, cpu.State.CS);
        Assert.Equal((ushort)0x0FF4, cpu.State.SP);
        Assert.Equal((byte)0x00, memory.Read(0x0FF4));
        Assert.Equal((byte)0x40, memory.Read(0x0FF5));
        Assert.Equal((byte)0x00, memory.Read(0x0FF6));
        Assert.Equal((byte)0x30, memory.Read(0x0FF7));
    }

    [Fact]
    public void TrapFlag_RestoredByIretTrapsAfterFollowingProgramInstruction()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(4, 0x10);
        memory.Write(5, 0x00);
        memory.Write(6, 0x00);
        memory.Write(7, 0x02);
        memory.Write(0x2010, 0xCF);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0102;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0010, cpu.State.IP);
        Assert.Equal((ushort)0x0200, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
        cpu.State.QueueHead = 0;
        cpu.State.Queue = [0xCF, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal((ushort)0x0100, cpu.State.CS);
        Assert.Equal((ushort)0x1000, cpu.State.SP);
        Assert.NotEqual((ushort)0, cpu.State.Flags & 0x0100);
        cpu.State.QueueHead = 0;
        cpu.State.Queue = [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 16 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0022, cpu.State.IP);
        Assert.Equal((ushort)0x1000, cpu.State.SP);

        for (int cycle = 0; cycle < 80 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x0010, cpu.State.IP);
        Assert.Equal((ushort)0x0200, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
        Assert.Equal((byte)0x22, memory.Read(0x0FFA));
    }

    [Fact]
    public void Hlt_ConsumesTwoCyclesAndWaitsForUnmaskedInterrupt()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(8, 0x34);
        memory.Write(9, 0x12);
        memory.Write(10, 0x78);
        memory.Write(11, 0x56);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0002;
        cpu.State.Queue = [0xF4, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;

        cpu.Tick();
        Assert.False(cpu.Halted);
        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal(1, cpu.Pending);

        cpu.Tick();
        Assert.True(cpu.Halted);
        Assert.Equal(2, cpu.Cycles);
        Assert.Equal(0, cpu.Pending);
        Assert.Equal("HALT", cpu.LastBusCycle.BusStatus);
        Assert.Equal((byte)1, cpu.LastBusCycle.PinBits);

        for (int cycle = 0; cycle < 4; cycle++)
        {
            cpu.Tick();
        }

        Assert.True(cpu.Halted);
        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal((ushort)0x1000, cpu.State.SP);
        Assert.Equal("HALT", cpu.LastBusCycle.BusStatus);

        cpu.NmiLevel = true;
        cpu.Tick();
        Assert.False(cpu.Halted);
        Assert.True(cpu.Pending > 0);

        for (int cycle = 0; cycle < 80 && cpu.Pending != 0; cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x1234, cpu.State.IP);
        Assert.Equal((ushort)0x5678, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
    }

    [Fact]
    public void Hlt_EnabledIntrWakesAndUsesExternalVector()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x80, 0x45);
        memory.Write(0x81, 0x23);
        memory.Write(0x82, 0x89);
        memory.Write(0x83, 0x67);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0xF4, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqLevel = true;
        cpu.IrqVector = 0x20;

        cpu.Tick();
        cpu.Tick();
        Assert.True(cpu.Halted);

        cpu.Tick();
        Assert.False(cpu.Halted);
        Assert.True(cpu.Pending > 0);
        for (int cycle = 0; cycle < 100 && cpu.Pending != 0; cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((ushort)0x2345, cpu.State.IP);
        Assert.Equal((ushort)0x6789, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
    }

    [Fact]
    public void ResetHardware_ClearsHaltedState()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.Queue = [0xF4, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        cpu.Tick();
        cpu.Tick();
        Assert.True(cpu.Halted);

        cpu.ResetHardware();

        Assert.False(cpu.Halted);
        Assert.Equal(0xFFFF0u, cpu.PhysicalInstructionAddress);
    }

    [Fact]
    public void Wait_WhenTestIsInactivePollsEveryFiveClocksAndResumes()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x9B, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 3; cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal(3, cpu.Cycles);
        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.True(cpu.WaitingForTest);
        Assert.Equal(0, cpu.Pending);

        for (int cycle = 0; cycle < 4; cycle++)
        {
            cpu.Tick();
        }

        Assert.True(cpu.WaitingForTest);
        Assert.Equal((ushort)0x0021, cpu.State.IP);

        cpu.TestLevel = false;
        cpu.Tick();
        Assert.False(cpu.WaitingForTest);
        Assert.Equal((ushort)0x0021, cpu.State.IP);

        cpu.IrqLevel = true;
        for (int cycle = 0; cycle < 3; cycle++)
        {
            cpu.Tick();
            Assert.NotEqual("INTA", cpu.LastBusCycle.BusStatus);
        }

        Assert.Equal((ushort)0x0022, cpu.State.IP);
        Assert.Equal((ushort)0, cpu.State.SP);

        cpu.Tick();
        Assert.True(cpu.Pending > 0);
    }

    [Fact]
    public void Wait_WhenTestIsActiveAtInstructionCheckCompletesInThreeClocks()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000))
        {
            TestLevel = false,
        };
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x9B, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        for (int cycle = 0; cycle < 3; cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal(3, cpu.Cycles);
        Assert.False(cpu.WaitingForTest);
        Assert.Equal((ushort)0x0021, cpu.State.IP);
        Assert.Equal((ushort)0x0202, cpu.State.Flags);
    }

    [Fact]
    public void Wait_EnabledIntrDuringWaitSavesWaitInstructionAddress()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x80, 0x45);
        memory.Write(0x81, 0x23);
        memory.Write(0x82, 0x89);
        memory.Write(0x83, 0x67);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.SS = 0;
        cpu.State.SP = 0x1000;
        cpu.State.Flags = 0x0202;
        cpu.State.Queue = [0x9B, 0x90, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;
        cpu.IrqVector = 0x20;

        for (int cycle = 0; cycle < 3; cycle++)
        {
            cpu.Tick();
        }

        Assert.True(cpu.WaitingForTest);
        cpu.IrqLevel = true;
        List<Intel8086BusCycle> interruptCycles = [];
        for (int cycle = 0; cycle < 100 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            interruptCycles.Add(cpu.LastBusCycle);
        }

        Assert.False(cpu.WaitingForTest);
        Assert.Equal((ushort)0x2345, cpu.State.IP);
        Assert.Equal((ushort)0x6789, cpu.State.CS);
        Assert.Equal((ushort)0x0FFA, cpu.State.SP);
        Assert.Equal((byte)0x20, memory.Read(0x0FFA));
        Assert.Equal((byte)0x00, memory.Read(0x0FFB));
        Assert.Equal((byte)0x00, memory.Read(0x0FFC));
        Assert.Equal((byte)0x01, memory.Read(0x0FFD));
        Assert.Contains(interruptCycles, cycle => cycle.BusStatus == "INTA");
    }

    [Fact]
    public void Ready_LowInsertsTwStatesAfterMemoryReadT3UntilReadyReturnsHigh()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x20100, 0xA5);
        memory.Write(0x20101, 0x00);
        var cpu = new Intel8086Core(memory);
        cpu.State.CS = 0x0100;
        cpu.State.IP = 0x0020;
        cpu.State.DS = 0x2000;
        cpu.State.BX = 0x0100;
        cpu.State.AX = 0x0001;
        cpu.State.Queue = [0x01, 0x07, 0x90, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = 6;

        bool assertedReadyLowAtT2 = false;
        for (int cycle = 0; cycle < 50 && cpu.LastBusCycle.TState != "TW"; cycle++)
        {
            cpu.Tick();
            if (cpu.LastBusCycle.TState == "T2" && cpu.LastBusCycle.MemoryStatus == "R--")
            {
                cpu.ReadyLevel = false;
                assertedReadyLowAtT2 = true;
            }
        }

        Assert.True(assertedReadyLowAtT2);
        Assert.Equal("TW", cpu.LastBusCycle.TState);
        Assert.Equal("R--", cpu.LastBusCycle.MemoryStatus);
        long cyclesAtFirstWait = cpu.Cycles;
        int pendingAtFirstWait = cpu.Pending;

        cpu.Tick();
        Assert.Equal("TW", cpu.LastBusCycle.TState);
        Assert.Equal(cyclesAtFirstWait + 1, cpu.Cycles);
        Assert.Equal(pendingAtFirstWait, cpu.Pending);

        cpu.ReadyLevel = true;
        cpu.Tick();
        Assert.NotEqual("TW", cpu.LastBusCycle.TState);

        for (int cycle = 0; cycle < 50 && cpu.Pending != 0; cycle++)
        {
            cpu.Tick();
        }

        Assert.Equal((byte)0xA6, memory.Read(0x20100));
        Assert.Equal((byte)0x00, memory.Read(0x20101));
    }

    [Fact]
    public void Tick_WithEmptyPrefetchQueue_Throws()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));

        Assert.Throws<InvalidOperationException>(() => cpu.Tick());
    }

    [Fact]
    public void LockPrefix_AssertsMaximumModeLockForFollowingInstructionOnly()
    {
        var cpu = new Intel8086Core(new FlatAddressMemory(0x100000));
        cpu.State.Queue = [0xF0, 0x90, 0x90, 0, 0, 0];
        cpu.State.QueueCount = 3;
        ushort initialFlags = cpu.State.Flags;

        for (int cycle = 0; cycle < 5; cycle++)
        {
            cpu.Tick();

            Assert.True(cpu.LastBusCycle.LockActive, $"LOCK must remain asserted through locked NOP T-state {cycle + 1}.");
        }

        Assert.Equal((ushort)2, cpu.State.IP);
        Assert.Equal((byte)0x90, cpu.State.PendingQueue[0]);
        Assert.Equal(initialFlags, cpu.State.Flags);
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void LockPrefix_RemainsAssertedAcrossLockedMemoryReadModifyWrite()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0100, 0x7F);
        memory.Write(0x0006, 0x90);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xFE, 0x06, 0x00, 0x01, 0x90];
        cpu.State.QueueCount = 6;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 40 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((byte)0x80, memory.Read(0x0100));
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void LockPrefix_DoesNotRepeatStringOperationWithoutRepPrefix()
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0200, 0x5A);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xA4, 0x90, 0, 0, 0];
        cpu.State.QueueCount = 3;
        cpu.State.SI = 0x0200;
        cpu.State.DI = 0x0300;
        cpu.State.CX = 2;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 40 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((byte)0x5A, memory.Read(0x0300));
        Assert.Equal((byte)0x00, memory.Read(0x0301));
        Assert.Equal((ushort)0x0201, cpu.State.SI);
        Assert.Equal((ushort)0x0301, cpu.State.DI);
        Assert.Equal((ushort)2, cpu.State.CX);
        Assert.Equal(0, cpu.Pending);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void LockPrefix_WithRepMovsb_HoldsLockAcrossTheWholeStringBlock(int queueCount)
    {
        var memory = new FlatAddressMemory(0x100000);
        memory.Write(0x0200, 0x11);
        memory.Write(0x0201, 0x22);
        memory.Write(0x0202, 0x33);
        var cpu = new Intel8086Core(memory);
        cpu.State.Queue = [0xF0, 0xF3, 0xA4, 0x90, 0x90, 0x90];
        cpu.State.QueueCount = queueCount;
        cpu.State.SI = 0x0200;
        cpu.State.DI = 0x0300;
        cpu.State.CX = 3;
        var lockSamples = new List<bool>();

        for (int cycle = 0; cycle < 100 && (cycle == 0 || cpu.Pending != 0); cycle++)
        {
            cpu.Tick();
            lockSamples.Add(cpu.LastBusCycle.LockActive);
        }

        Assert.NotEmpty(lockSamples);
        Assert.All(lockSamples, Assert.True);
        Assert.Equal((ushort)0, cpu.State.CX);
        Assert.Equal((ushort)0x0203, cpu.State.SI);
        Assert.Equal((ushort)0x0303, cpu.State.DI);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, new[] { memory.Read(0x0300), memory.Read(0x0301), memory.Read(0x0302) });
        Assert.Equal(0, cpu.Pending);

        cpu.Tick();

        Assert.False(cpu.LastBusCycle.LockActive, "LOCK must be deasserted before the next instruction executes.");
    }

    [Fact]
    public void Manifest_ListsOnlyActiveSamplesAndEachFileHasDeclaredCount()
    {
        string vectorsDirectory = Path.Combine(AppContext.BaseDirectory, "Vectors");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(vectorsDirectory, "manifest.json")));
        Assert.Equal("e71c68d215a6bb8c356bd4cb3842de3bef345ca9", manifest.RootElement.GetProperty("sourceSha").GetString());

        foreach (JsonElement entry in manifest.RootElement.GetProperty("active").EnumerateArray())
        {
            string file = entry.GetProperty("file").GetString()!;
            using var sample = JsonDocument.Parse(File.ReadAllText(Path.Combine(vectorsDirectory, file)));
            Assert.Equal(entry.GetProperty("records").GetInt32(), sample.RootElement.GetArrayLength());
        }
    }
}
