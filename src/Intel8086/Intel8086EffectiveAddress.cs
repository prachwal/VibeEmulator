namespace VibeEmulator.Intel8086;

/// <summary>Adres segmentowy i fizyczny operandu wyznaczony z bajtu ModR/M.</summary>
public readonly record struct Intel8086EffectiveAddress(ushort Segment, ushort Offset, uint PhysicalAddress);

/// <summary>Oblicza 16-bitowy adres efektywny ModR/M dla trybu adresowania 8086.</summary>
public static class Intel8086EffectiveAddressDecoder
{
    public static Intel8086EffectiveAddress Decode(
        byte modRm,
        in Intel8086State state,
        ushort displacement = 0,
        Intel8086SegmentOverride segmentOverride = Intel8086SegmentOverride.None)
    {
        int mod = modRm >> 6;
        int rm = modRm & 7;
        if (mod == 3)
        {
            throw new ArgumentException("ModR/M mod=11 wskazuje rejestr, a nie adres efektywny.", nameof(modRm));
        }

        ushort offset = mod == 0 && rm == 6
            ? displacement
            : unchecked((ushort)(GetBaseOffset(rm, in state) + GetDisplacement(mod, displacement)));
        bool usesStackSegment = mod != 0 && rm is 2 or 3 or 6;
        ushort defaultSegment = usesStackSegment ? state.SS : state.DS;
        ushort segment = GetSegment(segmentOverride, in state, defaultSegment);
        uint physicalAddress = (uint)(((segment << 4) + offset) & 0xFFFFF);
        return new Intel8086EffectiveAddress(segment, offset, physicalAddress);
    }

    private static int GetBaseOffset(int rm, in Intel8086State state) => rm switch
    {
        0 => state.BX + state.SI,
        1 => state.BX + state.DI,
        2 => state.BP + state.SI,
        3 => state.BP + state.DI,
        4 => state.SI,
        5 => state.DI,
        6 => state.BP,
        7 => state.BX,
        _ => throw new ArgumentOutOfRangeException(nameof(rm)),
    };

    private static int GetDisplacement(int mod, ushort displacement) => mod switch
    {
        0 => 0,
        1 => unchecked((sbyte)displacement),
        2 => unchecked((short)displacement),
        _ => throw new ArgumentOutOfRangeException(nameof(mod)),
    };

    private static ushort GetSegment(Intel8086SegmentOverride segmentOverride, in Intel8086State state, ushort defaultSegment) => segmentOverride switch
    {
        Intel8086SegmentOverride.None => defaultSegment,
        Intel8086SegmentOverride.Es => state.ES,
        Intel8086SegmentOverride.Cs => state.CS,
        Intel8086SegmentOverride.Ss => state.SS,
        Intel8086SegmentOverride.Ds => state.DS,
        _ => throw new ArgumentOutOfRangeException(nameof(segmentOverride)),
    };
}
