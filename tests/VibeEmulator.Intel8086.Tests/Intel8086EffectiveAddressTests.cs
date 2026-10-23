using VibeEmulator.Intel8086;

namespace VibeEmulator.Intel8086.Tests;

public sealed class Intel8086EffectiveAddressTests
{
    [Theory]
    [InlineData(0, 0x300)]
    [InlineData(1, 0x400)]
    [InlineData(2, 0x400)]
    [InlineData(3, 0x500)]
    [InlineData(4, 0x100)]
    [InlineData(5, 0x200)]
    [InlineData(6, 0x000)]
    [InlineData(7, 0x200)]
    public void Decode_Mod00RegisterPair_ComputesBaseOffset(int rm, int expectedOffset)
    {
        Intel8086State state = CreateState();

        Intel8086EffectiveAddress result = Intel8086EffectiveAddressDecoder.Decode((byte)rm, in state);

        Assert.Equal((ushort)expectedOffset, result.Offset);
        Assert.Equal((ushort)0x1000, result.Segment);
        Assert.Equal((uint)(0x10000 + expectedOffset), result.PhysicalAddress);
    }

    [Theory]
    [InlineData(0, 0x2FD)]
    [InlineData(1, 0x3FD)]
    [InlineData(2, 0x3FD)]
    [InlineData(3, 0x4FD)]
    [InlineData(4, 0x00FD)]
    [InlineData(5, 0x01FD)]
    [InlineData(6, 0x02FD)]
    [InlineData(7, 0x01FD)]
    public void Decode_Mod01SignExtendsDisplacementAndWrapsOffset(int rm, int expectedOffset)
    {
        Intel8086State state = CreateState();

        Intel8086EffectiveAddress result = Intel8086EffectiveAddressDecoder.Decode((byte)(0x40 | rm), in state, 0x00FD);

        Assert.Equal((ushort)expectedOffset, result.Offset);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void Decode_SelectsStackSegmentOnlyForBpBasedForms(int rm, bool usesStackSegment)
    {
        Intel8086State state = CreateState();

        Intel8086EffectiveAddress result = Intel8086EffectiveAddressDecoder.Decode((byte)(0x80 | rm), in state, 0);

        Assert.Equal(usesStackSegment ? state.SS : state.DS, result.Segment);
    }

    [Fact]
    public void Decode_Mod00Rm6UsesDirectDisplacementAndDs()
    {
        Intel8086State state = CreateState();

        Intel8086EffectiveAddress result = Intel8086EffectiveAddressDecoder.Decode(0x06, in state, 0xABCD);

        Assert.Equal((ushort)0xABCD, result.Offset);
        Assert.Equal(state.DS, result.Segment);
    }

    [Theory]
    [InlineData(Intel8086SegmentOverride.Es, 0x4000)]
    [InlineData(Intel8086SegmentOverride.Cs, 0x5000)]
    [InlineData(Intel8086SegmentOverride.Ss, 0x2000)]
    [InlineData(Intel8086SegmentOverride.Ds, 0x1000)]
    public void Decode_OverrideReplacesDefaultSegment(Intel8086SegmentOverride segmentOverride, int expectedSegment)
    {
        Intel8086State state = CreateState();

        Intel8086EffectiveAddress result = Intel8086EffectiveAddressDecoder.Decode(0x02, in state, 0, segmentOverride);

        Assert.Equal((ushort)expectedSegment, result.Segment);
    }

    [Fact]
    public void Decode_Mod11RejectsRegisterOperand()
    {
        Intel8086State state = CreateState();

        Assert.Throws<ArgumentException>(() => Intel8086EffectiveAddressDecoder.Decode(0xC0, in state));
    }

    private static Intel8086State CreateState() => new()
    {
        AX = 0x0101,
        BX = 0x0200,
        SI = 0x0100,
        DI = 0x0200,
        BP = 0x0300,
        CS = 0x5000,
        SS = 0x2000,
        DS = 0x1000,
        ES = 0x4000,
        Queue = new byte[6],
    };
}
