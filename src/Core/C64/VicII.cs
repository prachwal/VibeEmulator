
namespace VibeEmulator.Core.C64;

/// <summary>Wynik jednego cyklu VIC-II: czy CPU dostaje dostęp do magistrali.</summary>
public readonly record struct VicTickResult(bool Aec, bool Ba, int Cycle, int Line);

/// <summary>
/// Mockup VIC-II (PAL 6569R5): raster 63 × 312, rejestry i — co najważniejsze —
/// badline z kradzieżą cykli CPU przez linie BA/AEC.
/// </summary>
/// <remarks>
/// Renderingu nie ma. Jest timing, który decyduje o zgodności cyklowej:
/// BA opada w cyklu 12, AEC w 15, wraca w 55 — CPU traci 40 cykli na linię badline.
/// </remarks>
public sealed class VicII
{
    public const int CyclesPerLine = 63;
    public const int LinesPerFrame = 312;

    private const int BadLineFirstLine = 0x30;
    private const int BadLineLastLine = 0xF7;

    private readonly byte[] _registers = new byte[0x40];

    private int _cycle;
    private int _line;

    public VicII()
    {
        // Wartości startowe zbliżone do stanu po resecie KERNAL-a.
        _registers[0x11] = 0x1B; // DEN=1, RSEL=1, YSCROLL=3
        _registers[0x16] = 0xC8;
        _registers[0x18] = 0x14;
    }

    public int Cycle => _cycle;

    public int Line => _line;

    public int Raster => _line;

    public long Ticks { get; private set; }

    /// <summary>Stan AEC z ostatniego <see cref="Tick"/> — czy CPU miał dostęp.</summary>
    public bool LastAec { get; private set; } = true;

    /// <summary>Stan BA z ostatniego <see cref="Tick"/> — czy VIC żąda magistrali.</summary>
    public bool LastBa { get; private set; } = true;

    public bool DisplayEnable => (_registers[0x11] & 0x10) != 0;

    public int YScroll => _registers[0x11] & 0x07;

    public bool TwentyFiveRows => (_registers[0x11] & 0x08) != 0;

    public bool IrqRaised => (_registers[0x19] & _registers[0x1A] & 0x0F) != 0;

    public void Reset()
    {
        _cycle = 0;
        _line = 0;
        Ticks = 0;
        Array.Clear(_registers);
        _registers[0x11] = 0x1B;
        _registers[0x16] = 0xC8;
        _registers[0x18] = 0x14;
    }

    /// <summary>Warunek badline: DEN, zakres linii i zgodność YSCROLL z dolnymi bitami rastra.</summary>
    public bool IsBadLine(int line) =>
        DisplayEnable
        && line >= BadLineFirstLine
        && line <= BadLineLastLine
        && (line & 0x07) == YScroll;

    /// <summary>
    /// Jeden cykl VIC. Zwraca stan linii AEC (dostęp CPU) i BA (odczyt VIC).
    /// </summary>
    public VicTickResult Tick()
    {
        bool badLine = IsBadLine(_line);

        // BA opada od cyklu 12, AEC od 15. Oba wracają po cyklu 54.
        bool ba = !(badLine && _cycle >= 12 && _cycle <= 54);
        bool aec = !(badLine && _cycle >= 15 && _cycle <= 54);

        LastBa = ba;
        LastAec = aec;

        var result = new VicTickResult(aec, ba, _cycle, _line);

        Ticks++;
        Advance();

        return result;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x3F;

        return register switch
        {
            0x12 => (byte)_line,
            0x19 => (byte)(_registers[0x19] | 0x70),
            0x1E => _registers[0x1E],
            0x1F => _registers[0x1F],
            < 0x2F => _registers[register],
            _ => 0xFF,
        };
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x3F;

        if (register >= 0x2F)
        {
            return;
        }

        switch (register)
        {
            case 0x19:
            case 0x1E:
            case 0x1F:
                // Zapis jedynki kasuje flagę.
                _registers[register] &= (byte)~value;
                break;

            default:
                _registers[register] = value;
                break;
        }
    }

    private void Advance()
    {
        _cycle++;

        if (_cycle >= CyclesPerLine)
        {
            _cycle = 0;
            _line++;

            if (_line >= LinesPerFrame)
            {
                _line = 0;
            }
        }
    }
}
