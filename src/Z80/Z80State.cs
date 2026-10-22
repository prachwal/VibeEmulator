namespace VibeEmulator.Z80;

/// <summary>
/// Stan procesora Z80. Pola odpowiadają polom wektorów SingleStepTests/z80
/// (płaskie: <c>af_</c>, nie para <c>af'</c>).
/// </summary>
public struct Z80State
{
    public ushort PC;
    public ushort SP;
    public byte A;
    public byte B;
    public byte C;
    public byte D;
    public byte E;
    public byte F;
    public byte H;
    public byte L;
    public byte I;
    public byte R;
    public ushort IX;
    public ushort IY;
    public ushort AF_;
    public ushort BC_;
    public ushort DE_;
    public ushort HL_;
    public ushort WZ;
    public byte IM;
    public bool IFF1;
    public bool IFF2;
    public byte EI;
    public byte P;
    public byte Q;

    public static Z80State Reset() => new()
    {
        PC = 0,
        SP = 0xFFFF,
        IFF1 = false,
        IFF2 = false,
        IM = 0,
        R = 0,
        I = 0,
    };
}
