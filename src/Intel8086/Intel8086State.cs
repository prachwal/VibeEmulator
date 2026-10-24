namespace VibeEmulator.Intel8086;

/// <summary>Stan widoczny dla testów Intel 8086 oraz zawartość kolejki prefetch.</summary>
public struct Intel8086State
{
    public ushort AX { get; set; }
    public ushort BX { get; set; }
    public ushort CX { get; set; }
    public ushort DX { get; set; }
    public ushort CS { get; set; }
    public ushort SS { get; set; }
    public ushort DS { get; set; }
    public ushort ES { get; set; }
    public ushort SP { get; set; }
    public ushort BP { get; set; }
    public ushort SI { get; set; }
    public ushort DI { get; set; }
    public ushort IP { get; set; }
    public ushort Flags { get; set; }
    public byte[] Queue { get; set; }
    public int QueueHead { get; set; }
    public int QueueCount { get; set; }

    public static Intel8086State Initial(ushort cs = 0, ushort ip = 0) => new()
    {
        CS = cs,
        IP = ip,
        Flags = 0x0002,
        Queue = new byte[6],
    };

    /// <summary>Tworzy deterministyczny stan po sprzętowym RESET 8086.</summary>
    public static Intel8086State AfterHardwareReset() => new()
    {
        CS = 0xFFFF,
        IP = 0,
        DS = 0,
        SS = 0,
        ES = 0,
        Flags = 0,
        Queue = new byte[6],
    };

    public readonly ReadOnlySpan<byte> PendingQueue => Queue.AsSpan(QueueHead, QueueCount);
}
