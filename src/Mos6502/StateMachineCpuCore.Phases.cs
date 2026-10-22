using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy maszyny stanów: fetch, operand, indeksowanie, dummy read, dostęp i RMW.
/// Część <c>partial</c> rdzenia — operacje i ALU są w <c>StateMachineCpuCore.Operations.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void Execute()
    {
        switch (_phase)
        {
            case Phase.Fetch:
                Fetch();
                break;

            case Phase.OperandLow:
                FetchOperandLow();
                break;

            case Phase.IndexInternal:
                // Dummy read spod adresu zerowej strony przed dodaniem indeksu — obie warianty (wektory).
                _ = _bus.Read(_lo);
                _index = SelectIndex();
                _address = (byte)(_lo + _index);
                _phase = _entry.Mode == AddressingMode.IndexedIndirect ? Phase.PointerLow : Phase.Access;
                break;

            case Phase.OperandHigh:
                FetchOperandHigh();
                break;

            case Phase.DummyRead:
                // Adres dummy read zależy od wariantu (IndexedDummyRead); odczyt jest tylko w cyklu dodatkowym.
                // Etap B: RMW w abs,X/abs,Y w 65C02 czyta spod PC-1 (jak odczyt), nie spod adresu bez korekty.
                // Wektory: FE/DE bez page cross pokazują odczyt spod PC-1 (cykl 4 = ostatni bajt operandu),
                // a 5E/7E z page cross — też spod PC-1, nie spod bazy+indeks.
                _ = _bus.Read(_ghostIndexedDummy
                    ? (ushort)((_baseAddress & 0xFF00) | ((_baseAddress + _index) & 0xFF))
                    : (ushort)(_state.PC - 1));
                _phase = Phase.Access;
                break;

            case Phase.Access:
                Access();
                break;

            case Phase.ImpliedRead:
                // Drugi cykl instrukcji jednobajtowej: dummy read spod PC; operacja wykonuje się w tym cyklu.
                _ = _bus.Read(_state.PC);
                ApplyImplied(_entry.Action);
                _internal--;
                _phase = _internal > 0 ? Phase.Internal : Phase.Fetch;
                break;

            case Phase.Internal:
                _internal--;
                if (_internal <= 0)
                {
                    _phase = Phase.Fetch;
                }

                break;

            case Phase.AccumulatorRead:
                // 65C02 INC A / DEC A: dummy read spod PC i zmiana akumulatora w tym samym cyklu.
                _ = _bus.Read(_state.PC);
                ApplyImplied(_entry.Action);
                _phase = Phase.Fetch;
                break;

            case Phase.PointerLow:
                _pointer = _bus.Read(_address);
                _address = (byte)(_address + 1); // wskaźnik zawija się w stronie zerowej
                _phase = Phase.PointerHigh;
                break;

            case Phase.PointerHigh:
                FetchPointerHigh();
                break;

            case Phase.RmwOld:
                if (_doubleReadRmw)
                {
                    _ = _bus.Read(_address);
                }
                else
                {
                    _bus.Write(_address, _value);
                }

                _phase = Phase.RmwNew;
                break;

            case Phase.RmwNew:
                // Etap B: ApplyRead liczy wartość bez dostępu do _state (metoda statyczna).
                // Dla przesunięć/rotacji Carry liczymy tutaj z _value i bieżącego C (wejście ROL/ROR).
                byte result = ApplyRead(_entry.Action, _value);
                if (_entry.Action is CpuAction.Asl or CpuAction.Lsr or CpuAction.Rol or CpuAction.Ror)
                {
                    bool carryIn = _state.P.Has(StatusFlags.Carry);
                    bool carry;
                    result = _entry.Action switch
                    {
                        CpuAction.Asl => ShiftLeft(_value, out carry),
                        CpuAction.Lsr => ShiftRight(_value, out carry),
                        CpuAction.Rol => RotateLeft(_value, carryIn, out carry),
                        _ => RotateRight(_value, carryIn, out carry),
                    };
                    _state.P = _state.P.With(StatusFlags.Carry, carry);
                }

                _bus.Write(_address, result);
                SetZeroNegative(result);
                _phase = Phase.Fetch;
                break;
        }
    }

    private void Fetch()
    {
        InstructionPc = _state.PC;
        byte opcode = _bus.Read(_state.PC);
        Opcode = opcode;
        _state.PC++;

        CpuOpcode? found = _table[opcode];

        if (found is null)
        {
            _jammed = true;
            return;
        }

        _entry = found.Value;

        switch (_entry.Kind)
        {
            case OpKind.Implied:
                _internal = Math.Max(1, _entry.InternalCycles);
                _phase = Phase.ImpliedRead;
                break;

            case OpKind.Accumulator:
                _phase = Phase.AccumulatorRead;
                break;

            case OpKind.Jump:
                _phase = Phase.OperandLow;
                break;

            default:
                _phase = _entry.Mode switch
                {
                    AddressingMode.Immediate => Phase.Access,
                    AddressingMode.ZeroPage
                        or AddressingMode.ZeroPageX
                        or AddressingMode.ZeroPageY
                        or AddressingMode.Absolute
                        or AddressingMode.AbsoluteX
                        or AddressingMode.AbsoluteY
                        or AddressingMode.IndexedIndirect
                        or AddressingMode.IndirectIndexed => Phase.OperandLow,
                    _ => Jam(),
                };
                break;
        }
    }

    private Phase Jam()
    {
        _jammed = true;
        return Phase.Fetch;
    }

    private void FetchOperandLow()
    {
        _lo = _bus.Read(_state.PC);
        _state.PC++;

        switch (_entry.Mode)
        {
            case AddressingMode.ZeroPage:
                _address = _lo;
                _phase = Phase.Access;
                break;

            case AddressingMode.ZeroPageX:
            case AddressingMode.ZeroPageY:
            case AddressingMode.IndexedIndirect:
                // Dodanie indeksu w stronie zerowej to osobny cykl (z dummy read spod bazy).
                _phase = Phase.IndexInternal;
                break;

            case AddressingMode.IndirectIndexed:
                // (zp),Y: indeks dodawany dopiero po pobraniu wskaźnika.
                _address = _lo;
                _phase = Phase.PointerLow;
                break;

            default:
                _phase = Phase.OperandHigh;
                break;
        }
    }

    private void FetchOperandHigh()
    {
        _hi = _bus.Read(_state.PC);
        _state.PC++;

        _baseAddress = (ushort)(_lo | (_hi << 8));

        if (_entry.Kind == OpKind.Jump)
        {
            _state.PC = _baseAddress;
            _phase = Phase.Fetch;
            return;
        }

        if (_entry.Mode == AddressingMode.Absolute)
        {
            _address = _baseAddress;
            _phase = Phase.Access;
            return;
        }

        _index = SelectIndex();
        StartIndexedAccess();
    }

    private void FetchPointerHigh()
    {
        byte high = _bus.Read(_address);
        _baseAddress = (ushort)(_pointer | (high << 8));

        if (_entry.Mode == AddressingMode.IndexedIndirect)
        {
            // (zp,X): wskaźnik już zawiera indeks, adres efektywny to wskaźnik.
            _address = _baseAddress;
            _phase = Phase.Access;
            return;
        }

        _index = _state.Y;
        StartIndexedAccess();
    }

    /// <summary>
    /// Adres efektywny = baza + indeks. Zapis i RMW zawsze płacą dummy read; odczyt tylko przy przekroczeniu strony.
    /// Wyjątek (etap B, wektory): 65C02 RMW w <c>abs,X</c>/<c>abs,Y</c> dla przesunięć/rotacji (ASL/LSR/ROL/ROR)
    /// płaci dummy read tylko przy page cross (jak odczyt); INC/DEC płaci zawsze (jak NMOS).
    /// Adres dummy read zależy od wariantu (faza DummyRead): NMOS czyta spod adresu bez korekty strony,
    /// 65C02 — spod ostatniego bajtu operandu (PC-1).
    /// </summary>
    private void StartIndexedAccess()
    {
        _address = (ushort)(_baseAddress + _index);
        bool pageCrossed = (_baseAddress & 0xFF00) != (_address & 0xFF00);
        bool needsDummy = pageCrossed || _entry.Kind is OpKind.Write or OpKind.ReadModifyWrite;

        // Etap B: w 65C02 (_doubleReadRmw) przesunięcia/rotacje w abs,X/abs,Y zachowują się jak odczyt
        // (dummy read tylko przy page cross); INC/DEC — jak zapis (zawsze). Rozróżnienie po akcji,
        // bo rodzaj (ReadModifyWrite) jest ten sam. W NMOS wszystkie RMW płacą zawsze.
        if (_doubleReadRmw
            && _entry.Kind is OpKind.ReadModifyWrite
            && _entry.Mode is AddressingMode.AbsoluteX or AddressingMode.AbsoluteY
            && _entry.Action is CpuAction.Asl or CpuAction.Lsr or CpuAction.Rol or CpuAction.Ror)
        {
            needsDummy = pageCrossed;
        }

        _phase = needsDummy ? Phase.DummyRead : Phase.Access;
    }

    /// <summary>Rejestr indeksu danego trybu: Y dla trybów z indeksem Y, X w pozostałych.</summary>
    private byte SelectIndex() => _entry.Mode is AddressingMode.ZeroPageY or AddressingMode.AbsoluteY or AddressingMode.IndirectIndexed
        ? _state.Y
        : _state.X;
}
