namespace VibeEmulator.Memory;

/// <summary>
/// Obserwator dostepów do magistrali. Służy do śledzenia wykonania (tracer, debugger);
/// w normalnej pracy pozostaje <c>null</c> i kosztuje jedno sprawdzenie.
/// </summary>
public interface IBusObserver
{
    /// <summary>Wywoływane po każdym odczycie lub zapisie.</summary>
    void OnAccess(bool write, ushort address, byte value);
}
