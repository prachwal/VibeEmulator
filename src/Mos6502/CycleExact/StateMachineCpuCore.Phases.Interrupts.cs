namespace VibeEmulator.Mos6502;

/// <summary>
/// Sekwencje przerwań (etap F): BRK, IRQ i NMI — siedem cykli każda, zgodnie z
/// <c>docs/cpu/interrupts-and-reset.md</c> i wektorami SingleStepTests (BRK walidowany próbką 00.json).
/// Część <c>partial</c> rdzenia — dispatch w <c>StateMachineCpuCore.Phases.cs</c>.
/// </summary>
/// <remarks>
/// Wspólna sekwencja: cykl 1 = odczyt opkodu (BRK przez własny Fetch z PC++; IRQ/NMI przez
/// <c>InterruptOpcodeRead</c> bez PC++), cykl 2 = bajt sygnatury odrzucony (PC++ tylko dla BRK),
/// cykle 3–5 = pushy PCH, PCL i P (z B=1 dla BRK, B=0 dla IRQ/NMI), cykle 6–7 = odczyt wektora
/// z ustawieniem I (i wyczyszczeniem D w 65C02). Sekwencje przerwań same nie próbkują przerwań.
/// </remarks>
public sealed partial class StateMachineCpuCore
{
    /// <summary>
    /// Rozpoczyna sekwencję przerwań. BRK wchodzi po własnym Fetch (PC już za opkod, cykl 1 wykonany);
    /// IRQ/NMI robią cykl 1 tutaj — odrzucony odczyt opkodu pod PC bez inkrementacji — bo ich Fetch
    /// nie pobiera opkodu. Cykl 2 (sygnatura) jest wspólny.
    /// </summary>
    private void StartInterrupt(InterruptSource source)
    {
        _interruptSource = source;
        _interruptVector = source == InterruptSource.Nmi ? (ushort)0xFFFA : (ushort)0xFFFE;

        if (source != InterruptSource.Brk)
        {
            _ = _bus.Read(_state.PC);
        }

        _phase = Phase.InterruptSignature;
    }

    private void ExecuteInterruptSignature()
    {
        // Cykl 2: bajt sygnatury odrzucony. BRK to instrukcja dwubajtowa, więc jego PC rośnie
        // (wektory: final PC = PC+2); IRQ/NMI zostawiają PC — odłożone zostanie PC bieżące.
        _ = _bus.Read(_state.PC);
        if (_interruptSource == InterruptSource.Brk)
        {
            _state.PC++;
        }

        _phase = Phase.InterruptPushPch;
    }

    private void ExecuteInterruptPushPch()
    {
        // Cykl 3: odłożenie PCH, S-- (zawijanie 8-bitowe).
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), (byte)(_state.PC >> 8));
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.InterruptPushPcl;
    }

    private void ExecuteInterruptPushPcl()
    {
        // Cykl 4: odłożenie PCL, S--.
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), (byte)_state.PC);
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.InterruptPushP;
    }

    private void ExecuteInterruptPushP()
    {
        // Cykl 5: odłożenie P z B=1 (BRK) albo B=0 (IRQ/NMI); bit 5 zawsze 1 w odłożonym bajcie
        // (wektory: write = P | 0x30 dla BRK; docs/cpu/registers-and-flags.md dla IRQ/NMI).
        byte pushed = (byte)_state.P;
        pushed |= _interruptSource == InterruptSource.Brk ? (byte)0x30 : (byte)0x20;
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), pushed);
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.InterruptVectorLow;
    }

    private void ExecuteInterruptVectorLow()
    {
        // Cykl 6: PCL z wektora; ustawienie I. 65C02 czyści D po odłożeniu P (dana wariantu).
        _lo = _bus.Read(_interruptVector);
        _state.P = _state.P.With(StatusFlags.InterruptDisable, true);
        if (_interruptClearsDecimal)
        {
            _state.P = _state.P.With(StatusFlags.Decimal, false);
        }

        _phase = Phase.InterruptVectorHigh;
    }

    private void ExecuteInterruptVectorHigh()
    {
        // Cykl 7: PCH z wektora i skok. Sekwencja nie próbkuje przerwań — następny Fetch zrobi to.
        _hi = _bus.Read((ushort)(_interruptVector + 1));
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }
}
