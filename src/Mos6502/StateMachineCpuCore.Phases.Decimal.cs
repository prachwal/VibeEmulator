namespace VibeEmulator.Mos6502;

/// <summary>Fazy trybu dziesiętnego (etap G) — część <c>partial</c> rdzenia.</summary>
public sealed partial class StateMachineCpuCore
{
    /// <summary>
    /// Dodatkowy cykl 65C02 dla ADC/SBC z D=1 (wektory wdc65c02; dana wariantu <c>DecimalExtraCycle</c>).
    /// Tryby pamięciowe powtarzają odczyt operandu; tryb immediate czyta pod stałym adresem per akcja
    /// (wektory: $7F dla ADC, $00 dla SBC — stała implementacji referencyjnej, nie zachowanie sprzętu;
    /// prawdziwy 65C02 wykonuje w tym cyklu odczyt wewnętrzny, którego adres nie jest obserwowalny).
    /// </summary>
    private void ExecuteDecimalExtra()
    {
        _ = _bus.Read(_entry.Mode == AddressingMode.Immediate
            ? (ushort)(_entry.Action == CpuAction.Adc ? 0x7F : 0x00)
            : _address);
        _phase = Phase.Fetch;
    }
}
