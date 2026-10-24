namespace VibeEmulator.Intel8086;

/// <summary>Surowe wyjścia jednej fazy T-state modelu SingleStepTests/8086.</summary>
public readonly record struct Intel8086BusCycle(
    byte PinBits,
    uint MultiplexedBus,
    string SegmentStatus,
    string MemoryStatus,
    string IoStatus,
    byte Bhe,
    ushort Data,
    string BusStatus,
    string TState,
    char QueueStatus,
    byte QueueData)
{
    /// <summary>True, gdy wyjście LOCK jest aktywne w trybie Maximum Mode.</summary>
    public bool LockActive { get; init; }
}
