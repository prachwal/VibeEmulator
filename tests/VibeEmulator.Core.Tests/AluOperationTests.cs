using VibeEmulator.Core.Cpu;

namespace VibeEmulator.Core.Tests;

public class AluOperationTests
{
    [Theory]
    [InlineData(0x00, true, false)]
    [InlineData(0x01, false, false)]
    [InlineData(0x80, false, true)]
    [InlineData(0xFF, false, true)]
    public void Lda_SetsZeroAndNegativeFlags(byte value, bool zero, bool negative)
    {
        Cpu<FlatMemory> cpu = Run(0x0200, [0xA9, value]);

        Assert.Equal(value, cpu.State.A);
        Assert.Equal(zero, cpu.State.P.Has(StatusFlags.Zero));
        Assert.Equal(negative, cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Adc_WithCarryIn_AddsOne()
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        cpu.State.A = 0x10;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, true);
        memory.Load(0x0200, [0x69, 0x05]); // ADC #$05

        cpu.Step();

        Assert.Equal((byte)0x16, cpu.State.A);
        Assert.False(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Adc_SumAboveByte_SetsCarryAndZero()
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        cpu.State.A = 0xFF;
        memory.Load(0x0200, [0x69, 0x01]); // ADC #$01

        cpu.Step();

        Assert.Equal((byte)0x00, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Zero));
        Assert.False(cpu.State.P.Has(StatusFlags.Overflow));
    }

    [Fact]
    public void Adc_SignedOverflow_SetsOverflowFlag()
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        cpu.State.A = 0x7F; // +127
        memory.Load(0x0200, [0x69, 0x01]); // ADC #$01

        cpu.Step();

        Assert.Equal((byte)0x80, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Overflow));
        Assert.False(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Sbc_WithoutCarry_SubtractsBorrow()
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        cpu.State.A = 0x00;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, false);
        memory.Load(0x0200, [0xE9, 0x01]); // SBC #$01

        cpu.Step();

        Assert.Equal((byte)0xFE, cpu.State.A);
        Assert.False(cpu.State.P.Has(StatusFlags.Carry));
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Sbc_WithCarry_SetsCarryWhenNoBorrow()
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        cpu.State.A = 0x10;
        cpu.State.P = cpu.State.P.With(StatusFlags.Carry, true);
        memory.Load(0x0200, [0xE9, 0x01]); // SBC #$01

        cpu.Step();

        Assert.Equal((byte)0x0F, cpu.State.A);
        Assert.True(cpu.State.P.Has(StatusFlags.Carry));
    }

    [Fact]
    public void Inc_WrapsToZero_AndSetsZeroFlag()
    {
        var memory = new FlatMemory();
        memory.Write(0x0300, 0xFF);
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        memory.Load(0x0200, [0xEE, 0x00, 0x03]); // INC $0300

        cpu.Step();

        Assert.Equal((byte)0x00, memory.Read(0x0300));
        Assert.True(cpu.State.P.Has(StatusFlags.Zero));
    }

    [Fact]
    public void Dec_Underflows_AndSetsNegativeFlag()
    {
        var memory = new FlatMemory();
        memory.Write(0x0300, 0x00);
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(0x0200);
        memory.Load(0x0200, [0xCE, 0x00, 0x03]); // DEC $0300

        cpu.Step();

        Assert.Equal((byte)0xFF, memory.Read(0x0300));
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Dex_DecrementsX()
    {
        Cpu<FlatMemory> cpu = Run(0x0200, [0xCA], cpu => cpu.State.X = 0x00);

        Assert.Equal((byte)0xFF, cpu.State.X);
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Inx_IncrementsX()
    {
        Cpu<FlatMemory> cpu = Run(0x0200, [0xE8], cpu => cpu.State.X = 0x7F);

        Assert.Equal((byte)0x80, cpu.State.X);
        Assert.True(cpu.State.P.Has(StatusFlags.Negative));
    }

    [Fact]
    public void Tax_CopiesAccumulatorToX()
    {
        Cpu<FlatMemory> cpu = Run(0x0200, [0xAA], cpu => cpu.State.A = 0x2A);

        Assert.Equal((byte)0x2A, cpu.State.X);
    }

    [Fact]
    public void Jmp_SetsProgramCounterToAbsoluteAddress()
    {
        Cpu<FlatMemory> cpu = Run(0x0200, [0x4C, 0x00, 0x03]); // JMP $0300

        Assert.Equal((ushort)0x0300, cpu.State.PC);
    }

    /// <summary>Ustawia program, opcjonalnie rejestry przez referencję i wykonuje jeden krok.</summary>
    private static Cpu<FlatMemory> Run(ushort origin, byte[] program, Action<Cpu<FlatMemory>>? arrange = null)
    {
        var memory = new FlatMemory();
        memory.Load(origin, program);
        var cpu = Cpu6502.Create(memory);
        cpu.Reset(origin);

        arrange?.Invoke(cpu);
        cpu.Step();

        return cpu;
    }
}
