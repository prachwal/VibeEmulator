using VibeEmulator.Memory;

namespace VibeEmulator.CpuHarness;

/// <summary>
/// Magistrala minimalnego systemu NES (CPU + NROM + RAM): 2 KB wewnętrznego RAM z lustrami, rejestry PPU/APU
/// ignorowane przy zapisie (odczyt zwraca 0), 8 KB RAM pod <c>$6000</c> i pamięć programu od <c>$8000</c>.
/// Bez PPU/APU — tyle wystarcza testom `nestest` i Blargga.
/// </summary>
public sealed class NesBus : IMemoryBus
{
    private readonly byte[] _memory = new byte[0x10000];

    public NesBus(InesImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        byte[] prg = image.PrgRom;
        prg.CopyTo(_memory.AsSpan(0x8000));

        // NROM-128 (16 KB) jest odbijane w $C000 — odbicie sprawdzone przebiegiem nestest (docs/cpu/tests.md).
        if (prg.Length == 0x4000)
        {
            prg.CopyTo(_memory.AsSpan(0xC000));
        }
    }

    /// <summary>Liczba zapisów do rejestrów I/O (<c>$2000</c>–<c>$401F</c>) — przyjmowane i ignorowane.</summary>
    public int IoWrites { get; private set; }

    public byte Read(ushort address)
    {
        if (address < 0x2000)
        {
            return _memory[address & 0x07FF];
        }

        if (address is >= 0x2000 and < 0x4000)
        {
            return 0; // rejestry PPU — bez PPU zwracamy 0
        }

        if (address is >= 0x4000 and < 0x4020)
        {
            return 0; // APU/IO — bez APU zwracamy 0
        }

        return _memory[address];
    }

    public void Write(ushort address, byte value)
    {
        if (address < 0x2000)
        {
            _memory[address & 0x07FF] = value;
            return;
        }

        if (address is >= 0x2000 and < 0x4020)
        {
            IoWrites++;
            return;
        }

        _memory[address] = value;
    }

    /// <summary>Odczyt bez efektów ubocznych (po zakończeniu przebiegu).</summary>
    public byte Peek(ushort address) => Read(address);

    /// <summary>Wektor resetu z pamięci programu (<c>$FFFC</c>/<c>$FFFD</c>) — punkt wejścia ROM-u.</summary>
    public ushort ResetVector => (ushort)(_memory[0xFFFC] | (_memory[0xFFFD] << 8));
}
