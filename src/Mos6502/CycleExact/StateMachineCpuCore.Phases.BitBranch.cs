namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy etapu H: BBR/BBS (tryb zp,rel) i JMP (abs,X). Część <c>partial</c> rdzenia —
/// dispatch w <c>StateMachineCpuCore.Phases.cs</c>. Sekwencje z wektorów wdc65c02.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void ExecuteBitBranchDummy()
    {
        // Etap H: drugi odczyt bajtu zp (dummy — wektory pokazują dwa odczyty zp przed rel).
        _ = _bus.Read(_address);
        _phase = Phase.BitBranchRead;
    }

    private void ExecuteBitBranchRead()
    {
        // Etap H: odczyt bajtu zp i test bitu z CpuOpcode.BitNumber. Rel czytany zawsze (wektory).
        _value = _bus.Read(_address);
        _phase = Phase.BitBranchRel;
    }

    private void ExecuteBitBranchRel()
    {
        // Etap H: operand rel — zawsze pobierany, niezależnie od warunku (wektory wdc65c02).
        _lo = _bus.Read(_state.PC);
        _state.PC++;
        _phase = BitBranchCondition() ? Phase.BitBranchTaken : Phase.Fetch;
    }

    private void ExecuteBitBranchTaken()
    {
        // Etap H: dummy read spod PC; skok, przy przekroczeniu strony — dodatkowy dummy (jak etap C).
        _ = _bus.Read(_state.PC);

        sbyte offset = (sbyte)_lo;
        ushort target = (ushort)(_state.PC + offset);
        if ((_state.PC & 0xFF00) == (target & 0xFF00))
        {
            _state.PC = target;
            _phase = Phase.Fetch;
            return;
        }

        _baseAddress = _state.PC;
        _address = target;
        _phase = Phase.BitBranchCrossed;
    }

    private void ExecuteBitBranchCrossed()
    {
        // Etap H: drugi dummy przy przekroczeniu strony — też pod PC, nie ghost
        // (wektory wdc65c02: BBR/BBS przy cross czytają PC dwa razy; BRA czyta ghost).
        _ = _bus.Read(_state.PC);
        _state.PC = _address;
        _phase = Phase.Fetch;
    }

    /// <summary>Warunek BBR/BBS: bit <see cref="CpuOpcode.BitNumber"/> wyczyszony (BBR) albo ustawiony (BBS).</summary>
    private bool BitBranchCondition()
    {
        bool set = (_value & (1 << _entry.BitNumber)) != 0;
        return _entry.Action == CpuAction.Bbr ? !set : set;
    }

    private void ExecuteJmpAxPointerLow()
    {
        // Etap H: JMP (abs,X) — odczyt low-byte wskaźnika spod baza+X (bez zawijania strony).
        _lo = _bus.Read(_address);
        _phase = Phase.JmpAxPointerHigh;
    }

    private void ExecuteJmpAxPointerHigh()
    {
        // Etap H: JMP (abs,X) — odczyt high-byte i skok (wektory: 6 cykli łącznie).
        _hi = _bus.Read((ushort)(_address + 1));
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }
}
