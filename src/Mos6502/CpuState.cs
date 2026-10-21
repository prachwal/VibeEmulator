namespace VibeEmulator.Mos6502;

/// <summary>
/// Stan procesora. Pola publiczne i struktura (nie klasa), żeby:
/// 1) dało się manipulować rejestrami z poziomu opkodu przez <c>ref</c>,
/// 2) nie alokować w pętli wykonawczej.
/// </summary>
public struct CpuState
{
    public byte A;
    public byte X;
    public byte Y;
    public ushort PC;
    public ushort SP;
    public StatusFlags P;

    public static CpuState Reset() => new()
    {
        A = 0,
        X = 0,
        Y = 0,
        PC = 0,
        SP = 0xFD,
        P = StatusFlags.Unused | StatusFlags.InterruptDisable,
    };
}
