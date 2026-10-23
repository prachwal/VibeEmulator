namespace VibeEmulator.Intel8086VectorRunner;

public readonly record struct VectorRunResult(bool Passed, int TStates, string? Difference);
