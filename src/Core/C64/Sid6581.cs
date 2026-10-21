namespace VibeEmulator.Core.C64;

/// <summary>
/// Mockup SID 6581: tylko model rejestrowy (32 bajty, mirror co 32).
/// Brak syntezy dźwięku — do etapu 10 planu.
/// </summary>
public sealed class Sid6581
{
    private readonly byte[] _registers = new byte[0x20];

    public byte Read(ushort address)
    {
        int register = address & 0x1F;

        return register switch
        {
            // Rejestry potencjometrów — gry ich nie zawieszają, gdy zwracają stałą.
            0x19 or 0x1A => 0x00,
            _ => _registers[register],
        };
    }

    public void Write(ushort address, byte value) => _registers[address & 0x1F] = value;

    public static void Tick()
    {
        // Zegar SID-a nie wpływa na CPU w tym modelu.
    }

    public void Reset() => Array.Clear(_registers);
}
