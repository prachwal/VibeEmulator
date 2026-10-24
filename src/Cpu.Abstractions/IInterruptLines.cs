namespace VibeEmulator.Cpu.Abstractions;

/// <summary>
/// Piny przerwań rdzenia — opcjonalny kontrakt implementowany przez rdzenie obsługujące linie żądań
/// przerwań. Poziomy ustawia maszyna (np. <c>C64Machine</c>) raz na cykl, przed <c>ICpuCore.Tick()</c>;
/// rdzeń sam próbkuje linie zgodnie z wymaganiami swojej rodziny. Wartość <see langword="true"/>
/// oznacza zgłoszony sygnał; warstwa maszyny normalizuje polaryzację fizycznego pinu. Kontrakt
/// nie definiuje zboczy, cyklu próbkowania, wektorów ani priorytetów.
/// </summary>
public interface IInterruptLines
{
    /// <summary>Żądanie IRQ; <c>true</c> = sygnał zgłoszony, z polaryzacją fizyczną znormalizowaną przez maszynę.</summary>
    bool IrqLevel { get; set; }

    /// <summary>Żądanie NMI; <c>true</c> = sygnał zgłoszony, z polaryzacją fizyczną znormalizowaną przez maszynę.</summary>
    bool NmiLevel { get; set; }
}
