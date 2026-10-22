using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Punkt kompozycji wariantu Ricoh 2A03 (NES) dla modelu atomowego: standardowy zestaw NMOS 6502.
/// <para>
/// <b>Dlaczego tabela jest identyczna z <see cref="Cpu6502"/></b>: jedyna różnica 2A03 wobec NMOS 6502 to
/// **brak trybu dziesiętnego**, a model atomowy liczy <c>ADC</c>/<c>SBC</c> wyłącznie binarnie
/// (<see cref="Operations.Adc"/>, <see cref="Operations.Sbc"/>) — nie ma tu gałęzi BCD do wyłączenia.
/// Flaga <c>D</c> nadal istnieje i <c>CLD</c>/<c>SED</c> ją zmieniają, tak jak na układzie.
/// </para>
/// <para>
/// Różnica jest modelowana w rdzeniu cyklowym: <see cref="CpuVariants.Nes2A03"/> z właściwością
/// <see cref="CpuVariant.HasDecimalMode"/>. Ten typ istnieje po to, żeby wołający mógł nazwać wariant
/// (<c>Cpu2A03.Create(bus)</c>) bez sięgania po <see cref="Cpu6502"/>, i żeby dokumentacja nie milczała
/// o braku trybu dziesiętnego. Model atomowy i tak nie jest zgodny cyklowo — nie używaj go do emulacji NES.
/// </para>
/// </summary>
public static class Cpu2A03
{
    public static OpcodeTable CreateTable() =>
        new OpcodeTableBuilder()
            .Compose(OpcodeGroups.Standard)
            .Build();

    public static Cpu<TBus> Create<TBus>(TBus bus) where TBus : IMemoryBus => new(bus, CreateTable());
}
