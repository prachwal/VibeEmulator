namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy sterowania (etap E): JSR/RTS/RTI i JMP (ind.). Część <c>partial</c> rdzenia —
/// dispatch w <c>StateMachineCpuCore.Phases.cs</c>. Sekwencje z wektorów SingleStepTests.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void ExecuteJsrDummy()
    {
        // Etap E: czwarty cykl JSR — dummy read spod $0100+S (stare S, bez zmiany).
        _ = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _phase = Phase.JsrPushHigh;
    }

    private void ExecuteJsrPushHigh()
    {
        // Etap E: czwarty cykl JSR — zapis PCH adresu powrotu, potem S--.
        // Adres powrotu to adres high-byte operandu = PC w chwili pusha (PC stoi za low operandu).
        // Wektory: JSR pod $5289 odkłada $528B (PC high-byte), nie $528C.
        ushort returnAddress = _state.PC;
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), (byte)(returnAddress >> 8));
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.JsrPushLow;
    }

    private void ExecuteJsrPushLow()
    {
        // Etap E: piąty cykl JSR — zapis PCL adresu powrotu (jak wyżej), potem S--.
        ushort returnAddress = _state.PC;
        _bus.Write((ushort)(0x0100 | (_state.SP & 0xFF)), (byte)returnAddress);
        _state.SP = (ushort)((_state.SP - 1) & 0xFF);
        _phase = Phase.JsrFetch;
    }

    private void ExecuteJsrFetch()
    {
        // Etap E: szósty cykl JSR — odczyt high-byte celu spod PC i skok pod cel (low z _lo).
        _hi = _bus.Read(_state.PC);
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }

    private void ExecuteRtsDummy()
    {
        // Etap E: drugi cykl RTS — dummy read spod PC.
        _ = _bus.Read(_state.PC);
        _phase = Phase.RtsPullDummy;
    }

    private void ExecuteRtsPullDummy()
    {
        // Etap E: trzeci cykl RTS — dummy read spod $0100+S (stare S), potem S++.
        _ = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.RtsPullLow;
    }

    private void ExecuteRtsPullLow()
    {
        // Etap E: czwarty cykl RTS — odczyt PCL spod $0100+S (nowe S), potem S++.
        _lo = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.RtsPullHigh;
    }

    private void ExecuteRtsPullHigh()
    {
        // Etap E: piąty cykl RTS — odczyt PCH spod $0100+S (bez S++); PC = zdjęty adres.
        _hi = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.RtsFetch;
    }

    private void ExecuteRtsFetch()
    {
        // Etap E: szósty cykl RTS — dummy read spod zdjętego PC, potem PC++ (powrót za JSR).
        _ = _bus.Read(_state.PC);
        _state.PC++;
        _phase = Phase.Fetch;
    }

    private void ExecuteRtiDummy()
    {
        // Etap E: drugi cykl RTI — dummy read spod PC.
        _ = _bus.Read(_state.PC);
        _phase = Phase.RtiPullDummy;
    }

    private void ExecuteRtiPullDummy()
    {
        // Etap E: trzeci cykl RTI — dummy read spod $0100+S (stare S), potem S++.
        _ = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.RtiPullStatus;
    }

    private void ExecuteRtiPullStatus()
    {
        // Etap E: czwarty cykl RTI — odczyt P spod $0100+S z maską PLP, potem S++.
        byte stackValue = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.P = (StatusFlags)((stackValue & 0xCF) | 0x20);
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.RtiPullLow;
    }

    private void ExecuteRtiPullLow()
    {
        // Etap E: piąty cykl RTI — odczyt PCL spod $0100+S, potem S++.
        _lo = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.SP = (ushort)((_state.SP + 1) & 0xFF);
        _phase = Phase.RtiPullHigh;
    }

    private void ExecuteRtiPullHigh()
    {
        // Etap E: szósty cykl RTI — odczyt PCH i skok bez inkrementacji (w przeciwieństwie do RTS).
        _hi = _bus.Read((ushort)(0x0100 | (_state.SP & 0xFF)));
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }

    private void ExecuteJmpIndirectLow()
    {
        // Etap E: czwarty cykl JMP (ind.) — odczyt low-byte spod wskaźnika z _baseAddress.
        _lo = _bus.Read(_baseAddress);
        _phase = Phase.JmpIndirectHigh;
    }

    private void ExecuteJmpIndirectHigh()
    {
        // Etap E: piąty cykl JMP (ind.) — odczyt high-byte. NMOS z błędem strony
        // (zawijanie w obrębie strony wskaźnika); 65C02 czyta tak samo, ale traktuje
        // odczyt jako dummy przy wrapie i dokłada JmpIndirectFixup (wektory wdc65c02).
        ushort wrapped = (ushort)((_baseAddress & 0xFF00) | ((_baseAddress + 1) & 0xFF));
        _hi = _bus.Read(wrapped);
        if (_jmpNoWrap)
        {
            _phase = Phase.JmpIndirectFixup;
            return;
        }

        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }

    private void ExecuteJmpIndirectFixup()
    {
        // Etap E: szósty cykl JMP (ind.) tylko w 65C02 — ponowny odczyt high-byte spod
        // poprawnego adresu (bez zawijania). Bez wrapa czyta ten sam adres co cykl 5.
        _hi = _bus.Read((ushort)(_baseAddress + 1));
        _state.PC = (ushort)(_lo | (_hi << 8));
        _phase = Phase.Fetch;
    }
}
