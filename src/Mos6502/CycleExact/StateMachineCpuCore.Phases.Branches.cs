namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy rozgałęzień warunkowych (etap C): odczyt offsetu, dummy read spod PC z testem warunku,
/// dummy read przy przejściu strony. Część <c>partial</c> rdzenia — dispatch w <c>StateMachineCpuCore.Phases.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void ExecuteBranchOperand()
    {
        // Etap C: drugi cykl rozgałęzienia. Odczyt offsetu i test warunku.
        // Warunek fałszywy: koniec po 2 cyklach. Warunek prawdziwy: BranchTaken (dummy read).
        _lo = _bus.Read(_state.PC);
        _state.PC++;
        _phase = BranchCondition(_entry.Action) ? Phase.BranchTaken : Phase.Fetch;
    }

    private void ExecuteBranchTaken()
    {
        // Etap C: trzeci cykl rozgałęzienia (tylko gdy warunek prawdziwy). Dummy read spod PC,
        // potem PC += offset zapisany w _lo (jako sbyte); przy przejściu strony — BranchCrossed.
        _ = _bus.Read(_state.PC);
        if (!BranchCondition(_entry.Action))
        {
            // Nieosiągalne: warunek sprawdzany w BranchOperand. Zabezpieczenie przed JAM.
            _phase = Phase.Fetch;
            return;
        }

        sbyte offset = (sbyte)_lo;
        ushort target = (ushort)(_state.PC + offset);
        if ((_state.PC & 0xFF00) == (target & 0xFF00))
        {
            _state.PC = target;
            _phase = Phase.Fetch;
            return;
        }

        // Przejście strony: drugi dummy read spod adresu bez korekty, potem PC = target.
        _baseAddress = _state.PC;
        _address = target;
        _phase = Phase.BranchCrossed;
    }

    private void ExecuteBranchCrossed()
    {
        // Etap C: czwarty cykl rozgałęzienia przy przejściu strony. Dummy read spod adresu
        // bez korekty strony: (PC & 0xFF00) | (target & 0xFF).
        _ = _bus.Read((ushort)((_baseAddress & 0xFF00) | (_address & 0xFF)));
        _state.PC = _address;
        _phase = Phase.Fetch;
    }
}
