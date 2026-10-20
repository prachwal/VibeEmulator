namespace VibeEmulator.Core.Cpu;

/// <summary>Punkt kompozycji: gotowy procesor z pełnym standardowym zestawem instrukcji.</summary>
public static class Cpu6502
{
    public static OpcodeTable CreateTable() =>
        new OpcodeTableBuilder()
            .Compose(OpcodeGroups.Standard)
            .Build();

    public static Cpu<TBus> Create<TBus>(TBus bus) where TBus : IMemoryBus => new(bus, CreateTable());
}
