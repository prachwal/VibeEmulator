using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Wykonanie instrukcji: dostęp do magistrali, zapis rejestru, akcje niejawne i ALU.
/// Część <c>partial</c> rdzenia — maszyna stanów jest w <c>StateMachineCpuCore.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void Access()
    {
        switch (_entry.Kind)
        {
            case OpKind.Read:
                // Tryb immediate: operand jest pod PC, a nie pod adresem wyliczonym.
                if (_entry.Mode == AddressingMode.Immediate)
                {
                    _address = _state.PC;
                    _state.PC++;
                }

                byte value = _bus.Read(_address);
                ApplyReadToRegister(_entry.Action, value);
                _phase = Phase.Fetch;
                break;

            case OpKind.Write:
                _bus.Write(_address, ReadSourceRegister(_entry.Action));
                _phase = Phase.Fetch;
                break;

            case OpKind.ReadModifyWrite:
                _value = _bus.Read(_address);
                _phase = Phase.RmwOld;
                break;

            default:
                _jammed = true;
                break;
        }
    }

    private byte ReadSourceRegister(CpuAction action) => action switch
    {
        CpuAction.Sta => _state.A,
        CpuAction.Stx => _state.X,
        CpuAction.Sty => _state.Y,
        _ => 0, // STZ zapisuje zero.
    };

    private void ApplyReadToRegister(CpuAction action, byte value)
    {
        switch (action)
        {
            case CpuAction.Lda:
                _state.A = value;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Ldx:
                _state.X = value;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Ldy:
                _state.Y = value;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Adc:
                Adc(value);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Sbc:
                Sbc(value);
                SetZeroNegative(_state.A);
                break;

            // Etap A: logika. N i Z z wyniku; C i V bez zmian.
            case CpuAction.And:
                _state.A &= value;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Ora:
                _state.A |= value;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Eor:
                _state.A ^= value;
                SetZeroNegative(_state.A);
                break;

            // Etap A: porównania. C/N/Z z różnicy, V bez zmian (wektory).
            case CpuAction.Cmp:
                Compare(_state.A, value);
                break;
            case CpuAction.Cpx:
                Compare(_state.X, value);
                break;
            case CpuAction.Cpy:
                Compare(_state.Y, value);
                break;

            // Etap A: BIT. Z z A & M, N i V z operandu (bity 7 i 6), C bez zmian.
            case CpuAction.Bit:
                _state.P = _state.P
                    .With(StatusFlags.Zero, (_state.A & value) == 0)
                    .With(StatusFlags.Negative, (value & 0x80) != 0)
                    .With(StatusFlags.Overflow, (value & 0x40) != 0);
                break;

            default:
                _jammed = true;
                break;
        }
    }

    private static byte ApplyRead(CpuAction action, byte value) => action switch
    {
        CpuAction.Inc => (byte)(value + 1),
        CpuAction.Dec => (byte)(value - 1),
        // Etap B: wersja pamięciowa nie ma dostępu do _state (metoda statyczna) — Carry liczy RmwNew.
        CpuAction.Asl => ShiftLeft(value, out _),
        CpuAction.Lsr => ShiftRight(value, out _),
        CpuAction.Rol => RotateLeft(value, carryIn: false, out _),
        CpuAction.Ror => RotateRight(value, carryIn: false, out _),
        _ => value,
    };

    /// <summary>
    /// Etap B: przesunięcia i rotacje w wersji pamięciowej (RMW). Flagi C/N/Z ustawia <c>RmwNew</c>
    /// po zapisie — te metody liczą tylko wartość i Carry, bez dotykania flag (stan jest w <c>_state</c>).
    /// Statyczne, bo <c>ApplyRead</c> jest statyczne; Carry przekazywane jawnie, żeby nie czytać stanu.
    /// </summary>
    private static byte ShiftLeft(byte value, out bool carry)
    {
        carry = (value & 0x80) != 0;
        return (byte)(value << 1);
    }

    private static byte ShiftRight(byte value, out bool carry)
    {
        carry = (value & 0x01) != 0;
        return (byte)(value >> 1);
    }

    private static byte RotateLeft(byte value, bool carryIn, out bool carry)
    {
        carry = (value & 0x80) != 0;
        return (byte)((value << 1) | (carryIn ? 1 : 0));
    }

    private static byte RotateRight(byte value, bool carryIn, out bool carry)
    {
        carry = (value & 0x01) != 0;
        return (byte)((value >> 1) | (carryIn ? 0x80 : 0));
    }

    private void ApplyImplied(CpuAction action)
    {
        switch (action)
        {
            case CpuAction.Inx:
                _state.X++;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Dex:
                _state.X--;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Iny:
                _state.Y++;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Dey:
                _state.Y--;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Tax:
                _state.X = _state.A;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Tay:
                _state.Y = _state.A;
                SetZeroNegative(_state.Y);
                break;
            case CpuAction.Txa:
                _state.A = _state.X;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Tya:
                _state.A = _state.Y;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Tsx:
                _state.X = (byte)_state.SP;
                SetZeroNegative(_state.X);
                break;
            case CpuAction.Txs:
                // TXS nie zmienia flag.
                _state.SP = _state.X;
                break;
            case CpuAction.IncA:
                _state.A++;
                SetZeroNegative(_state.A);
                break;
            case CpuAction.DecA:
                _state.A--;
                SetZeroNegative(_state.A);
                break;

            // Etap B: wersje akumulatorowe. Dummy read spod PC już wykonany w fazie AccumulatorRead;
            // akcja zmienia A i ustawia C/N/Z w tym samym cyklu.
            case CpuAction.Asl:
                _state.A = ShiftLeft(_state.A, out bool aslCarry);
                _state.P = _state.P.With(StatusFlags.Carry, aslCarry);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Lsr:
                _state.A = ShiftRight(_state.A, out bool lsrCarry);
                _state.P = _state.P.With(StatusFlags.Carry, lsrCarry);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Rol:
                _state.A = RotateLeft(_state.A, _state.P.Has(StatusFlags.Carry), out bool rolCarry);
                _state.P = _state.P.With(StatusFlags.Carry, rolCarry);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Ror:
                _state.A = RotateRight(_state.A, _state.P.Has(StatusFlags.Carry), out bool rorCarry);
                _state.P = _state.P.With(StatusFlags.Carry, rorCarry);
                SetZeroNegative(_state.A);
                break;
            case CpuAction.Clc:
                _state.P = _state.P.With(StatusFlags.Carry, false);
                break;
            case CpuAction.Sec:
                _state.P = _state.P.With(StatusFlags.Carry, true);
                break;
            case CpuAction.Cli:
                _state.P = _state.P.With(StatusFlags.InterruptDisable, false);
                break;
            case CpuAction.Sei:
                _state.P = _state.P.With(StatusFlags.InterruptDisable, true);
                break;
            case CpuAction.Clv:
                _state.P = _state.P.With(StatusFlags.Overflow, false);
                break;
            case CpuAction.Cld:
                _state.P = _state.P.With(StatusFlags.Decimal, false);
                break;
            case CpuAction.Sed:
                _state.P = _state.P.With(StatusFlags.Decimal, true);
                break;
            case CpuAction.Nop:
                break;
            case CpuAction.Brk:
                _state.P = _state.P.With(StatusFlags.Break, true);
                break;
            default:
                _jammed = true;
                break;
        }
    }

    /// <summary>
    /// Wspólna semantyka CMP/CPX/CPY: odejmowanie bez zapisu. Carry = brak pożyczki (rejestr ≥ operand),
    /// N i Z z różnicy, V bez zmian (wektory SingleStepTests).
    /// </summary>
    private void Compare(byte register, byte operand)
    {
        int difference = register - operand;

        _state.P = _state.P
            .With(StatusFlags.Carry, difference >= 0)
            .With(StatusFlags.Zero, register == operand)
            .With(StatusFlags.Negative, (difference & 0x80) != 0);
    }

    /// <summary>
    /// Etap C: warunek rozgałęzienia. Zwraca true, gdy skok ma nastąpić.
    /// </summary>
    private bool BranchCondition(CpuAction action) => action switch
    {
        CpuAction.Bpl => !_state.P.Has(StatusFlags.Negative),
        CpuAction.Bmi => _state.P.Has(StatusFlags.Negative),
        CpuAction.Bvc => !_state.P.Has(StatusFlags.Overflow),
        CpuAction.Bvs => _state.P.Has(StatusFlags.Overflow),
        CpuAction.Bcc => !_state.P.Has(StatusFlags.Carry),
        CpuAction.Bcs => _state.P.Has(StatusFlags.Carry),
        CpuAction.Bne => !_state.P.Has(StatusFlags.Zero),
        CpuAction.Beq => _state.P.Has(StatusFlags.Zero),
        _ => false,
    };

    private void Adc(byte operand)
    {
        int carry = _state.P.Has(StatusFlags.Carry) ? 1 : 0;
        int sum = _state.A + operand + carry;
        byte result = (byte)sum;
        bool overflow = (~(_state.A ^ operand) & (_state.A ^ result) & 0x80) != 0;

        _state.A = result;
        _state.P = _state.P
            .With(StatusFlags.Carry, sum > 0xFF)
            .With(StatusFlags.Overflow, overflow);
    }

    private void Sbc(byte operand)
    {
        int borrow = _state.P.Has(StatusFlags.Carry) ? 0 : 1;
        int difference = _state.A - operand - borrow;
        byte result = (byte)difference;
        bool overflow = ((_state.A ^ operand) & (_state.A ^ result) & 0x80) != 0;

        _state.A = result;
        _state.P = _state.P
            .With(StatusFlags.Carry, difference >= 0)
            .With(StatusFlags.Overflow, overflow);
    }

    private void SetZeroNegative(byte value)
    {
        _state.P = _state.P
            .With(StatusFlags.Zero, value == 0)
            .With(StatusFlags.Negative, (value & 0x80) != 0);
    }
}
