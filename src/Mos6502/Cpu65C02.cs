using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Punkt kompozycji wariantu CMOS 65C02 dla modelu atomowego: standardowy zestaw plus STZ, INC A, DEC A.
/// Model atomowy nie rozróżnia stylu RMW (to rola rdzenia cyklowego <see cref="StateMachineCpuCore"/>).
/// </summary>
public static class Cpu65C02
{
    public static OpcodeTable CreateTable() =>
        new OpcodeTableBuilder()
            .Compose(OpcodeGroups.Standard)
            .Compose(OpcodeGroups.Cmos65C02Additions)
            .Build();

    public static Cpu<TBus> Create<TBus>(TBus bus) where TBus : IMemoryBus => new(bus, CreateTable());
}
