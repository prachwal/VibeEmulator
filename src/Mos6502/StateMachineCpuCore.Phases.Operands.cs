namespace VibeEmulator.Mos6502;

/// <summary>
/// Pobieranie operandów i wyliczanie adresu efektywnego: low/high operandu, wskaźniki
/// dla trybów pośrednich, dummy read przy indeksowaniu. Część <c>partial</c> rdzenia —
/// dispatch w <c>StateMachineCpuCore.Phases.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void FetchOperandLow()
    {
        _lo = _bus.Read(_state.PC);
        _state.PC++;

        // Etap E: JSR po low operandu robi dummy read stosu (cykl 3), nie czyta high-byte.
        if (_entry.Kind == OpKind.Subroutine)
        {
            _phase = Phase.JsrDummy;
            return;
        }

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
            case AddressingMode.IndirectZeroPage:
                // (zp),Y i (zp): indeks dodawany dopiero po pobraniu wskaźnika (wektory:
                // (zp) bez indeksu — 5 cykli, bez dummy read zp).
                _address = _lo;
                _phase = Phase.PointerLow;
                break;

            case AddressingMode.ZeroPageRelative:
                // Etap H: BBR/BBS zp,rel — bajt zp, potem dummy read zp (BitBranchDummy).
                _address = _lo;
                _phase = Phase.BitBranchDummy;
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
            if (_entry.Mode == AddressingMode.Absolute)
            {
                _state.PC = _baseAddress;
                _phase = Phase.Fetch;
                return;
            }

            if (_entry.Mode == AddressingMode.IndirectAbsoluteX)
            {
                // Etap H: JMP (abs,X) — dummy read spod PC-1 (wektory wdc65c02), potem wskaźnik
                // spod baza+X. Faza DummyRead rozpoznaje Jump i przechodzi do JmpAxPointerLow.
                _index = _state.X;
                _address = (ushort)(_baseAddress + _index);
                _phase = Phase.DummyRead;
                return;
            }

            // Etap E: JMP (ind.) — _baseAddress to wskaźnik, nie cel. Odczyt low-byte w JmpIndirectLow.
            _phase = Phase.JmpIndirectLow;
            return;
        }

        if (_entry.Kind == OpKind.Subroutine)
        {
            // Etap E: JSR — high-byte celu czytany dopiero w 6. cyklu (po pushach).
            _phase = Phase.JsrFetch;
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

        if (_entry.Mode is AddressingMode.IndexedIndirect or AddressingMode.IndirectZeroPage)
        {
            // (zp,X) i (zp): wskaźnik już zawiera indeks (albo go nie ma), adres efektywny to wskaźnik.
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
