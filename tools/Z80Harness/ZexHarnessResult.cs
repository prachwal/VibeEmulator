namespace VibeEmulator.Z80Harness;

public enum ZexHarnessOutcome
{
    Pass,
    Fail,
    Limit,
}

public sealed record ZexHarnessResult(
    ZexHarnessOutcome Outcome,
    long TStates,
    TimeSpan Elapsed,
    string ProgramOutput,
    bool OutputTruncated,
    string? Error)
{
    public int ExitCode => Outcome switch
    {
        ZexHarnessOutcome.Pass => 0,
        ZexHarnessOutcome.Fail => 2,
        ZexHarnessOutcome.Limit => 3,
        _ => 4,
    };
}
