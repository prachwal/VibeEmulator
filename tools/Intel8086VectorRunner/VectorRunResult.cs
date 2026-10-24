namespace VibeEmulator.Intel8086VectorRunner;

public enum VectorRunStatus
{
    Passed,
    Skipped,
    Mismatched,
    TimedOut,
    ParseError,
}

public readonly record struct VectorRunResult(VectorRunStatus Status, int TStates, string? Difference)
{
    public VectorRunResult(bool passed, int tStates, string? difference)
        : this(passed ? VectorRunStatus.Passed : VectorRunStatus.Mismatched, tStates, difference)
    {
    }

    public bool Passed => Status == VectorRunStatus.Passed;
}
