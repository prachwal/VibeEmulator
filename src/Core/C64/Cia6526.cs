namespace VibeEmulator.Core.C64;

/// <summary>
/// Mockup CIA 6526: rejestry i dwa timery z przerwaniem.
/// Brak TOD i trybów CNT — do uzupełnienia w fazie 9 planu.
/// </summary>
public sealed class Cia6526
{
    private readonly byte[] _registers = new byte[16];
    private int _timerA;
    private int _timerB;

    public byte Read(ushort address)
    {
        int register = address & 0x0F;

        return register switch
        {
            0x04 => (byte)_timerA,
            0x05 => (byte)(_timerA >> 8),
            0x06 => (byte)_timerB,
            0x07 => (byte)(_timerB >> 8),
            _ => _registers[register],
        };
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x0F;
        _registers[register] = value;

        if (register == 0x04)
        {
            _timerA = (_timerA & 0xFF00) | value;
        }
        else if (register == 0x05)
        {
            _timerA = (_timerA & 0x00FF) | (value << 8);

            if (_timerA == 0)
            {
                _timerA = 0x10000;
            }
        }
        else if (register == 0x06)
        {
            _timerB = (_timerB & 0xFF00) | value;
        }
        else if (register == 0x07)
        {
            _timerB = (_timerB & 0x00FF) | (value << 8);

            if (_timerB == 0)
            {
                _timerB = 0x10000;
            }
        }
        else if (register == 0x0D)
        {
            _registers[0x0D] &= (byte)~value;
        }
    }

    public void Tick()
    {
        if (--_timerA <= 0)
        {
            _timerA = Math.Max(1, LatchA());
            _registers[0x0D] |= 0x01;
        }

        if (--_timerB <= 0)
        {
            _timerB = Math.Max(1, LatchB());
            _registers[0x0D] |= 0x02;
        }
    }

    public bool IrqRaised => (_registers[0x0D] & _registers[0x0E] & 0x1F) != 0;

    public void Reset()
    {
        Array.Clear(_registers);
        _timerA = 0xFFFF;
        _timerB = 0xFFFF;
    }

    private int LatchA() => _registers[0x04] | (_registers[0x05] << 8);

    private int LatchB() => _registers[0x06] | (_registers[0x07] << 8);
}
