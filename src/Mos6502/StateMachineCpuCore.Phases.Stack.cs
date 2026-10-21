namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy stosu (etap D): push (PHA/PHP) i pull (PLA/PLP). Część <c>partial</c> rdzenia —
/// dispatch w <c>StateMachineCpuCore.Phases.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void ExecuteStackDummy()
    {
        // Etap D: drugi cykl push/pull — dummy read spod PC (wektory PHA/PHP/PLA/PLP).
        _ = _bus.Read(_state.PC);
        _phase = _entry.Kind == OpKind.Push ? Phase.Push : Phase.PullDummy;
    }

    private void ExecutePush()
    {
        // Etap D: trzeci cykl push — zapis pod $0100+S, potem S-- (zawijanie 8-bitowe).
        // PHA odkłada A; PHP odkłada P z bitami B i Unused (wektory: write = P | 0x30).
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), _entry.Action == CpuAction.Pha
            ? _state.A
            : (byte)((byte)_state.P | 0x30));
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.Fetch;
    }

    private void ExecutePullDummy()
    {
        // Etap D: trzeci cykl pull — dummy read spod $0100+S (stare S), potem S++.
        _ = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.Pull;
    }

    private void ExecutePull()
    {
        // Etap D: czwarty cykl pull — odczyt spod $0100+S (nowe S).
        // PLA ładuje A i ustawia N/Z; PLP ładuje P z wyzerowanymi B i bit5 i wymuszonym
        // Unused=1 (wektory: P = (stack & 0xCF) | 0x20).
        byte stackValue = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        if (_entry.Action == CpuAction.Pla)
        {
            _state.A = stackValue;
            SetZeroNegative(_state.A);
        }
        else
        {
            // Etap F: PLP zmienia I po próbkowaniu — przy oczekującym IRQ i starym I=1
            // obsługa czeka jeszcze jedną instrukcję (jak CLI/SEI, docs/cpu/interrupts-and-reset.md).
            _deferIrq = IrqLevel && _state.P.Has(StatusFlags.InterruptDisable);
            _state.P = (StatusFlags)((stackValue & 0xCF) | 0x20);
        }

        _phase = Phase.Fetch;
    }
}
