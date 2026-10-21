namespace VibeEmulator.Cpu.Abstractions;

/// <summary>
/// Piny przerwań rdzenia — opcjonalny kontrakt implementowany przez rdzenie obsługujące linie żądań
/// przerwań. Poziomy ustawia maszyna (np. <c>C64Machine</c>) raz na cykl, przed <c>ICpuCore.Tick()</c>;
/// rdzeń sam próbkuje IRQ na granicy instrukcji i wykrywa zbocze NMI. Kontrakt jest neutralny
/// rodzinowo — nie definiuje wektorów ani priorytetów, tylko poziomy linii.
/// </summary>
public interface IInterruptLines
{
    /// <summary>Linia IRQ (aktywny poziom niski na magistrali); <c>true</c> = przerwanie zgłoszone.</summary>
    bool IrqLevel { get; set; }

    /// <summary>Linia NMI (aktywny poziom niski); rdzeń wykrywa zbocze opadające (wysoki → niski).</summary>
    bool NmiLevel { get; set; }
}
