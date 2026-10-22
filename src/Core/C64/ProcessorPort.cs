namespace VibeEmulator.Core.C64;

/// <summary>
/// Port procesora 6510 pod <c>$0000</c>/<c>$0001</c>: DDR + dane.
/// Steruje bankowaniem pamięci. Przełączenie banku działa z opóźnieniem 3 cykli —
/// to realne zachowanie, nie uproszczenie.
/// </summary>
public sealed class ProcessorPort
{
    private const byte OutputMask = 0x17; // LORAM, HIRAM, CHAREN + 2 piny
    private const int BankDelayCycles = 3;

    private byte _pendingValue;
    private int _delay;

    public byte Direction { get; set; } = 0x2F;

    public byte Data { get; set; } = 0x37;

    private byte _effective;
    private bool _basicRomEnabled;
    private bool _kernalRomEnabled;
    private bool _ioVisible;

    /// <summary>Wartość skuteczna — po opóźnieniu bankowania.</summary>
    public byte Effective
    {
        get => _effective;
        private set
        {
            _effective = value;
            _basicRomEnabled = (value & 0x01) != 0;
            _kernalRomEnabled = (value & 0x02) != 0;
            _ioVisible = (value & 0x04) != 0;
        }
    }

    public bool BasicRomEnabled => _basicRomEnabled;

    public bool KernalRomEnabled => _kernalRomEnabled;

    /// <summary>CHAREN: 1 = I/O widoczne, 0 = CHARACTER ROM.</summary>
    public bool IoVisible => _ioVisible;

    public ProcessorPort()
    {
        Effective = Settle();
        _pendingValue = Effective;
    }

    /// <summary>Odczyt <c>$01</c>: wyjście to dane, wejście to piny wejściowe (0x17).</summary>
    public byte ReadData() => (byte)((Data & Direction) | (OutputMask & ~Direction));

    public void WriteDirection(byte value)
    {
        Direction = value;
        ScheduleBankSwitch();
    }

    public void WriteData(byte value)
    {
        Data = value;
        ScheduleBankSwitch();
    }

    /// <summary>Postęp opóźnienia bankowania — wywoływane raz na cykl przez maszynę.</summary>
    public void Tick()
    {
        if (_delay <= 0)
        {
            return;
        }

        _delay--;

        if (_delay == 0)
        {
            Effective = Settle();
            _pendingValue = Effective;
        }
    }

    private void ScheduleBankSwitch()
    {
        _pendingValue = Settle();
        _delay = BankDelayCycles;
    }

    private byte Settle() => (byte)((Data & Direction) | (OutputMask & ~Direction));
}
