namespace VibeEmulator.Memory;

/// <summary>
/// Magistrala, z której można czytać bez skutków ubocznych (podgląd diagnostyczny).
/// </summary>
/// <remarks>
/// Kontrakt: <see cref="Peek"/> zwraca to, co zwróciłby <see cref="IMemoryBus.Read"/> pod tym adresem
/// w tym samym stanie, ale nie zmienia stanu magistrali ani urządzeń i nie powiadamia obserwatorów.
/// Przykład: <c>C64Bus.Peek</c> nie rusza open bus i nie woła <c>Observer</c>.
/// </remarks>
public interface IPeekableBus
{
    /// <summary>Odczyt bez skutków ubocznych.</summary>
    byte Peek(ushort address);
}
