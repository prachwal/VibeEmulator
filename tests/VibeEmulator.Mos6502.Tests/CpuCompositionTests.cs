using VibeEmulator.Memory;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Mos6502.Tests;

public class CpuCompositionTests
{
    [Fact]
    public void MemoryMap_RoutesAccessToMappedRegion()
    {
        var ram = new FlatMemory(0x2000);
        var rom = new FlatMemory(0x2000);
        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x1FFF, ram)
            .Map(0x8000, 0x9FFF, rom)
            .Build();

        map.Write(0x0005, 0xAB);
        map.Write(0x8001, 0xCD);

        Assert.Equal((byte)0xAB, ram.Read(0x0005));
        Assert.Equal((byte)0xCD, rom.Read(0x0001));
        Assert.Equal((byte)0xAB, map.Read(0x0005));
        Assert.Equal((byte)0xCD, map.Read(0x8001));
    }

    [Fact]
    public void MemoryMapBuilder_OverlappingRegions_Throws()
    {
        MemoryMapBuilder builder = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x1FFF, new FlatMemory(0x2000));

        Assert.Throws<InvalidOperationException>(
            () => builder.Map(0x1000, 0x2FFF, new FlatMemory(0x2000)));
    }

    [Fact]
    public void MemoryMapBuilder_EndBelowStart_Throws()
    {
        MemoryMapBuilder builder = MemoryMap.CreateBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.Map(0x2000, 0x1000, new FlatMemory(0x2000)));
    }

    [Fact]
    public void MemoryMap_UnmappedRead_Throws()
    {
        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x1FFF, new FlatMemory(0x2000))
            .Build();

        Assert.Throws<InvalidOperationException>(() => map.Read(0x8000));
    }

    [Fact]
    public void MemoryMap_UnmappedWrite_Throws()
    {
        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x1FFF, new FlatMemory(0x2000))
            .Build();

        Assert.Throws<InvalidOperationException>(() => map.Write(0x8000, 0x01));
    }

    [Fact]
    public void GenericCpu_WithCompositeBus_ExecutesAcrossRegions()
    {
        var ram = new FlatMemory(0x2000);
        var rom = new FlatMemory(0x2000);
        rom.Load(0x0000, [0xA9, 0x10, 0x8D, 0x00, 0x02, 0x00]); // LDA #$10; STA $0200; BRK

        MemoryMap map = MemoryMap.CreateBuilder()
            .Map(0x0000, 0x1FFF, ram)
            .Map(0x8000, 0x9FFF, rom)
            .Build();

        Cpu<MemoryMap> cpu = Cpu6502.Create(map);
        cpu.Reset(0x8000);
        cpu.Step(); // LDA #$10
        cpu.Step(); // STA $0200

        Assert.Equal((byte)0x10, cpu.State.A);
        Assert.Equal((byte)0x10, ram.Read(0x0200));
        Assert.Equal((ushort)0x8005, cpu.State.PC);
    }

    [Fact]
    public void Builder_ComposesCustomInstructionSet()
    {
        var memory = new FlatMemory();
        memory.Load(0x0200, [0xA9, 0x05]); // LDA #$05

        Cpu<FlatMemory> cpu = new CpuBuilder<FlatMemory>(memory)
            .WithGroup(OpcodeGroups.ControlFlow)
            .With(0xA9, "LDA", AddressingMode.Immediate, Operations.Lda)
            .Build();

        cpu.Reset(0x0200);
        cpu.Step();

        Assert.Equal((byte)0x05, cpu.State.A);
        Assert.True(cpu.Table.IsDefined(0xA9));
        Assert.True(cpu.Table.IsDefined(0xEA));
    }

    [Fact]
    public void Builder_WithoutOpcodeTable_StillBuildsEmptyTable()
    {
        var cpu = new CpuBuilder<FlatMemory>(new FlatMemory()).Build();

        Assert.Equal(0, cpu.Table.DefinedCount);
    }

    [Fact]
    public void TwoCpus_DoNotShareRegisterState()
    {
        Cpu<FlatMemory> first = Cpu6502.Create(new FlatMemory());
        Cpu<FlatMemory> second = Cpu6502.Create(new FlatMemory());

        first.State.A = 0x11;
        first.State.PC = 0x1234;

        Assert.Equal((byte)0, second.State.A);
        Assert.Equal((ushort)0, second.State.PC);
    }

    [Fact]
    public void CustomBus_ReceivesReadsAndWritesFromCpu()
    {
        var bus = new RecordingBus();
        bus.Load(0x0200, [0xA9, 0x42, 0x8D, 0x00, 0x03]); // LDA #$42; STA $0300

        Cpu<RecordingBus> cpu = Cpu6502.Create(bus);
        cpu.Reset(0x0200);
        cpu.Step();
        cpu.Step();

        Assert.Equal((byte)0x42, bus.Read(0x0300));
        Assert.True(bus.ReadCount >= 4);
        Assert.Equal(1, bus.WriteCount);
    }

    /// <summary>Magistrala zliczająca dostępy — dowód, że CPU zależy tylko od interfejsu.</summary>
    private sealed class RecordingBus : IMemoryBus
    {
        private readonly byte[] _memory = new byte[0x10000];

        public int ReadCount { get; private set; }

        public int WriteCount { get; private set; }

        public byte Read(ushort address)
        {
            ReadCount++;
            return _memory[address];
        }

        public void Write(ushort address, byte value)
        {
            WriteCount++;
            _memory[address] = value;
        }

        public void Load(ushort address, ReadOnlySpan<byte> data) => data.CopyTo(_memory.AsSpan(address));
    }
}
