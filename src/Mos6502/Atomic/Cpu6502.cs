using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Punkt kompozycji: gotowy procesor z pełnym standardowym zestawem instrukcji.
/// <para>
/// To jest <b>model atomowy</b> (<c>Step()</c> wykonuje całą instrukcję naraz, 29 opkodów, bez weryfikacji
/// cyklowej). Warianty rodziny dla ścieżki cycle-exact — NMOS 6502, 65C02 i Ricoh 2A03 (NES) — są w
/// <see cref="CpuVariants"/>; tam też mieszka pełna tabela 256 opkodów.
/// </para>
/// </summary>
public static class Cpu6502
{
    public static OpcodeTable CreateTable() =>
        new OpcodeTableBuilder()
            .Compose(OpcodeGroups.Standard)
            .Build();

    public static Cpu<TBus> Create<TBus>(TBus bus) where TBus : IMemoryBus => new(bus, CreateTable());
}
