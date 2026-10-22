using VibeEmulator.Mos6502;

namespace VibeEmulator.Mos6502.Tests;

public class OpcodeTableCompositionTests
{
    [Fact]
    public void Define_AddsInstruction()
    {
        var builder = new OpcodeTableBuilder();

        OpcodeTable table = builder
            .Define(0xEA, "NOP", AddressingMode.Implied, Operations.Nop)
            .Build();

        Assert.True(table.IsDefined(0xEA));
        Assert.Equal("NOP", table[0xEA].Mnemonic);
        Assert.Equal(1, table.DefinedCount);
    }

    [Fact]
    public void Define_DuplicateOpcode_Throws()
    {
        var builder = new OpcodeTableBuilder();
        builder.Define(0xEA, "NOP", AddressingMode.Implied, Operations.Nop);

        Assert.Throws<InvalidOperationException>(
            () => builder.Define(0xEA, "AAA", AddressingMode.Implied, Operations.Nop));
    }

    [Fact]
    public void Define_EmptyMnemonic_Throws()
    {
        var builder = new OpcodeTableBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.Define(0xEA, " ", AddressingMode.Implied, Operations.Nop));
    }

    [Fact]
    public void Define_NullOperation_Throws()
    {
        var builder = new OpcodeTableBuilder();

        Assert.Throws<ArgumentNullException>(
            () => builder.Define(0xEA, "NOP", AddressingMode.Implied, null!));
    }

    [Fact]
    public void Compose_AddsWholeGroup()
    {
        var builder = new OpcodeTableBuilder();

        OpcodeTable table = builder.Compose(OpcodeGroups.ControlFlow).Build();

        Assert.True(table.IsDefined(0xEA)); // NOP
        Assert.True(table.IsDefined(0x4C)); // JMP
        Assert.True(table.IsDefined(0x00)); // BRK
        Assert.Equal(7, table.DefinedCount);
    }

    [Fact]
    public void Compose_Null_Throws()
    {
        var builder = new OpcodeTableBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Compose(null!));
    }

    [Fact]
    public void Build_ReturnsSnapshot_UnaffectedByLaterChanges()
    {
        var builder = new OpcodeTableBuilder();
        builder.Define(0xEA, "NOP", AddressingMode.Implied, Operations.Nop);

        OpcodeTable table = builder.Build();
        builder.Define(0x4C, "JMP", AddressingMode.Absolute, Operations.Jmp);

        Assert.True(table.IsDefined(0xEA));
        Assert.False(table.IsDefined(0x4C));
    }

    [Fact]
    public void Get_UndefinedOpcode_Throws()
    {
        OpcodeTable table = new OpcodeTableBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => table.Get(0xFF));
        Assert.False(table.IsDefined(0xFF));
    }

    [Fact]
    public void Standard_ComposesAllGroups()
    {
        OpcodeTable table = new OpcodeTableBuilder()
            .Compose(OpcodeGroups.Standard)
            .Build();

        // LoadStore 10 + Arithmetic 12 + ControlFlow 7.
        Assert.Equal(29, table.DefinedCount);
        Assert.Equal("LDA", table[0xA9].Mnemonic);
        Assert.Equal("ADC", table[0x69].Mnemonic);
        Assert.Equal("JMP", table[0x4C].Mnemonic);
    }

    [Fact]
    public void Cpu6502_CreateTable_ContainsExpectedInstructions()
    {
        OpcodeTable table = Cpu6502.CreateTable();

        Assert.Equal("LDA", table[0xA9].Mnemonic);
        Assert.Equal(AddressingMode.Immediate, table[0xA9].Mode);
        Assert.Equal("STA", table[0x8D].Mnemonic);
        Assert.Equal(AddressingMode.Absolute, table[0x8D].Mode);
        Assert.Equal("INX", table[0xE8].Mnemonic);
        Assert.Equal(AddressingMode.Implied, table[0xE8].Mode);
    }

    [Fact]
    public void Cpu2A03_CreateTable_MatchesCpu6502()
    {
        // Model atomowy liczy ADC/SBC wyłącznie binarnie, więc brak trybu dziesiętnego w 2A03
        // niczego tu nie zmienia: tabela jest ta sama co w Cpu6502. Różnicę modeluje
        // CpuVariants.Nes2A03 (właściwość HasDecimalMode) — sprawdzenie w CpuVariantTests.
        OpcodeTable nes = Cpu2A03.CreateTable();
        OpcodeTable nmos = Cpu6502.CreateTable();

        Assert.Equal(29, nes.DefinedCount);
        Assert.Equal(nmos.DefinedCount, nes.DefinedCount);

        for (int opcode = 0; opcode < 256; opcode++)
        {
            Assert.Equal(nmos.IsDefined((byte)opcode), nes.IsDefined((byte)opcode));

            if (nmos.IsDefined((byte)opcode))
            {
                Assert.Equal(nmos[(byte)opcode].Mnemonic, nes[(byte)opcode].Mnemonic);
                Assert.Equal(nmos[(byte)opcode].Mode, nes[(byte)opcode].Mode);
            }
        }
    }

    [Fact]
    public void Cpu2A03_Create_ProducesWorkingCpu()
    {
        var memory = new VibeEmulator.Memory.FlatMemory();
        memory.Load(0x0200, [0xA9, 0x42, 0xE8]);
        Cpu<VibeEmulator.Memory.FlatMemory> cpu = Cpu2A03.Create(memory);
        cpu.Reset(0x0200);

        cpu.Step();
        cpu.Step();

        Assert.Equal((byte)0x42, cpu.State.A);
        Assert.Equal((byte)0x01, cpu.State.X);
        Assert.Equal((ushort)0x0203, cpu.State.PC);
    }

    [Fact]
    public void Groups_AreIndependent()
    {
        OpcodeTable onlyLoadStore = new OpcodeTableBuilder()
            .Compose(OpcodeGroups.LoadStore)
            .Build();

        Assert.True(onlyLoadStore.IsDefined(0xA9));
        Assert.False(onlyLoadStore.IsDefined(0x69));
        Assert.False(onlyLoadStore.IsDefined(0x4C));
    }
}
