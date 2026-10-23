namespace VibeEmulator.Intel8086;

/// <summary>Rejestr segmentowy używany przez prefiks nadpisania segmentu.</summary>
public enum Intel8086SegmentOverride : byte
{
    None,
    Es,
    Cs,
    Ss,
    Ds,
}
